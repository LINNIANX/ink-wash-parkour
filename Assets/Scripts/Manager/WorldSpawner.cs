using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 预设组合信息：定义预制体、出现阶段、权重
/// </summary>
[System.Serializable]
public class LevelPresetInfo
{
    [Tooltip("编辑器中显示的名称")]
    public string name = "新预设";

    [Tooltip("预设预制体（包含所有子物体的组合）")]
    public GameObject prefab;

    [Tooltip("最低难度阶段（1~4）")]
    [Range(1, 4)]
    public int minStage = 1;

    [Tooltip("相对权重，越大出现频率越高")]
    [Range(0.1f, 10f)]
    public float weight = 1f;

    [Tooltip("生成时的 Y 轴偏移（正值上、负值下）")]
    public float yOffset = 0f;
}

/// <summary>
/// 地图预设库：一套预设组合的集合。打完 Boss 后随机切换到其他库，形成多地图循环
/// </summary>
[System.Serializable]
public class LevelPresetLibrary
{
    [Tooltip("地图库名称（编辑器标识）")]
    public string name = "新地图库";

    [Tooltip("该库的预设组合列表")]
    public LevelPresetInfo[] presets;
}

/// <summary>
/// 世界生成器：从预设组合库中加权随机选取预制体，展开生成交互物。
/// 4 阶段难度递增控制哪些预设可用，自动清理后方物体。
/// 挂在 Managers 物体上。
/// </summary>
public class WorldSpawner : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private float segmentSize = 15f;
    [SerializeField] private int spawnAheadSegments = 3;
    [SerializeField] private float cleanupBehindDistance = 20f;

    [Header("地图预设库（打完 Boss 随机切换，每库一套预设组合）")]
    [SerializeField] private LevelPresetLibrary[] libraries;

    [Header("难度递增 — 阶段阈值")]
    [SerializeField] private int stage2AfterSegments = 10;
    [SerializeField] private int stage3AfterSegments = 30;
    [SerializeField] private int stage4AfterSegments = 60;

    [Header("难度递增 — 速度")]
    [SerializeField] private float speedStage1 = 8f;
    [SerializeField] private float speedStage2 = 8f;
    [SerializeField] private float speedStage3 = 10f;
    [SerializeField] private float speedStage4 = 12f;

    /// <summary>是否启用生成（Boss 战时设为 false）</summary>
    public bool IsSpawningEnabled { get; set; } = true;

    private Transform playerTransform;
    private int lastSpawnedSegment = -1;
    private int totalSegmentsSpawned;
    private int currentLibraryIndex;
    private readonly List<GameObject> spawnedObjects = new List<GameObject>();

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化：查找 Player，预先生成初始段落
    /// </summary>
    private void Initialize()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogError("[WorldSpawner] 未找到 Player");
            return;
        }

        float currentSegmentIndex = Mathf.Floor(playerTransform.position.x / segmentSize);
        lastSpawnedSegment = (int)currentSegmentIndex - 1;
        GenerateSegmentsUpTo((int)currentSegmentIndex + spawnAheadSegments);
    }

    private void Update()
    {
        if (!IsSpawningEnabled) return;
        if (playerTransform == null) return;

        float currentSegmentIndex = Mathf.Floor(playerTransform.position.x / segmentSize);
        int targetSegment = (int)currentSegmentIndex + spawnAheadSegments;

        if (targetSegment > lastSpawnedSegment)
        {
            GenerateSegmentsUpTo(targetSegment);
        }

        CleanupBehindPlayer();
    }

    /// <summary>
    /// 生成从 lastSpawnedSegment+1 到 targetSeg（含）的所有段落
    /// </summary>
    private void GenerateSegmentsUpTo(int targetSeg)
    {
        while (lastSpawnedSegment < targetSeg)
        {
            lastSpawnedSegment++;
            GenerateSegment(lastSpawnedSegment);
            totalSegmentsSpawned++;
        }
    }

    /// <summary>
    /// 生成单个段落：按难度筛选可用预设 → 加权随机选取 → Instantiate 到段落起点
    /// </summary>
    private void GenerateSegment(int segIndex)
    {
        int stage = GetDifficultyStage();
        Vector3 basePos = new Vector3(segIndex * segmentSize, 0f, 0f);

        LevelPresetInfo chosen = PickRandomPreset(stage);
        if (chosen == null || chosen.prefab == null) return;

        Vector3 spawnPos = basePos + Vector3.up * chosen.yOffset;
        GameObject obj = Instantiate(chosen.prefab, spawnPos, Quaternion.identity);
        spawnedObjects.Add(obj);
    }

    /// <summary>
    /// 从当前地图库中按阶段过滤后加权随机选取
    /// </summary>
    private LevelPresetInfo PickRandomPreset(int stage)
    {
        LevelPresetInfo[] currentPresets = GetCurrentPresets();
        if (currentPresets == null) return null;

        var available = new List<LevelPresetInfo>();
        float totalWeight = 0f;

        foreach (LevelPresetInfo p in currentPresets)
        {
            if (p == null || p.prefab == null || p.weight <= 0f) continue;
            if (p.minStage > stage) continue;
            available.Add(p);
            totalWeight += p.weight;
        }

        if (available.Count == 0) return null;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        foreach (LevelPresetInfo p in available)
        {
            cumulative += p.weight;
            if (roll <= cumulative) return p;
        }

        return available[available.Count - 1];
    }

    /// <summary>
    /// 获取当前激活地图库的预设列表
    /// </summary>
    private LevelPresetInfo[] GetCurrentPresets()
    {
        if (libraries == null || libraries.Length == 0) return null;
        if (currentLibraryIndex < 0 || currentLibraryIndex >= libraries.Length) return null;

        LevelPresetLibrary lib = libraries[currentLibraryIndex];
        return lib != null ? lib.presets : null;
    }

    /// <summary>
    /// 随机切换到下一个地图库（排除当前库，保证每次换风格）。
    /// 由 GameManager.EndBossFight 调用；只有 1 个库时不切换
    /// </summary>
    public void SwitchToNextLibrary()
    {
        if (libraries == null || libraries.Length == 0) return;
        if (libraries.Length == 1) return;

        int next;
        do
        {
            next = Random.Range(0, libraries.Length);
        } while (next == currentLibraryIndex);

        currentLibraryIndex = next;
        Debug.Log("[WorldSpawner] 切换到地图库: " + (libraries[currentLibraryIndex] != null ? libraries[currentLibraryIndex].name : "未命名"));
    }

    /// <summary>
    /// 清理 Player 后方超过 cleanupBehindDistance 的物体
    /// </summary>
    private void CleanupBehindPlayer()
    {
        float threshold = playerTransform.position.x - cleanupBehindDistance;

        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            GameObject obj = spawnedObjects[i];
            if (obj == null)
            {
                spawnedObjects.RemoveAt(i);
                continue;
            }

            if (obj.transform.position.x < threshold)
            {
                Destroy(obj);
                spawnedObjects.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 根据已生成段落数获取当前难度阶段（1~4）
    /// </summary>
    private int GetDifficultyStage()
    {
        if (totalSegmentsSpawned >= stage4AfterSegments) return 4;
        if (totalSegmentsSpawned >= stage3AfterSegments) return 3;
        if (totalSegmentsSpawned >= stage2AfterSegments) return 2;
        return 1;
    }

    /// <summary>获取当前阶段的推荐速度</summary>
    public float GetCurrentSpeed()
    {
        int stage = GetDifficultyStage();
        switch (stage)
        {
            case 2: return speedStage2;
            case 3: return speedStage3;
            case 4: return speedStage4;
            default: return speedStage1;
        }
    }

    /// <summary>
    /// 只清理 spawnedObjects 中已被外部销毁（变成 null）的条目，不销毁任何存活物体。
    /// 供 GlobalSpawnCoordinator 清场后调用，避免列表里堆积已销毁的引用。
    /// </summary>
    public void CleanupNullEntries()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] == null)
                spawnedObjects.RemoveAt(i);
        }
    }

    /// <summary>
    /// 清除所有已生成的物体（Boss 战时清场用）
    /// </summary>
    public void ClearAllSpawned()
    {
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedObjects.Clear();
    }
}
