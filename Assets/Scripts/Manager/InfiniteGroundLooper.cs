using UnityEngine;

/// <summary>
/// 无限跑道（地面/绳子）循环复用器
/// 适用于 2D 跑酷游戏中"世界不动、玩家向右跑"模式下的地面段或绳索段无限循环。
///
/// 原理：
///   场景中放 N 段相同的地面/绳子（首尾相接，按从左到右排好），每帧检查最左侧的段是否已经
///   完全被玩家甩到了相机左边界之外。如果是，就把这一段整块 teleport 到当前最右段的正右侧，
///   形成"玩家无限向右跑，跑道段循环复用永不掉下去"的效果。
///
/// 和 ParallaxLooper 的区别：
///   - ParallaxLooper：父物体按视差系数平移，用于纯视觉装饰（无碰撞）
///   - InfiniteGroundLooper：子物体原地不动，只在跑出屏幕时 teleport，用于带 BoxCollider2D 的物理地面/绳子
///
/// 用法示例（搭地面跑道）：
///   1. 建一个空物体，命名 GroundLooper，Position = (0,0,0)
///   2. 在它下面放 3~5 个 Ground 段子物体：
///        - 子物体 1：Position X = 0，BoxCollider2D Size = (20, 1)，Layer = Ground
///        - 子物体 2：Position X = 20，BoxCollider2D Size = (20, 1)，Layer = Ground
///        - 子物体 3：Position X = 40，BoxCollider2D Size = (20, 1)，Layer = Ground
///        - （保证每段 X 方向首尾相接，段宽一致）
///   3. 把本脚本挂到 GroundLooper 上
///   4. 脚本会自动识别段宽，按 Player 位置循环搬运
///
/// 用法示例（搭高空中的绳索跑道）：
///   1. 建一个空物体，命名 RopeLooper_1，Position = (0, 2, 0)  ← Y=2 表示高度 2 米
///   2. 子物体 3~5 段，每段 Size=(10, 0.3)，Position 依次 +10
///   3. Layer = Rope，挂本脚本
///   4. 这样玩家跳到 Y=2 高度时，可以无限在绳子上跑
/// </summary>
[DisallowMultipleComponent]
public class InfiniteGroundLooper : MonoBehaviour
{
    [Header("段宽（0 = 自动根据 Collider/Renderer 计算）")]
    [Tooltip("每一段地面/绳子在 X 方向的宽度（世界单位）。\n如果为 0，自动读取第一个子物体 BoxCollider2D.size.x 或 SpriteRenderer.bounds.size.x。\n如果每段有重叠想让循环更紧凑，可手动设置比实际稍小的值。")]
    public float segmentWidth = 0f;

    [Header("回收阈值（相对于相机左边界）")]
    [Tooltip("当段的右边缘 < 相机左边界 - cleanupOffset 时回收。\n默认 5 米，保证完全出屏再搬，避免玩家看到跳变。")]
    public float cleanupOffset = 5f;

    [Header("接缝重叠（防止玩家被接缝侧墙卡住）")]
    [Tooltip("循环搬运时相邻段保留多少重叠（>0）。\nBoxCollider2D 零缝隙相接时接缝处会形成隐形侧墙，方形玩家高速跑过会被挡住；\n保持少量重叠可让接缝侧墙埋进相邻段内部，地面顶面保持连续。")]
    public float seamOverlap = 0.5f;

    [Header("提前生成预留（段数）")]
    [Tooltip("保证玩家前方至少有 reserveSegments 段地面，不出现脚下空洞。\n一般不用改。")]
    [Range(1, 10)]
    public int reserveSegments = 2;

    [Header("（可选）绑定 Player；不绑则自动找 Tag=Player")]
    public Transform player;

    [Header("（可选）绑定 Camera；不绑则自动找 MainCamera")]
    public Camera targetCamera;

    // —— 运行时缓存 ——
    private Transform[] segments;   // 所有跑道段（直接子节点）
    private float segWidth;         // 单段宽度（计算后）
    private int totalSegs;          // 段数

    private void Awake()
    {
        // 1. 收集所有直接子物体作为跑道段
        totalSegs = transform.childCount;
        if (totalSegs == 0)
        {
            Debug.LogError("[InfiniteGroundLooper] '" + gameObject.name + "' 下面没有任何跑道段子物体！");
            enabled = false;
            return;
        }

        segments = new Transform[totalSegs];
        for (int i = 0; i < totalSegs; i++)
        {
            segments[i] = transform.GetChild(i);
        }

        // 2. 计算段宽
        if (segmentWidth > 0.001f)
        {
            segWidth = segmentWidth;
        }
        else
        {
            // 尝试从 Collider 获取
            BoxCollider2D bc = segments[0].GetComponent<BoxCollider2D>();
            if (bc != null)
            {
                segWidth = bc.size.x;
            }
            else
            {
                // 再试 SpriteRenderer
                SpriteRenderer sr = segments[0].GetComponent<SpriteRenderer>();
                if (sr != null && sr.sprite != null)
                {
                    segWidth = sr.bounds.size.x;
                }
                else
                {
                    // 最后手段：根据前两个子物体的 X 差
                    if (totalSegs >= 2)
                    {
                        segWidth = Mathf.Abs(segments[1].position.x - segments[0].position.x);
                    }
                    else
                    {
                        Debug.LogError("[InfiniteGroundLooper] 无法自动获取段宽，请在 Inspector 手动填 Segment Width。");
                        enabled = false;
                        return;
                    }
                }
            }
        }

        // 3. 找 Player 和 Camera
        if (player == null)
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) player = go.transform;
        }
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (totalSegs < reserveSegments + 1)
        {
            Debug.LogWarning("[InfiniteGroundLooper] '" + gameObject.name + "' 段数太少（=" + totalSegs + "），建议至少放 " + (reserveSegments + 1) + " 段，避免循环衔接不畅。");
        }

        // 4. 把所有段的盒形碰撞体合并为一条复合碰撞带，彻底消除接缝卡点
        SetupCompositeCollider();
    }

    /// <summary>
    /// 在父物体上构建 Kinematic Rigidbody2D + CompositeCollider2D，
    /// 让所有子段的 BoxCollider2D（勾选 Used By Composite）融合成一整条连续碰撞带。
    ///
    /// 为什么这样能根治：
    ///   - 独立 Box/Edge 拼接时，接缝处（含重叠、零半径 Edge 的端点）都会被物理引擎
    ///     产生水平法线接触点，高速玩家撞上就被挡住（原地踏步）。
    ///   - 复合碰撞体会把重叠/相邻的盒形求并集，内部接缝完全融合，
    ///     侧面只存在于整条带子最左、最右的远端——它们永远在玩家身后/前方很远处。
    ///   - 保留 0.3 米厚度，绳索倒挂定位与原始效果一致（不会穿模）。
    ///   - 段被 teleport 时复合碰撞体设为 Synchronous 自动同帧重建。
    /// </summary>
    private void SetupCompositeCollider()
    {
        // 1) 父物体需要 Kinematic 刚体，复合碰撞体才能工作
        Rigidbody2D rootRb = GetComponent<Rigidbody2D>();
        if (rootRb == null) rootRb = gameObject.AddComponent<Rigidbody2D>();
        rootRb.bodyType = RigidbodyType2D.Kinematic;
        rootRb.simulated = true;
        rootRb.useAutoMass = false;
        rootRb.gravityScale = 0f;
        rootRb.constraints = RigidbodyConstraints2D.None;

        // 2) 父物体上的复合碰撞体：多边形并集，搬运段时同步重建
        CompositeCollider2D composite = GetComponent<CompositeCollider2D>();
        if (composite == null) composite = gameObject.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;
        composite.offsetDistance = 0f;
        composite.enabled = true;

        // 3) 每个子段：盒形交给复合；停用之前临时加的 Edge
        int merged = 0;
        foreach (Transform seg in segments)
        {
            BoxCollider2D box = seg.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.usedByComposite = true;
                box.enabled = true;
                merged++;
            }

            EdgeCollider2D edge = seg.GetComponent<EdgeCollider2D>();
            if (edge != null) edge.enabled = false;
        }

        Debug.Log("[InfiniteGroundLooper] '" + gameObject.name + "' 已构建复合碰撞带，合并 "
            + merged + " 个段（内部无缝，唯一侧墙在整条带两端远处）。");
    }

    private void LateUpdate()
    {
        if (segments == null || segments.Length == 0) return;
        if (targetCamera == null || player == null) return;

        // —— 核心：先回收左后方废弃的段，搬到最前面 ——
        RecycleRearSegments();

        // —— 保险：如果玩家前方段数不足 reserveSegments，再补搬一次 ——
        EnsureFrontCoverage();
    }

    /// <summary>
    /// 找到当前最左段，如果它完全在相机左边界外，就搬到最右段右边。
    /// 每帧最多搬 1 段，避免一次搬多段跳变。
    /// </summary>
    private void RecycleRearSegments()
    {
        int leftIdx = FindLeftmostIndex();
        Transform leftSeg = segments[leftIdx];

        // 相机左边界（世界坐标），-cleanupOffset 给富余
        float camLeft = targetCamera.ViewportToWorldPoint(
            new Vector3(0f, 0f, -targetCamera.transform.position.z)).x - cleanupOffset;

        // 以"段的中心 + 半宽"作为段的右边缘
        float rightEdge = leftSeg.position.x + segWidth * 0.5f;

        if (rightEdge < camLeft)
        {
            // 搬到当前最右段的右边
            int rightIdx = FindRightmostIndex();
            Transform rightSeg = segments[rightIdx];
            Vector3 newPos = rightSeg.position;
            newPos.x += segWidth;
            leftSeg.position = newPos;
        }
    }

    /// <summary>
    /// 检查玩家前方是否至少有 reserveSegments 段。
    /// 不够就把最左段再搬过来（防止段数偏少出现脚下空洞）。
    /// </summary>
    private void EnsureFrontCoverage()
    {
        // 找最右段
        int rightIdx = FindRightmostIndex();
        Transform rightSeg = segments[rightIdx];
        float rightSegRightEdge = rightSeg.position.x + segWidth * 0.5f;

        // 需要的最小右边界 = 玩家 X + reserveSegments * 段宽
        float needRightEdge = player.position.x + reserveSegments * segWidth;

        if (rightSegRightEdge < needRightEdge)
        {
            int leftIdx = FindLeftmostIndex();
            Transform leftSeg = segments[leftIdx];
            Vector3 newPos = rightSeg.position;
            newPos.x += PlacementPitch();
            leftSeg.position = newPos;
        }
    }

    // —— 工具方法 ——

    /// <summary>
    /// 相邻段中心间距 = 段宽 - 接缝重叠。
    /// 略小于碰撞宽度，保证接缝侧墙互相埋入，玩家不会被接缝挡住。
    /// </summary>
    private float PlacementPitch()
    {
        float overlap = Mathf.Max(0f, Mathf.Min(seamOverlap, segWidth * 0.25f));
        return segWidth - overlap;
    }

    private int FindLeftmostIndex()
    {
        int idx = 0;
        for (int i = 1; i < totalSegs; i++)
        {
            if (segments[i].position.x < segments[idx].position.x)
                idx = i;
        }
        return idx;
    }

    private int FindRightmostIndex()
    {
        int idx = 0;
        for (int i = 1; i < totalSegs; i++)
        {
            if (segments[i].position.x > segments[idx].position.x)
                idx = i;
        }
        return idx;
    }

    /// <summary>
    /// 外部调用：GameOver 时重置所有段到初始相对位置（按子节点顺序从 X=0 开始排）。
    /// 根物体的 Y/Z 保持不变，只改子物体 X。
    /// </summary>
    public void ResetSegments()
    {
        if (segments == null) return;
        float pitch = PlacementPitch();
        for (int i = 0; i < totalSegs; i++)
        {
            Vector3 p = segments[i].position;
            p.x = transform.position.x + i * pitch;
            segments[i].position = p;
        }
    }
}
