#pragma warning disable CS0414
using System;
using UnityEngine;

/// <summary>
/// 能量与技能系统（MVP 阶段：仅实现能量积累，技能调用为预留空方法）
/// </summary>
public class EnergySkillSystem : MonoBehaviour
{
    public static EnergySkillSystem Instance { get; private set; }

    /// <summary>能量增加事件（数量 + 来源世界位置），供墨水粒子等表现层监听</summary>
    public event Action<int, Vector3> OnEnergyGained;

    /// <summary>受击墨水反馈事件（纯视觉，不涨数值），供 Boss 受击演出</summary>
    public event Action<Vector3> OnHitInkFeedback;

    [Header("能量参数（当前生效）")]
    [SerializeField] private int maxEnergy = 100;
    [SerializeField] private int energyPerHit = 20;
    [SerializeField] private int currentEnergy = 0;

    [Header("技能消耗（预留）")]
    [SerializeField] private int skill1Cost = 30;
    [SerializeField] private int skill2Cost = 50;
    [SerializeField] private int skill3Cost = 80;

    [Header("技能冷却（预留）")]
    [SerializeField] private float skillCooldown = 2.0f;
    [SerializeField] private float shieldDuration = 3.0f;

    /// <summary>每次攻击命中获得的能量值</summary>
    public int EnergyPerHit => energyPerHit;

    /// <summary>当前能量值</summary>
    public int CurrentEnergy => currentEnergy;

    /// <summary>能量上限</summary>
    public int MaxEnergy => maxEnergy;

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
    /// 初始化能量系统
    /// </summary>
    private void Initialize()
    {
        currentEnergy = 0;
    }

    /// <summary>
    /// 增加能量值（无来源位置，用于未携带位置信息的调用）
    /// </summary>
    /// <param name="amount">增加的能量值</param>
    public void AddEnergy(int amount)
    {
        AddEnergy(amount, Vector3.zero);
    }

    /// <summary>
    /// 增加能量值（上限为 maxEnergy），满能量时触发 Boss 战，并广播能量事件
    /// </summary>
    /// <param name="amount">增加的能量值</param>
    /// <param name="worldPosition">能量来源世界位置（用于墨水粒子流向 UI）</param>
    public void AddEnergy(int amount, Vector3 worldPosition)
    {
        currentEnergy = Mathf.Min(currentEnergy + amount, maxEnergy);
        Debug.Log("[EnergySkillSystem] 当前能量: " + currentEnergy + " / " + maxEnergy);

        OnEnergyGained?.Invoke(amount, worldPosition);

        if (currentEnergy >= maxEnergy)
        {
            Debug.Log("[EnergySkillSystem] 能量已满！GameManager=" + (GameManager.Instance != null ? "存在" : "NULL") + " State=" + (GameManager.Instance != null ? GameManager.Instance.CurrentState.ToString() : "?") + " hasBossPrefab=" + (GameManager.Instance != null && GameManager.Instance.HasBossPrefab ? "是" : "否"));
            if (GameManager.Instance != null)
                GameManager.Instance.EnterBossFight();
        }
    }

    /// <summary>
    /// 播放受击墨水反馈（纯视觉，不涨数值）
    /// </summary>
    /// <param name="worldPosition">受击世界位置</param>
    public void PlayHitInkFeedback(Vector3 worldPosition)
    {
        OnHitInkFeedback?.Invoke(worldPosition);
    }

    /// <summary>
    /// 重置能量到 0
    /// </summary>
    public void ResetEnergy()
    {
        currentEnergy = 0;
    }

    /// <summary>
    /// 使用技能（预留：MVP 阶段仅打印日志）
    /// </summary>
    /// <param name="skillId">技能 ID（1=冲刺, 2=护盾, 3=全屏清敌）</param>
    public void UseSkill(int skillId)
    {
        Debug.Log("[EnergySkillSystem] 使用技能 " + skillId + "（预留，当前不生效）");
    }
}
