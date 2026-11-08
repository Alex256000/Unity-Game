using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Выполняется после обычного Awake компонентов игрока.
[DefaultExecutionOrder(100)]
public class PlayerBootstrapper : MonoBehaviour
{
    public static PlayerBootstrapper Instance { get; private set; }

    [Header("Игрок")]
    [SerializeField] private GameObject playerPrefab;

    [Header("Интерфейс игрока")]
    [SerializeField] private PlayerUIController playerUIPrefab;

    [Header("Служебные сцены")]
    [SerializeField] private string bootSceneName = "BootScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    private int handledScene = -1;
    private Coroutine initialization;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning(
                "[PlayerBootstrapper] Удаляется дубликат загрузчика.",
                this
            );

            Destroy(gameObject);
            return;
        }

        // DontDestroyOnLoad требует корневой объект.
        // Отделяем только объект загрузчика, а не сохраняем
        // весь Canvas или другой родительский объект сцены.
        if (transform.parent != null)
        {
            Debug.LogWarning(
                "[PlayerBootstrapper] Загрузчик был вложенным. " +
                "Переношу его в корень Hierarchy.",
                this
            );

            transform.SetParent(null, true);
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        Debug.Log(
            "[PlayerBootstrapper] Загрузчик создан и сохраняется между сценами.",
            this
        );
    }


    private void Start()
    {
        // Поддерживаем и запуск сразу из игровой сцены в редакторе.
        HandleScene(SceneManager.GetActiveScene());
    }

    private void OnDestroy()
    {
        // Уничтожение дубликата не должно сбрасывать основной экземпляр.
        if (Instance != this)
            return;

        Debug.LogWarning(
            "[PlayerBootstrapper] Основной загрузчик уничтожен. " +
            "При обычном переходе между игровыми сценами " +
            "этого происходить не должно.",
            this
        );

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
}


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Эта схема предназначена для одиночной загрузки уровней.
        // Добавочные сцены не должны повторно инициализировать игрока.
        if (mode != LoadSceneMode.Single)
            return;

        HandleScene(scene);
    }

    /// <summary>
    /// Создаёт один экземпляр интерфейса для игровой сессии.
    /// Существующий интерфейс при переходе между уровнями сохраняется.
    /// </summary>
    private void EnsurePlayerUI()
    {
        if (PlayerUIController.Instance != null)
            return;

        if (playerUIPrefab == null)
        {
            Debug.LogError(
                "[PlayerBootstrapper] Не назначен префаб " +
                "интерфейса игрока — Player UI Prefab.",
                this
            );

            return;
        }

        PlayerUIController ui = Instantiate(playerUIPrefab);
        ui.gameObject.name = "PlayerUI_Persistent";

        // Корень активного префаба проходит Awake при создании.
        // В нём PlayerUIController назначает Instance
        // и сохраняет интерфейс через DontDestroyOnLoad.
    }

    private void HandleScene(Scene scene)
    {
        if (handledScene == scene.handle)
            return;

        handledScene = scene.handle;

        if (initialization != null)
        {
            StopCoroutine(initialization);
            initialization = null;
        }

        if (IsServiceScene(scene.name))
        {
            // Между меню и новой игровой сессией
            // не переносим ни игрока, ни его интерфейс.
            if (PlayerUIController.Instance != null)
            {
                GameObject uiObject =
                    PlayerUIController.Instance.gameObject;

                // Destroy выполняется не мгновенно:
                // скрываем интерфейс до его удаления.
                uiObject.SetActive(false);
                Destroy(uiObject);
            }

            if (PlayerController.Instance != null)
                Destroy(PlayerController.Instance.gameObject);

            RespawnManager.Instance?.ClearCheckpoint();
            return;
        }

        PlayerController player = GetOrCreatePlayer();

        if (player == null)
            return;

        // Переход и возрождение сами восстановят позицию и управление.
        if (IsPlacementOwnedByManager())
            return;

        initialization = StartCoroutine(InitializeScene(player, scene));
    }

    public PlayerController GetOrCreatePlayer()
    {
        PlayerController player = PlayerController.Instance;

        if (player == null)
        {
            // Учитываем игрока, уже размещённого на сцене.
            player = FindFirstObjectByType<PlayerController>();
        }

        if (player == null)
        {
            if (playerPrefab == null)
            {
                Debug.LogError(
                    "[PlayerBootstrapper] Не назначен playerPrefab.",
                    this
                );

                return null;
            }

            GameObject instance = Instantiate(playerPrefab);
            player = instance.GetComponent<PlayerController>();

            if (player == null)
            {
                Debug.LogError(
                    "[PlayerBootstrapper] На корне playerPrefab " +
                    "нет PlayerController.",
                    instance
                );

                Destroy(instance);
                return null;
            }
        }

        // DontDestroyOnLoad работает с корневыми объектами.
        // Отделяем самого игрока, а не переносим весь родительский уровень.
        player.transform.SetParent(null, true);
        DontDestroyOnLoad(player.gameObject);
        // Интерфейс создаётся в любой игровой локации,
        // в том числе при загрузке сохранения сразу во вторую.
        EnsurePlayerUI();
        return player;
    }


    private bool IsServiceScene(string sceneName)
    {
        return sceneName == bootSceneName ||
               sceneName == mainMenuSceneName;
    }

    private bool IsPlacementOwnedByManager()
    {
        return
            (RespawnManager.Instance != null &&
             RespawnManager.Instance.IsRespawning) ||
            (SceneTransitionManager.Instance != null &&
             SceneTransitionManager.Instance.IsTransitioning);
    }

    private IEnumerator InitializeScene(
        PlayerController player,
        Scene scene)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        bool wasSimulated = body != null && body.simulated;

        player.SetInputEnabled(false);

        if (body != null)
            body.simulated = false;

        try
        {
            // Ждём Start подсистем игрока и интерфейса.
            yield return null;

            if (player == null ||
                SceneManager.GetActiveScene().handle != scene.handle)
            {
                yield break;
            }

            // Отсутствие флага отличается от явно выбранной новой игры.
            int startMode = PlayerPrefs.GetInt("IsNewGame", -1);

            SaveManager saves = SaveManager.Instance;
            SaveData loadedData =
                saves != null ? saves.CurrentSaveData : null;

            bool loadingSave = startMode == 0;

            if (loadingSave)
            {
                if (loadedData == null ||
                    loadedData.sceneName != scene.name)
                {
                    Debug.LogError(
                        "[PlayerBootstrapper] Нет подходящих данных " +
                        "для загрузки текущей сцены."
                    );

                    yield break;
                }

                PlayerSaveHandler handler =
                    player.GetComponent<PlayerSaveHandler>();

                if (handler == null)
                {
                    Debug.LogError(
                        "[PlayerBootstrapper] Не найден PlayerSaveHandler."
                    );

                    yield break;
                }

                handler.ApplyLoadedData(loadedData);
            }
            else
            {
                SpawnPoint spawn = SpawnPoint.FindDefaultInActiveScene();

                if (spawn == null)
                {
                    Debug.LogError(
                        $"[PlayerBootstrapper] Не найдена точка появления " +
                        $"в сцене '{scene.name}'. " +
                        $"Активная сцена: '{SceneManager.GetActiveScene().name}'. " +
                        $"Служебные сцены в настройках: " +
                        $"'{bootSceneName}' и '{mainMenuSceneName}'.",
                        this
                    );

                    yield break;
                }

                player.TeleportTo(spawn.GetSpawnPosition());

                if (startMode == 1)
                {
                    // Новая игра не наследует состояние прежней сессии.
                    player.Health.SetHealth(player.Health.maxHealth);
                    player.SoulSystem.SetSoul(player.SoulSystem.GetMaxSoul());
                }
            }

            // После загрузки сохранения смерть возвращает игрока
            // к загруженной позиции, пока не активирован новый чекпоинт.
            RespawnManager.Instance?.SetCheckpoint(
                player.transform.position
            );

            Physics2D.SyncTransforms();
            PlayerCameraBinding.Refresh(player.transform);
            if (PlayerUIController.Instance != null)
                PlayerUIController.Instance.OnSceneLoaded();
                
            if (startMode == 1 && saves != null)
            {
                int slot = PlayerPrefs.GetInt("CurrentSaveSlot", 0);

                if (slot >= 1 && slot <= 3)
                {
                    // Перезаписываем стартовый файл реальными значениями
                    // игрока и фактической позицией SpawnPoint.
                    // В меню раньше записывались health = 100
                    // и условные координаты (0, 1, 0).
                    SaveData initialData =
                        player.SaveHandler.CollectSaveData();

                    saves.SaveGame(slot, initialData);
                }
            }

            // Флаг обрабатывается один раз, а не на каждом уровне.
            saves?.ClearCurrentSaveData();

            PlayerPrefs.DeleteKey("IsNewGame");
            PlayerPrefs.Save();
        }
        finally
        {
            if (body != null)
                body.simulated = wasSimulated;

            if (player != null)
                player.SetInputEnabled(true);

            initialization = null;
        }
    }
}
