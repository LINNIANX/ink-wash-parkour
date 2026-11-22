using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 通用菜单场景跳转：开始 / 设置 / 结算三个场景都挂这一个脚本即可。
/// 运行时会按固定按钮名自动补绑定，防止场景里编辑器事件被覆盖丢失：
///   StartButton    → 开始/重新开始游戏（SampleScene）
///   SettingButton  → 系统设置（shezhi）
///   fanhuiButton   → 返回菜单（ksishi，设置场景用）
///   BackButton     → 返回菜单（ksishi，结算场景用）
///   RestartButton  → 重新开始（SampleScene，结算场景用）
/// 若按钮在 Inspector 里已经有事件绑定，则不重复添加。
/// </summary>
public class StartMenu : MonoBehaviour
{
    [Header("场景名称")]
    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private string settingsSceneName = "shezhi";
    [SerializeField] private string menuSceneName = "ksishi";
    [SerializeField] private string resultSceneName = "jiesuan";

    [Header("结算分数显示")]
    [Tooltip("结算场景的分数文本（留空则自动查找名为 ScoreText 的 Text）")]
    [SerializeField] private Text scoreText;

    private void Awake()
    {
        BindIfUnbound("StartButton", StartGame);
        BindIfUnbound("SettingButton", OpenSettings);
        BindIfUnbound("QuitButton", QuitGame);
        BindIfUnbound("fanhuiButton", BackToMenu);
        BindIfUnbound("BackButton", BackToMenu);
        BindIfUnbound("RestartButton", StartGame);

        // 只在结算场景显示分数，菜单/设置场景不显示并隐藏残留的 ScoreText
        if (SceneManager.GetActiveScene().name == resultSceneName)
            DisplayResultScore();
        else
            HideStaleScoreText();
    }

    /// <summary>隐藏菜单/设置场景里残留的 ScoreText 物体（之前运行时可能自动创建过）</summary>
    private void HideStaleScoreText()
    {
        GameObject go = GameObject.Find("ScoreText");
        if (go != null && go.activeSelf)
            go.SetActive(false);
    }

    /// <summary>
    /// 结算场景：从 PlayerPrefs 读取 GameManager 保存的最终分数并显示。
    /// 自愈逻辑：优先用 Inspector 指定的 Text；否则查找名为 ScoreText 的 Text；
    /// 若 ScoreText 物体存在但 Text 组件损坏/缺失，则自动补挂并配置；
    /// 若物体根本不存在，则在 Canvas 下自动创建。
    /// </summary>
    private void DisplayResultScore()
    {
        if (scoreText == null)
        {
            Text[] texts = FindObjectsOfType<Text>(true);
            foreach (Text t in texts)
            {
                if (t != null && t.gameObject.name == "ScoreText")
                {
                    scoreText = t;
                    break;
                }
            }
        }

        // 场景里已有可用的 Text：统一应用样式（黑色等），但位置保留编辑器里的设置
        if (scoreText != null)
        {
            SetupScoreText(scoreText, null);
        }

        // ScoreText 物体存在但 Text 组件损坏/缺失 → 自动补挂
        if (scoreText == null)
        {
            GameObject existing = GameObject.Find("ScoreText");
            if (existing != null)
            {
                scoreText = existing.GetComponent<Text>();
                if (scoreText == null) scoreText = existing.AddComponent<Text>();
                SetupScoreText(scoreText, existing.GetComponent<RectTransform>());
                Debug.Log("[StartMenu] ScoreText 的 Text 组件已自动修复");
            }
        }

        // 物体根本不存在 → 在 Canvas 下自动创建
        if (scoreText == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[StartMenu] 场景中没有 Canvas，无法显示结算分数");
                return;
            }

            GameObject obj = new GameObject("ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(canvas.transform, false);
            obj.transform.SetAsLastSibling();
            scoreText = obj.GetComponent<Text>();
            SetupScoreText(scoreText, obj.GetComponent<RectTransform>());
            Debug.Log("[StartMenu] 已自动创建 ScoreText 并显示结算分数");
        }

        int score = PlayerPrefs.GetInt("FinalScore", 0);
        scoreText.text = score.ToString();
        Debug.Log("[StartMenu] 结算分数已显示: " + score);
    }

    /// <summary>配置分数文本的字体、字号、颜色与位置（按钮上方居中）</summary>
    private void SetupScoreText(Text text, RectTransform rt)
    {
        Font font = null;
        try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
        if (font == null)
        {
            try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
        }

        if (font != null) text.font = font;
        text.fontSize = 40;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        text.text = "0";

        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, -35f);
            rt.sizeDelta = new Vector2(400f, 60f);
            rt.localScale = Vector3.one;
        }
    }

    /// <summary>
    /// 指定名称的按钮若没有任何持久化事件，运行时补上监听
    /// </summary>
    private void BindIfUnbound(string buttonName, UnityEngine.Events.UnityAction action)
    {
        Button button = FindButtonByName(buttonName);
        if (button == null) return;

        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
            Debug.Log("[StartMenu] 运行时自动绑定按钮：" + buttonName);
        }
    }

    /// <summary>
    /// 在当前场景中按名字查找 Button（包括未激活物体的子物体）
    /// </summary>
    private Button FindButtonByName(string buttonName)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button b in buttons)
        {
            if (b != null && b.gameObject.name == buttonName)
                return b;
        }
        return null;
    }

    /// <summary>
    /// 点击"开始游戏 / 重新开始"按钮：加载游戏场景
    /// </summary>
    public void StartGame()
    {
        Debug.Log("[StartMenu] 进入游戏场景：" + gameSceneName);
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// 点击"系统设置"按钮：加载设置场景
    /// </summary>
    public void OpenSettings()
    {
        Debug.Log("[StartMenu] 进入设置场景：" + settingsSceneName);
        SceneManager.LoadScene(settingsSceneName);
    }

    /// <summary>
    /// 点击"返回菜单"按钮：回到开始场景
    /// </summary>
    public void BackToMenu()
    {
        Debug.Log("[StartMenu] 返回开始菜单：" + menuSceneName);
        SceneManager.LoadScene(menuSceneName);
    }

    /// <summary>
    /// 点击"退出游戏"按钮：关闭游戏。
    /// 编辑器里只会打印日志（Application.Quit 在编辑器不生效），打包后真正退出。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("[StartMenu] 退出游戏");
        Application.Quit();
    }
}
