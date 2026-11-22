using UnityEngine;

/// <summary>
/// 蝙蝠敌人：在两点之间往返移动（水平或垂直），被攻击即销毁，触碰玩家造成伤害
/// MVP 阶段：仅实现最简单的两点往返移动
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BatController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float patrolRange = 4f;

    [Header("方向")]
    [SerializeField] private bool moveHorizontal = false;

    [Header("水平漂移")]
    [SerializeField] private float horizontalDriftSpeed = 8f;

    [Header("伤害")]
    [SerializeField] private int collisionDamage = 15;

    [Header("远程攻击")]
    [SerializeField] private bool canBeTargetedByProjectile = true;

    /// <summary>小球远程攻击是否可以锁定此敌人</summary>
    public bool CanBeTargetedByProjectile => canBeTargetedByProjectile;

    private Rigidbody2D rb;
    private Vector2 startPosition;
    private int direction = 1;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化蝙蝠：记录初始位置，设置 Kinematic 刚体
    /// </summary>
    private void Initialize()
    {
        startPosition = transform.position;
        rb.bodyType = RigidbodyType2D.Kinematic;
        direction = Random.value < 0.5f ? 1 : -1;
    }

    private void FixedUpdate()
    {
        PatrolMove();
    }

    /// <summary>
    /// 往返巡逻移动：在巡逻范围内来回移动，到达边界时反转方向
    /// </summary>
    private void PatrolMove()
    {
        startPosition.x += horizontalDriftSpeed * Time.fixedDeltaTime;

        Vector2 targetPosition = startPosition;
        if (moveHorizontal)
        {
            targetPosition.x += direction * patrolRange;
        }
        else
        {
            targetPosition.y += direction * patrolRange;
        }

        Vector2 newPosition = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.fixedDeltaTime);
        rb.MovePosition(newPosition);

        if (Vector2.Distance(transform.position, targetPosition) < 0.01f)
        {
            direction *= -1;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(collisionDamage);
            }
        }
    }

    /// <summary>获取当前移动速度（供 WorldSpawner 随机化）</summary>
    public float GetMoveSpeed() => moveSpeed;

    /// <summary>设置移动速度</summary>
    public void SetMoveSpeed(float speed) => moveSpeed = speed;

    /// <summary>获取当前巡逻范围</summary>
    public float GetPatrolRange() => patrolRange;

    /// <summary>设置巡逻范围</summary>
    public void SetPatrolRange(float range) => patrolRange = range;

    /// <summary>
    /// 在编辑器中绘制巡逻范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector2 pos = Application.isPlaying ? startPosition : (Vector2)transform.position;
        Vector2 endPoint = pos;

        if (moveHorizontal)
        {
            endPoint.x += patrolRange;
            Gizmos.DrawLine(new Vector2(pos.x - patrolRange, pos.y), new Vector2(pos.x + patrolRange, pos.y));
            Gizmos.DrawWireSphere(endPoint, 0.2f);
            endPoint.x -= patrolRange * 2;
            Gizmos.DrawWireSphere(endPoint, 0.2f);
        }
        else
        {
            endPoint.y += patrolRange;
            Gizmos.DrawLine(new Vector2(pos.x, pos.y - patrolRange), new Vector2(pos.x, pos.y + patrolRange));
            Gizmos.DrawWireSphere(endPoint, 0.2f);
            endPoint.y -= patrolRange * 2;
            Gizmos.DrawWireSphere(endPoint, 0.2f);
        }
    }
}
