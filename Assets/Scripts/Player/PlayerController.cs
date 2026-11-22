using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 玩家控制器：自动向右移动、二连跳、滑铲、攻击、受击、生命管理
/// 不直接调用 Input.GetKey，统一通过 InputManager 读取输入标志
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerController : MonoBehaviour
{
    [Header("移动")]
    [SerializeField] private float fallbackSpeed = 8f;

    [Header("跳跃")]
    [SerializeField] private float jumpForce = 12f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;
    [SerializeField] private int maxJumps = 2;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask ropeLayer;

    [Header("地面检测")]
    [SerializeField] private float groundCheckDistance = 0.1f;
    [SerializeField] private float groundCheckWidthRatio = 0.8f;

    [Header("滑铲")]
    [SerializeField] private float slideColliderHeight = 0.9f;
    [SerializeField] private float standColliderHeight = 1.8f;
    [SerializeField] private float slideCooldown = 0.1f;

    [Header("快速下落（空中按 S 加速落地）")]
    [SerializeField] private float fastFallAcceleration = 50f;
    [SerializeField] private float fastFallMaxSpeed = 20f;

    [Header("绳索")]
    [SerializeField] private float ropeFlipDelay = 0.5f;

    [Header("吸金币（长按 K）")]
    [SerializeField] private float coinMagnetRange = 5f;
    [SerializeField] private float coinMagnetPullSpeed = 10f;

    [Header("远程技能（长按 Space）")]
    [SerializeField] private GameObject skillProjectilePrefab;
    [SerializeField] private float skillFireInterval = 0.5f;
    [SerializeField] private float skillSearchRadius = 20f;
    [SerializeField] private int skillDamage = 30;
    [SerializeField] private float skillProjectileSpeed = 12f;
    [SerializeField] private float skillArcHeight = 2f;
    [SerializeField] private LayerMask skillTargetMask;

    [Header("攻击")]
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackHeight = 1.2f;
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float attackCooldown = 0.15f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("生命值")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth = 100;

    private Rigidbody2D rb;
    private BoxCollider2D col;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private Vector2 standColliderSize;
    private Vector2 standColliderOffset;

    private bool isGrounded;
    private float coyoteTimeCounter;
    private float jumpBufferCounter;
    private int currentJumps;
    private bool jumpTriggeredThisFrame;

    private bool isSliding;
    private float slideCooldownTimer;

    private bool isFastFalling;

    private bool isAttacking;
    private float attackCooldownTimer;
    private float skillFireTimer;
    private Coroutine attackCoroutine;

    private bool isDead;

    private float capsuleBuffTimer;

    private bool isRopeInverted;
    private Collider2D lastGroundHit;
    private Collider2D cachedRopeCollider;
    private Coroutine pendingFlipCoroutine;
    private List<Transform> magnetTargets = new List<Transform>();
    private HashSet<Collider2D> targetedObstacles = new HashSet<Collider2D>();

    /// <summary>当前是否站立在绳索上（每帧由 GroundCheck 更新）</summary>
    private bool IsStandingOnRope => isGrounded && lastGroundHit != null
        && lastGroundHit.gameObject.layer == LayerMask.NameToLayer("Rope");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        standColliderSize = col.size;
        standColliderOffset = col.offset;
        standColliderHeight = standColliderSize.y;
    }

    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// 初始化玩家：设置物理参数、生命值、跳跃计数
    /// </summary>
    private void Initialize()
    {
        rb.gravityScale = gravityScale;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        currentHealth = maxHealth;
        currentJumps = 0;
        isGrounded = false;
        coyoteTimeCounter = coyoteTime;
        jumpBufferCounter = 0f;
        isDead = false;
        isSliding = false;
        isAttacking = false;
        attackCooldownTimer = 0f;
        slideCooldownTimer = 0f;
        jumpTriggeredThisFrame = false;

        isRopeInverted = false;

        if (spriteRenderer != null)
            spriteRenderer.flipY = false;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (capsuleBuffTimer > 0f)
            capsuleBuffTimer -= Time.deltaTime;

        GroundCheck();
        HandleInput();
        UpdateTimers();
        UpdateAnimator();
    }

    /// <summary>
    /// 从 InputManager 读取输入标志，处理跳跃、滑铲、攻击和绳索翻转
    /// </summary>
    private void HandleInput()
    {
        if (InputManager.Instance == null) return;

        if (isRopeInverted)
        {
            if (InputManager.Instance.JumpKeyDown)
            {
                if (pendingFlipCoroutine == null)
                {
                    if (animator != null)
                        animator.SetTrigger("Flip");

                    pendingFlipCoroutine = StartCoroutine(DelayedFlipToTopside());
                }
            }
        }
        else if (IsStandingOnRope)
        {
            if (InputManager.Instance.SlideKeyDown)
            {
                if (pendingFlipCoroutine == null)
                {
                    if (animator != null)
                        animator.SetTrigger("Flip");

                    pendingFlipCoroutine = StartCoroutine(DelayedFlipToUnderside());
                }
            }

            if (InputManager.Instance.JumpKeyDown)
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (jumpBufferCounter > 0f && (isGrounded || coyoteTimeCounter > 0f || currentJumps < maxJumps))
            {
                TryJump();
            }
        }
        else
        {
            if (InputManager.Instance.JumpKeyDown)
            {
                jumpBufferCounter = jumpBufferTime;
            }

            if (jumpBufferCounter > 0f && (isGrounded || coyoteTimeCounter > 0f || currentJumps < maxJumps))
            {
                TryJump();
            }

            if (InputManager.Instance.SlideKeyHold && isGrounded && slideCooldownTimer <= 0f)
            {
                if (!isSliding)
                {
                    EnterSlide();
                }
            }
            else if (isSliding && !InputManager.Instance.SlideKeyHold)
            {
                ExitSlide();
            }

            if (!InputManager.Instance.SlideKeyHold)
            {
                if (slideCooldownTimer > 0f)
                {
                    slideCooldownTimer -= Time.deltaTime;
                }
            }
        }

        if (InputManager.Instance.AttackKeyDown && attackCooldownTimer <= 0f && !isAttacking)
        {
            TryAttack();
        }

        if (skillFireTimer <= 0f && (InputManager.Instance.SkillKeyHold || capsuleBuffTimer > 0f))
        {
            TryShootProjectile();
        }
    }

    /// <summary>
    /// 更新地面检测计时器
    /// </summary>
    private void UpdateTimers()
    {
        if (jumpBufferCounter > 0f)
        {
            jumpBufferCounter -= Time.deltaTime;
        }

        if (!isGrounded && coyoteTimeCounter > 0f)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }

        if (skillFireTimer > 0f)
        {
            skillFireTimer -= Time.deltaTime;
        }

        jumpTriggeredThisFrame = false;
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        ApplyCoinMagnet();

        if (isRopeInverted)
        {
            LockToRopeUnderside();
            ApplyHorizontalMovement();
            return;
        }

        ApplyHorizontalMovement();
        if (!IsStandingOnRope) ApplyFastFall();
    }

    /// <summary>
    /// 地面检测：使用 OverlapBox 检测玩家底部是否接触地面层或绳索层
    /// </summary>
    private void GroundCheck()
    {
        float bottom = col.bounds.center.y - col.bounds.extents.y;
        float checkWidth = col.bounds.size.x * groundCheckWidthRatio;

        Vector2 checkCenter = new Vector2(col.bounds.center.x, bottom - groundCheckDistance * 0.5f);
        Vector2 checkSize = new Vector2(checkWidth, groundCheckDistance);

        LayerMask combinedMask = groundLayer | ropeLayer;
        Collider2D hit = Physics2D.OverlapBox(checkCenter, checkSize, 0f, combinedMask);
        lastGroundHit = hit;
        bool wasGrounded = isGrounded;
        isGrounded = hit != null;

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            if (!wasGrounded)
            {
                currentJumps = 0;
            }
        }
    }

    /// <summary>
    /// 延迟翻转协程：先播翻转动画，延迟 ropeFlipDelay 秒后真正翻转到绳索下方。
    /// 延迟期间跳走或离开绳索则自动取消
    /// </summary>
    private IEnumerator DelayedFlipToUnderside()
    {
        yield return new WaitForSeconds(ropeFlipDelay);

        pendingFlipCoroutine = null;

        if (IsStandingOnRope && !isRopeInverted && !isDead)
        {
            FlipToUnderside();
        }
    }

    /// <summary>
    /// 延迟翻回协程：先播翻转动画，延迟 ropeFlipDelay 秒后真正翻回绳索上方。
    /// 延迟期间脱离倒挂或死亡则自动取消
    /// </summary>
    private IEnumerator DelayedFlipToTopside()
    {
        yield return new WaitForSeconds(ropeFlipDelay);

        pendingFlipCoroutine = null;

        if (isRopeInverted && !isDead)
        {
            FlipToTopside();
        }
    }

    /// <summary>
    /// 翻转到绳索下方（倒挂）：零重力，锁定 Y 位置到绳索底面
    /// </summary>
    private void FlipToUnderside()
    {
        Collider2D rope = lastGroundHit;
        if (rope == null || rope.gameObject.layer != LayerMask.NameToLayer("Rope"))
        {
            Debug.LogWarning("[PlayerController] 脚下未检测到绳索，无法翻转");
            return;
        }

        cachedRopeCollider = rope;
        isRopeInverted = true;
        rb.gravityScale = 0f;
        SetFastFalling(false);

        if (spriteRenderer != null)
            spriteRenderer.flipY = true;

        LockToRopeUnderside();

        rb.velocity = Vector2.zero;

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlaySlideEffect();
        }
    }

    /// <summary>
    /// 从绳索下方翻回正立：恢复重力，放置到绳索顶面
    /// </summary>
    private void FlipToTopside()
    {
        Collider2D rope = cachedRopeCollider;
        if (rope == null || rope.gameObject.layer != LayerMask.NameToLayer("Rope"))
        {
            Debug.LogWarning("[PlayerController] 脚下未检测到绳索，无法翻回");
            return;
        }

        isRopeInverted = false;
        rb.gravityScale = gravityScale;

        float ropeTop = rope.bounds.center.y + rope.bounds.extents.y;
        float playerBottom = col.bounds.min.y;

        Vector3 pos = transform.position;
        pos.y += ropeTop - playerBottom;
        transform.position = pos;

        currentJumps = 0;
        isGrounded = true;
        coyoteTimeCounter = coyoteTime;
        SetFastFalling(false);

        if (spriteRenderer != null)
            spriteRenderer.flipY = false;

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayJumpEffect();
        }
    }

    /// <summary>
    /// 倒挂状态每帧锁定 Y 位置到绳索底面（用世界 bounds 增量对齐，兼容任意 scale/offset），清空 Y 速度
    /// </summary>
    private void LockToRopeUnderside()
    {
        Collider2D rope = cachedRopeCollider;
        if (rope == null || rope.gameObject.layer != LayerMask.NameToLayer("Rope")) return;

        float ropeBottom = rope.bounds.center.y - rope.bounds.extents.y;
        float playerTop = col.bounds.max.y;

        Vector3 pos = transform.position;
        pos.y += ropeBottom - playerTop;
        transform.position = pos;

        rb.velocity = new Vector2(rb.velocity.x, 0f);
    }

    /// <summary>
    /// 在 FixedUpdate 中设置水平速度实现自动向右移动
    /// </summary>
    private void ApplyHorizontalMovement()
    {
        float speed = fallbackSpeed;
        if (LevelSpeedController.Instance != null)
        {
            speed = LevelSpeedController.Instance.CurrentSpeed;
        }

        rb.velocity = new Vector2(speed, rb.velocity.y);
    }

    /// <summary>
    /// 每帧同步 Animator 布尔参数（IsGrounded）
    /// </summary>
    private void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetBool("IsGrounded", isGrounded);
    }

    /// <summary>
    /// 更新快速下落标记并同步 Animator（IsFastFalling）
    /// </summary>
    private void SetFastFalling(bool value)
    {
        if (isFastFalling == value) return;
        isFastFalling = value;
        if (animator != null) animator.SetBool("IsFastFalling", isFastFalling);
    }

    /// <summary>
    /// 空中按住 S 时加速下落（快速跌落）。只在空中生效，与地面滑铲互斥
    /// </summary>
    private void ApplyFastFall()
    {
        if (isGrounded)
        {
            SetFastFalling(false);
            return;
        }
        if (InputManager.Instance == null) return;

        SetFastFalling(InputManager.Instance.SlideKeyHold);

        if (InputManager.Instance.SlideKeyHold)
        {
            float newY = rb.velocity.y - fastFallAcceleration * Time.fixedDeltaTime;
            newY = Mathf.Max(newY, -fastFallMaxSpeed);
            rb.velocity = new Vector2(rb.velocity.x, newY);
        }
    }

    /// <summary>
    /// 尝试跳跃：检查地面/土狼时间/剩余跳跃次数
    /// </summary>
    private void TryJump()
    {
        if (jumpTriggeredThisFrame) return;

        if (!isGrounded && coyoteTimeCounter <= 0f && currentJumps >= maxJumps) return;

        PerformJump();
    }

    /// <summary>
    /// 执行跳跃：直接覆盖 Y 轴速度，消耗跳跃次数和缓冲
    /// </summary>
    private void PerformJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        currentJumps++;
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        jumpTriggeredThisFrame = true;

        if (animator != null)
        {
            if (currentJumps >= 2)
                animator.SetTrigger("DoubleJump");
            else
                animator.SetTrigger("Jump");
        }

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayJumpEffect();
        }
    }

    /// <summary>
    /// 尝试攻击：启动攻击协程进行 OverlapBox 检测
    /// </summary>
    private void TryAttack()
    {
        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
        }
        attackCoroutine = StartCoroutine(AttackCoroutine());
    }

    /// <summary>
    /// 攻击协程：在玩家右侧使用 Physics2D.OverlapBox 检测 Enemy 层，命中即销毁目标
    /// </summary>
    private IEnumerator AttackCoroutine()
    {
        isAttacking = true;
        attackCooldownTimer = attackCooldown;

        Vector2 attackCenter = GetAttackBoxCenter();
        Vector2 attackSize = new Vector2(attackRange, attackHeight);

        Collider2D[] hits = Physics2D.OverlapBoxAll(attackCenter, attackSize, 0f, enemyLayer);

        foreach (Collider2D hit in hits)
        {
            if (hit != null)
            {
                Destroy(hit.gameObject);

                if (FeedbackManager.Instance != null)
                {
                    FeedbackManager.Instance.PlayAttackHit();
                }
            }
        }

        yield return new WaitForSeconds(attackDuration);
        isAttacking = false;
        attackCoroutine = null;
    }

    private void TryShootProjectile()
    {
        if (skillProjectilePrefab == null)
        {
            Debug.LogWarning("[PlayerController] 未设置技能弹体预制体");
            return;
        }

        Transform nearest = FindNearestTarget();

        if (nearest == null && capsuleBuffTimer <= 0f) return;

        skillFireTimer = skillFireInterval;

        if (nearest != null)
        {
            Collider2D targetCol = nearest.GetComponent<Collider2D>();
            if (targetCol != null && targetCol.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                targetedObstacles.Add(targetCol);
            }
        }

        float arc = Random.value < 0.5f ? skillArcHeight : -skillArcHeight;
        GameObject projectile = Instantiate(skillProjectilePrefab, transform.position, Quaternion.identity);
        HommingProjectile homing = projectile.GetComponent<HommingProjectile>();
        if (homing != null)
        {
            if (nearest != null)
            {
                homing.SetTarget(nearest, skillProjectileSpeed, arc, skillDamage);
            }
            else
            {
                homing.FireStraight(skillProjectileSpeed, arc, skillDamage);
            }
        }
    }

    /// <summary>
    /// 搜索范围内最近的目标（仅前方，障碍物去重，敌人可重复锁定）
    /// </summary>
    private Transform FindNearestTarget()
    {
        targetedObstacles.RemoveWhere(c => c == null);

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, skillSearchRadius, skillTargetMask);
        if (hits.Length == 0) return null;

        Transform nearest = null;
        float minDist = float.MaxValue;
        Vector3 pos = transform.position;

        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (hit.transform.position.x < pos.x) continue;

            if (hit.gameObject.layer == LayerMask.NameToLayer("Obstacle")
                && targetedObstacles.Contains(hit))
                continue;

            Obstacle obs = hit.GetComponent<Obstacle>();
            if (obs != null && !obs.CanBeTargetedByProjectile) continue;

            BatController bat = hit.GetComponent<BatController>();
            if (bat != null && !bat.CanBeTargetedByProjectile) continue;

            FlyingSword sword = hit.GetComponent<FlyingSword>();
            if (sword != null && !sword.CanBeTargetedByProjectile) continue;

            SpiderTrap spider = hit.GetComponent<SpiderTrap>();
            if (spider != null && !spider.CanBeTargetedByProjectile) continue;

            float dist = Vector3.Distance(pos, hit.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = hit.transform;
            }
        }

        return nearest;
    }

    /// <summary>
    /// 计算攻击判定框的中心点（玩家右侧）
    /// 中心点 = 玩家碰撞盒右边缘 + attackRange / 2
    /// </summary>
    private Vector2 GetAttackBoxCenter()
    {
        float rightEdge = col.bounds.center.x + col.bounds.extents.x;
        return new Vector2(rightEdge + attackRange * 0.5f, col.bounds.center.y);
    }

    /// <summary>
    /// 吸金币：按住 K 将范围内金币加入捕获列表，已捕获金币持续飞向玩家直到拾取
    /// </summary>
    private void ApplyCoinMagnet()
    {
        Vector3 playerPos = transform.position;
        float pull = coinMagnetPullSpeed * Time.fixedDeltaTime;

        for (int i = magnetTargets.Count - 1; i >= 0; i--)
        {
            Transform coin = magnetTargets[i];
            if (coin == null)
            {
                magnetTargets.RemoveAt(i);
                continue;
            }
            Vector3 dir = (playerPos - coin.position).normalized;
            coin.position += dir * pull;
        }

        if (InputManager.Instance == null) return;
        if (!InputManager.Instance.CoinMagnetKeyHold) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(playerPos, coinMagnetRange, LayerMask.GetMask("Coin"));
        foreach (Collider2D hit in hits)
        {
            if (hit == null) continue;
            if (!magnetTargets.Contains(hit.transform))
                magnetTargets.Add(hit.transform);
        }
    }

    /// <summary>
    /// 进入滑铲：缩小碰撞盒高度，底部对齐保持不变
    /// </summary>
    private void EnterSlide()
    {
        isSliding = true;

        float oldHeight = col.size.y;
        float newHeight = slideColliderHeight;

        col.size = new Vector2(col.size.x, newHeight);

        float bottomOffset = standColliderOffset.y - oldHeight * 0.5f;
        col.offset = new Vector2(col.offset.x, bottomOffset + newHeight * 0.5f);

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlaySlideEffect();
        }
    }

    /// <summary>
    /// 退出滑铲：直接恢复站立碰撞盒大小，不做头顶碰撞检测（MVP）
    /// </summary>
    private void ExitSlide()
    {
        isSliding = false;
        slideCooldownTimer = slideCooldown;

        col.size = standColliderSize;
        col.offset = standColliderOffset;
    }

    /// <summary>
    /// 受到伤害：扣除生命值，无无敌帧，生命归零则死亡
    /// </summary>
    /// <param name="damage">伤害值</param>
    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        }

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayHitEffect();
        }

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Die();
        }
    }

    /// <summary>
    /// 玩家死亡：禁用控制，通知 GameManager 和 FeedbackManager
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        rb.velocity = Vector2.zero;
        rb.simulated = false;

        if (FeedbackManager.Instance != null)
        {
            FeedbackManager.Instance.PlayDeathEffect();
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.GameOver();
        }
    }

    /// <summary>获取当前生命值</summary>
    public int CurrentHealth => currentHealth;

    /// <summary>获取最大生命值</summary>
    public int MaxHealth => maxHealth;

    public void ActivateCapsuleBuff(float duration)
    {
        capsuleBuffTimer = duration;
    }

    /// <summary>
    /// 在编辑器中绘制攻击判定框（绿色线框）
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        BoxCollider2D collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;

        Gizmos.color = Color.green;

        float rightEdge = collider.bounds.center.x + collider.bounds.extents.x;
        Vector2 attackCenter = new Vector2(rightEdge + attackRange * 0.5f, collider.bounds.center.y);
        Vector2 attackSize = new Vector2(attackRange, attackHeight);

        Gizmos.DrawWireCube(attackCenter, attackSize);
    }
}
