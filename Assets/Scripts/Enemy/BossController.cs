using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class BossController : MonoBehaviour
{
    [Header("生命值")]
    [SerializeField] private int maxHealth = 500;
    [SerializeField] private int health;
    [SerializeField] private int summonHealthThreshold = 100;

    [Header("召唤蝙蝠")]
    [SerializeField] private GameObject batPrefab;
    [SerializeField] private float batSummonDuration = 3f;
    [SerializeField] private int batsPerSecond = 3;
    [SerializeField] private float batSpawnXDistance = 1f;
    [SerializeField] private float batSpawnYRange = 3f;

    [Header("炮弹")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private float fireInterval = 3f;
    [SerializeField] private float bulletSpeed = 8f;
    [SerializeField] private int bulletDamage = 15;

    [Header("连发")]
    [SerializeField] [Range(0f, 1f)] private float burstProbability = 0.25f;
    [SerializeField] private float burstDelay = 0.5f;
    [SerializeField] private float tripleAngle = 25f;

    [Header("屏幕跟随")]
    [SerializeField] private float screenXOffset = 5f;

    [Header("动画")]
    [SerializeField] private string attackTriggerName = "Attack";

    [Header("金币掉落")]
    [SerializeField] private GameObject dropCoinPrefab;
    [SerializeField] private float dropChance = 0.3f;

    private int nextSummonThreshold;
    private bool isDead;
    private Transform playerTransform;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        health = maxHealth;
        nextSummonThreshold = maxHealth - summonHealthThreshold;
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        StartCoroutine(FireLoop());
    }

    private void Update()
    {
        if (isDead || Camera.main == null) return;

        Vector3 pos = transform.position;
        pos.x = Camera.main.transform.position.x + screenXOffset;
        transform.position = pos;
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        int oldHealth = health;
        health -= damage;

        if (dropCoinPrefab != null && Random.value < dropChance)
        {
            Debug.Log("[BossDrop] 掉落 " + dropCoinPrefab.name + " @ " + transform.position);
            Instantiate(dropCoinPrefab, transform.position, Quaternion.identity);
        }

        if (EnergySkillSystem.Instance != null)
            EnergySkillSystem.Instance.PlayHitInkFeedback(transform.position);

        if (health <= nextSummonThreshold && oldHealth > nextSummonThreshold)
        {
            StartCoroutine(SummonBats());
            nextSummonThreshold -= summonHealthThreshold;
        }

        if (health <= 0)
        {
            health = 0;
            isDead = true;
            Die();
        }
    }

    private void Die()
    {
        StopAllCoroutines();

        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayHitEffect();

        if (GameManager.Instance != null)
            GameManager.Instance.EndBossFight();

        Destroy(gameObject);
    }

    private IEnumerator SummonBats()
    {
        if (batPrefab == null) yield break;

        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        float elapsed = 0f;
        float spawnInterval = 1f / batsPerSecond;

        while (elapsed < batSummonDuration)
        {
            SpawnBat();
            elapsed += spawnInterval;
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnBat()
    {
        if (batPrefab == null) return;

        float randomY = Random.Range(-batSpawnYRange, batSpawnYRange);
        Vector3 spawnPos = transform.position + new Vector3(batSpawnXDistance, randomY, 0f);
        Instantiate(batPrefab, spawnPos, Quaternion.identity);
    }

    private IEnumerator FireLoop()
    {
        while (!isDead)
        {
            yield return new WaitForSeconds(fireInterval);

            if (isDead) yield break;

            bool useTriple = Random.value < 0.5f;
            FireVolley(useTriple);

            if (Random.value < burstProbability)
            {
                yield return new WaitForSeconds(burstDelay);
                if (!isDead)
                    FireVolley(useTriple);
            }
        }
    }

    private void FireVolley(bool triple)
    {
        if (bulletPrefab == null) return;

        if (animator != null)
            animator.SetTrigger(attackTriggerName);

        if (triple)
        {
            FireBullet(Vector2.left);
            FireBullet(Quaternion.Euler(0f, 0f, tripleAngle) * Vector2.left);
            FireBullet(Quaternion.Euler(0f, 0f, -tripleAngle) * Vector2.left);
        }
        else
        {
            FireBullet(Vector2.left);
        }
    }

    private void FireBullet(Vector2 direction)
    {
        if (bulletPrefab == null) return;

        GameObject bullet = Instantiate(bulletPrefab, transform.position, Quaternion.identity);
        BossBullet bulletScript = bullet.GetComponent<BossBullet>();
        if (bulletScript != null)
            bulletScript.Fire(direction, bulletSpeed, bulletDamage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 summonCenter = transform.position + Vector3.right * batSpawnXDistance;
        Gizmos.DrawWireCube(summonCenter, new Vector3(0.2f, batSpawnYRange * 2f, 0f));

        Gizmos.color = Color.red;
        Vector3 left = transform.position;
        Gizmos.DrawRay(left, Vector2.left * 2f);
        Gizmos.DrawRay(left, Quaternion.Euler(0f, 0f, tripleAngle) * Vector2.left * 2f);
        Gizmos.DrawRay(left, Quaternion.Euler(0f, 0f, -tripleAngle) * Vector2.left * 2f);
    }
}
