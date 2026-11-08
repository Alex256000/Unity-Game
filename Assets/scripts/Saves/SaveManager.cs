using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const int SlotCount = 3;

    private string saveFolderPath;

    // Только данные, подготовленные для загрузки игрока.
    // Просмотр ячеек в меню этот кеш не изменяет.
    public SaveData CurrentSaveData { get; private set; }

    private int loadedSlotIndex;

    // Время текущего прохождения не зависит от кеша загрузки.
    private bool sessionActive;
    private int activeSlotIndex;
    private double playTimeSeconds;

    private string bootSceneName;
    private string menuSceneName;

    private bool applicationPaused;

    private readonly JsonSerializerSettings jsonSettings =
        new JsonSerializerSettings
        {
            DateFormatString = "yyyy-MM-ddTHH:mm:ss"
        };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Менеджер должен сохраняться отдельно от объектов сцены.
        if (transform.parent != null)
            transform.SetParent(null, true);

        DontDestroyOnLoad(gameObject);

        saveFolderPath = Path.Combine(
            Application.persistentDataPath,
            "Saves"
        );

        Directory.CreateDirectory(saveFolderPath);
    }

    private void Update()
    {
        if (!sessionActive || applicationPaused)
            return;

        // Пауза через Time.timeScale останавливает счётчик.
        if (Time.timeScale <= 0f)
            return;

        string activeScene = SceneManager.GetActiveScene().name;

        if (activeScene == bootSceneName ||
            activeScene == menuSceneName)
        {
            return;
        }

        if (PlayerController.Instance == null)
            return;

        // Скорость игры не ускоряет счётчик реального времени.
        playTimeSeconds += Time.unscaledDeltaTime;
    }

    private void OnApplicationPause(bool paused)
    {
        applicationPaused = paused;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Начинает отсчёт времени выбранного прохождения.
    /// Файл сохранения здесь не создаётся.
    /// </summary>
    public void BeginSession(
        int slotIndex,
        int savedSeconds,
        string bootScene,
        string menuScene)
    {
        if (!IsValidSlot(slotIndex))
            return;

        activeSlotIndex = slotIndex;
        playTimeSeconds = Math.Max(0, savedSeconds);

        bootSceneName = bootScene;
        menuSceneName = menuScene;

        sessionActive = true;
    }

    /// <summary>
    /// Останавливает отсчёт, но не перезаписывает сохранение.
    /// </summary>
    public void EndSession()
    {
        sessionActive = false;
        activeSlotIndex = 0;
        playTimeSeconds = 0d;
    }

    public int GetCurrentPlayTimeSeconds()
    {
        return (int)Math.Min(
            int.MaxValue,
            Math.Floor(playTimeSeconds)
        );
    }

    /// <summary>
    /// Записывает состояние игрока и время прохождения.
    /// Ошибки записи передаются вызывающему коду.
    /// </summary>
    public void SaveGame(int slotIndex, SaveData data)
    {
        if (!IsValidSlot(slotIndex))
            return;

        if (data == null)
            throw new ArgumentNullException(nameof(data));

        if (sessionActive && activeSlotIndex == slotIndex)
            data.playTimeSeconds = GetCurrentPlayTimeSeconds();

        data.saveDate = DateTime.Now;
        data.saveSlotName = $"Слот {slotIndex}";

        Directory.CreateDirectory(saveFolderPath);

        string json = JsonConvert.SerializeObject(
            data,
            Formatting.Indented,
            jsonSettings
        );

        File.WriteAllText(GetSlotFilePath(slotIndex), json);

        // Обычная запись не подменяет подготовленную загрузку.
        Debug.Log($"[SaveManager] Слот {slotIndex} сохранён.");
    }

    /// <summary>
    /// Читает ячейку для отображения в меню.
    /// Не меняет CurrentSaveData.
    /// </summary>
    public SaveData ReadSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return null;

        string path = GetSlotFilePath(slotIndex);

        if (!File.Exists(path))
            return null;

        try
        {
            string json = File.ReadAllText(path);

            SaveData data =
                JsonConvert.DeserializeObject<SaveData>(
                    json,
                    jsonSettings
                );

            if (data == null)
            {
                Debug.LogError(
                    $"[SaveManager] В слоте {slotIndex} " +
                    "не удалось получить данные."
                );
            }

            return data;
        }
        catch (Exception exception)
        {
            Debug.LogError(
                $"[SaveManager] Не удалось прочитать слот " +
                $"{slotIndex}: {exception.Message}"
            );

            return null;
        }
    }

    /// <summary>
    /// Подготавливает данные для загрузчика игрока.
    /// </summary>
    public SaveData LoadGame(int slotIndex)
    {
        ClearCurrentSaveData();

        SaveData data = ReadSlot(slotIndex);

        if (data == null)
            return null;

        CurrentSaveData = data;
        loadedSlotIndex = slotIndex;

        return data;
    }

    public void ClearCurrentSaveData()
    {
        CurrentSaveData = null;
        loadedSlotIndex = 0;

        // Время здесь не сбрасываем:
        // загрузчик очищает кеш после появления игрока.
    }

    /// <summary>
    /// Удаляет только указанную ячейку.
    /// Ошибки удаления передаются вызывающему коду.
    /// </summary>
    public void DeleteSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
            return;

        string path = GetSlotFilePath(slotIndex);

        if (File.Exists(path))
            File.Delete(path);

        if (loadedSlotIndex == slotIndex)
            ClearCurrentSaveData();

        if (activeSlotIndex == slotIndex)
            EndSession();

        Debug.Log($"[SaveManager] Слот {slotIndex} очищен.");
    }

    public bool SlotExists(int slotIndex)
    {
        return IsValidSlot(slotIndex) &&
               File.Exists(GetSlotFilePath(slotIndex));
    }

    public SaveData CreateNewSaveData()
    {
        return new SaveData
        {
            saveDate = DateTime.Now,
            saveSlotName = "Новая игра",
            playTimeSeconds = 0
        };
    }

    private bool IsValidSlot(int slotIndex)
    {
        if (slotIndex >= 1 && slotIndex <= SlotCount)
            return true;

        Debug.LogError(
            $"[SaveManager] Неверный номер слота: {slotIndex}."
        );

        return false;
    }

    private string GetSlotFilePath(int slotIndex)
    {
        return Path.Combine(
            saveFolderPath,
            $"save_slot_{slotIndex}.json"
        );
    }
}
