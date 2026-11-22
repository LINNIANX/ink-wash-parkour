using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 结算场景分数显示（完全自启动，不依赖场景里的任何配置，不会被场景覆盖/剥离影响）。
/// 通过 RuntimeInitializeOnLoadMethod 注册场景加载回调：
/// 每次加载名为 jiesuan 的场景时自动创建自身，然后在 Start 中显示纯数字分数。
/// 自愈逻辑：优先查找名为 ScoreText 的 Text 并统一样式（黑色，位置保留编辑器设置）；
/// 若物体存在但 Text 缺失则补挂；物体不存在则在 Canvas 下自动创建（y=-10）。
/// </summary>
public class ResultScoreDisplay : MonoBehaviour
{
    private const string ResultSceneName = "jiesuan";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryCreate(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryCreate(scene);
    }

    private static void TryCreate(Scene scene)
    {
        if (scene.name != ResultSceneName) return;
        if (FindObjectOfType<ResultScoreDisplay>() != null) return;

        new GameObject("ResultScoreDisplay").AddComponent<ResultScoreDisplay>();
        Debug.Log("[ResultScoreDisplay] 结算场景已自动创建分数显示组件");
    }

    private void Start()
    {
        Text scoreText = FindScoreText();

        // 场景里已有可用的 Text：统一样式并强制归位到 y=0
        // （StartMenu 的旧逻辑可能在 Awake 里以旧坐标修复过它，这里统一覆盖）
        if (scoreText != null)
        {
            SetupScoreText(scoreText, scoreText.transform as RectTransform);
        }

        // ScoreText 物体存在但 Text 缺失 → 补挂
        if (scoreText == null)
        {
            GameObject existing = GameObject.Find("ScoreText");
            if (existing != null)
            {
                scoreText = existing.GetComponent<Text>();
                if (scoreText == null) scoreText = existing.AddComponent<Text>();
                SetupScoreText(scoreText, existing.GetComponent<RectTransform>());
            }
        }

        // 物体不存在 → Canvas 下自动创建
        if (scoreText == null)
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                Debug.LogWarning("[ResultScoreDisplay] 场景中没有 Canvas，无法显示结算分数");
                return;
            }

            GameObject obj = new GameObject("ScoreText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            obj.transform.SetParent(canvas.transform, false);
            obj.transform.SetAsLastSibling();
            scoreText = obj.GetComponent<Text>();
            SetupScoreText(scoreText, obj.GetComponent<RectTransform>());
        }

        int score = PlayerPrefs.GetInt("FinalScore", 0);
        scoreText.text = score.ToString();
        Debug.Log("[ResultScoreDisplay] 结算分数已显示: " + score);
    }

    private Text FindScoreText()
    {
        Text[] texts = FindObjectsOfType<Text>(true);
        foreach (Text t in texts)
        {
            if (t != null && t.gameObject.name == "ScoreText") return t;
        }
        return null;
    }

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

        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0f, 0f);
            rt.sizeDelta = new Vector2(400f, 60f);
            rt.localScale = Vector3.one;
        }
    }
}
