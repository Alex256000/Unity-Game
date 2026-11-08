using System.Collections;
using UnityEngine;


public class PlayerUIController : MonoBehaviour
{
    public static PlayerUIController Instance { get; private set; }

    [Header("Health UI")]
    [SerializeField] private GameObject healthBarContainer; // Родительский объект полосы здоровья (Canvas element)
    [SerializeField] private HealthBarSpriteMapper healthBar; // Ссылка на компонент HealthBarSpriteMapper

    [Header("Soul UI")]
    [SerializeField] private GameObject soulDisplayContainer; // Контейнер, в котором лежит RuntimeVesselsSoulUI (или другой UI компонент)
    [SerializeField] private MonoBehaviour soulUIBehaviour; // назначьте RuntimeSoulUI компонент (или другой с UpdateSoulDisplay)

    [Header("Settings")]
    [SerializeField] private bool persistBetweenScenes = true;
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            if (debugLog) Debug.Log("[PlayerUIController] Destroying duplicate instance");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (persistBetweenScenes)
        {
            // DontDestroyOnLoad только для корня
            DontDestroyOnLoad(gameObject);
            // НЕ вызывать DontDestroyOnLoad для дочерних объектов — это вызывает предупреждение/проблемы в Unity.
            // Если вы хотите, чтобы Canvas был персистентен, разместите Canvas как root (на том же GameObject) или сделайте rootUI и поместите Canvas внутрь.
        }

        // Автонахожд��ние компонентов, если не назначены
        if (healthBar == null && healthBarContainer != null)
            healthBar = healthBarContainer.GetComponent<HealthBarSpriteMapper>();

        if (soulUIBehaviour == null && soulDisplayContainer != null)
            soulUIBehaviour = soulDisplayContainer.GetComponent<MonoBehaviour>(); // ожидается RuntimeSoulUI или аналог
    }

    private IEnumerator Start()
    {
        // Ждём один кадр, чтобы все Start/Awake других компонентов (например PlayerHealth, PlayerSoulSystem) успели выполниться.
        yield return null;

        LinkHealthBarToPlayer();
        LinkSoulUIToPlayer();

        if (debugLog) Debug.Log("[PlayerUIController] Start completed");
    }

    public void LinkHealthBarToPlayer()
    {
        if (healthBar == null)
        {
            Debug.LogWarning("[PlayerUIController] HealthBar not assigned!");
            return;
        }

        PlayerController player = PlayerController.Instance;
        if (player == null)
        {
            Debug.LogWarning("[PlayerUIController] Player not found in scene!");
            return;
        }

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("[PlayerUIController] PlayerHealth component not found!");
            return;
        }

        // Надёжная привязка через LinkToPlayer для подписки на события
        healthBar.LinkToPlayer(playerHealth);
    }

    public void LinkSoulUIToPlayer()
    {
        if (soulUIBehaviour == null)
        {
            if (debugLog) Debug.Log("[PlayerUIController] Soul UI behaviour not assigned");
            return;
        }

        PlayerController player = PlayerController.Instance;
        if (player == null)
        {
            if (debugLog) Debug.Log("[PlayerUIController] Player not found for soul linking");
            return;
        }

        var playerSoul = player.GetComponent<PlayerSoulSystem>();
        if (playerSoul == null)
        {
            if (debugLog) Debug.Log("[PlayerUIController] PlayerSoulSystem not found");
            return;
        }

        // Подпишемся на событие изменения души (PlayerSoulSystem.OnSoulChanged — UnityEvent<int>)
        playerSoul.OnSoulChanged.RemoveListener(OnSoulChangedHandler);
        playerSoul.OnSoulChanged.AddListener(OnSoulChangedHandler);

        // Обновим UI единожды
        UpdateSoulUI(playerSoul);
    }

    private void OnSoulChangedHandler(int currentSoul)
    {
        if (debugLog) Debug.Log($"[PlayerUIController] Soul changed: {currentSoul}");

        PlayerController player = PlayerController.Instance;
        if (player == null) return;
        var playerSoul = player.GetComponent<PlayerSoulSystem>();
        if (playerSoul == null) return;

        UpdateSoulUI(playerSoul);
    }

    private void UpdateSoulUI(PlayerSoulSystem playerSoul)
    {
        if (soulUIBehaviour == null || playerSoul == null) return;

        var method = soulUIBehaviour.GetType().GetMethod("UpdateSoulDisplay");
        if (method != null)
        {
            method.Invoke(soulUIBehaviour, new object[] {
                playerSoul.GetCurrentSoul(),
                playerSoul.GetVesselCount(),
                playerSoul.SoulPerVessel
            });
        }
    }

    public void OnSceneLoaded()
    {
        // Вызовится из менеджера сцен: перепривяжем UI
        LinkHealthBarToPlayer();
        LinkSoulUIToPlayer();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}