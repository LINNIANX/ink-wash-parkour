using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 飞剑生成器：每隔随机时间间隔在 Player 前方生成预警红点，5 秒后发射飞剑。
/// swordTrackDelays 数组控制每波飞剑数量和每个飞剑跟踪 Player Y 的延迟秒数。
/// 0 = 实时跟踪，0.5 = 跟踪半秒前的 Y。
/// </summary>
public class FlyingSwordSpawner : MonoBehaviour
{
    [Header("生成间隔")]
    [SerializeField] private float minSpawnInterval = 10f;
    [SerializeField] private float maxSpawnInterval = 15f;

    [Header("预警红点")]
    [SerializeField] private float warningTotalDuration = 5f;
    [SerializeField] private float warningLockTime = 2f;
    [SerializeField] private float warningXDistance = 20f;

    [Header("飞剑生成位置")]
    [SerializeField] private float swordSpawnXDistance = 20f;

    [Header("飞剑参数（传递给生成的飞剑）")]
    [SerializeField] private int swordDamage = 15;
    [SerializeField] private float swordSpeed = 8f;

    [Header("飞剑序列")]
    [Tooltip("每个元素=一把飞剑，值为跟踪 Player Y 的延迟秒数（0=实时）")]
    [SerializeField] private float[] swordTrackDelays = { 0f, 1f };

    [Header("历史记录")]
    [SerializeField] private float historySampleRate = 0.05f;

    [Header("预制体")]
    [SerializeField] private GameObject warningDotPrefab;
    [SerializeField] private GameObject swordPrefab;

    [Header("蝙蝠曲线攻击（三选一攻击之一，上下对称射向主角）")]
    [SerializeField] private bool enableBatWave = true;
    [SerializeField] private GameObject batWavePrefab;
    [SerializeField] private int batsPerWave = 6;
    [SerializeField] private float batWaveSpeed = 10f;
    [SerializeField] private float batWaveDistance = 20f;
    [SerializeField] private float batWaveHeight = 6f;

    [Header("绳索预警飞剑（三选一攻击之一：绳索上/下方竖列预警，统一发射）")]
    [SerializeField] private bool enableRopeWarning = true;
    [SerializeField] private int warnCount = 5;
    [SerializeField] private float warnAppearInterval = 0.3f;
    [SerializeField] private float warnTotalDelay = 3f;
    [SerializeField] private float ropeOffsetYMin = 1f;
    [SerializeField] private float ropeOffsetYMax = 5f;

    private Transform playerTransform;
    private List<GameObject> warningDots = new List<GameObject>();
    private List<float> yHistory = new List<float>();
    private List<float> lockedYs = new List<float>();

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        else
        {
            Debug.LogWarning("[FlyingSwordSpawner] 未找到 Player");
        }

        StartCoroutine(SpawnLoop());
    }

    private float yHistoryTimer;

    private void Update()
    {
        if (playerTransform == null) return;

        float maxDelay = 0f;
        foreach (float d in swordTrackDelays)
            if (d > maxDelay) maxDelay = d;

        yHistoryTimer += Time.deltaTime;
        int maxSamples = Mathf.CeilToInt(maxDelay / historySampleRate) + 1;

        while (yHistoryTimer >= historySampleRate)
        {
            yHistoryTimer -= historySampleRate;
            yHistory.Add(playerTransform.position.y);
            while (yHistory.Count > maxSamples)
                yHistory.RemoveAt(0);
        }
    }

    private float GetHistoryY(float delaySeconds)
    {
        if (delaySeconds <= 0f || yHistory.Count == 0)
            return playerTransform.position.y;

        int stepsBack = Mathf.RoundToInt(delaySeconds / historySampleRate);
        if (stepsBack >= yHistory.Count)
            stepsBack = yHistory.Count - 1;

        return yHistory[yHistory.Count - 1 - stepsBack];
    }

    private IEnumerator SpawnLoop()
    {
        if (playerTransform == null) yield break;

        float warningFollowDuration = warningTotalDuration - warningLockTime;
        if (warningFollowDuration < 0f) warningFollowDuration = 0f;

        while (true)
        {
            float waitTime = Random.Range(minSpawnInterval, maxSpawnInterval);
            yield return new WaitForSeconds(waitTime);

            // 三种攻击随机三选一（开关过滤，普通飞剑必参与）
            List<int> available = new List<int>();
            available.Add(0);
            if (enableBatWave) available.Add(1);
            if (enableRopeWarning) available.Add(2);

            switch (available[Random.Range(0, available.Count)])
            {
                case 0:
                    yield return StartCoroutine(ShowWarningAndSpawn(warningFollowDuration));
                    break;
                case 1:
                    LaunchBatWaves();
                    break;
                case 2:
                    yield return StartCoroutine(RopeWarningAttack());
                    break;
            }
        }
    }

    private IEnumerator ShowWarningAndSpawn(float followDuration)
    {
        if (playerTransform == null) yield break;
        if (swordTrackDelays == null || swordTrackDelays.Length == 0) yield break;

        CreateWarningDots();

        float elapsed = 0f;
        while (elapsed < followDuration)
        {
            if (playerTransform == null) yield break;
            bool allAlive = true;
            foreach (var dot in warningDots)
                if (dot == null) { allAlive = false; break; }
            if (!allAlive) yield break;

            for (int i = 0; i < swordTrackDelays.Length; i++)
            {
                UpdateWarningDot(warningDots[i], GetHistoryY(swordTrackDelays[i]));
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        lockedYs.Clear();
        for (int i = 0; i < warningDots.Count; i++)
        {
            lockedYs.Add(warningDots[i] != null ? warningDots[i].transform.position.y : playerTransform.position.y);
        }

        elapsed = 0f;
        while (elapsed < warningLockTime)
        {
            if (playerTransform == null) yield break;
            bool allAlive = true;
            foreach (var dot in warningDots)
                if (dot == null) { allAlive = false; break; }
            if (!allAlive) yield break;

            for (int i = 0; i < lockedYs.Count; i++)
            {
                UpdateWarningDot(warningDots[i], lockedYs[i]);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (var dot in warningDots)
            Destroy(dot);
        warningDots.Clear();

        for (int i = 0; i < lockedYs.Count; i++)
        {
            SpawnSword(lockedYs[i]);
        }
    }

    private void CreateWarningDots()
    {
        if (warningDotPrefab == null)
        {
            Debug.LogError("[FlyingSwordSpawner] 未设置预警红点预制体！请在 Inspector 中拖入");
            return;
        }

        Vector3 basePos = playerTransform.position + Vector3.right * warningXDistance;

        foreach (float delay in swordTrackDelays)
        {
            float y = GetHistoryY(delay);
            GameObject dot = Instantiate(warningDotPrefab, new Vector3(basePos.x, y, 0f), Quaternion.identity);
            warningDots.Add(dot);
        }
    }

    private void UpdateWarningDot(GameObject dot, float targetY)
    {
        if (dot == null || playerTransform == null) return;
        Vector3 pos = dot.transform.position;
        pos.x = playerTransform.position.x + warningXDistance;
        pos.y = targetY;
        dot.transform.position = pos;
    }

    private void SpawnSword(float targetY)
    {
        if (swordPrefab == null)
        {
            Debug.LogError("[FlyingSwordSpawner] 未设置飞剑预制体！请在 Inspector 中拖入");
            return;
        }

        Vector3 position = new Vector3(
            playerTransform.position.x + swordSpawnXDistance,
            targetY,
            0f);

        GameObject sword = Instantiate(swordPrefab, position, Quaternion.identity);
        FlyingSword swordScript = sword.GetComponent<FlyingSword>();
        if (swordScript != null)
        {
            swordScript.SetParameters(swordSpeed, swordDamage);
        }
    }

    /// <summary>
    /// 蝙蝠曲线攻击：概率触发，从上下对称两端射出两条贝塞尔蝙蝠链，
    /// 终点为发射时的主角位置（轨迹定死，主角跳跃可躲）
    /// </summary>
    private void LaunchBatWaves()
    {
        if (!enableBatWave || batWavePrefab == null) return;

        Vector3 playerPos = playerTransform.position;

        LaunchBatWave(playerPos, batWaveHeight);
        LaunchBatWave(playerPos, -batWaveHeight);
    }

    /// <summary>
    /// 生成一条蝙蝠链：batsPerWave 只蝙蝠相位错开，沿贝塞尔弧线射向终点
    /// </summary>
    private void LaunchBatWave(Vector3 playerPos, float heightSign)
    {
        Vector2 start = new Vector2(playerPos.x + batWaveDistance, playerPos.y + heightSign);
        Vector2 end = new Vector2(playerPos.x, playerPos.y);
        Vector2 mid = (start + end) * 0.5f;
        Vector2 control = mid + Vector2.up * heightSign * 0.5f;

        for (int i = 0; i < batsPerWave; i++)
        {
            GameObject bat = Instantiate(batWavePrefab, start, Quaternion.identity);
            BatWave wave = bat.GetComponent<BatWave>();
            if (wave != null)
                wave.Setup(playerTransform, start, control, end, batWaveSpeed, i * (1f / batsPerWave));
        }
    }

    /// <summary>
    /// 绳索预警飞剑：绳索上方或下方（随机）竖列均布 warnCount 个预警点依次出现，
    /// 等待 warnTotalDelay 秒后统一发射飞剑（向左飞，与其他飞剑一致）
    /// </summary>
    private IEnumerator RopeWarningAttack()
    {
        if (warningDotPrefab == null)
        {
            Debug.LogError("[FlyingSwordSpawner] 未设置预警红点预制体！请在 Inspector 中拖入");
            yield break;
        }
        if (swordPrefab == null)
        {
            Debug.LogError("[FlyingSwordSpawner] 未设置飞剑预制体！请在 Inspector 中拖入");
            yield break;
        }

        Collider2D rope = FindRope();
        if (rope == null || playerTransform == null) yield break;

        float sign = Random.value < 0.5f ? 1f : -1f;

        // 竖列均布：只存"相对绳索的 Y"（X 统一由生成器驱动）
        List<float> relYs = new List<float>();
        for (int i = 0; i < warnCount; i++)
        {
            float ratio = warnCount > 1 ? i / (float)(warnCount - 1) : 0f;
            relYs.Add(rope.bounds.center.y + sign * Mathf.Lerp(ropeOffsetYMin, ropeOffsetYMax, ratio));
        }

        // 统一循环：按时间依次生成点 + 每帧对齐所有点 X 到玩家前方（竖列 + 跟随，无跳变）
        List<GameObject> dots = new List<GameObject>();
        float effectiveInterval = Mathf.Max(0.05f, warnAppearInterval);
        float total = Mathf.Max(0f, (warnCount - 1) * effectiveInterval) + warnTotalDelay;
        float elapsed = 0f;
        int nextIndex = 0;

        while (elapsed < total)
        {
            if (playerTransform == null) yield break;

            while (nextIndex < warnCount && elapsed >= nextIndex * effectiveInterval)
            {
                dots.Add(Instantiate(warningDotPrefab, new Vector2(0f, relYs[nextIndex]), Quaternion.identity));
                nextIndex++;
            }

            float targetX = playerTransform.position.x + warningXDistance;
            for (int i = 0; i < dots.Count; i++)
            {
                if (dots[i] != null)
                {
                    Vector3 p = dots[i].transform.position;
                    p.x = targetX;
                    dots[i].transform.position = p;
                }
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        foreach (GameObject dot in dots)
            if (dot != null) Destroy(dot);

        // 发射：X 用"发射时刻"的玩家位置（与普通飞剑相同的 swordSpawnXDistance）
        float fireX = playerTransform.position.x + swordSpawnXDistance;
        foreach (float y in relYs)
        {
            GameObject sword = Instantiate(swordPrefab, new Vector2(fireX, y), Quaternion.identity);
            FlyingSword fs = sword.GetComponent<FlyingSword>();
            if (fs != null)
                fs.SetParameters(swordSpeed, swordDamage);
        }
    }

    /// <summary>
    /// 运行时查找最宽的 Rope 层碰撞器（场景主绳 1000m 最宽，排除蜘蛛绳等干扰）
    /// </summary>
    private Collider2D FindRope()
    {
        Collider2D best = null;
        float bestWidth = 0f;

        foreach (Collider2D col in FindObjectsOfType<Collider2D>())
        {
            if (col.gameObject.layer == LayerMask.NameToLayer("Rope"))
            {
                float w = col.bounds.size.x;
                if (w > bestWidth)
                {
                    bestWidth = w;
                    best = col;
                }
            }
        }
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerTransform == null) return;

        Gizmos.color = Color.red;
        Vector3 warningPos = playerTransform.position + Vector3.right * warningXDistance;
        Gizmos.DrawWireSphere(warningPos, 0.3f);
        Gizmos.DrawLine(playerTransform.position, warningPos);
    }
}
