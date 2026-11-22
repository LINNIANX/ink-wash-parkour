using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI 管理器：更新分数、生命值等 UI 显示，控制 GameOver 面板。
/// 分数数据源为 EnergySkillSystem（能量=分数，杀敌和吃金币都涨）。
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD 文本引用")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text healthText;

    [Header("GameOver 面板")]
    [SerializeField] private GameObject gameOverPanel;

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
    /// 初始化 UI：刷新分数与血量显示，隐藏 GameOver 面板
    /// </summary>
    private void Initialize()
    {
        UpdateScore(0);
        UpdateHealth(100, 100);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }

    private void Update()
    {
        if (EnergySkillSystem.Instance != null)
            UpdateScore(EnergySkillSystem.Instance.CurrentEnergy);
    }

    /// <summary>
    /// 更新分数显示
    /// </summary>
    private void UpdateScore(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = "分数: " + score;
        }
    }

    /// <summary>
    /// 更新生命值显示
    /// </summary>
    /// <param name="current">当前生命值</param>
    /// <param name="max">最大生命值</param>
    public void UpdateHealth(int current, int max)
    {
        if (healthText != null)
        {
            healthText.text = "生命: " + current + " / " + max;
        }
    }

    /// <summary>
    /// 显示 GameOver 面板
    /// </summary>
    public void ShowGameOverPanel()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
    }
}
