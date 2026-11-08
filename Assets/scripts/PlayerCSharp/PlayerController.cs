using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
[RequireComponent(typeof(PlayerHealInput))]
[RequireComponent(typeof(PlayerSoulSystem))]
[RequireComponent(typeof(PlayerSaveHandler))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Сохранять объект игрока при смене сцен.")]
    [SerializeField] private bool persistBetweenScenes = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogLifecycle = false;

    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerHealth _health;
    private PlayerHealInput _healInput;
    private PlayerSoulSystem _soulSystem;
    private PlayerSaveHandler _saveHandler;

    // Доступ к подсистемам игрока из других компонентов.
    public PlayerMovement Movement => _movement;
    public PlayerAttack Attack => _attack;
    public PlayerHealth Health => _health;
    public PlayerHealInput HealInput => _healInput;
    public PlayerSoulSystem SoulSystem => _soulSystem;
    public PlayerSaveHandler SaveHandler => _saveHandler;

    private void Awake()
    {
        // Не допускаем существования двух основных игроков.
        if (Instance != null && Instance != this)
        {
            if (debugLogLifecycle)
                Debug.Log("[PlayerController] Удалён дубликат игрока.", this);

            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistBetweenScenes)
            DontDestroyOnLoad(gameObject);

        // Кэшируем все подсистемы, включая обработчик сохранений.
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
        _health = GetComponent<PlayerHealth>();
        _healInput = GetComponent<PlayerHealInput>();
        _soulSystem = GetComponent<PlayerSoulSystem>();
        _saveHandler = GetComponent<PlayerSaveHandler>();

        if (debugLogLifecycle)
            Debug.Log("[PlayerController] Компоненты игрока получены.", this);
    }

    private void OnDestroy()
    {
        // Уничтожение дубликата не должно сбрасывать основной Instance.
        if (Instance == this)
            Instance = null;
    }

    public void TeleportTo(Vector3 newPosition)
    {
        // При таком телепорте старая скорость и отталкивание не сохраняются.
        // Телепорты с preserveMomentum продолжают управлять инерцией отдельно.
        if (_movement != null)
            _movement.ResetMovementState();

        Rigidbody2D body = GetComponent<Rigidbody2D>();

        transform.position = newPosition;

        if (body != null)
        {
            body.position = newPosition;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        if (debugLogLifecycle)
            Debug.Log($"[PlayerController] Телепорт: {newPosition}", this);
    }

    // Основной переключатель управления для переходов и возрождения.
    public void SetInputEnabled(bool enabled)
    {
        if (_movement != null)
            _movement.SetInputEnabled(enabled);
    }

    // Автодвижение может работать при отключённом пользовательском вводе.
    public void SetAutoMove(Vector2 direction)
    {
        if (_movement != null)
            _movement.SetAutoMove(direction);
    }

    public bool RequestHeal()
    {
        return _healInput != null && _healInput.RequestHeal();
    }

    public bool CanHealNow()
    {
        return _healInput != null && _healInput.CanHealNow();
    }

    public bool TrySpendSoul(int amount)
    {
        return _soulSystem != null && _soulSystem.TrySpendSoul(amount);
    }

    public void AddSoul(int amount)
    {
        if (_soulSystem != null)
            _soulSystem.AddSoul(amount);
    }

    public int GetCurrentSoul()
    {
        return _soulSystem != null ? _soulSystem.GetCurrentSoul() : 0;
    }

    public int GetMaxSoul()
    {
        return _soulSystem != null ? _soulSystem.GetMaxSoul() : 0;
    }

    public int GetVesselCount()
    {
        return _soulSystem != null ? _soulSystem.GetVesselCount() : 0;
    }
}
