using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public float patrolSpeed = 1f;
    public float patrolDistance = 2f;
    public float patrolWaitTime = 1f;
    public float wallWaitTime = 2f;
    public float chaseSpeed = 2f;
    public float lostChaseWait = 1f;
    public EnemyVision vision;

    private Rigidbody2D rb;
    private bool isWaiting = false;
    private float waitTimer = 0f;

    private Vector2 moveDirection = Vector2.right;
    private float movedDistance = 0f;
    private bool blockedDirLeft = false;
    private bool blockedDirRight = false;

    // Для погони
    private bool isChasing = false;
    private Vector2 lastSeenPlayerPos;
    private bool lostPlayer = false;
    private bool justLostPlayer = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (vision == null) vision = GetComponentInChildren<EnemyVision>();
        ChooseNewDirection();
    }

    void Update()
    {
        // 1. Проверка видимости игрока
        if (vision.PlayerInSight)
        {
            isChasing = true;
            lostPlayer = false;
            justLostPlayer = false;
            lastSeenPlayerPos = vision.lastSeenPosition;
            isWaiting = false;
        }
        else if (isChasing && !lostPlayer)
        {
            // Игрок потерян из вида — идём к последней позиции
            lostPlayer = true;
            justLostPlayer = true; // флаг для ожидания после потери
        }

        if (isChasing)
        {
            ChaseLogic();
            return;
        }

        // --- Патрулирование ---
        if (isWaiting)
        {
            rb.velocity = Vector2.zero;
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                ChooseNewDirection();
            }
            return;
        }

        rb.velocity = moveDirection * patrolSpeed;
        movedDistance += patrolSpeed * Time.deltaTime;

        if (movedDistance >= patrolDistance)
        {
            isWaiting = true;
            waitTimer = patrolWaitTime;
            rb.velocity = Vector2.zero;
        }
    }

    void ChaseLogic()
    {
        Vector2 target;
        float speed = chaseSpeed;

        if (vision.PlayerInSight)
        {
            target = vision.lastSeenPosition;
        }
        else
        {
            target = lastSeenPlayerPos;
        }

        float dist = Vector2.Distance(transform.position, target);

        // Если дошли до места, где последний раз видели игрока — возвращаемся к патрулю через короткую паузу
        if (!vision.PlayerInSight && dist < 0.2f)
        {
            if (justLostPlayer)
            {
                rb.velocity = Vector2.zero;
                isWaiting = true;
                waitTimer = lostChaseWait;
                justLostPlayer = false;
            }
            else if (!isWaiting)
            {
                // После ожидания возвращаемся к патрулю
                isChasing = false;
                lostPlayer = false;
                movedDistance = 0f;
                ChooseNewDirection();
            }
            return;
        }

        // Движение к цели
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = new Vector2(direction.x * speed, rb.velocity.y);
    }

    void ChooseNewDirection()
    {
        movedDistance = 0f;

        // Если упирались в стену — движемся только в разрешённую сторону
        if (blockedDirLeft && !blockedDirRight)
        {
            moveDirection = Vector2.right;
            blockedDirLeft = false;
        }
        else if (!blockedDirLeft && blockedDirRight)
        {
            moveDirection = Vector2.left;
            blockedDirRight = false;
        }
        else
        {
            // Выбираем случайное направление (50/50)
            moveDirection = (Random.value < 0.5f) ? Vector2.left : Vector2.right;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Wall"))
        {
            rb.velocity = Vector2.zero;
            isWaiting = true;
            waitTimer = wallWaitTime;

            // FIX: Проверяем все точки контакта
            foreach (var contact in col.contacts)
            {
                float contactX = contact.point.x;
                if (contactX > transform.position.x)
                    blockedDirRight = true;
                else
                    blockedDirLeft = true;
            }
            if (isChasing)
            {
                isChasing = false;
                lostPlayer = false;
            }
        }
        else if (col.gameObject.CompareTag("Player"))
        {
            PlayerHealth health = col.gameObject.GetComponent<PlayerHealth>();
            if (health != null) health.TakeDamage(1);
        }
    }
}