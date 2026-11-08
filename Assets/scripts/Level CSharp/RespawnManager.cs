using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Respawn Settings")]
    [Min(0f)] public float respawnDelay = 0.4f;
    [Min(0f)] public float postRespawnInvul = 1f;

    [Tooltip("Необязательный тег границ камеры.")]
    public string cameraBoundsTag = "CameraBounds";

    public bool IsRespawning { get; private set; }

    private string checkpointScene = "";
    private Vector3 checkpointPosition;
    private bool hasCheckpoint;

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

    public void SetCheckpoint(Vector3 worldPosition)
    {
        checkpointScene = SceneManager.GetActiveScene().name;
        checkpointPosition = worldPosition;
        hasCheckpoint = true;
    }

    /// <summary>
    /// Сбросить чекпоинт при завершении игровой сессии.
    /// </summary>
    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        checkpointScene = "";
        checkpointPosition = Vector3.zero;
    }

    public void RespawnPlayer(GameObject player)
    {
        if (IsRespawning)
            return;

        IsRespawning = true;
        StartCoroutine(RespawnRoutine(player));
    }

    private IEnumerator RespawnRoutine(GameObject previousPlayer)
    {
        PlayerController player = null;
        Rigidbody2D body = null;
        bool restoreSimulation = false;

        try
        {
            // Если смерть произошла на выходе из локации,
            // сначала даём менеджеру закончить переход.
            while (SceneTransitionManager.Instance != null &&
                   SceneTransitionManager.Instance.IsTransitioning)
            {
                yield return null;
            }

            player = PlayerController.Instance;

            if (player == null && previousPlayer != null)
                player = previousPlayer.GetComponent<PlayerController>();

            if (player == null)
            {
                Debug.LogError(
                    "[RespawnManager] Не найден игрок для возрождения."
                );

                yield break;
            }

            player.HealInput?.CancelHeal();
            player.SetAutoMove(Vector2.zero);
            player.SetInputEnabled(false);
            player.Movement?.ResetMovementState();

            body = player.GetComponent<Rigidbody2D>();
            restoreSimulation = body != null && body.simulated;

            if (body != null)
                body.simulated = false;

            Time.timeScale = 1f;

            if (respawnDelay > 0f)
                yield return new WaitForSecondsRealtime(respawnDelay);

            ScreenFader fader = ScreenFader.Instance;

            if (fader != null)
                yield return fader.FadeOut();

            string destinationScene = hasCheckpoint
                ? checkpointScene
                : SceneManager.GetActiveScene().name;

            if (!Application.CanStreamedLevelBeLoaded(destinationScene))
            {
                Debug.LogError(
                    $"[RespawnManager] Сцена '{destinationScene}' недоступна."
                );

                yield break;
            }

            bool needsSceneLoad =
                !hasCheckpoint ||
                SceneManager.GetActiveScene().name != destinationScene;

            if (needsSceneLoad)
            {
                AsyncOperation loading =
                    SceneManager.LoadSceneAsync(destinationScene);

                if (loading == null)
                {
                    Debug.LogError(
                        "[RespawnManager] Не удалось начать загрузку сцены."
                    );

                    yield break;
                }

                while (!loading.isDone)
                    yield return null;

                yield return null;

                // Объект мог быть уничтожен и создан заново.
                PlayerController current = PlayerController.Instance;

                if (current == null && PlayerBootstrapper.Instance != null)
                {
                    current =
                        PlayerBootstrapper.Instance.GetOrCreatePlayer();
                }

                if (current == null)
                {
                    Debug.LogError(
                        "[RespawnManager] После загрузки игрок не найден."
                    );

                    yield break;
                }

                if (current != player)
                {
                    // Восстанавливаем состояние старого тела, если оно
                    // ещё существует, прежде чем переключить ссылку.
                    if (body != null)
                        body.simulated = restoreSimulation;

                    player = current;
                    body = player.GetComponent<Rigidbody2D>();
                    restoreSimulation = body != null && body.simulated;
                }

                player.SetInputEnabled(false);

                if (body != null)
                    body.simulated = false;
            }

            Vector3 destination;

            if (hasCheckpoint)
            {
                destination = checkpointPosition;
            }
            else
            {
                SpawnPoint spawn = SpawnPoint.FindDefaultInActiveScene();

                if (spawn == null)
                {
                    Debug.LogError(
                        "[RespawnManager] Для возрождения без чекпоинта " +
                        "нужна точка появления по умолчанию."
                    );

                    yield break;
                }

                destination = spawn.GetSpawnPosition();
                SetCheckpoint(destination);
            }

            player.TeleportTo(destination);

            if (CoinManager.Instance != null)
                CoinManager.Instance.ResetForDeath();

            player.Health.SetHealth(player.Health.maxHealth);
            player.Health.MakeInvulnerable(postRespawnInvul);

            Physics2D.SyncTransforms();

            PlayerCameraBinding.Refresh(
                player.transform,
                cameraBoundsTag
            );

            if (body != null)
                body.simulated = restoreSimulation;

            // Старый фейдер мог принадлежать выгруженной сцене.
            fader = ScreenFader.Instance;

            if (fader != null)
                yield return fader.FadeIn();
        }
        finally
        {
            if (body != null)
                body.simulated = restoreSimulation;

            if (player != null)
            {
                player.SetAutoMove(Vector2.zero);
                player.SetInputEnabled(true);
            }

            // При ошибке не оставляем экран закрытым навсегда.
            if (ScreenFader.Instance != null)
                ScreenFader.Instance.SetInstant(0f);

            IsRespawning = false;
        }
    }
}
