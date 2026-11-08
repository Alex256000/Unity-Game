using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum WallCheckType
{
    Raycast,
    BoxCast
}

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;

    [Tooltip("Дополнительная сила вверх при удержании кнопки прыжка.")]
    [SerializeField] private float jumpHoldForce = 5f;

    [SerializeField] private float jumpTimeMax = 0.35f;

    [Tooltip("Время, в течение которого можно прыгнуть после схода с края.")]
    [SerializeField] private float coyoteTimeMax = 0.15f;

    [Tooltip("Время хранения нажатия прыжка перед приземлением.")]
    [SerializeField] private float jumpBufferMax = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize =
        new Vector2(0.8f, 0.2f);

    [SerializeField] private Vector2 groundCheckOffset =
        new Vector2(0f, -0.5f);

    [SerializeField] private LayerMask groundLayer;

    [Header("Wall Check Settings")]
    [SerializeField] private WallCheckType wallCheckType =
        WallCheckType.Raycast;

    [SerializeField] private float _lastWallCheckDirection = 1f;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallCheckHeight = 1f;
    [SerializeField] private Vector2 wallCheckSize =
        new Vector2(0.1f, 1f);

    [SerializeField] private LayerMask wallLayer;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpInputLockTime = 0.15f;

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpForceX = 8f;
    [SerializeField] private float wallJumpForceY = 10f;

    [Header("Sprite Settings")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackUpFactor = 0.7f;
    [SerializeField] private float knockbackDuration = 0.25f;

    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private PlayerHealth playerHealth;
    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector2 moveInput;
    private Vector2 autoMoveDir;

    private bool inputEnabled = true;
    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isJumping;
    private bool jumpHeld;
    private bool groundJumpConsumed;
    private bool _isFacingRight = true;

    private float jumpTimeCounter;
    private float coyoteTimeCounter;
    private float jumpBufferedUntil = -1f;
    private float wallJumpTimer;
    private float knockbackTimer;

    // Каждая система снимает только собственную временную блокировку.
    // Например, завершение лечения не отменяет блокировку перехода сцены.
    private readonly HashSet<object> inputLocks = new HashSet<object>();

    public bool InputEnabled =>
        isActiveAndEnabled &&
        inputEnabled &&
        inputLocks.Count == 0 &&
        playerInput != null &&
        playerInput.isActiveAndEnabled &&
        (playerHealth == null || playerHealth.currentHealth > 0);

    public bool IsFacingRight => _isFacingRight;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        playerHealth = GetComponent<PlayerHealth>();

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (playerInput.actions != null)
        {
            moveAction = playerInput.actions.FindAction("Move");
            jumpAction = playerInput.actions.FindAction("Jump");
        }

        if (moveAction == null)
            Debug.LogError("[PlayerMovement] Не найдено действие Move.", this);

        if (jumpAction == null)
            Debug.LogError("[PlayerMovement] Не найдено действие Jump.", this);

        _lastWallCheckDirection =
            _lastWallCheckDirection < 0f ? -1f : 1f;

        _isFacingRight = _lastWallCheckDirection > 0f;
    }

    private void OnEnable()
    {
        if (jumpAction != null)
        {
            jumpAction.started += OnJumpStarted;
            jumpAction.canceled += OnJumpCanceled;
        }
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.started -= OnJumpStarted;
            jumpAction.canceled -= OnJumpCanceled;
        }

        ClearJumpInput();
        moveInput = Vector2.zero;
    }

    private void Update()
    {
        moveInput = CanMove() && moveAction != null
            ? moveAction.ReadValue<Vector2>()
            : Vector2.zero;

        // Во время отталкивания от стены сохраняем направление прыжка.
        if (wallJumpTimer > 0f || knockbackTimer > 0f)
            return;

        float horizontal = GetHorizontalInput();

        if (Mathf.Abs(horizontal) > 0.01f)
        {
            _isFacingRight = horizontal > 0f;
            _lastWallCheckDirection = _isFacingRight ? 1f : -1f;
        }

        UpdateSpriteDirection();
    }

    private void FixedUpdate()
    {
        CheckGround();
        CheckWalls();

        // Не вмешиваемся в полностью замороженную физику телепорта.
        if (!rb.simulated ||
            (rb.constraints & RigidbodyConstraints2D.FreezeAll) ==
            RigidbodyConstraints2D.FreezeAll)
        {
            ClearJumpInput();
            return;
        }

        if (playerHealth != null && playerHealth.currentHealth <= 0)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            ClearJumpInput();
            return;
        }

        if (knockbackTimer > 0f)
        {
            knockbackTimer =
                Mathf.Max(0f, knockbackTimer - Time.fixedDeltaTime);

            return;
        }

        wallJumpTimer =
            Mathf.Max(0f, wallJumpTimer - Time.fixedDeltaTime);

        UpdateCoyoteTime();
        HandleMovement();
        HandleWallSlide();
        HandleJump();
    }

    private void CheckGround()
    {
        Vector2 position = (Vector2)transform.position + groundCheckOffset;

        isGrounded = Physics2D.OverlapBox(
            position,
            groundCheckSize,
            0f,
            groundLayer
        ) != null;

        // После реального отрыва от земли разрешаем следующий прыжок
        // при новом приземлении.
        if (!isGrounded)
            groundJumpConsumed = false;
    }

    private void CheckWalls()
    {
        float horizontal = GetHorizontalInput();

        if (Mathf.Abs(horizontal) < 0.01f)
        {
            isTouchingWall = false;
            return;
        }

        float direction = Mathf.Sign(horizontal);
        Vector2 origin = transform.position;
        RaycastHit2D hit;

        // Настройка типа проверки теперь влияет на реальную проверку,
        // а не только на рисунок Gizmos.
        if (wallCheckType == WallCheckType.Raycast)
        {
            hit = Physics2D.Raycast(
                origin,
                Vector2.right * direction,
                wallCheckDistance,
                wallLayer
            );
        }
        else
        {
            hit = Physics2D.BoxCast(
                origin,
                new Vector2(wallCheckSize.x, wallCheckHeight),
                0f,
                Vector2.right * direction,
                wallCheckDistance,
                wallLayer
            );
        }

        isTouchingWall = hit.collider != null;

        if (isTouchingWall)
            _lastWallCheckDirection = direction;
    }

    private void UpdateCoyoteTime()
    {
        if (isGrounded && !groundJumpConsumed)
        {
            coyoteTimeCounter = coyoteTimeMax;
        }
        else
        {
            coyoteTimeCounter =
                Mathf.Max(0f, coyoteTimeCounter - Time.fixedDeltaTime);
        }
    }

    private float GetHorizontalInput()
    {
        // Временные блокировки, например лечение, останавливают
        // и обычное движение, и автодвижение.
        if (!isActiveAndEnabled || inputLocks.Count > 0)
            return 0f;

        if (playerHealth != null && playerHealth.currentHealth <= 0)
            return 0f;

        // Автодвижение разрешено даже при SetInputEnabled(false):
        // это нужно существующему менеджеру переходов.
        if (autoMoveDir != Vector2.zero)
            return Mathf.Clamp(autoMoveDir.x, -1f, 1f);

        return InputEnabled ? moveInput.x : 0f;
    }

    private void HandleMovement()
    {
        if (wallJumpTimer > 0f)
            return;

        rb.velocity = new Vector2(
            GetHorizontalInput() * moveSpeed,
            rb.velocity.y
        );
    }

    private void HandleWallSlide()
    {
        float horizontal = GetHorizontalInput();

        bool isPushingWall =
            Mathf.Abs(horizontal) > 0.01f &&
            Mathf.Sign(horizontal) == _lastWallCheckDirection;

        isWallSliding =
            isTouchingWall &&
            !isGrounded &&
            rb.velocity.y <= 0f &&
            isPushingWall &&
            wallJumpTimer <= 0f;

        if (isWallSliding)
        {
            rb.velocity = new Vector2(
                rb.velocity.x,
                Mathf.Max(rb.velocity.y, -wallSlideSpeed)
            );
        }
    }

    private void OnJumpStarted(InputAction.CallbackContext context)
    {
        if (!CanMove())
            return;

        jumpHeld = true;
        jumpBufferedUntil = Time.time + Mathf.Max(0f, jumpBufferMax);
    }

    private void OnJumpCanceled(InputAction.CallbackContext context)
    {
        jumpHeld = false;
        isJumping = false;
    }

    private void HandleJump()
    {
        if (!InputEnabled)
        {
            ClearJumpInput();
            return;
        }

        bool hasBufferedJump = jumpBufferedUntil >= Time.time;

        if (hasBufferedJump && wallJumpTimer <= 0f)
        {
            if (isWallSliding)
                DoWallJump();
            else if (coyoteTimeCounter > 0f)
                DoGroundJump();
        }

        // Удержание продлевает подъём, но не задаёт скорость напрямую.
        if (isJumping && jumpHeld && rb.velocity.y > 0f)
        {
            if (jumpTimeCounter > 0f)
            {
                rb.AddForce(
                    Vector2.up * jumpHoldForce,
                    ForceMode2D.Force
                );

                jumpTimeCounter -= Time.fixedDeltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
        else
        {
            isJumping = false;
        }
    }

    private void DoGroundJump()
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        isJumping = true;
        jumpTimeCounter = jumpTimeMax;
        coyoteTimeCounter = 0f;
        jumpBufferedUntil = -1f;
        groundJumpConsumed = true;
    }

    private void DoWallJump()
    {
        float direction = -_lastWallCheckDirection;

        rb.velocity = Vector2.zero;
        rb.AddForce(
            new Vector2(
                direction * wallJumpForceX,
                wallJumpForceY
            ),
            ForceMode2D.Impulse
        );

        _isFacingRight = direction > 0f;
        UpdateSpriteDirection();

        wallJumpTimer = Mathf.Max(0f, wallJumpInputLockTime);
        isWallSliding = false;
        isJumping = false;
        coyoteTimeCounter = 0f;
        jumpBufferedUntil = -1f;
    }

    private void UpdateSpriteDirection()
    {
        if (_spriteRenderer != null)
            _spriteRenderer.flipX = !_isFacingRight;
    }

    private void ClearJumpInput()
    {
        jumpHeld = false;
        isJumping = false;
        jumpBufferedUntil = -1f;
        jumpTimeCounter = 0f;
    }

    /// <summary>
    /// Основной переключатель ввода.
    /// Не включает и не выключает общие InputAction:
    /// их состоянием управляет PlayerInput.
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;

        if (!enabled)
        {
            moveInput = Vector2.zero;
            ClearJumpInput();
        }
    }

    /// <summary>
    /// Установить или снять блокировку от конкретной системы.
    /// Снимать нужно с тем же owner, с которым она была установлена.
    /// </summary>
    public void SetInputLock(object owner, bool locked)
    {
        if (owner == null)
            return;

        if (locked)
        {
            inputLocks.Add(owner);
            moveInput = Vector2.zero;
            ClearJumpInput();
        }
        else
        {
            inputLocks.Remove(owner);
        }
    }

    public void SetAutoMove(Vector2 direction)
    {
        autoMoveDir = direction;
    }

    public bool CanMove()
    {
        return InputEnabled && knockbackTimer <= 0f;
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    /// <summary>
    /// Отталкивание от полученного урона.
    /// Управление временно игнорируется без отключения InputAction.
    /// </summary>
    public void ApplyKnockback(Vector2 hitDirection)
    {
        if (rb == null)
            return;

        ClearJumpInput();
        wallJumpTimer = 0f;
        isWallSliding = false;

        Vector2 direction = new Vector2(
            hitDirection.x,
            knockbackUpFactor
        ).normalized;

        rb.velocity = Vector2.zero;
        rb.AddForce(
            direction * knockbackForce,
            ForceMode2D.Impulse
        );

        knockbackTimer = Mathf.Max(0f, knockbackDuration);
    }

    /// <summary>
    /// Краткая отдача от собственной атаки.
    /// Небольшая блокировка не даёт обычному движению сразу стереть импульс.
    /// </summary>
    public void ApplyAttackKnockback(Vector2 direction, float force)
    {
        if (rb == null || force <= 0f)
            return;

        rb.velocity = new Vector2(0f, rb.velocity.y);
        rb.AddForce(
            direction.normalized * force,
            ForceMode2D.Impulse
        );

        knockbackTimer = Mathf.Max(
            knockbackTimer,
            Time.fixedDeltaTime * 2f
        );
    }

    /// <summary>
    /// Сбросить физическое состояние перед загрузкой или телепортом.
    /// Внешние блокировки ввода здесь намеренно не снимаются.
    /// </summary>
    public void ResetMovementState()
    {
        ClearJumpInput();

        moveInput = Vector2.zero;
        autoMoveDir = Vector2.zero;
        coyoteTimeCounter = 0f;
        wallJumpTimer = 0f;
        knockbackTimer = 0f;

        isWallSliding = false;
        isTouchingWall = false;
        groundJumpConsumed = false;

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;

        Gizmos.DrawWireCube(
            (Vector2)transform.position + groundCheckOffset,
            groundCheckSize
        );

        float direction = _lastWallCheckDirection < 0f ? -1f : 1f;
        Vector3 origin = transform.position;

        Gizmos.color = isTouchingWall ? Color.green : Color.blue;

        if (wallCheckType == WallCheckType.Raycast)
        {
            Gizmos.DrawLine(
                origin,
                origin + Vector3.right * direction * wallCheckDistance
            );
        }
        else
        {
            Vector3 startSize =
                new Vector3(wallCheckSize.x, wallCheckHeight, 0f);

            Vector3 end =
                origin + Vector3.right * direction * wallCheckDistance;

            Gizmos.DrawWireCube(origin, startSize);
            Gizmos.DrawWireCube(end, startSize);
            Gizmos.DrawLine(origin, end);
        }
    }
#endif
}
