using UnityEngine;

/// <summary>
/// 曲线蝙蝠：由 FlyingSwordSpawner 生成，沿预设二次贝塞尔曲线飞行（轨迹发射时定死），
/// 多只蝙蝠相位错开形成"蝙蝠链"射向发射时的主角位置，玩家可跳跃躲避。
/// 碰玩家扣血（穿过不销毁），到达终点自动销毁。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BatWave : MonoBehaviour
{
    [Header("伤害")]
    [SerializeField] private int damage = 15;

    private Vector2 start;
    private Vector2 control;
    private Vector2 end;
    private float speed;
    private float phaseOffset;
    private float traveled;
    private float totalLength;
    private Transform playerTransform;
    private float launchPlayerX;

    /// <summary>
    /// 由生成器注入：贝塞尔三点 + 速度 + 队列相位（0~1 错开）。
    /// player 为主角引用（用于叠加水平位移跟随世界滚动，轨迹本身发射时定死）
    /// </summary>
    public void Setup(Transform player, Vector2 startPos, Vector2 controlPos, Vector2 endPos, float moveSpeed, float phase)
    {
        playerTransform = player;
        launchPlayerX = player != null ? player.position.x : 0f;
        start = startPos;
        control = controlPos;
        end = endPos;
        speed = moveSpeed;
        phaseOffset = phase;
        totalLength = Vector2.Distance(start, control) + Vector2.Distance(control, end);
        transform.position = start;
    }

    private void Update()
    {
        traveled += speed * Time.deltaTime;
        float t = traveled / totalLength + phaseOffset;

        if (t >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        float inv = 1f - t;
        Vector2 bezierPos = inv * inv * start + 2f * inv * t * control + t * t * end;

        // 只叠加主角 X 位移（跟随世界滚动）；Y 不跟随，轨迹不受主角跳跃影响
        if (playerTransform != null)
        {
            float deltaX = playerTransform.position.x - launchPlayerX;
            transform.position = bezierPos + new Vector2(deltaX, 0f);
        }
        else
        {
            transform.position = bezierPos;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
                player.TakeDamage(damage);
        }
    }
}
