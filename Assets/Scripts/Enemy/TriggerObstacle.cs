using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TriggerObstacle : MonoBehaviour
{
    [Header("伤害")]
    [SerializeField] private int damage = 20;

    [Header("销毁")]
    [SerializeField] private float destroyDelay = 5f;

    [Header("远程技能")]
    [SerializeField] private bool canBeTargetedByProjectile = true;

    public bool CanBeTargetedByProjectile => canBeTargetedByProjectile;

    private bool hasBeenHit;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenHit) return;
        if (!other.CompareTag("Player")) return;

        hasBeenHit = true;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.TakeDamage(damage);
        Invoke(nameof(DestroySelf), destroyDelay);
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
