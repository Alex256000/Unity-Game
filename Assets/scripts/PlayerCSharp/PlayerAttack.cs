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

    [SerializeField] private Vector2 attackOffsetRight =
        new Vector2(0.5f, 0f);

    [SerializeField] private Vector2 attackOffsetLeft =
        new Vector2(-0.5f, 0f);

    [Header("Attack Visual")]
    [SerializeField] private SpriteRenderer attackAreaSprite;
    [SerializeField] private float attackVisualDuration = 0.15f;

    [Header("Attack Cooldown")]
    [SerializeField] private float attackCooldown = 0.4f;

    [Header("Attack Knockback")]
    [SerializeField] private float knockbackOnGround = 2f;
    [SerializeField] private float knockbackInAir = 6f;

    [Header("Hit Effects")]
    [SerializeField] private GameObject hitParticlesPrefab;
    [SerializeField] private AudioClip hitSound;

    [Header("Soul Gain on Hit")]
    [SerializeField] private int soulPerHit = 1;

    [Range(0f, 1f)]
    [SerializeField] private float soulGainChance = 1f;

    private PlayerInput playerInput;
    private InputAction attackAction;
    private InputAction moveAction;

    private PlayerMovement playerMovement;
    private PlayerSoulSystem playerSoul;
    private PlayerHealth playerHealth;

    private bool facingRight = true;
    private float lastAttackTime = float.NegativeInfinity;
    private Coroutine attackVisualRoutine;

    // Один противник может иметь несколько коллайдеров.
    // За одну атаку обрабатываем каждый компонент противника только один раз.
    private readonly HashSet<Component> hitTargets =
        new HashSet<Component>();

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        playerMovement = GetComponent<PlayerMovement>();
        playerSoul = GetComponent<PlayerSoulSystem>();
        playerHealth = GetComponent<PlayerHealth>();

        if (playerInput.actions != null)
        {
            attackAction = playerInput.actions.FindAction("Attack");
            moveAction = playerInput.actions.FindAction("Move");
        }

        if (attackAction == null)
            Debug.LogError("[PlayerAttack] Не найдено действие Attack.", this);

        if (attackAreaSprite != null)
        {
            attackAreaSprite.enabled = false;

            if (attackPoint != null &&
                attackAreaSprite.transform != attackPoint)
            {
                attackAreaSprite.transform.SetParent(attackPoint, false);
                attackAreaSprite.transform.localPosition = Vector3.zero;
                attackAreaSprite.transform.localScale =
                    Vector3.one * attackRange * 2f;
            }
        }
    }

    private void OnEnable()
    {
        if (attackAction != null)
            attackAction.performed += HandleAttack;
    }

    private void OnDisable()
    {
        if (attackAction != null)
            attackAction.performed -= HandleAttack;

        if (attackVisualRoutine != null)
        {
            StopCoroutine(attackVisualRoutine);
            attackVisualRoutine = null;
        }

        if (attackAreaSprite != null)
            attackAreaSprite.enabled = false;
    }

    private void Update()
    {
        // Направление атаки согласовано с основным движением игрока.
        if (playerMovement != null)
        {
            facingRight = playerMovement.IsFacingRight;
        }
        else if (moveAction != null)
        {
            float horizontal = moveAction.ReadValue<Vector2>().x;

            if (Mathf.Abs(horizontal) > 0.01f)
                facingRight = horizontal > 0f;
        }

        UpdateAttackPointPosition();
    }

    private void UpdateAttackPointPosition()
    {
        if (attackPoint == null)
            return;

        attackPoint.localPosition =
            facingRight ? attackOffsetRight : attackOffsetLeft;
    }

    private void HandleAttack(InputAction.CallbackContext context)
    {
        if (!isActiveAndEnabled ||
            !playerInput.isActiveAndEnabled ||
            attackPoint == null)
        {
            return;
        }

        if (playerHealth != null && playerHealth.currentHealth <= 0)
            return;

        // Блокируем атаку во время лечения, перехода и отталкивания.
        if (playerMovement != null && !playerMovement.CanMove())
            return;

        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        if (playerMovement != null)
            facingRight = playerMovement.IsFacingRight;

        UpdateAttackPointPosition();
        ShowAttackVisual();

        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        hitTargets.Clear();

        bool hasHit = false;

        foreach (Collider2D targetCollider in colliders)
        {
            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();

            if (enemy != null)
            {
                if (enemy.health <= 0 || !hitTargets.Add(enemy))
                    continue;

                Vector2 direction =
                    (enemy.transform.position - transform.position).normalized;

                int healthBefore = enemy.health;

                enemy.TakeDamage(damage, direction);

                // Enemy может отклонить удар из-за своей неуязвимости.
                // Тогда не выдаём душу за фактически непринятый урон.
                if (enemy.health >= healthBefore)
                    continue;

                RegisterHit(enemy.transform.position);
                hasHit = true;

                continue;
            }

            FlyingEyeController flyingEye =
                targetCollider.GetComponentInParent<FlyingEyeController>();

            if (flyingEye != null)
            {
                if (!hitTargets.Add(flyingEye))
                    continue;

                if (damage <= 0)
                    continue;

                Vector3 hitPosition = flyingEye.transform.position;

                flyingEye.TakeDamage(damage);
                RegisterHit(hitPosition);

                hasHit = true;
            }
        }

        if (hasHit && playerMovement != null)
        {
            float force = playerMovement.IsGrounded()
                ? knockbackOnGround
                : knockbackInAir;

            Vector2 direction =
                facingRight ? Vector2.left : Vector2.right;

            playerMovement.ApplyAttackKnockback(direction, force);
        }

        // Сохраняем исходное поведение: звук взмаха слышен и при промахе.
        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position);
    }

    private void RegisterHit(Vector3 position)
    {
        if (hitParticlesPrefab != null)
        {
            GameObject particles = Instantiate(
                hitParticlesPrefab,
                position,
                Quaternion.identity
            );

            Destroy(particles, 1f);
        }

        if (playerSoul != null &&
            soulPerHit > 0 &&
            Random.value < Mathf.Clamp01(soulGainChance))
        {
            playerSoul.AddSoul(soulPerHit);
        }
    }

    private void ShowAttackVisual()
    {
        if (attackAreaSprite == null)
            return;

        if (attackVisualRoutine != null)
            StopCoroutine(attackVisualRoutine);

        attackVisualRoutine = StartCoroutine(ShowAttackVisualRoutine());
    }

    private IEnumerator ShowAttackVisualRoutine()
    {
        attackAreaSprite.enabled = true;

        yield return new WaitForSeconds(
            Mathf.Max(0f, attackVisualDuration)
        );

        if (attackAreaSprite != null)
            attackAreaSprite.enabled = false;

        attackVisualRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}
