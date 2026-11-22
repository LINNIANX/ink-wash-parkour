using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Playing,
    GameOver,
    BossFight
}

/// <summary>
/// 游戏总管理器：管理游戏状态（进行中/结束/Boss战），提供游戏结束与重新开始功能
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏状态")]
    [SerializeField] private GameState currentState = GameState.Playing;

    [Header("Boss")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private float bossSpawnDistance = 15f;

    /// <summary>Boss 预制体是否已设置</summary>
    public bool HasBossPrefab => bossPrefab != null;

    /// <summary>当前游戏状态（只读）</summary>
    public GameState CurrentState => currentState;

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
    /// 初始化游戏：设置状态为进行中，恢复时间缩放
    /// </summary>
    private void Initialize()
    {
        currentState = GameState.Playing;
        Time.timeScale = 1f;
        Debug.Log("[GameManager] 游戏开始");
    }

    /// <summary>
    /// 进入 Boss 战：清场、暂停生成、生成 Boss、重置能量
    /// </summary>
    public void EnterBossFight()
    {
        if (currentState != GameState.Playing) return;
        currentState = GameState.BossFight;
        Debug.Log("[GameManager] 进入 Boss 战");

        WorldSpawner spawner = FindObjectOfType<WorldSpawner>();
        if (spawner != null)
        {
            spawner.IsSpawningEnabled = false;
            spawner.ClearAllSpawned();
        }

        if (EnergySkillSystem.Instance != null)
            EnergySkillSystem.Instance.ResetEnergy();

        if (bossPrefab != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            float spawnX = player != null ? player.transform.position.x + bossSpawnDistance : 30f;
            Vector3 spawnPos = new Vector3(spawnX, 0f, 0f);
            Instantiate(bossPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            Debug.LogError("[GameManager] 未设置 Boss 预制体！请在 Inspector 中拖入");
        }
    }

    /// <summary>
    /// 结束 Boss 战：恢复生成、重置为 Playing 状态
    /// </summary>
    public void EndBossFight()
    {
        if (currentState != GameState.BossFight) return;
        currentState = GameState.Playing;
        Debug.Log("[GameManager] Boss 战结束");

        WorldSpawner spawner = FindObjectOfType<WorldSpawner>();
        if (spawner != null)
        {
            spawner.IsSpawningEnabled = true;
            spawner.SwitchToNextLibrary();
        }
    }

    /// <summary>
    /// 触发游戏结束：切换到 GameOver 状态，通知 UI，延迟后重新加载场景
    /// </summary>
    public void GameOver()
    {
        if (currentState == GameState.GameOver) return;

        currentState = GameState.GameOver;
        Debug.Log("[GameManager] 游戏结束");

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOverPanel();
        }

        Invoke(nameof(RestartGame), 2f);
    }

    /// <summary>
    /// 重新开始游戏：重新加载当前场景
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
