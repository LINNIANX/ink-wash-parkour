using UnityEngine;

/// <summary>
/// 物理掉落金币：Boss 受击掉落的金币，具有正常物理特性（重力下落、地面碰撞），
/// 掉到绳索上会弹跳（只对金币生效，不影响玩家）。拾取/加分逻辑复用 Coin.cs。
/// 通过主动查询（OverlapCircle）检测绳索并贴面反弹，不依赖物理碰撞回调，保证功能稳定。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class CoinDrop : MonoBehaviour
{
    [Header("反弹（只对绳索生效，不影响玩家）")]
    [SerializeField] private float bounceForce = 8f;
    [SerializeField] private int maxBounces = 2;
    [SerializeField] private LayerMask bounceLayers;
    [SerializeField] private float detectRadius = 0.6f;

    private Rigidbody2D rb;
    private int bounceCount;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;
        rb.simulated = true;
    }

    private void Update()
    {
        if (rb.velocity.y > 0f) return;

        Collider2D rope = Physics2D.OverlapCircle(transform.position, detectRadius, bounceLayers);
        if (rope == null) return;

        float ropeTop = rope.bounds.max.y;
        float ext = GetComponent<Collider2D>().bounds.extents.y;

        Vector3 p = transform.position;
        p.y = ropeTop + ext;
        transform.position = p;

        if (bounceCount < maxBounces)
        {
            bounceCount++;
            rb.velocity = new Vector2(rb.velocity.x, bounceForce);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (bounceCount >= maxBounces) return;
        if (((1 << collision.gameObject.layer) & bounceLayers) == 0) return;

        bounceCount++;
        rb.velocity = new Vector2(rb.velocity.x, bounceForce);
    }
}
