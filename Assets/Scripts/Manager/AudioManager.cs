using UnityEngine;

/// <summary>
/// 音频管理器（MVP 阶段：所有方法仅 Debug.Log，不播放任何音频）
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("调试开关")]
    [SerializeField] private bool enableDebugLog = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    /// <summary>播放背景音乐</summary>
    public void PlayBGM()
    {
        if (enableDebugLog) Debug.Log("[AudioManager] 播放 BGM");
    }

    /// <summary>播放音效</summary>
    /// <param name="sfxName">音效名称</param>
    public void PlaySFX(string sfxName)
    {
        if (enableDebugLog) Debug.Log("[AudioManager] 播放 SFX: " + sfxName);
    }

    /// <summary>停止背景音乐</summary>
    public void StopBGM()
    {
        if (enableDebugLog) Debug.Log("[AudioManager] 停止 BGM");
    }
}
