using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 池化墨滴：匀速直线推进 + 正弦拱形抛物线（与 HommingProjectile 攻击小球同构），
/// 到达终点后自动回池（SetActive(false)）
/// 外观三级回退：Animator 多帧动画 > 静态贴图 > 占位圆点（由 EnergyInkFX 配置）
/// </summary>
public class InkDroplet : MonoBehaviour
{
    private Image image;
    private Animator animator;

    private Sprite staticSprite;
    private bool useAnimator;
    private string animStateName;

    private Vector2 start;
    private Vector2 end;
    private float arc;
    private float duration;
    private float elapsed;

    /// <summary>
    /// 配置外观（由 EnergyInkFX 创建池时调用；物体未激活时 GetComponent 同样可用）
    /// </summary>
    /// <param name="staticSprite">单帧/占位贴图</param>
    /// <param name="clip">多帧动画剪辑（优先于贴图）</param>
    /// <param name="stateName">Animator 中播放的状态名</param>
    public void Configure(Sprite staticSprite, AnimationClip clip, string stateName)
    {
        if (image == null) image = GetComponent<Image>();
        if (animator == null) animator = GetComponent<Animator>();

        this.staticSprite = staticSprite;
        this.animStateName = stateName;
        useAnimator = clip != null && animator != null && animator.runtimeAnimatorController != null;

        if (animator != null)
            animator.enabled = useAnimator;
    }

    /// <summary>
    /// 开始一次飞行（当前物体应为池中未激活状态）
    /// </summary>
    /// <param name="startPos">起点（Canvas 本地坐标）</param>
    /// <param name="endPos">终点（Canvas 本地坐标）</param>
    /// <param name="arcHeight">正弦弧高（可为负，表示向下拱）</param>
    /// <param name="flyDuration">飞行时长（秒）</param>
    public void Fly(Vector2 startPos, Vector2 endPos, float arcHeight, float flyDuration)
    {
        start = startPos;
        end = endPos;
        arc = arcHeight;
        duration = Mathf.Max(flyDuration, 0.01f);
        elapsed = 0f;

        gameObject.SetActive(true);

        if (useAnimator)
        {
            animator.Play(animStateName, 0, 0f);
        }
        else if (image != null && staticSprite != null)
        {
            image.sprite = staticSprite;
        }

        UpdatePosition(0f);
    }

    private void Update()
    {
        if (elapsed >= duration) return;

        elapsed += Time.deltaTime;
        UpdatePosition(Mathf.Clamp01(elapsed / duration));

        if (elapsed >= duration)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 与 HommingProjectile.FlyToTarget 同构：线性插值推进 + Sin(t·π) 正弦拱形，匀速无缓动
    /// </summary>
    private void UpdatePosition(float t)
    {
        float x = Mathf.Lerp(start.x, end.x, t);
        float baseY = Mathf.Lerp(start.y, end.y, t);
        float y = baseY + Mathf.Sin(t * Mathf.PI) * arc;
        ((RectTransform)transform).anchoredPosition = new Vector2(x, y);
    }
}
