using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Transition Settings")]
    [Min(0f)]
    public float autoMoveTime = 0.7f;

    public ScreenFader screenFader;

    public bool IsTransitioning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Сигнатура сохранена для существующих компонентов перехода.
    public void StartTransition(
        string sceneName,
        string spawnPoint,
        Vector2 direction,
        int health)
    {
        if (IsTransitioning)
            return;

        if (RespawnManager.Instance != null &&
            RespawnManager.Instance.IsRespawning)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName) ||
            !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError(
                $"[SceneTransitionManager] Сцена '{sceneName}' недоступна."
            );

            return;
        }

        PlayerController player = PlayerController.Instance;

        if (player == null || player.Health.currentHealth <= 0)
            return;

        // Параметр health оставлен ради совместимости вызова.
        // Источник истины — актуальный PlayerHealth, а не копия у триггера.
        int savedHealth = player.Health.currentHealth;
        int savedSoul = player.GetCurrentSoul();

        IsTransitioning = true;

        StartCoroutine(
            TransitionRoutine(
                sceneName,
                spawnPoint,
                direction,
                savedHealth,
                savedSoul,
                player
            )
        );
    }

    private IEnumerator TransitionRoutine(
        string sceneName,
        string spawnName,
        Vector2 direction,
        int savedHealth,
        int savedSoul,
        PlayerController player)
    {
        Rigidbody2D body = null;
        bool restoreSimulation = false;
        Coroutine autoMoveRoutine = null;

        try
        {
            Time.timeScale = 1f;

            player.HealInput?.CancelHeal();
            player.SetInputEnabled(false);
            player.SetAutoMove(direction);

            ScreenFader fader = GetFader();

            if (fader != null)
                yield return fader.FadeOut();

            if (player == null || player.Health.currentHealth <= 0)
                yield break;

            // Останавливаем физику на время загрузки и размещения.
            player.SetAutoMove(Vector2.zero);
            player.Movement?.ResetMovementState();

            body = player.GetComponent<Rigidbody2D>();
            restoreSimulation = body != null && body.simulated;

            if (body != null)
                body.simulated = false;

            AsyncOperation loading =
                SceneManager.LoadSceneAsync(sceneName);

            if (loading == null)
            {
                Debug.LogError(
                    "[SceneTransitionManager] Не удалось начать загрузку."
                );

                yield break;
            }

            while (!loading.isDone)
                yield return null;

            // Даём завершиться первичной инициализации новой сцены.
            yield return null;

            PlayerController current = PlayerController.Instance;

            if (current == null && PlayerBootstrapper.Instance != null)
                current = PlayerBootstrapper.Instance.GetOrCreatePlayer();

            if (current == null)
            {
                Debug.LogError(
                    "[SceneTransitionManager] В новой сцене нет игрока."
                );

                yield break;
            }

            if (current != player)
            {
                if (body != null)
                    body.simulated = restoreSimulation;

                player = current;
                body = player.GetComponent<Rigidbody2D>();
                restoreSimulation = body != null && body.simulated;
            }

            player.SetInputEnabled(false);

            if (body != null)
                body.simulated = false;

            Transform spawn = FindSpawnPoint(spawnName);

            if (spawn == null)
            {
                Debug.LogError(
                    "[SceneTransitionManager] Не найдена точка входа " +
                    "или SpawnPoint по умолчанию в новой сцене."
                );

                yield break;
            }

            player.TeleportTo(spawn.position);

            // Используем методы систем, чтобы обновился интерфейс.
            player.Health.SetHealth(savedHealth);
            player.SoulSystem.SetSoul(savedSoul);

            Physics2D.SyncTransforms();
            PlayerCameraBinding.Refresh(player.transform);

            if (body != null)
                body.simulated = restoreSimulation;

            // Автодвижение и открытие экрана выполняются параллельно.
            // Таймер автодвижения не прибавляется к длительности FadeIn.
            autoMoveRoutine = StartCoroutine(
                AutoMoveFor(player, direction)
            );

            fader = GetFader();

            if (fader != null)
                yield return fader.FadeIn();

            yield return autoMoveRoutine;
            autoMoveRoutine = null;
        }
        finally
        {
            if (autoMoveRoutine != null)
                StopCoroutine(autoMoveRoutine);

            if (body != null)
                body.simulated = restoreSimulation;

            if (player != null)
            {
                player.SetAutoMove(Vector2.zero);
                player.SetInputEnabled(true);
            }

            ScreenFader fader = GetFader();

            if (fader != null)
                fader.SetInstant(0f);

            IsTransitioning = false;
        }
    }

    private IEnumerator AutoMoveFor(
        PlayerController player,
        Vector2 direction)
    {
        float elapsed = 0f;

        if (player != null)
            player.SetAutoMove(direction);

        while (elapsed < Mathf.Max(0f, autoMoveTime))
        {
            if (player == null || player.Health.currentHealth <= 0)
                break;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (player != null)
            player.SetAutoMove(Vector2.zero);
    }

    private Transform FindSpawnPoint(string spawnName)
    {
        Scene activeScene = SceneManager.GetActiveScene();

        if (!string.IsNullOrWhiteSpace(spawnName))
        {
            GameObject namedPoint = GameObject.Find(spawnName);

            if (namedPoint != null &&
                namedPoint.scene == activeScene)
            {
                return namedPoint.transform;
            }

            Debug.LogWarning(
                $"[SceneTransitionManager] Точка '{spawnName}' " +
                "не найдена. Проверяю точку по умолчанию."
            );
        }

        SpawnPoint defaultPoint =
            SpawnPoint.FindDefaultInActiveScene();

        if (defaultPoint != null)
            return defaultPoint.transform;

        // Совместимость со сценами, где остался только старый тег.
        try
        {
            GameObject taggedPoint =
                GameObject.FindGameObjectWithTag("PlayerSpawn");

            if (taggedPoint != null &&
                taggedPoint.scene == activeScene)
            {
                return taggedPoint.transform;
            }
        }
        catch (UnityException)
        {
            // Тег PlayerSpawn необязателен, если используется SpawnPoint.
        }

        return null;
    }

    private ScreenFader GetFader()
    {
        if (ScreenFader.Instance != null)
            return ScreenFader.Instance;

        return screenFader;
    }
}
