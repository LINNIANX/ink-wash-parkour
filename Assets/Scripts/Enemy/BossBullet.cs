using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossBullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 10f;

    private int damage;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void Fire(Vector2 direction, float speed, int bulletDamage)
    {
        damage = bulletDamage;
        rb.velocity = direction.normalized * speed;
        Invoke(nameof(DestroySelf), lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
            player.TakeDamage(damage);

        DestroySelf();
    }

    private void DestroySelf()
    {
        CancelInvoke(nameof(DestroySelf));
        Destroy(gameObject);
    }
}
