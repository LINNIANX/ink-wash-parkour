#pragma warning disable CS0414
using UnityEngine;

/// <summary>
/// 坐骑系统（MVP 阶段：仅保留类框架和字段，暂不实现完整功能）
/// </summary>
public class MountSystem : MonoBehaviour
{
    public static MountSystem Instance { get; private set; }

    [Header("坐骑属性")]
    [SerializeField] private int mountExtraHealth = 50;
    [SerializeField] private float mountSpeedMultiplier = 1.2f;
    [SerializeField] private float mountLostStun = 0.5f;

    [Header("状态（只读）")]
    [SerializeField] private int currentShield = 0;
    [SerializeField] private bool isMounted = false;

    /// <summary>当前护盾值</summary>
    public int CurrentShield => currentShield;

    /// <summary>是否处于骑乘状态</summary>
    public bool IsMounted => isMounted;

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
    /// 初始化坐骑系统（MVP 阶段仅打印日志）
    /// </summary>
    private void Initialize()
    {
        Debug.Log("[MountSystem] 初始化（MVP 阶段为空壳）");
    }

    /// <summary>激活坐骑（预留）</summary>
    public void ActivateMount()
    {
        Debug.Log("[MountSystem] 激活坐骑（预留）");
    }

    /// <summary>移除坐骑（预留）</summary>
    public void DeactivateMount()
    {
        Debug.Log("[MountSystem] 移除坐骑（预留）");
    }
}
