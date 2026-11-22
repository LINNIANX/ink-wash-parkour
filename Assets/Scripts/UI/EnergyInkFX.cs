using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 墨水粒子管理器（挂在 Canvas 层级下）：
/// 监听能量事件与受击反馈事件，在命中位置生成墨滴，沿贝塞尔弧线飞向能量条
/// 墨滴外观三级回退：inkClip(Animator 多帧动画) > singleFrameSprite(单帧贴图) > 代码生成占位圆点
/// </summary>
public class EnergyInkFX : MonoBehaviour
{
    [Header("目标（能量条/分数文本）")]
    [SerializeField] private RectTransform energyTextRect;

    [Header("墨滴外观（三级回退：动画 > 贴图 > 占位）")]
    [SerializeField] private AnimationClip inkClip;
    [SerializeField] private string animStateName = "Ink";
    [SerializeField] private RuntimeAnimatorController animatorController;
    [SerializeField] private Sprite singleFrameSprite;

    [Header("墨滴参数")]
    [SerializeField] private int poolSize = 50;
    [SerializeField] private int dropletsPerHit = 4;
    [SerializeField] private float flyDuration = 0.5f;
    [SerializeField] private float arcHeight = 60f;
    [SerializeField] private Vector2 dropletSize = new Vector2(10f, 10f);

    [Header("占位外观")]
    [SerializeField] private Color dropletColor = new Color(0f, 0f, 0f, 0.85f);

    private readonly List<InkDroplet> pool = new List<InkDroplet>();
    private RectTransform canvasRect;

    private void Awake()
    {
        canvasRect = GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("[EnergyInkFX] 必须挂在 Canvas 层级下的物体上");
            enabled = false;
            return;
        }

        if (energyTextRect == null)
        {
            Debug.LogWarning("[EnergyInkFX] 未设置 energyTextRect，墨水粒子不会生成");
        }

        Sprite effectiveSprite = singleFrameSprite != null ? singleFrameSprite : CreateCircleSprite();

        for (int i = 0; i < poolSize; i++)
        {
            GameObject go = new GameObject("InkDroplet_" + i, typeof(RectTransform), typeof(Image), typeof(Animator), typeof(InkDroplet));
            go.transform.SetParent(transform, false);

            RectTransform rt = (RectTransform)go.transform;
            rt.sizeDelta = dropletSize;

            Image img = go.GetComponent<Image>();
            img.color = dropletColor;
            img.raycastTarget = false;

            Animator anim = go.GetComponent<Animator>();
            if (animatorController != null)
                anim.runtimeAnimatorController = animatorController;

            InkDroplet droplet = go.GetComponent<InkDroplet>();
            droplet.Configure(effectiveSprite, inkClip, animStateName);

            pool.Add(droplet);
            go.SetActive(false);
        }
    }

    private void Start()
    {
        if (EnergySkillSystem.Instance != null)
        {
            EnergySkillSystem.Instance.OnEnergyGained += HandleEnergyGained;
            EnergySkillSystem.Instance.OnHitInkFeedback += HandleHitFeedback;
        }
    }

    private void OnDestroy()
    {
        if (EnergySkillSystem.Instance != null)
        {
            EnergySkillSystem.Instance.OnEnergyGained -= HandleEnergyGained;
            EnergySkillSystem.Instance.OnHitInkFeedback -= HandleHitFeedback;
        }
    }

    private void HandleEnergyGained(int amount, Vector3 worldPos) => SpawnBurst(worldPos, dropletsPerHit);

    private void HandleHitFeedback(Vector3 worldPos) => SpawnBurst(worldPos, dropletsPerHit);

    /// <summary>
    /// 在命中点生成一簇墨滴，飞向能量条
    /// </summary>
    private void SpawnBurst(Vector3 worldPos, int count)
    {
        if (energyTextRect == null) return;

        if (!TryConvertWorldToLocal(worldPos, out Vector2 startPos)) return;

        Vector2 endPos = ConvertWorldToLocal(energyTextRect.position);

        int spawned = 0;
        foreach (InkDroplet droplet in pool)
        {
            if (droplet.gameObject.activeSelf) continue;

            // 与攻击小球同构：弧高方向随机（±），幅度小幅随机让多颗墨滴不重叠
            float sign = Random.value < 0.5f ? -1f : 1f;
            float arc = sign * arcHeight * (0.7f + Random.value * 0.6f);

            droplet.Fly(startPos, endPos, arc, flyDuration);
            spawned++;
            if (spawned >= count) break;
        }
    }

    /// <summary>
    /// 世界坐标 → Canvas 本地坐标（Overlay 模式相机传 null）
    /// </summary>
    private bool TryConvertWorldToLocal(Vector3 worldPos, out Vector2 localPos)
    {
        localPos = Vector2.zero;
        if (Camera.main == null) return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, Camera.main.WorldToScreenPoint(worldPos), null, out localPos);
    }

    /// <summary>
    /// UI 世界坐标（RectTransform.position）→ Canvas 本地坐标
    /// </summary>
    private Vector2 ConvertWorldToLocal(Vector3 uiWorldPos)
    {
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, RectTransformUtility.WorldToScreenPoint(null, uiWorldPos), null, out Vector2 localPos))
        {
            return localPos;
        }
        return Vector2.zero;
    }

    /// <summary>运行时生成圆形占位 Sprite（小样阶段用，正式素材拖入后自动替换）</summary>
    private static Sprite CreateCircleSprite()
    {
        int size = 16;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
