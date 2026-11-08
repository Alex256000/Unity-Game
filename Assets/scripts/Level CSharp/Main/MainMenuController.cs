using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.IO;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Containers")]
    public GameObject buttonsContainer;
    public GameObject savesContainer;
    
    [Header("Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button exitButton;
    public Button[] saveSlots = new Button[3];
    public Button backButton;
    
    [Header("Settings")]
    public string firstSceneName = "GameScene";
    
    [Header("Save System")]
    public string saveFolderName = "Saves";
    public string saveFileExtension = ".json";
    
    private string savePath;
    
    void Awake()
    {
        // Устанавливаем путь для сохранений
        savePath = Path.Combine(Application.persistentDataPath, saveFolderName);
        
        // Создаем папку, если её нет
        if (!Directory.Exists(savePath))
        {
            Directory.CreateDirectory(savePath);
        }
    }
    
    void Start()
    {
        SetupButtonListeners();
        UpdateSaveSlotsVisuals();
    }
    
    void SetupButtonListeners()
    {
        // Основные кнопки
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);
            
        // Слоты сохранения
        for (int i = 0; i < saveSlots.Length; i++)
        {
            int slotIndex = i; // Захватываем значение для замыкания
            if (saveSlots[i] != null)
            {
                saveSlots[i].onClick.AddListener(() => OnSaveSlotClicked(slotIndex));
                
                // Добавляем возможность удаления сохранения по правому клику (опционально)
                saveSlots[i].onClick.AddListener(() => OnSaveSlotRightClick(slotIndex));
            }
        }
        
        // Кнопка назад
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }
    
    void OnPlayClicked()
    {
        // Показываем слоты сохранения, скрываем основные кнопки
        buttonsContainer.SetActive(false);
        savesContainer.SetActive(true);
        UpdateSaveSlotsVisuals();
    }
    
    void OnSaveSlotClicked(int slotIndex)
    {
        // Создаем новые данные сохранения
        SaveData newSave = new SaveData();
        newSave.sceneName = firstSceneName;
        newSave.posX = 0;
        newSave.posY = 0;
        newSave.posZ = 0;
        newSave.health = 100; // Начальное здоровье
        newSave.saveDate = DateTime.Now;
        newSave.saveSlotName = $"Слот {slotIndex + 1}";
        newSave.playTimeSeconds = 0;
        
        // Сохраняем данные в файл
        SaveGame(slotIndex, newSave);
        
        // Сохраняем выбранный слот в PlayerPrefs для последующей загрузки
        PlayerPrefs.SetInt("SelectedSaveSlot", slotIndex);
        PlayerPrefs.SetInt("IsNewGame", 1); // 1 - новая игра
        PlayerPrefs.Save();
        
        // Загружаем первую сцену
        SceneManager.LoadScene(firstSceneName);
    }
    
    void OnSaveSlotRightClick(int slotIndex)
    {
        // Опционально: удаление сохранения по правому клику
        if (Input.GetMouseButton(1)) // Правая кнопка мыши
        {
            DeleteSave(slotIndex);
            UpdateSaveSlotsVisuals();
        }
    }
    
    void OnSettingsClicked()
    {
        // Пока без функционала
        Debug.Log("Settings clicked - not implemented yet");
    }
    
    void OnExitClicked()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnBackClicked()
    {
        // Возвращаемся к основным кнопкам
        savesContainer.SetActive(false);
        buttonsContainer.SetActive(true);
    }
    
    void UpdateSaveSlotsVisuals()
    {
        for (int i = 0; i < saveSlots.Length; i++)
        {
            if (saveSlots[i] != null)
            {
                Text slotText = saveSlots[i].GetComponentInChildren<Text>();
                Button slotButton = saveSlots[i].GetComponent<Button>();
                
                if (slotText != null)
                {
                    // Проверяем, существует ли сохранение
                    if (SaveExists(i))
                    {
                        SaveData data = LoadGame(i);
                        if (data != null)
                        {
                            // Форматируем время игры
                            TimeSpan playTime = TimeSpan.FromSeconds(data.playTimeSeconds);
                            string timeString = $"{(int)playTime.TotalHours:D2}:{playTime.Minutes:D2}:{playTime.Seconds:D2}";
                            
                            slotText.text = $"{data.saveSlotName}\n{data.saveDate:dd.MM.yyyy HH:mm}\nВремя: {timeString}";
                            
                            // Меняем цвет кнопки для занятого слота
                            if (slotButton != null)
                            {
                                ColorBlock colors = slotButton.colors;
                                colors.normalColor = new Color(0.3f, 0.5f, 0.3f); // Зеленоватый для занятого слота
                                slotButton.colors = colors;
                            }
                        }
                    }
                    else
                    {
                        slotText.text = $"Сохранение {i + 1}\n[Пусто]";
                        
                        // Возвращаем стандартный цвет для пустого слота
                        if (slotButton != null)
                        {
                            ColorBlock colors = slotButton.colors;
                            colors.normalColor = new Color(0.4f, 0.4f, 0.4f); // Серый для пустого слота
                            slotButton.colors = colors;
                        }
                    }
                }
            }
        }
    }
    
    // Методы для работы с сохранениями
    
    string GetSaveFilePath(int slotIndex)
    {
        return Path.Combine(savePath, $"save_{slotIndex}{saveFileExtension}");
    }
    
    bool SaveExists(int slotIndex)
    {
        return File.Exists(GetSaveFilePath(slotIndex));
    }
    
    public void SaveGame(int slotIndex, SaveData saveData)
    {
        try
        {
            string jsonData = JsonUtility.ToJson(saveData, true);
            File.WriteAllText(GetSaveFilePath(slotIndex), jsonData);
            Debug.Log($"Game saved to slot {slotIndex}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game: {e.Message}");
        }
    }
    
    public SaveData LoadGame(int slotIndex)
    {
        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                string jsonData = File.ReadAllText(filePath);
                SaveData saveData = JsonUtility.FromJson<SaveData>(jsonData);
                Debug.Log($"Game loaded from slot {slotIndex}");
                return saveData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game: {e.Message}");
        }
        
        return null;
    }
    
    public void DeleteSave(int slotIndex)
    {
        try
        {
            string filePath = GetSaveFilePath(slotIndex);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Save deleted from slot {slotIndex}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save: {e.Message}");
        }
    }
}