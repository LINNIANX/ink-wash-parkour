using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 内容类型：场景中同一时刻只能有一种激活
/// </summary>
public enum ContentType
{
    None,               // 空闲，让 WorldSpawner 决定下一段类型
    CoinsOnly,          // 纯金币（场景只有金币 + 地面/绳子/Player/UI）
    ObstaclesAndEnemies,// 纯障碍+敌人（尖刺/木箱/蝙蝠/蜘蛛，不能有金币/飞刀/Boss）
    FlyingSwordsOnly,   // 纯飞刀攻击波（飞剑/预警点/蝙蝠波，不能有其他）
    BossOnly            // 纯Boss战（Boss + 能量胶囊，其他全清）
}

/// <summary>
/// 全局生成调度器（单例，挂在 Managers 上）
/// 目标：保证"有金币时只有金币，有飞刀时只有飞刀，有Boss时只有Boss"。
///
/// 核心机制：
///   1. 任意时刻只有一个激活的 ContentType。
///   2. 想生成某种内容 → 先调用 RequestContentType(申请者, 类型) → 成功后场景会被清场，
///      只保留该类型允许的物体，之后申请者才能生成自己的物体。
///   3. 生成结束 → 调用 ReleaseContentType(申请者) → 状态回到 None，WorldSpawner 可以继续
///      推进下一段。
///   4. 如果已激活的是更高优先级类型（如 BossOnly > FlyingSwordsOnly > CoinsOnly/Obstacles），
///      低优先级的申请会被拒绝，申请者直接跳过本轮。
/// </summary>
public class GlobalSpawnCoordinator : MonoBehaviour
{
    public static GlobalSpawnCoordinator Instance { get; private set; }

    [Header("调试")]
    [SerializeField] private bool enableDebugLog = true;

    /// <summary>当前激活的互斥内容类型</summary>
    public ContentType CurrentType { get; private set; } = ContentType.None;

    /// <summary>当前持有激活类型的申请者（GameObject名，用于防止释放错）</summary>
    private object currentOwner;

    private readonly List<GameObject> toDestroyBuffer = new List<GameObject>();

    // —— 优先级：数字越大越优先。Boss 最优先，其次飞刀波，再次金币/障碍段 ——
    private static readonly System.Collections.Generic.Dictionary<ContentType, int> Priority
        = new System.Collections.Generic.Dictionary<ContentType, int>
        {
            { ContentType.None, 0 },
            { ContentType.CoinsOnly, 1 },
            { ContentType.ObstaclesAndEnemies, 1 },
            { ContentType.FlyingSwordsOnly, 2 },
            { ContentType.BossOnly, 3 },
        };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ============================================================
    //  公共 API：申请 / 释放 类型
    // ============================================================

    /// <summary>
    /// 申请切换到 targetType。
    /// - 如果当前类型优先级更高 → 返回 false（申请者跳过本轮）。
    /// - 如果当前类型就是目标类型且 owner 相同 → 返回 true（已经持有）。
    /// - 否则 → 执行清场（只保留 targetType 允许的物体），切换类型，返回 true。
    /// </summary>
    public bool RequestContentType(object owner, ContentType targetType)
    {
        if (targetType == ContentType.None)
        {
            Log("申请 None 请用 ReleaseContentType()。");
            return false;
        }

        int curPrio = Priority[CurrentType];
        int newPrio = Priority[targetType];

        if (CurrentType == targetType && currentOwner == owner)
        {
            return true; // 重复申请同一个，直接通过
        }

        if (newPrio < curPrio)
        {
            Log($"申请被拒：{targetType}(优先级{newPrio}) 不能打断 当前 {CurrentType}(优先级{curPrio})");
            return false;
        }

        // —— 允许切换：先清场 ——
        Log($"切换内容类型：{CurrentType} → {targetType}（申请者: {(owner != null ? owner.ToString() : "null")}）");
        CleanupSceneForType(targetType);

        CurrentType = targetType;
        currentOwner = owner;
        return true;
    }

    /// <summary>
    /// 释放当前持有类型。只有真正持有者才能释放，其他人调用被忽略。
    /// </summary>
    public void ReleaseContentType(object owner)
    {
        if (owner == null) return;
        if (currentOwner != owner) return; // 不是你拿的，别乱关

        Log($"{owner} 释放类型 {CurrentType}，回到 None");
        CurrentType = ContentType.None;
        currentOwner = null;
    }

    /// <summary>
    /// 强制清除所有类型并清场（场景重置时用）。
    /// </summary>
    public void ForceReset()
    {
        Log("强制重置调度器 + 清场到干净状态（仅保留地面基础设施）");
        CleanupSceneForType(ContentType.None);
        CurrentType = ContentType.None;
        currentOwner = null;
    }

    // ============================================================
    //  清场逻辑：按允许的类型销毁物体
    // ============================================================
    private void CleanupSceneForType(ContentType type)
    {
        // 先把所有活跃 GameObject 过一遍（FindObjectsOfType 慢但次数不多，只在切换类型时调一次）
        GameObject[] all = FindObjectsOfType<GameObject>();
        toDestroyBuffer.Clear();

        foreach (GameObject go in all)
        {
            if (go == null) continue;
            if (IsNeverDestroyed(go)) continue; // 基础设施永远不删

            if (ShouldDestroyForType(go, type))
                toDestroyBuffer.Add(go);
        }

        // 实际删除（另起遍历避免 FindObjects 列表被改）
        int count = 0;
        foreach (GameObject go in toDestroyBuffer)
        {
            if (go != null)
            {
                Destroy(go);
                count++;
            }
        }
        toDestroyBuffer.Clear();

        if (count > 0)
            Log($"  清场完成，销毁 {count} 个物体");

        // —— 清掉 WorldSpawner 记录的已生成列表（它记录的东西可能已经被上面 Destroy 了）——
        WorldSpawner ws = FindObjectOfType<WorldSpawner>();
        if (ws != null)
        {
            // 不清空 spawnedObjects（那是 WorldSpawner 自己管理的生命周期），
            // 但给它一次通知让它清理 null 项。
            ws.CleanupNullEntries();
        }
    }

    /// <summary>
    /// 判断物体是否为"基础设施"，永远不删。
    /// </summary>
    private bool IsNeverDestroyed(GameObject go)
    {
        // Player / Managers / Camera / UI / Canvas / EventSystem
        if (go.CompareTag("Player")) return true;
        if (go.CompareTag("MainCamera")) return true;

        // 常见管理器组件
        if (go.GetComponent<GameManager>() != null) return true;
        if (go.GetComponent<UIManager>() != null) return true;
        if (go.GetComponent<InputManager>() != null) return true;
        if (go.GetComponent<LevelSpeedController>() != null) return true;
        if (go.GetComponent<FeedbackManager>() != null) return true;
        if (go.GetComponent<AudioManager>() != null) return true;
        if (go.GetComponent<GlobalSpawnCoordinator>() != null) return true;
        if (go.GetComponent<FlyingSwordSpawner>() != null) return true;
        if (go.GetComponent<EnergyCapsuleSpawner>() != null) return true;
        if (go.GetComponent<WorldSpawner>() != null) return true;
        if (go.GetComponent<CameraFollow>() != null) return true;
        if (go.GetComponent<ParallaxLooper>() != null) return true;
        if (go.GetComponent<InfiniteGroundLooper>() != null) return true;

        // Canvas / EventSystem / TMP Essentials 根
        if (go.GetComponent<Canvas>() != null) return true;
        if (go.GetComponent<UnityEngine.EventSystems.EventSystem>() != null) return true;

        // Ground 层 / Rope 层（地面和绳子本体）
        int layer = go.layer;
        if (layer == LayerMask.NameToLayer("Ground")) return true;
        if (layer == LayerMask.NameToLayer("Rope")) return true;

        return false;
    }

    /// <summary>
    /// 核心判断：对当前 targetType，这个 go 是否应该被销毁。
    /// 返回 true = 销毁，false = 保留。
    /// </summary>
    private bool ShouldDestroyForType(GameObject go, ContentType type)
    {
        int layer = go.layer;
        string tag = go.tag;

        // —— 先按层快速过滤 ——
        bool isCoinLayer   = layer == LayerMask.NameToLayer("Coin");
        bool isEnemyLayer  = layer == LayerMask.NameToLayer("Enemy");
        bool isObstacle    = layer == LayerMask.NameToLayer("Obstacle");
        bool isProjectile  = layer == LayerMask.NameToLayer("Projectile");  // 技能追踪弹
        bool isPlayer      = layer == LayerMask.NameToLayer("Player");

        // —— 再按组件判断飞刀相关物体（它们不一定在 Enemy 层）——
        bool isFlyingSword     = go.GetComponent<FlyingSword>() != null;
        bool isBatWave         = go.GetComponent<BatWave>() != null;
        bool isWarningDot      = (go.name != null && go.name.Contains("WarningDot"))
                                 || (go.GetComponent<SpriteRenderer>() != null
                                     && go.transform.childCount == 0
                                     && go.GetComponent<Collider2D>() == null
                                     && tag == "Untagged"
                                     && go.name.StartsWith("Warning"));

        // —— 按组件判断其他 ——
        bool isCoinObj         = go.GetComponent<Coin>() != null;
        bool isCoinDrop        = go.GetComponent<CoinDrop>() != null;
        bool isBat             = go.GetComponent<BatController>() != null;
        bool isObstacleObj     = go.GetComponent<Obstacle>() != null;
        bool isSpider          = go.GetComponent<SpiderTrap>() != null;
        bool isBoss            = go.GetComponent<BossController>() != null;
        bool isBossBullet      = go.GetComponent<BossBullet>() != null;
        bool isHommingProj     = go.GetComponent<HommingProjectile>() != null;
        bool isEnergyCapsule   = go.GetComponent<EnergyCapsule>() != null;
        bool isTriggerObstacle = go.GetComponent<TriggerObstacle>() != null;
        bool isFlyingSwordSpawned = isFlyingSword || isBatWave || isWarningDot;

        // 把"明确是某类物体"聚合一下，比纯 layer 更可靠
        bool anyCoin     = isCoinLayer || isCoinObj || isCoinDrop;
        bool anyObstacle = isObstacle || isObstacleObj || isSpider || isTriggerObstacle;
        bool anyEnemy    = isEnemyLayer || isBat;
        bool anySword    = isFlyingSwordSpawned || isProjectile || isHommingProj;
        bool anyBoss     = isBoss || isBossBullet;
        bool anyCapsule  = isEnergyCapsule;

        switch (type)
        {
            case ContentType.CoinsOnly:
                // 只留金币，其他全删
                if (anyCoin) return false;
                if (anyObstacle || anyEnemy || anySword || anyBoss || anyCapsule) return true;
                return false;

            case ContentType.ObstaclesAndEnemies:
                // 只留障碍 + 敌人
                if (anyObstacle || anyEnemy) return false;
                if (anyCoin || anySword || anyBoss || anyCapsule) return true;
                return false;

            case ContentType.FlyingSwordsOnly:
                // 只留飞剑攻击相关（飞剑、预警点、蝙蝠波、追踪弹）
                if (anySword) return false;
                if (anyCoin || anyObstacle || anyEnemy || anyBoss || anyCapsule) return true;
                return false;

            case ContentType.BossOnly:
                // 只留 Boss + Boss 子弹 + 能量胶囊
                if (anyBoss || anyCapsule) return false;
                if (anyCoin || anyObstacle || anyEnemy || anySword) return true;
                return false;

            case ContentType.None:
            default:
                // None：销毁所有生成物（全清），留下干净地面 + 玩家
                if (anyCoin || anyObstacle || anyEnemy || anySword || anyBoss || anyCapsule) return true;
                return false;
        }
    }

    private void Log(string msg)
    {
        if (enableDebugLog)
            Debug.Log("[GlobalSpawnCoordinator] " + msg);
    }
}
