using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    public enum ExitSaveMode
    {
        WithoutSaving,
        SaveBeforeExit
    }

    public enum ExitSavePositionMode
    {
        CurrentPlayerPosition,
        ActivatedCheckpointOrDefaultSpawn
    }

    private enum ExitTarget
    {
        None,
        MainMenu,
        Application
    }

    [Header("Правило сохранения для обоих выходов")]
    [SerializeField] private ExitSaveMode exitSaveMode =
        ExitSaveMode.WithoutSaving;

    [Header("Позиция при сохранении перед выходом")]
    [Tooltip("Текущая позиция игрока либо последний активированный " +
        "чекпоинт текущей локации. Если чекпоинта нет — основной SpawnPoint.")]
    [SerializeField] private ExitSavePositionMode exitSavePositionMode =
        ExitSavePositionMode.ActivatedCheckpointOrDefaultSpawn;

    [Header("Служебные сцены")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string bootSceneName = "PreLoadScene";

    [Header("Панели")]
    [SerializeField] private GameObject pauseWindow;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject confirmationPanel;

    [Header("Основные кнопки")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;

    [Header("Подтверждение")]
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Сообщения")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Звук")]
    [SerializeField] private bool pauseAudio = true;

    private InputAction pauseAction;
    private PlayerMovement lockedMovement;
    private GameObject fallbackEventSystem;

    private ExitTarget pendingExit;
    private bool isPaused;
    private bool exiting;

    private float previousTimeScale;
    private bool previousAudioPause;
    private bool previousCursorVisible;
    private CursorLockMode previousCursorLock;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        // Отдельное действие не зависит от карты ввода игрока.
        pauseAction = new InputAction(
            "TogglePause",
            InputActionType.Button
        );

        pauseAction.AddBinding("<Keyboard>/escape");
        pauseAction.AddBinding("<Gamepad>/start");
        pauseAction.performed += OnPausePerformed;
    }

    private void OnEnable()
    {
        pauseAction.Enable();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (pauseWindow == null ||
            mainPanel == null ||
            confirmationPanel == null ||
            resumeButton == null ||
            mainMenuButton == null ||
            quitButton == null ||
            confirmationText == null ||
            yesButton == null ||
            noButton == null ||
            statusText == null)
        {
            Debug.LogError(
                "[PauseMenu] Не назначены ссылки интерфейса.",
                this
            );

            enabled = false;
            return;
        }

        resumeButton.onClick.AddListener(Resume);
        mainMenuButton.onClick.AddListener(AskMainMenu);
        quitButton.onClick.AddListener(AskQuit);
        yesButton.onClick.AddListener(ConfirmExit);
        noButton.onClick.AddListener(CancelConfirmation);

        pauseWindow.SetActive(false);
        confirmationPanel.SetActive(false);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (pauseAction != null)
            pauseAction.Disable();

        ClosePause();
    }

    private void OnDestroy()
    {
        if (pauseAction != null)
        {
            pauseAction.performed -= OnPausePerformed;
            pauseAction.Dispose();
        }

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(Resume);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(AskMainMenu);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(AskQuit);

        if (yesButton != null)
            yesButton.onClick.RemoveListener(ConfirmExit);

        if (noButton != null)
            noButton.onClick.RemoveListener(CancelConfirmation);
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        if (exiting)
            return;

        if (!isPaused)
        {
            OpenPause();
        }
        else if (pendingExit != ExitTarget.None)
        {
            CancelConfirmation();
        }
        else
        {
            Resume();
        }
    }

    private bool CanOpenPause()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == mainMenuSceneName ||
            sceneName == bootSceneName)
        {
            return false;
        }

        PlayerController player = PlayerController.Instance;

        if (player == null ||
            player.Health == null ||
            player.Health.currentHealth <= 0 ||
            player.Movement == null ||
            !player.Movement.isActiveAndEnabled)
        {
            return false;
        }

        if (RespawnManager.Instance != null &&
            RespawnManager.Instance.IsRespawning)
        {
            return false;
        }

        if (SceneTransitionManager.Instance != null &&
            SceneTransitionManager.Instance.IsTransitioning)
        {
            return false;
        }

        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        // Не открываем паузу во время размещения игрока загрузчиком.
        if (body == null || !body.simulated)
            return false;

        // Не присваиваем себе остановку времени другой системы.
        return Time.timeScale > 0f;
    }

    public void OpenPause()
    {
        if (isPaused || exiting || !CanOpenPause())
            return;

        previousTimeScale = Time.timeScale;
        previousAudioPause = AudioListener.pause;
        previousCursorVisible = Cursor.visible;
        previousCursorLock = Cursor.lockState;

        lockedMovement = PlayerController.Instance.Movement;

        // Не меняем основной переключатель переходов и возрождения.
        lockedMovement.SetInputLock(this, true);

        isPaused = true;
        pendingExit = ExitTarget.None;

        Time.timeScale = 0f;

        if (pauseAudio)
            AudioListener.pause = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureEventSystem();

        mainPanel.SetActive(true);
        confirmationPanel.SetActive(false);
        statusText.text = string.Empty;
        pauseWindow.SetActive(true);

        SelectButton(resumeButton);
    }

    public void Resume()
    {
        if (!exiting)
            ClosePause();
    }

    private void ClosePause()
    {
        if (!isPaused)
            return;

        // Снимаем только собственную блокировку.
        if (lockedMovement != null)
            lockedMovement.SetInputLock(this, false);

        lockedMovement = null;

        Time.timeScale = previousTimeScale;

        if (pauseAudio)
            AudioListener.pause = previousAudioPause;

        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;

        isPaused = false;
        pendingExit = ExitTarget.None;

        if (pauseWindow != null)
            pauseWindow.SetActive(false);

        if (fallbackEventSystem != null)
            fallbackEventSystem.SetActive(false);
    }

    private void AskMainMenu()
    {
        ShowConfirmation(ExitTarget.MainMenu);
    }

    private void AskQuit()
    {
        ShowConfirmation(ExitTarget.Application);
    }

    private void ShowConfirmation(ExitTarget target)
    {
        if (!isPaused || exiting)
            return;

        pendingExit = target;
        statusText.text = string.Empty;

        string destination = target == ExitTarget.MainMenu
            ? "Выйти в главное меню?"
            : "Выйти из игры?";

        string saveWarning =
            exitSaveMode == ExitSaveMode.SaveBeforeExit
                ? "Перед выходом текущее состояние будет сохранено."
                : "Прогресс после последнего сохранения будет потерян.";

        confirmationText.text =
            destination + "\n\n" + saveWarning;

        mainPanel.SetActive(false);
        confirmationPanel.SetActive(true);

        SelectButton(noButton);
    }

    private void CancelConfirmation()
    {
        if (!isPaused || exiting)
            return;

        pendingExit = ExitTarget.None;
        statusText.text = string.Empty;

        confirmationPanel.SetActive(false);
        mainPanel.SetActive(true);

        SelectButton(resumeButton);
    }

    private void ConfirmExit()
    {
        if (!isPaused ||
            exiting ||
            pendingExit == ExitTarget.None)
        {
            return;
        }

        ExitTarget target = pendingExit;

        // Проверяем сцену до записи сохранения.
        if (target == ExitTarget.MainMenu &&
            (string.IsNullOrWhiteSpace(mainMenuSceneName) ||
             !Application.CanStreamedLevelBeLoaded(mainMenuSceneName)))
        {
            ShowError(
                "Главное меню недоступно. Проверь имя сцены " +
                "и её наличие в списке сцен сборки."
            );

            return;
        }

        if (exitSaveMode == ExitSaveMode.SaveBeforeExit &&
            !TrySaveBeforeExit())
        {
            return;
        }

        exiting = true;

        // Восстанавливаем время до загрузки меню или выхода.
        ClosePause();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (target == ExitTarget.MainMenu)
        {
            try
            {
                SceneManager.LoadScene(
                    mainMenuSceneName,
                    LoadSceneMode.Single
                );
            }
            catch (Exception exception)
            {
                exiting = false;

                OpenPause();
                ShowConfirmation(target);
                ShowError(
                    "Не удалось открыть главное меню: " +
                    exception.Message
                );
            }

            return;
        }

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Меняет только координаты в данных сохранения.
    /// Самого игрока перед выходом не перемещает.
    /// </summary>
    private void ApplyExitSavePosition(SaveData data)
    {
        if (exitSavePositionMode ==
            ExitSavePositionMode.CurrentPlayerPosition)
        {
            return;
        }

        RespawnManager respawns = RespawnManager.Instance;

        if (respawns == null)
        {
            throw new InvalidOperationException(
                "Не найден RespawnManager. Нельзя надёжно " +
                "определить активированный чекпоинт."
            );
        }

        Vector3 destination;

        if (!respawns.TryGetActivatedCheckpointInActiveScene(
                out destination))
        {
            SpawnPoint spawn = SpawnPoint.FindDefaultInActiveScene();

            if (spawn == null)
            {
                throw new InvalidOperationException(
                    "В текущей локации нет активированного " +
                    "чекпоинта и не найдена основная точка SpawnPoint."
                );
            }

            destination = spawn.GetSpawnPosition();
        }

        data.posX = destination.x;
        data.posY = destination.y;
        data.posZ = destination.z;
    }

    private bool TrySaveBeforeExit()
    {
        PlayerController player = PlayerController.Instance;
        SaveManager saves = SaveManager.Instance;
        int slot = PlayerPrefs.GetInt("CurrentSaveSlot", 0);

        if (player == null ||
            player.SaveHandler == null ||
            player.Health == null ||
            player.Health.currentHealth <= 0 ||
            saves == null ||
            slot < 1 ||
            slot > 3)
        {
            ShowError(
                "Нельзя сохранить текущее состояние. " +
                "Выход отменён."
            );

            return false;
        }

        try
        {
            SaveData data = player.SaveHandler.CollectSaveData();

            if (data == null)
                throw new InvalidOperationException(
                    "Не удалось собрать данные игрока.");
            ApplyExitSavePosition(data);

            saves.SaveGame(slot, data);
            return true;
        }
        catch (Exception exception)
        {
            ShowError(
                "Сохранение не удалось. Выход отменён.\n" +
                exception.Message
            );

            return false;
        }
    }

    private void ShowError(string message)
    {
        statusText.text = message;
        Debug.LogError("[PauseMenu] " + message, this);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Внешний переход не должен переносить паузу в другую сцену.
        if (mode == LoadSceneMode.Single)
            ClosePause();
    }

    private void EnsureEventSystem()
    {
        // Не создаём второй активный EventSystem.
        if (EventSystem.current != null)
            return;

        if (fallbackEventSystem == null)
        {
            fallbackEventSystem =
                new GameObject("PauseEventSystem");

            fallbackEventSystem.transform.SetParent(transform, false);
            fallbackEventSystem.AddComponent<EventSystem>();

            InputSystemUIInputModule module =
                fallbackEventSystem
                    .AddComponent<InputSystemUIInputModule>();

            module.AssignDefaultActions();
        }

        fallbackEventSystem.SetActive(true);
    }

    private void SelectButton(Button button)
    {
        if (EventSystem.current == null || button == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
