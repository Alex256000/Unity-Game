using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 0.8f;
    [SerializeField] private int damage = 1;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private Vector2 attackOffsetRight = new Vector2(0.5f, 0f);
    [SerializeField] private Vector2 attackOffsetLeft = new Vector2(-0.5f, 0f);

    [Header("Attack Visual")]
    [SerializeField] private SpriteRenderer attackAreaSprite;
    [SerializeField] private float attackVisualDuration = 0.15f;

    [Header("Attack Cooldown")]
    [SerializeField] private float attackCooldown = 0.4f;
    private float lastAttackTime = -999f;

    [Header("Attack Knockback")]
    [SerializeField] private float knockbackOnGround = 2f;
    [SerializeField] private float knockbackInAir = 6f;

    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction moveAction;
    private bool facingRight = true;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        attackAction = playerInput.actions.FindAction("Attack");
        moveAction = playerInput.actions.FindAction("Move");
        playerMovement = GetComponent<PlayerMovement>();

        if (attackAction != null)
            attackAction.performed += HandleAttack;

        if (attackAreaSprite)
        {
            attackAreaSprite.enabled = false;
            attackAreaSprite.transform.SetParent(attackPoint, false);
            attackAreaSprite.transform.localPosition = Vector3.zero;
            attackAreaSprite.transform.localScale = Vector3.one * attackRange * 2f;
        }
    }

    private void OnDestroy()
    {
        if (attackAction != null)
            attackAction.performed -= HandleAttack;
    }

    private void Update()
    {
        if (moveAction != null)
        {
            float moveInput = moveAction.ReadValue<Vector2>().x;
            if (moveInput != 0)
            {
                facingRight = moveInput > 0;
                UpdateAttackPointPosition();
            }
        }
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null) return;
        attackPoint.localPosition = facingRight ? attackOffsetRight : attackOffsetLeft;
    }

    private void HandleAttack(InputAction.CallbackContext ctx)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (attackPoint == null) return;

        if (attackAreaSprite != null)
            StartCoroutine(ShowAttackVisual());

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayer);
        bool hit = false;

        foreach (Collider2D enemy in enemies)
        {
            if (enemy.TryGetComponent<Enemy>(out var enemyScript))
            {
                // Вычисляем направление отталкивания от позиции игрока к позиции врага
                Vector2 knockbackDirection = (enemy.transform.position - transform.position).normalized;
                enemyScript.TakeDamage(damage, knockbackDirection);
                hit = true;
            }
        }

        // Отталкивание игрока при попадании по врагу
        if (hit && playerMovement != null)
        {
            bool isGrounded = playerMovement.IsGrounded();
            float knockbackForce = isGrounded ? knockbackOnGround : knockbackInAir;
            Vector2 knockbackDir = facingRight ? Vector2.left : Vector2.right;
            playerMovement.ApplyAttackKnockback(knockbackDir, knockbackForce);
        }
    }

    private IEnumerator ShowAttackVisual()
    {
        if (attackAreaSprite == null) yield break; // FIX: Защита от null
        attackAreaSprite.enabled = true;
        yield return new WaitForSeconds(attackVisualDuration);
        attackAreaSprite.enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}