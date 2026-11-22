using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 挂在玩家头顶 World Space 血条 Canvas 上。
/// 场景中 Player 根节点是非均匀缩放，因此 Awake 时把血条脱离到场景根节点，
/// LateUpdate 用世界坐标跟随玩家头顶。
/// 血量通过直接修改 Fill 的 RectTransform 宽度（锚点）表现，
/// 不依赖 Image.fillAmount / Filled 类型 / Sprite，任何情况下都可见。
/// </summary>
[RequireComponent(typeof(Canvas))]
public class PlayerHealthBar : MonoBehaviour
{
    [Tooltip("血量填充图")]
    [SerializeField] private Image fillImage;

    [Tooltip("留空则自动向上查找父物体上的 PlayerController")]
    [SerializeField] private PlayerController player;

    [Header("世界空间定位")]
    [SerializeField] private float heightOffset = 1.15f;
    [SerializeField] private float horizontalOffset = 0.3f;
    [SerializeField] private float worldScale = 0.005f;

    [Header("边框（像素）")]
    [SerializeField] private float padding = 2f;

    [Header("回血动画")]
    [Tooltip("回血时填充条上涨速度（比例/秒），掉血瞬间生效")]
    [SerializeField] private float regenSpeed = 1.5f;

    private RectTransform fillRect;
    private float displayedRatio = 1f;
    private bool ratioInitialized;
    private int lastLoggedHealth = int.MinValue;

    private void Awake()
    {
        if (player == null)
            player = GetComponentInParent<PlayerController>();

        if (fillImage == null)
        {
            Transform fillTf = transform.Find("Background/Fill");
            if (fillTf == null) fillTf = FindDeepChild(transform, "Fill");
            if (fillTf != null) fillImage = fillTf.GetComponent<Image>();
        }

        transform.SetParent(null, true);

        if (fillImage != null)
        {
            // 用尺寸表现血量，使用最普通的 Simple 类型即可
            fillImage.type = Image.Type.Simple;
            fillRect = fillImage.rectTransform;
        }

        Debug.Log($"[PlayerHealthBar] 初始化 id={GetInstanceID()}：" +
                  $"player={(player != null ? player.name : "null")}, " +
                  $"fill={(fillImage != null ? fillImage.name : "null")}");
    }

    private void LateUpdate()
    {
        if (player == null) return;

        // 世界坐标跟随头顶
        transform.position = player.transform.position
            + Vector3.up * heightOffset
            + player.transform.right * horizontalOffset;
        transform.rotation = Quaternion.identity;
        transform.localScale = Vector3.one * worldScale;

        if (fillRect == null) return;

        float targetRatio = Mathf.Clamp01((float)player.CurrentHealth / player.MaxHealth);

        if (!ratioInitialized)
        {
            displayedRatio = targetRatio;
            ratioInitialized = true;
        }
        else if (targetRatio < displayedRatio)
        {
            // 掉血立即缩短
            displayedRatio = targetRatio;
        }
        else if (targetRatio > displayedRatio)
        {
            // 回血平滑上涨
            displayedRatio = Mathf.MoveTowards(displayedRatio, targetRatio, regenSpeed * Time.deltaTime);
        }

        // 直接用锚点控制宽度：左边界固定 0，右边界 = 血量比例
        fillRect.anchorMin = new Vector2(0f, 0f);
        fillRect.anchorMax = new Vector2(displayedRatio, 1f);
        fillRect.pivot = new Vector2(0f, 0.5f);
        fillRect.offsetMin = new Vector2(padding, padding);
        fillRect.offsetMax = new Vector2(-padding, -padding);

        if (player.CurrentHealth != lastLoggedHealth)
        {
            Debug.Log($"[PlayerHealthBar] 血量变化 id={GetInstanceID()}：" +
                      $"{player.CurrentHealth}/{player.MaxHealth} => 宽度比例={displayedRatio:F3}");
            lastLoggedHealth = player.CurrentHealth;
        }
    }

    private static Transform FindDeepChild(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name) return child;
            Transform found = FindDeepChild(child, name);
            if (found != null) return found;
        }
        return null;
    }
}
