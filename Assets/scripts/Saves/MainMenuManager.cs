using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Интерфейс")]
    [SerializeField] private GameObject menuInterface;
    [SerializeField] private GameObject mainButtonsContainer;
    [SerializeField] private GameObject slotsContainer;

    [Header("Основные кнопки")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button backButton;

    [Header("Ячейки: строго в порядке 1, 2, 3")]
    [SerializeField] private SaveSlotView[] slots =
        new SaveSlotView[3];

    [Header("Подтверждение удаления")]
    [SerializeField] private GameObject deleteConfirmationPanel;
    [SerializeField] private TextMeshProUGUI confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Сообщения меню")]
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Сцены")]
    [SerializeField] private string firstLevelSceneName = "Level_01";
    [SerializeField] private string bootSceneName = "PreLoadScene";

    private bool loading;
    private bool confirmationOpen;
    private int pendingDeleteSlot;

    private void Start()
    {
        if (!ValidateReferences())
        {
            enabled = false;
            return;
        }

        SaveManager.Instance.EndSession();
        SaveManager.Instance.ClearCurrentSaveData();

        ClearLaunchFlags();

        playButton.onClick.AddListener(ShowSlots);
        settingsButton.onClick.AddListener(OpenSettings);
        exitButton.onClick.AddListener(QuitGame);
        backButton.onClick.AddListener(ShowMainButtons);

        yesButton.onClick.AddListener(ConfirmDelete);
        noButton.onClick.AddListener(CancelDelete);

        for (int i = 0; i < slots.Length; i++)
            slots[i].Initialize(this, i + 1);

        deleteConfirmationPanel.SetActive(false);
        menuInterface.SetActive(true);

        ShowMainButtons();
    }

    private bool ValidateReferences()
    {
        bool valid =
            SaveManager.Instance != null &&
            menuInterface != null &&
            mainButtonsContainer != null &&
            slotsContainer != null &&
            playButton != null &&
            settingsButton != null &&
            exitButton != null &&
            backButton != null &&
            deleteConfirmationPanel != null &&
            confirmationText != null &&
            yesButton != null &&
            noButton != null &&
            slots != null &&
            slots.Length == 3;

        if (valid)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null || !slots[i].IsConfigured)
                {
                    valid = false;
                    break;
                }

                // Один объект нельзя назначить двум ячейкам.
                for (int j = 0; j < i; j++)
                {
                    if (slots[i] == slots[j])
                    {
                        valid = false;
                        break;
                    }
                }
            }
        }

        if (!valid)
        {
            Debug.LogError(
                "[MainMenu] Проверь SaveManager, ссылки меню " +
                "и настройку всех трёх SaveSlotView.",
                this
            );
        }

        return valid;
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(ShowSlots);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OpenSettings);

        if (exitButton != null)
            exitButton.onClick.RemoveListener(QuitGame);

        if (backButton != null)
            backButton.onClick.RemoveListener(ShowMainButtons);

        if (yesButton != null)
            yesButton.onClick.RemoveListener(ConfirmDelete);

        if (noButton != null)
            noButton.onClick.RemoveListener(CancelDelete);
    }

    public void ShowMainButtons()
    {
        if (loading || confirmationOpen)
            return;

        mainButtonsContainer.SetActive(true);
        slotsContainer.SetActive(false);

        SetStatus(string.Empty);
    }

    public void ShowSlots()
    {
        if (loading || confirmationOpen)
            return;

        mainButtonsContainer.SetActive(false);
        slotsContainer.SetActive(true);

        SetStatus(string.Empty);
        RefreshSlotDisplay();
    }

    public void RefreshSlotDisplay()
    {
        SaveManager saves = SaveManager.Instance;

        if (saves == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            int slotIndex = i + 1;
            bool exists = saves.SlotExists(slotIndex);

            if (!exists)
            {
                slots[i].SetState(false, string.Empty);
                continue;
            }

            SaveData data = saves.ReadSlot(slotIndex);

            string description = data != null
                ? $"Слот {slotIndex}\n" +
                  FormatPlayTime(data.playTimeSeconds)
                : $"Слот {slotIndex}\n" +
                  "Не удалось прочитать сохранение";

            // Нечитаемый файл не считаем пустой ячейкой.
            // Это защищает его от случайной новой игры поверх.
            slots[i].SetState(true, description);
        }
    }

    private string FormatPlayTime(int seconds)
    {
        seconds = Math.Max(0, seconds);

        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;

        return $"{hours} ч {minutes:00} мин";
    }

    public void OpenSlot(int slotIndex)
    {
        if (!CanUseSlot(slotIndex))
            return;

        if (SaveManager.Instance.SlotExists(slotIndex))
            LoadSlot(slotIndex);
        else
            StartNewGameInSlot(slotIndex);
    }

    public void LoadSlot(int slotIndex)
    {
        if (!CanUseSlot(slotIndex))
            return;

        SaveData data = SaveManager.Instance.LoadGame(slotIndex);

        if (data == null)
        {
            SetStatus(
                $"Не удалось прочитать сохранение в слоте {slotIndex}."
            );

            return;
        }

        BeginGame(
            slotIndex,
            data.sceneName,
            false,
            data.playTimeSeconds
        );
    }

    public void StartNewGameInSlot(int slotIndex)
    {
        if (!CanUseSlot(slotIndex))
            return;

        if (SaveManager.Instance.SlotExists(slotIndex))
        {
            SetStatus(
                "Этот слот занят. Сначала очисти его " +
                "через кнопку удаления."
            );

            return;
        }

        SaveManager.Instance.ClearCurrentSaveData();

        // Стартовый файл создаёт PlayerBootstrapper
        // после размещения игрока на реальной SpawnPoint.
        BeginGame(
            slotIndex,
            firstLevelSceneName,
            true,
            0
        );
    }

    private void BeginGame(
        int slotIndex,
        string targetScene,
        bool isNewGame,
        int savedSeconds)
    {
        if (string.IsNullOrWhiteSpace(targetScene) ||
            !Application.CanStreamedLevelBeLoaded(targetScene))
        {
            SetStatus(
                $"Сцена '{targetScene}' недоступна. " +
                "Проверь имя и список сцен сборки."
            );

            return;
        }

        if (string.IsNullOrWhiteSpace(bootSceneName) ||
            !Application.CanStreamedLevelBeLoaded(bootSceneName))
        {
            SetStatus(
                $"Сцена предзагрузки '{bootSceneName}' " +
                "недоступна."
            );

            return;
        }

        SaveManager saves = SaveManager.Instance;

        saves.BeginSession(
            slotIndex,
            savedSeconds,
            bootSceneName,
            gameObject.scene.name
        );

        PlayerPrefs.SetString("TargetScene", targetScene);
        PlayerPrefs.SetInt("CurrentSaveSlot", slotIndex);
        PlayerPrefs.SetInt("IsNewGame", isNewGame ? 1 : 0);
        PlayerPrefs.SetInt("LoadFromMenu", 1);
        PlayerPrefs.Save();

        loading = true;

        float previousTimeScale = Time.timeScale;
        Time.timeScale = 1f;

        try
        {
            SceneManager.LoadScene(bootSceneName);
        }
        catch (Exception exception)
        {
            loading = false;
            Time.timeScale = previousTimeScale;

            saves.EndSession();
            saves.ClearCurrentSaveData();
            ClearLaunchFlags();

            SetStatus(
                "Не удалось запустить игру: " +
                exception.Message
            );
        }
    }

    /// <summary>
    /// Вызывается крестиком конкретной ячейки.
    /// Здесь файл ещё не удаляется.
    /// </summary>
    public void DeleteSlot(int slotIndex)
    {
        if (!CanUseSlot(slotIndex))
            return;

        if (!SaveManager.Instance.SlotExists(slotIndex))
        {
            RefreshSlotDisplay();
            return;
        }

        pendingDeleteSlot = slotIndex;
        confirmationOpen = true;

        confirmationText.text =
            "Вы уверены, что хотите очистить " +
            "этот слот сохранения?\n\n" +
            $"Слот {slotIndex}\n" +
            "Это действие нельзя отменить.";

        // Панель подтверждения находится ВНЕ MenuInterface.
        menuInterface.SetActive(false);
        deleteConfirmationPanel.SetActive(true);

        // При управлении клавиатурой безопаснее выбрать «Нет».
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(
                noButton.gameObject
            );
        }
    }

    private void ConfirmDelete()
    {
        if (!confirmationOpen || pendingDeleteSlot == 0)
            return;

        try
        {
            SaveManager.Instance.DeleteSlot(pendingDeleteSlot);
        }
        catch (Exception exception)
        {
            confirmationText.text =
                $"Не удалось очистить слот {pendingDeleteSlot}.\n\n" +
                "Нажмите «Нет», чтобы вернуться в меню.";

            Debug.LogError(
                "[MainMenu] Ошибка удаления: " +
                exception.Message,
                this
            );

            return;
        }

        CloseConfirmation();
    }

    private void CancelDelete()
    {
        if (confirmationOpen)
            CloseConfirmation();
    }

    private void CloseConfirmation()
    {
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);

        deleteConfirmationPanel.SetActive(false);

        pendingDeleteSlot = 0;
        confirmationOpen = false;

        menuInterface.SetActive(true);

        SetStatus(string.Empty);
        RefreshSlotDisplay();
    }

    private bool CanUseSlot(int slotIndex)
    {
        return
            enabled &&
            !loading &&
            !confirmationOpen &&
            SaveManager.Instance != null &&
            slotIndex >= 1 &&
            slotIndex <= 3;
    }

    private void ClearLaunchFlags()
    {
        PlayerPrefs.DeleteKey("LoadFromMenu");
        PlayerPrefs.DeleteKey("TargetScene");
        PlayerPrefs.DeleteKey("IsNewGame");
        PlayerPrefs.DeleteKey("CurrentSaveSlot");
        PlayerPrefs.Save();
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (!string.IsNullOrEmpty(message))
            Debug.LogWarning("[MainMenu] " + message, this);
    }

    private void OpenSettings()
    {
        SetStatus("Настройки пока не подключены.");
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
