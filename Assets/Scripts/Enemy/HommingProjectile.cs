using System.Collections;
using UnityEngine;

/// <summary>
/// 自导引弹体：沿抛物线弧线飞向目标。
/// 击中 Obstacle → 两者消失，击中 Enemy → 扣血/销毁 + 弹体消失。
/// 运行时由 PlayerController.SetTarget 注入目标参数。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class HommingProjectile : MonoBehaviour
{
    [Header("飞行参数")]
    [SerializeField] private float speed = 12f;
    [SerializeField] private float arcHeight = 2f;

    [Header("伤害")]
    [SerializeField] private int damage = 30;

    private Transform target;
    private Vector3 startPos;
    private Coroutine flightRoutine;

    private void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
        Collider2D col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// 由 PlayerController 调用，设置目标并启动飞行
    /// </summary>
    public void SetTarget(Transform targetTransform, float moveSpeed, float arc, int projectileDamage)
    {
        target = targetTransform;
        speed = moveSpeed;
        arcHeight = arc;
        damage = projectileDamage;
        startPos = transform.position;
        flightRoutine = StartCoroutine(FlyToTarget());
    }

    /// <summary>
    /// 由 PlayerController 调用，直线向右飞行（无目标，Buff 模式）
    /// </summary>
    public void FireStraight(float moveSpeed, float arc, int projectileDamage)
    {
        speed = moveSpeed;
        arcHeight = arc;
        damage = projectileDamage;
        flightRoutine = StartCoroutine(FlyStraight());
    }

    private IEnumerator FlyStraight()
    {
        float maxDistance = 30f;
        Vector3 start = transform.position;
        float totalDuration = maxDistance / speed;
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            float t = elapsed / totalDuration;
            float x = start.x + t * maxDistance;
            float y = start.y + Mathf.Sin(t * Mathf.PI) * arcHeight;

            transform.position = new Vector3(x, y, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator FlyToTarget()
    {
        while (target != null)
        {
            startPos = transform.position;
            float distance = Vector3.Distance(startPos, target.position);
            float duration = distance / speed;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (target == null) { Destroy(gameObject); yield break; }

                float t = elapsed / duration;
                float x = Mathf.Lerp(startPos.x, target.position.x, t);
                float baseY = Mathf.Lerp(startPos.y, target.position.y, t);
                float y = baseY + Mathf.Sin(t * Mathf.PI) * arcHeight;

                transform.position = new Vector3(x, y, 0f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (target == null) { Destroy(gameObject); yield break; }
        }
        Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
        {
            SpiderTrap spider = other.GetComponent<SpiderTrap>();
            if (spider != null) { Destroy(gameObject); return; }

            Destroy(other.gameObject);
            Destroy(gameObject);
            return;
        }

        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            BossController boss = other.GetComponent<BossController>();
            if (boss != null)
            {
                boss.TakeDamage(damage);
            }
            else
            {
                Destroy(other.gameObject);
            }

            if (FeedbackManager.Instance != null)
                FeedbackManager.Instance.PlayAttackHit();

            Destroy(gameObject);
            return;
        }
    }
}
