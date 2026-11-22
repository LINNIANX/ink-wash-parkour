using System.Collections;
using UnityEngine;

/// <summary>
/// 蜘蛛机关：绳子悬吊方块，Player 水平接近时方块坠落一定距离，
/// 然后弹性回弹到原位。下落及回弹期间碰撞 Player 扣血（每周期一次）。
/// Layer 建议设为 Obstacle，碰撞矩阵已有 Player×Obstacle。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class SpiderTrap : MonoBehaviour
{
    private enum State { Idle, Dropping, Bouncing, Settled }

    [Header("触发")]
    [SerializeField] private float triggerDistance = 10f;

    [Header("下落")]
    [SerializeField] private float dropDistance = 5f;
    [SerializeField] private float dropSpeed = 15f;

    [Header("回弹")]
    [SerializeField] private float bounceAmount = 1.5f;
    [SerializeField] private float bounceDuration = 0.3f;
    [SerializeField] private float bounceDamping = 3f;

    [Header("伤害")]
    [SerializeField] private int damage = 25;

    [Header("远程攻击")]
    [SerializeField] private bool canBeTargetedByProjectile = false;

    /// <summary>小球远程攻击是否可以锁定此蜘蛛</summary>
    public bool CanBeTargetedByProjectile => canBeTargetedByProjectile;

    [Header("绳子")]
    [SerializeField] private LineRenderer ropeLine;

    private State currentState = State.Idle;
    private bool hasDealtDamage;
    private Transform playerTransform;
    private float startY;

    private void Awake()
    {
        GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
    }

    private void Start()
    {
        startY = transform.position.y;

        if (ropeLine != null)
        {
            ropeLine.positionCount = 2;
            ropeLine.SetPosition(0, transform.position);
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
        }

        if (currentState == State.Idle && playerTransform != null)
        {
            float horizontalDist = Mathf.Abs(transform.position.x - playerTransform.position.x);
            if (horizontalDist < triggerDistance)
            {
                currentState = State.Dropping;
                StartCoroutine(DropCoroutine());
            }
        }

        if (ropeLine != null)
        {
            ropeLine.SetPosition(1, transform.position);
        }
    }

    /// <summary>
    /// 下落协程：向下移动到 startY - dropDistance，然后进入回弹
    /// </summary>
    private IEnumerator DropCoroutine()
    {
        hasDealtDamage = false;
        float targetY = startY - dropDistance;
        while (transform.position.y > targetY)
        {
            float step = dropSpeed * Time.deltaTime;
            Vector3 pos = transform.position;
            pos.y = Mathf.Max(pos.y - step, targetY);
            transform.position = pos;

            if (transform.position.y <= targetY) break;
            yield return null;
        }

        currentState = State.Bouncing;
        StartCoroutine(BounceCoroutine());
    }

    /// <summary>
    /// 回弹协程：到达 targetY 后单次下沉 bounceAmount，0.3s 内弹性回升到 targetY
    /// y = targetY - bounceAmount × sin(π·t) × e^(-d·t)
    /// </summary>
    private IEnumerator BounceCoroutine()
    {
        float elapsed = 0f;
        float targetY = startY - dropDistance;

        while (elapsed < bounceDuration)
        {
            float t = elapsed / bounceDuration;
            float dip = bounceAmount * Mathf.Sin(t * Mathf.PI) * Mathf.Exp(-bounceDamping * t);
            float y = targetY - dip;

            transform.position = new Vector3(transform.position.x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = new Vector3(transform.position.x, targetY, 0f);
        currentState = State.Settled;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasDealtDamage) return;
        if (!other.CompareTag("Player")) return;

        hasDealtDamage = true;
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null) player.TakeDamage(damage);

        if (FeedbackManager.Instance != null)
            FeedbackManager.Instance.PlayHitEffect();
    }
}
