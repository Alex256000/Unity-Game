using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinManager : MonoBehaviour
{
    // Синглтон - делает объект доступным из любого скрипта
    public static CoinManager Instance;
    
    // Ссылка на текстовый элемент UI
    public Text coinText;
    
    // Текущее количество собранных монет
    private int _coinsCollected;
    
    // Сохранённое количество (для сброса при смерти)
    private int _savedCoins;

    // Вызывается при создании объекта
    private void Awake()
    {
        // Если Instance ещё не назначен
        if (Instance == null)
        {
            Instance = this; // Делаем этот объект единственным CoinManager
            DontDestroyOnLoad(gameObject); // Запрещаем уничтожение при загрузке новой сцены
        }
        else
        {
            // Если уже есть другой CoinManager - уничтожаем этот
            Destroy(gameObject);
        }
    }

    // Добавляет 1 монетку и обновляет UI
    public void AddCoin()
    {
        _coinsCollected++;
        UpdateUI();
    }

    // Сбрасывает счётчик до последнего сохранённого значения (при смерти)
    public void ResetForDeath()
    {
        _coinsCollected = _savedCoins;
        UpdateUI();
    }

    // Сохраняет текущий прогресс (при достижении финиша)
    public void SaveProgress()
    {
        _savedCoins = _coinsCollected;
    }

    // Обновляет текст на экране
    private void UpdateUI()
    {
        if (coinText != null)
            coinText.text = $"Coins: {_coinsCollected}";
    }

    // Вызывается при загрузке нового уровня
    public void OnLevelLoaded()
    {
        // Переподключаем текстовый элемент, т.к. новая сцена
        coinText = GameObject.Find("CoinText").GetComponent<Text>();
        UpdateUI();
    }
}