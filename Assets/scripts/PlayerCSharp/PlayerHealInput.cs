using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerSoulSystem))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerHealInput : MonoBehaviour
{
    [Header("Input Settings")]
    [SerializeField] private InputActionReference healAction;

    [Header("Heal Settings")]
    [Min(1)]
    [SerializeField] private int healAmount = 1;

    [Min(0f)]
    [SerializeField] private float healCooldown = 2f;

    [Tooltip("Задержка перед восстановлением здоровья для анимации.")]
    [Min(0f)]
    [SerializeField] private float healDelay = 0.5f;

    [Header("Soul Cost Settings")]
    [SerializeField] private bool useSoulCost = true;

    [Tooltip("Стоимость лечения в единицах души, а не в сосудах.")]
    [Min(0)]
    [SerializeField] private int soulCostPerHeal = 1;

    [Header("Feedback")]
    [SerializeField] private AudioClip healSound;
    [SerializeField] private GameObject healEffectPrefab;
    [SerializeField] private bool showDebugMessages = true;

    private PlayerHealth playerHealth;
    private PlayerSoulSystem playerSoul;
    private PlayerMovement movement;
    private PlayerInput playerInput;

    private InputAction boundHealAction;
    private bool enabledStandaloneAction;

    private bool isHealing;
    private float healTimer;
    private float cooldownTimer;

    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        playerSoul = GetComponent<PlayerSoulSystem>();
        movement = GetComponent<PlayerMovement>();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        playerHealth.OnPlayerDeath += OnPlayerDeath;
        BindHealAction();
    }

    private void OnDisable()
    {
        playerHealth.OnPlayerDeath -= OnPlayerDeath;

        UnbindHealAction();
        CancelHeal();
    }

    private void BindHealAction()
    {
        InputAction referenceAction =
            healAction != null ? healAction.action : null;

        if (playerInput != null && playerInput.actions != null)
        {
            // PlayerInput может использовать собственную копию действий.
            // Подписываемся на действие именно этого игрока.
            if (referenceAction != null)
            {
                boundHealAction = playerInput.actions.FindAction(
                    referenceAction.id.ToString()
                );

                if (boundHealAction == null)
                {
                    boundHealAction = playerInput.actions.FindAction(
                        referenceAction.name
                    );
                }
            }
            else
            {
                boundHealAction =
                    playerInput.actions.FindAction("Heal");
            }

            // Не включаем управляемое PlayerInput действие вручную:
            // иначе оно могло бы остаться активным после смены карты ввода.
        }
        else
        {
            // Запасной вариант для использования без PlayerInput.
            boundHealAction = referenceAction;

            if (boundHealAction != null && !boundHealAction.enabled)
            {
                boundHealAction.Enable();
                enabledStandaloneAction = true;
            }
        }

        if (boundHealAction != null)
        {
            boundHealAction.performed += OnHealPerformed;
        }
        else
        {
            Debug.LogWarning(
                "[PlayerHealInput] Не найдено действие лечения.",
                this
            );
        }
    }

    private void UnbindHealAction()
    {
        if (boundHealAction == null)
            return;

        boundHealAction.performed -= OnHealPerformed;

        // Отключаем только то действие, которое включили самостоятельно.
        if (enabledStandaloneAction)
            boundHealAction.Disable();

        boundHealAction = null;
        enabledStandaloneAction = false;
    }

    private void Update()
    {
        cooldownTimer =
            Mathf.Max(0f, cooldownTimer - Time.deltaTime);

        if (!isHealing)
            return;

        // Смерть, переход или отключение PlayerInput отменяют лечение.
        if (playerHealth.currentHealth <= 0 ||
            (playerInput != null && !playerInput.isActiveAndEnabled))
        {
            CancelHeal();
            return;
        }

        healTimer -= Time.deltaTime;

        if (healTimer <= 0f)
            CompleteHeal();
    }

    private void OnHealPerformed(InputAction.CallbackContext context)
    {
        RequestHeal();
    }

    public bool CanHealNow()
    {
        return CanHeal();
    }

    public bool RequestHeal()
    {
        if (!CanHeal())
            return false;

        StartHeal();
        return true;
    }

    private bool CanHeal()
    {
        if (!isActiveAndEnabled ||
            isHealing ||
            cooldownTimer > 0f ||
            playerHealth == null ||
            !playerHealth.CanBeHealed())
        {
            return false;
        }

        // Нельзя начать лечение во время смерти, перехода или отталкивания.
        if (movement != null && !movement.CanMove())
            return false;

        if (useSoulCost)
        {
            if (playerSoul == null ||
                !playerSoul.HasEnoughSoul(soulCostPerHeal))
            {
                return false;
            }
        }

        return true;
    }

    private void StartHeal()
    {
        isHealing = true;
        healTimer = Mathf.Max(0f, healDelay);

        // Устанавливаем только собственную блокировку.
        // Основной переключатель переходов и возрождения не изменяем.
        movement.SetInputLock(this, true);

        // Здесь можно подключить анимацию лечения.
        // GetComponent<Animator>().SetTrigger("Heal");

        // Заготовка для будущей корутины анимации:
        // StartCoroutine(PlayHealAnimation());

        if (showDebugMessages)
            Debug.Log("[PlayerHealInput] Начало лечения.", this);

        if (healTimer <= 0f)
            CompleteHeal();
    }

    private void CompleteHeal()
    {
        if (!isHealing)
            return;

        // За время анимации игрок мог умереть или получить полное здоровье.
        // В таком случае душа не расходуется.
        if (!playerHealth.CanBeHealed())
        {
            CancelHeal();
            return;
        }

        // Другие действия могли успеть потратить душу за время задержки.
        if (useSoulCost &&
            (playerSoul == null ||
             !playerSoul.TrySpendSoul(soulCostPerHeal)))
        {
            CancelHeal();
            return;
        }

        int healthBefore = playerHealth.currentHealth;

        playerHealth.Heal(healAmount);

        int restoredHealth = playerHealth.currentHealth - healthBefore;

        isHealing = false;
        healTimer = 0f;
        cooldownTimer = Mathf.Max(0f, healCooldown);

        movement.SetInputLock(this, false);

        PlayHealEffects();

        if (showDebugMessages)
        {
            Debug.Log(
                $"[PlayerHealInput] Восстановлено здоровья: {restoredHealth}.",
                this
            );
        }
    }

    public void CancelHeal()
    {
        isHealing = false;
        healTimer = 0f;

        if (movement != null)
            movement.SetInputLock(this, false);
    }

    private void OnPlayerDeath()
    {
        CancelHeal();
    }

    private void PlayHealEffects()
    {
        if (healSound != null)
            AudioSource.PlayClipAtPoint(healSound, transform.position);

        if (healEffectPrefab != null)
        {
            // Префаб эффекта должен самостоятельно завершаться:
            // например, ParticleSystem с Stop Action = Destroy.
            Instantiate(
                healEffectPrefab,
                transform.position,
                Quaternion.identity
            );
        }
    }

    public void SetHealAmount(int amount)
    {
        healAmount = Mathf.Max(1, amount);
    }

    public void SetCooldown(float cooldown)
    {
        healCooldown = Mathf.Max(0f, cooldown);
    }

    // Прямое лечение для внешних игровых событий.
    // Не расходует душу и не запускает анимацию.
    public void ForceHeal()
    {
        if (playerHealth != null && playerHealth.CanBeHealed())
            playerHealth.Heal(healAmount);
    }

    public bool IsOnCooldown()
    {
        return cooldownTimer > 0f;
    }

    public float GetCooldownRemaining()
    {
        return Mathf.Max(0f, cooldownTimer);
    }

    public bool IsHealing()
    {
        return isHealing;
    }

    // Заготовка анимации сохранена намеренно.
    /*
    private System.Collections.IEnumerator PlayHealAnimation()
    {
        // Здесь можно запустить Animator Trigger "Heal".
        yield return new WaitForSeconds(healDelay);
    }
    */
}
