using UnityEngine;

/// <summary>
/// 交互反馈管理器（MVP 阶段：所有方法仅 Debug.Log，不引入粒子/震屏/音效）
/// </summary>
public class FeedbackManager : MonoBehaviour
{
    public static FeedbackManager Instance { get; private set; }

    [Header("反馈开关")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>拾取金币反馈</summary>
    public void PlayCoinPickup()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 拾取金币");
    }

    /// <summary>玩家受伤反馈</summary>
    public void PlayHitEffect()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 玩家受伤");
    }

    /// <summary>攻击命中反馈</summary>
    public void PlayAttackHit()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 攻击命中");
    }

    /// <summary>滑铲反馈</summary>
    public void PlaySlideEffect()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 滑铲");
    }

    /// <summary>跳跃反馈</summary>
    public void PlayJumpEffect()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 跳跃");
    }

    /// <summary>死亡反馈</summary>
    public void PlayDeathEffect()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 玩家死亡");
    }

    /// <summary>击杀敌人反馈</summary>
    public void PlayEnemyKilled()
    {
        if (enableDebugLog) Debug.Log("[FeedbackManager] 击杀敌人");
    }
}
