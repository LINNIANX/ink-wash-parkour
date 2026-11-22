using UnityEngine;

/// <summary>
/// 关卡速度控制器：管理玩家自动向右移动的速度
/// MVP 阶段仅使用固定匀速，不做随时间线性加速
/// </summary>
public class LevelSpeedController : MonoBehaviour
{
    public static LevelSpeedController Instance { get; private set; }

    [Header("速度设置（MVP: 仅固定匀速）")]
    [SerializeField] private float fixedSpeed = 8f;

    /// <summary>当前世界移动速度（只读）</summary>
    public float CurrentSpeed => fixedSpeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化速度控制器
    /// </summary>
    private void Initialize()
    {
        Debug.Log("[LevelSpeedController] 初始化，固定速度: " + fixedSpeed);
    }
}
