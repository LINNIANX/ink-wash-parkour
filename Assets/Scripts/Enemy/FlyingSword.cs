using UnityEngine;

/// <summary>
/// 飞剑弹体：向左匀速飞行，碰撞 Player 造成伤害后自毁
/// 使用 Enemy 层，可被玩家攻击砍掉
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class FlyingSword : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float speed = 8f;

    [Header("伤害")]
    [SerializeField] private int damage = 15;

    [Header("远程攻击")]
    [SerializeField] private bool canBeTargetedByProjectile = false;

    /// <summary>小球远程攻击是否可以锁定此飞剑（默认否）</summary>
    public bool CanBeTargetedByProjectile => canBeTargetedByProjectile;

    private void Start()
    {
    }

    /// <summary>
    /// 由 FlyingSwordSpawner 调用，设置速度和伤害后开始飞行
    /// </summary>
    public void SetParameters(float moveSpeed, int swordDamage)
    {
        speed = moveSpeed;
        damage = swordDamage;
        Initialize();
    }

    /// <summary>
    /// 初始化飞剑：设 Kinematic 刚体，向左匀速飞行
    /// </summary>
    private void Initialize()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.velocity = Vector2.left * speed;
    }

    private void Update()
    {
        if (Camera.main != null && transform.position.x < Camera.main.transform.position.x - 15f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }
            Destroy(gameObject);
        }
    }
}
