using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public enum WallCheckType { Raycast, BoxCast }

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Collider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 12f;             // Начальный импульс
    [SerializeField] private float jumpHoldForce = 5f;          // Дополнительная сила при удержании
    [SerializeField] private float jumpTimeMax = 0.35f;         // Максимальная длительность удержания
    [SerializeField] private float coyoteTimeMax = 0.15f;       // "Прощающая" зона
    [SerializeField] private float jumpBufferMax = 0.1f;        // Буфер прыжка

    private bool isJumping = false;
    private float jumpTimeCounter = 0f;
    private float coyoteTimeCounter = 0f;
    private float jumpBufferCounter = 0f;

    [Header("Ground Check")]
    [SerializeField] private Vector2 groundCheckSize = new Vector2(0.8f, 0.2f); // чуть больше
    [SerializeField] private Vector2 groundCheckOffset = new Vector2(0, -0.5f);
    [SerializeField] private LayerMask groundLayer;
    private bool isGrounded;

    [Header("Wall Check Settings")]
    [SerializeField] private WallCheckType wallCheckType = WallCheckType.Raycast;
    [SerializeField] private float _lastWallCheckDirection = 1;
    [SerializeField] private float wallCheckDistance = 0.5f;
    [SerializeField] private float wallCheckHeight = 1f;
    [SerializeField] private Vector2 wallCheckSize = new Vector2(0.1f, 1f);
    [SerializeField] private LayerMask wallLayer;

    [Header("Wall Jump")]
    [SerializeField] private float wallJumpInputLockTime = 0.15f;
    private bool _isWallJumping;
    private float _wallJumpTimer;

    [Header("Wall Slide")]
    [SerializeField] private float wallSlideSpeed = 2f;
    [SerializeField] private float wallJumpForceX = 8f;
    [SerializeField] private float wallJumpForceY = 10f;
    private bool isTouchingWall;
    private bool isWallSliding;
    private ContactPoint2D[] wallContacts = new ContactPoint2D[2];

    [Header("Sprite Settings")]
    [Tooltip("Ссылка на компонент SpriteRenderer персонажа")]
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private bool _isFacingRight = true;

    private bool _forceFlip = false;

    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private Rigidbody2D rb;
    private Vector2 moveInput;

    private System.Action<InputAction.CallbackContext> jumpStartedHandler;
    private System.Action<InputAction.CallbackContext> jumpCanceledHandler;

    // === Knockback Section ===
    [Header("Knockback Settings")]
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private float knockbackUpFactor = 0.7f;
    [SerializeField] private float knockbackDuration = 0.25f;
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    // =========================

    // --- Новые поля для управления внешними блокировками/автодвижением ---
    private bool inputEnabled = true;         // публичный API через SetInputEnabled управляет этим
    private Vector2 autoMoveDir = Vector2.zero; // если не zero — приоритет при движении
    // -------------------------------------------------------------------

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();

        if (playerInput != null)
        {
            moveAction = playerInput.actions.FindAction("Move");
            jumpAction = playerInput.actions.FindAction("Jump");

            if (jumpAction != null)
            {
                jumpStartedHandler = ctx => OnJumpStarted();
                jumpCanceledHandler = ctx => OnJumpCanceled();
                jumpAction.started += jumpStartedHandler;
                jumpAction.canceled += jumpCanceledHandler;
            }
            else
            {
                Debug.LogError("Jump action не назначена в Input Actions!");
            }
            if (moveAction == null)
            {
                Debug.LogError("Move action не назначена в Input Actions!");
            }
        }
        else
        {
            Debug.LogError("PlayerInput компонент отсутствует!");
        }

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();

        if (_spriteRenderer == null)
            Debug.LogWarning("SpriteRenderer не назначен!");

        _lastWallCheckDirection = 1f;
    }

    private void OnDestroy()
    {
        if (jumpAction != null)
        {
            if (jumpStartedHandler != null)
                jumpAction.started -= jumpStartedHandler;
            if (jumpCanceledHandler != null)
                jumpAction.canceled -= jumpCanceledHandler;
        }
    }

    private void Update()
    {
        if (isKnockedBack)
            return;

        // Читаем ввод только если включён inputEnabled. Если выключен — не читаем, но логика прыжков/таймеров остаётся.
        if (inputEnabled && moveAction != null)
            moveInput = moveAction.ReadValue<Vector2>();
        else
            moveInput = Vector2.zero;

        // --- Coyote Time: обновляем таймер прощения прыжка ---
        if (isGrounded)
            coyoteTimeCounter = coyoteTimeMax;
        else
            coyoteTimeCounter -= Time.deltaTime;

        // --- Jump Buffer: уменьшаем таймер ---
        if (jumpBufferCounter > 0f)
            jumpBufferCounter -= Time.deltaTime;

        // --- Таймер блокировки ввода после wall jump ---
        if (_isWallJumping)
        {
            _wallJumpTimer -= Time.deltaTime;
            if (_wallJumpTimer <= 0f)
            {
                _isWallJumping = false;
                _forceFlip = false;
            }
        }

        // --- Обновляем направление движения ---
        if (!_isWallJumping && moveInput.x != 0)
        {
            _lastWallCheckDirection = Mathf.Sign(moveInput.x);
            _isFacingRight = moveInput.x > 0;
        }

        // Если авто-движение задано (например, при переходе между сценами), учитываем направление для спрайта
        if (autoMoveDir != Vector2.zero)
        {
            _lastWallCheckDirection = Mathf.Sign(autoMoveDir.x != 0 ? autoMoveDir.x : _lastWallCheckDirection);
            _isFacingRight = (_lastWallCheckDirection > 0);
        }

        // --- Управление поворотом спрайта ---
        if (_spriteRenderer != null)
        {
            if (!_forceFlip)
                _spriteRenderer.flipX = !_isFacingRight;
            else
                _spriteRenderer.flipX = _lastWallCheckDirection > 0;
        }

        // --- Регулируемый прыжок ---
        if (isJumping && !isWallSliding)
        {
            if (jumpTimeCounter > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpHoldForce);
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        // --- Исполнение прыжка по jumpBuffer и coyoteTime ---
        if (jumpBufferCounter > 0f && coyoteTimeCounter > 0f && !isJumping && !isWallSliding && !_isWallJumping)
        {
            DoGroundJump();
            jumpBufferCounter = 0f;
        }
    }

    private void FixedUpdate()
    {
        // Knockback logic
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
                if (moveAction != null && inputEnabled)
                    moveAction.Enable();
            }
            return;
        }

        CheckGround();
        CheckWalls();
        HandleMovement();
        HandleWallSlide();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) != 0)
        {
            collision.GetContacts(wallContacts);
        }
    }
    private void CheckGround()
    {
        Vector2 checkPos = (Vector2)transform.position + groundCheckOffset;
        isGrounded = Physics2D.OverlapBox(checkPos, groundCheckSize, 0f, groundLayer) != null;
    }

    private void CheckWalls()
    {
        float direction = moveInput.x != 0 ? Mathf.Sign(moveInput.x) : 0;
        // Если авто-движение активно, используем его направление для определения столкновения со стеной
        if (autoMoveDir != Vector2.zero)
            direction = Mathf.Sign(autoMoveDir.x);

        if (direction == 0)
        {
            isTouchingWall = false;
            return;
        }

        Vector2 origin = transform.position;
        Vector2 size = wallCheckSize;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin,
            size,
            0f,
            Vector2.right * direction,
            wallCheckDistance,
            wallLayer
        );

        isTouchingWall = hit.collider != null;

        if (isTouchingWall)
        {
            _lastWallCheckDirection = direction;
        }
    }
    private void HandleMovement()
    {
        if (_isWallJumping) return;

        // Если авто-движение задано — оно имеет приоритет над вводом игрока
        float horizontalInput = 0f;
        if (autoMoveDir != Vector2.zero)
            horizontalInput = autoMoveDir.x;
        else
            horizontalInput = moveInput.x;

        if (isWallSliding && Mathf.Sign(horizontalInput) == _lastWallCheckDirection)
        {
            horizontalInput = 0;
        }

        rb.velocity = new Vector2(horizontalInput * moveSpeed, rb.velocity.y);
    }

    // --- Новый код для прыжка с jump buffer и coyote time ---
    private void OnJumpStarted()
    {
        if (isKnockedBack) return;
        jumpBufferCounter = jumpBufferMax;

        // Прыжок от стены (wall jump)
        if (isWallSliding)
        {
            rb.velocity = Vector2.zero;
            float jumpDirection = -_lastWallCheckDirection;
            _forceFlip = true;
            _isFacingRight = jumpDirection > 0;

            if (_spriteRenderer != null)
                _spriteRenderer.flipX = !_isFacingRight;

            rb.AddForce(new Vector2(jumpDirection * wallJumpForceX, wallJumpForceY), ForceMode2D.Impulse);

            isWallSliding = false;
            _isWallJumping = true;
            _wallJumpTimer = wallJumpInputLockTime;

            isJumping = false;
            jumpBufferCounter = 0f;
            coyoteTimeCounter = 0f;
        }
        // Прыжок по земле произойдёт в Update через jumpBuffer + coyoteTime
    }

    private void OnJumpCanceled()
    {
        isJumping = false;
    }

    private void DoGroundJump()
    {
        isJumping = true;
        jumpTimeCounter = jumpTimeMax;
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        coyoteTimeCounter = 0f; // чтобы нельзя было прыгнуть дважды
    }
    // --- Конец нового кода ---

    private void HandleWallSlide()
    {
        bool isPushingWall = Mathf.Sign(moveInput.x) == _lastWallCheckDirection;
        // Учесть autoMoveDir при скольжении
        if (autoMoveDir != Vector2.zero)
            isPushingWall = Mathf.Sign(autoMoveDir.x) == _lastWallCheckDirection;

        bool shouldSlide = isTouchingWall && !isGrounded && rb.velocity.y <= 0 && isPushingWall;

        if (shouldSlide)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -wallSlideSpeed));
        }
        else
        {
            isWallSliding = false;
        }
    }
    // === Knockback Method ===
    /// Применить отскок от урона (диагональ назад-вверх)
    public void ApplyKnockback(Vector2 hitDirection)
    {
        if (rb == null) return;

        Vector2 knockDir = new Vector2(hitDirection.x, knockbackUpFactor).normalized;
        rb.velocity = Vector2.zero;
        rb.AddForce(knockDir * knockbackForce, ForceMode2D.Impulse);
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;

        bool wasEnabled = inputEnabled;

        if (moveAction != null)
        {
            moveAction.Disable();
            // FIX: Проверяем, что MonoBehaviour для корутины
            if (this is MonoBehaviour mono)
                mono.StartCoroutine(ReenableMovementAfterKnockback(wasEnabled));
            else
                Debug.LogError("PlayerMovement must inherit MonoBehaviour for coroutines!");
        }
    }

    private IEnumerator ReenableMovementAfterKnockback(bool wasEnabled)
    {
        yield return new WaitForSeconds(knockbackDuration);
        if (moveAction != null && wasEnabled)
        {
            // Восстанавливаем состояние ввода (только если внешняя блокировка не установлена)
            if (inputEnabled)
                moveAction.Enable();
        }
    }

    private IEnumerator ReenableMovement(bool wasEnabled)
    {
        yield return new WaitForSeconds(knockbackDuration);
        if (moveAction != null && wasEnabled)
        {
            moveAction.Enable();
        }
    }

    /// Можно ли сейчас управлять движением игрока (например, для других скриптов)
    public bool CanMove()
    {
        return !isKnockedBack && inputEnabled;
    }
    /// Отдача от атаки (эффект Hollow Knight)
    public void ApplyAttackKnockback(Vector2 direction, float force)
    {
        rb.velocity = new Vector2(0, rb.velocity.y); // Сбросить горизонтальную скорость, сохранить вертикальную
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }
    /// Находится ли игрок на земле
    public bool IsGrounded()
    {
        return isGrounded;
    }

    // ---------------- API методы, которые зовёт внешняя логика (PlayerCross и т.п.) ----------------
    /// Включает/выключает чтение ввода в этом компоненте.
    /// false — внешний код запрещает игроку контролировать персонажа.
    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
        if (moveAction != null)
        {
            if (enabled)
                moveAction.Enable();
            else
                moveAction.Disable();
        }
    }

    /// Устанавливает автоматическое движение (приоритет перед вводом).
    /// Передать Vector2.zero чтобы отключить авто-движение.
    public void SetAutoMove(Vector2 dir)
    {
        autoMoveDir = dir;
    }
    // ---------------------------------------------------------------------------------------------

    #if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (_spriteRenderer == null)
            _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube((Vector2)transform.position + groundCheckOffset, groundCheckSize);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position + (Vector3.right * wallCheckDistance),
            new Vector3(wallCheckSize.x, wallCheckSize.y, 0));

        Gizmos.color = isTouchingWall ? Color.green : Color.red;
        float direction = moveInput != null ? moveInput.x : 1f;

        if (direction == 0) direction = 1f;

        Vector2 origin = transform.position;

        switch (wallCheckType)
        {
            case WallCheckType.Raycast:
                Vector2 rayEnd = origin + new Vector2(direction * wallCheckDistance, 0);
                Gizmos.DrawLine(origin, rayEnd);
                break;

            case WallCheckType.BoxCast:
                Gizmos.DrawWireCube(
                    origin + new Vector2(direction * wallCheckDistance * 0.5f, 0),
                    new Vector2(wallCheckDistance, wallCheckHeight)
                );
                break;
        }
    }
    #endif
}