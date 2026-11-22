using UnityEngine;

/// <summary>
/// 输入管理器：集中检测键盘输入（W/S/J），提供公共布尔标志供其他模块读取
/// PlayerController 不直接调用 Input.GetKey，而是通过此管理器获取输入状态
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    /// <summary>当前帧按下 W 键（跳跃）</summary>
    public bool JumpKeyDown { get; private set; }

    /// <summary>当前帧按下 S 键（滑铲/翻转绳索）</summary>
    public bool SlideKeyDown { get; private set; }

    /// <summary>持续按住 S 键（滑铲/快速下落）</summary>
    public bool SlideKeyHold { get; private set; }

    /// <summary>当前帧按下 J 键（攻击）</summary>
    public bool AttackKeyDown { get; private set; }

    /// <summary>持续按住 K 键（吸金币）</summary>
    public bool CoinMagnetKeyHold { get; private set; }

    /// <summary>持续按住 Space 键（远程技能）</summary>
    public bool SkillKeyHold { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState == GameState.GameOver)
        {
            JumpKeyDown = false;
            SlideKeyDown = false;
            SlideKeyHold = false;
            AttackKeyDown = false;
            CoinMagnetKeyHold = false;
            SkillKeyHold = false;
            return;
        }

        JumpKeyDown = Input.GetKeyDown(KeyCode.W);
        SlideKeyDown = Input.GetKeyDown(KeyCode.S);
        SlideKeyHold = Input.GetKey(KeyCode.S);
        AttackKeyDown = Input.GetKeyDown(KeyCode.J);
        CoinMagnetKeyHold = Input.GetKey(KeyCode.K);
        SkillKeyHold = Input.GetKey(KeyCode.Space);
    }
}
