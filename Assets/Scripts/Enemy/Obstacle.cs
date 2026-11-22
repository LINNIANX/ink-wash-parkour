using UnityEngine;

/// <summary>
/// 障碍物：非触发器物理碰撞器，玩家触碰后扣除生命值，
/// 碰撞后延迟自毁（避免被 WorldSpawner 立即清理）
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Obstacle : MonoBehaviour
{
    [Header("伤害参数")]
    [SerializeField] private int damage = 20;

    [Header("自毁")]
    [SerializeField] private float destroyDelay = 5f;

    [Header("远程攻击")]
    [SerializeField] private bool canBeTargetedByProjectile = true;

    /// <summary>小球远程攻击是否可以锁定此障碍物</summary>
    public bool CanBeTargetedByProjectile => canBeTargetedByProjectile;

    private bool hasBeenHit;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage);
            }

            if (!hasBeenHit)
            {
                hasBeenHit = true;
                Invoke(nameof(DestroySelf), destroyDelay);
            }
        }
    }

    private void DestroySelf()
    {
        Destroy(gameObject);
    }
}
