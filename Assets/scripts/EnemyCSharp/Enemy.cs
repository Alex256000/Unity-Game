using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class Enemy : MonoBehaviour
{
    [Header("Health Settings")]
    public int health = 3;

    [Header("Patrol Settings")]
    public Transform patrolPointA;
    public Transform patrolPointB;
    public float speed = 2f;
    public float pauseTimeAtPoints = 2f;
    public bool useVerticalMovement = false;

    private Rigidbody2D rb;
    private Vector3 pointA;
    private Vector3 pointB;
    private bool movingToB = true;
    private float pauseTimer = 0f;
    private bool isPaused = false;

    [Header("Soul Orb Obtain")]
    [SerializeField] private GameObject soulOrbPrefab;

    [Header("Knockback Settings")]
    private bool isKnockedBack = false;
    private float knockbackTimer = 0f;
    private float knockbackDuration = 0.15f;

    [Header("Attack Settings")]
    public int damageToPlayer = 1;
    public float attackCooldown = 1f;
    public float playerKnockbackForce = 8f;
    private float lastAttackTime = -999f;

    // FIX: Неуязвимость после удара
    private bool isInvulnerable = false;
    private float invulTimer = 0f;
    private float invulDuration = 0.15f;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (patrolPointA == null || patrolPointB == null)
        {
            Debug.LogError("Patrol points not set on Enemy: " + gameObject.name);
            enabled = false;
            return;
        }
        pointA = patrolPointA.position;
        pointB = patrolPointB.position;
    }

    private void FixedUpdate()
    {
        if (isKnockedBack)
        {
            knockbackTimer -= Time.fixedDeltaTime;
            if (knockbackTimer <= 0f)
            {
                isKnockedBack = false;
            }
            return;
        }

        // FIX: Таймер неуязвимости
        if (isInvulnerable)
        {
            invulTimer -= Time.fixedDeltaTime;
            if (invulTimer <= 0f)
                isInvulnerable = false;
        }

        if (isPaused)
        {
            rb.velocity = Vector2.zero;
            pauseTimer -= Time.fixedDeltaTime;
            if (pauseTimer <= 0f)
            {
                isPaused = false;
            }
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = movingToB ? pointB : pointA;

        float distanceToTarget = Vector2.Distance(currentPosition, targetPosition);
        if (distanceToTarget < 0.05f)
        {
            isPaused = true;
            pauseTimer = pauseTimeAtPoints;
            rb.velocity = Vector2.zero;
            movingToB = !movingToB;
            return;
        }

        Vector2 direction;
        if (useVerticalMovement)
        {
            float verticalDirection = Mathf.Sign(targetPosition.y - currentPosition.y);
            direction = new Vector2(0f, verticalDirection);
        }
        else
        {
            float horizontalDirection = Mathf.Sign(targetPosition.x - currentPosition.x);
            direction = new Vector2(horizontalDirection, 0f);
        }

        rb.velocity = direction * speed;
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        // FIX: Неуязвимость после удара
        if (isInvulnerable) return;
        isInvulnerable = true;
        invulTimer = invulDuration;

        health -= damage;
        Debug.Log("Враг получил урон: " + damage);

        if (health <= 0)
        {
           
            Destroy(gameObject);
            return;
        }

        rb.velocity = Vector2.zero;
        rb.AddForce(hitDirection * 8f, ForceMode2D.Impulse);
        isKnockedBack = true;
        knockbackTimer = knockbackDuration;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        // FIX: Атакуем только игрока по тегу
        if (collision.collider.CompareTag("Player"))
        {
            PlayerHealth player = collision.collider.GetComponent<PlayerHealth>();
            if (player != null)
            {
                Vector2 knockDir = ((Vector2)collision.transform.position - (Vector2)transform.position).normalized;
                player.TakeDamage(damageToPlayer, knockDir);

                Rigidbody2D playerRb = collision.collider.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.velocity = Vector2.zero;
                    playerRb.AddForce(knockDir * playerKnockbackForce, ForceMode2D.Impulse);
                }

                lastAttackTime = Time.time;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (patrolPointA && patrolPointB)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(patrolPointA.position, 0.1f);
            Gizmos.DrawSphere(patrolPointB.position, 0.1f);
            Gizmos.DrawLine(patrolPointA.position, patrolPointB.position);
        }
    }
#endif
}