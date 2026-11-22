using UnityEngine;

/// <summary>
/// 视差循环滚动器
/// 用于 2D 跑酷游戏中实现背景（慢速视差）和前景装饰（快速滚动）的无限循环。
/// 原理：将多个 SpriteRenderer 子物体首尾相接排列，每帧按视差系数移动，
/// 当某一块完全移出相机左边界时，把它 teleport 到当前最右块的右边，实现无缝循环。
///
/// 用法：
///   1. 创建一个空物体（如 BackgroundLayer）
///   2. 将本脚本挂到该物体上
///   3. 在其子物体下拖入 2~4 张相同的背景 SpriteRenderer（并排摆好）
///   4. 设置 Parallax Factor：
///      - 远景背景 → 0.1 ~ 0.3（很慢）
///      - 中景背景 → 0.4 ~ 0.6（中等）
///      - 近景装饰（前景绳子、前景树叶等纯装饰）→ 1.0 ~ 1.2（同步或稍快）
/// </summary>
[DisallowMultipleComponent]
public class ParallaxLooper : MonoBehaviour
{
    [Header("视差系数")]
    [Tooltip("0 = 完全不动(贴屏)，0.3 = 慢速远景，1 = 和相机/世界同步，1.2 = 前景更快")]
    [Range(0f, 2f)]
    public float parallaxFactor = 0.3f;

    [Header("自定义块宽（0=自动取子物体 SpriteRenderer 的 bounds.size.x）")]
    [Tooltip("如果 Sprite 的 Pivot 不居中导致自动宽度不准，可手动填写单块图片的 X 方向宽度（世界单位）")]
    public float tileWidthOverride = 0f;

    [Header("（可选）绑定相机；不绑则自动找 MainCamera")]
    public Camera targetCamera;

    // —— 运行时缓存 ——
    private Transform[] childTiles;   // 所有子块（按初始从左到右顺序）
    private float tileWidth;          // 单块宽度
    private Vector3 lastCameraPos;    // 相机上一帧位置（用于算差值）
    private float totalShiftX;        // 累计移动位移（用于微调精度，可选）
    private int cameraLeftId;         // 相机左边界对应的实例ID，避免每帧 Camera.main

    private void Awake()
    {
        // 1. 收集子块（只取直接子节点，按 Hierarchy 从左到右顺序）
        childTiles = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            childTiles[i] = transform.GetChild(i);
        }

        if (childTiles.Length == 0)
        {
            Debug.LogError("[ParallaxLooper] '" + gameObject.name + "' 没有任何子 SpriteRenderer 块！");
            enabled = false;
            return;
        }

        // 2. 计算单块宽度
        if (tileWidthOverride > 0.001f)
        {
            tileWidth = tileWidthOverride;
        }
        else
        {
            SpriteRenderer sr0 = childTiles[0].GetComponent<SpriteRenderer>();
            if (sr0 != null && sr0.sprite != null)
            {
                tileWidth = sr0.bounds.size.x;
            }
            else
            {
                // 没有 SpriteRenderer 就取所有子块之间的平均间距
                if (childTiles.Length >= 2)
                {
                    tileWidth = Mathf.Abs(childTiles[1].position.x - childTiles[0].position.x);
                }
                else
                {
                    Debug.LogError("[ParallaxLooper] 无法自动获取块宽，请在 Inspector 填写 Tile Width Override。");
                    enabled = false;
                    return;
                }
            }
        }

        // 3. 找相机
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Start()
    {
        if (targetCamera != null)
        {
            lastCameraPos = targetCamera.transform.position;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null || childTiles == null || childTiles.Length == 0) return;

        Vector3 curCamPos = targetCamera.transform.position;
        float deltaCamX = curCamPos.x - lastCameraPos.x;
        lastCameraPos = curCamPos;

        if (Mathf.Abs(deltaCamX) < 0.0001f) return;  // 相机没动就跳过

        // —— 核心 1：按视差系数平移本层 ——
        float moveX = deltaCamX * parallaxFactor;
        transform.position += Vector3.right * moveX;

        // —— 核心 2：把左边出屏幕的块搬到最右边 ——
        RecycleTiles(curCamPos);
    }

    /// <summary>
    /// 循环回收：
    ///   找到左边界的块，判断它的右边缘是否已在相机左边界之外。
    ///   如果是，就把它整个搬到"当前最右块的右边"，实现无缝接尾。
    /// </summary>
    private void RecycleTiles(Vector3 curCamPos)
    {
        // 相机左边界的世界坐标（给一点 padding 防止边缘闪烁）
        float camLeft = targetCamera.ViewportToWorldPoint(new Vector3(-0.01f, 0, -curCamPos.z)).x;

        // 找左边界的块和右边界的块
        int leftmostIdx = 0;
        int rightmostIdx = 0;
        for (int i = 1; i < childTiles.Length; i++)
        {
            if (childTiles[i].position.x < childTiles[leftmostIdx].position.x)
                leftmostIdx = i;
            if (childTiles[i].position.x > childTiles[rightmostIdx].position.x)
                rightmostIdx = i;
        }

        Transform leftTile = childTiles[leftmostIdx];
        float leftTileRightEdge = leftTile.position.x + tileWidth * 0.5f;  // 以中心点+半宽为右边缘

        // 如果 leftTile 完全出了相机左边 → 搬到 rightTile 右边
        if (leftTileRightEdge < camLeft)
        {
            Transform rightTile = childTiles[rightmostIdx];
            Vector3 newPos = rightTile.position;
            newPos.x += tileWidth;  // 右边界+width = 正好接在后面
            leftTile.position = newPos;
        }

        // —— 可选反向：如果玩家后退（比如调试），最右块跑到相机右边外，搬到最左 ——
        float camRight = targetCamera.ViewportToWorldPoint(new Vector3(1.01f, 0, -curCamPos.z)).x;
        Transform rightTile2 = childTiles[rightmostIdx];
        float rightTileLeftEdge = rightTile2.position.x - tileWidth * 0.5f;
        if (rightTileLeftEdge > camRight)
        {
            Transform leftTile2 = childTiles[leftmostIdx];
            Vector3 newPos = leftTile2.position;
            newPos.x -= tileWidth;
            rightTile2.position = newPos;
        }
    }

    /// <summary>
    /// 外部调用：手动重置所有子块到初始相对位置（比如 GameOver 重新开始时）
    /// </summary>
    public void ResetTiles()
    {
        if (childTiles == null || childTiles.Length == 0) return;
        // 简单做法：按顺序从 x=0 开始重新排
        for (int i = 0; i < childTiles.Length; i++)
        {
            Vector3 p = childTiles[i].position;
            p.x = i * tileWidth;
            childTiles[i].position = p;
        }
    }
}
