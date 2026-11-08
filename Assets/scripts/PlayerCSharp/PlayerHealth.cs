using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [Min(1)] public int maxHealth = 3;
    public int currentHealth;

    [Header("Invulnerability Settings")]
    [Min(0f)] public float invulTime = 0.6f;

    private bool initialized;
    private bool isDead;
    private bool isInvul;
    private float invulTimer;

    private PlayerMovement movement;

    public event Action OnPlayerDeath;

    // Передаёт актуальное здоровье подписчикам, например полосе HP.
    public event Action<int> OnHealthChanged;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void Start()
    {
        // Уведомление для UI, который мог подписаться после Awake.
        // Само здоровье здесь уже не переинициализируем.
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        initialized = true;
        maxHealth = Mathf.Max(1, maxHealth);

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Ноль в Inspector означает старт с полным здоровьем.
        // SetHealth(0), вызванный позже загрузчиком, останется нулём.
        if (currentHealth == 0)
            currentHealth = maxHealth;

        movement = GetComponent<PlayerMovement>();
        isDead = false;
    }

    private void FixedUpdate()
    {
        if (!isInvul)
            return;

        invulTimer -= Time.fixedDeltaTime;

        if (invulTimer <= 0f)
        {
            invulTimer = 0f;
            isInvul = false;
        }
    }

    public bool CanBeHealed()
    {
        return !isDead &&
               currentHealth > 0 &&
               currentHealth < maxHealth;
    }

    public bool IsInvulnerable()
    {
        return isInvul;
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        InitializeIfNeeded();

        if (damage <= 0 || isDead || currentHealth <= 0 || isInvul)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth > 0)
        {
            MakeInvulnerable(invulTime);

            if (movement != null)
                movement.ApplyKnockback(hitDirection);
        }

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth == 0)
            Die();
    }

    /// <summary>
    /// Урон от источников, которые должны обходить неуязвимость:
    /// например, осколков или смертельной зоны.
    /// </summary>
    public void TakeDamageIgnoreInvulnerability(int damage)
    {
        InitializeIfNeeded();

        if (damage <= 0 || isDead || currentHealth <= 0)
            return;

        currentHealth = Mathf.Max(0, currentHealth - damage);

        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth == 0)
            Die();
    }

    public void Heal(int amount)
    {
        if (amount <= 0 || !CanBeHealed())
            return;

        // Сначала ограничиваем прибавку, чтобы не выйти за maxHealth.
        int actualAmount = Mathf.Min(amount, maxHealth - currentHealth);

        currentHealth += actualAmount;
        OnHealthChanged?.Invoke(currentHealth);
    }

    private void Die()
    {
        // Несколько коллайдеров или источников урона не должны
        // запускать несколько процедур смерти.
        if (isDead)
            return;

        isDead = true;
        isInvul = false;
        invulTimer = 0f;

        if (movement != null)
            movement.ResetMovementState();

        // Сначала оповещаем подсистемы: например, отменяем лечение.
        OnPlayerDeath?.Invoke();

        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.RespawnPlayer(gameObject);
            return;
        }

        // Запасной путь для сцен без менеджера возрождения.
        DeadZone deadZone = FindAnyObjectByType<DeadZone>();

        if (deadZone != null)
        {
            deadZone.RestartLevel();
            return;
        }

        // Даже без DeadZone игрок не должен навсегда остаться мёртвым.
        SetHealth(maxHealth);
        MakeInvulnerable(invulTime);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Устанавливает здоровье при загрузке или возрождении.
    /// Сам по себе SetHealth(0) не запускает процедуру смерти.
    /// </summary>
    public void SetHealth(int newHealth)
    {
        InitializeIfNeeded();

        currentHealth = Mathf.Clamp(newHealth, 0, maxHealth);
        isDead = currentHealth <= 0;

        // Старое состояние неуязвимости не переносится из прошлого состояния.
        isInvul = false;
        invulTimer = 0f;

        OnHealthChanged?.Invoke(currentHealth);
    }

    public void MakeInvulnerable(float duration)
    {
        if (duration <= 0f || isDead)
            return;

        isInvul = true;

        // Короткая дополнительная защита не сокращает уже действующую.
        invulTimer = Mathf.Max(invulTimer, duration);
    }
}
