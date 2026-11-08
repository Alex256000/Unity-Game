using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // --- Сцена и позиция ---
    public string sceneName;
    public float posX, posY, posZ;

    // --- Здоровье ---
    public int health;

    //Душа
     public int souls;

    // --- Инвентарь (пока закомментировано) ---
    // public List<string> inventoryItems = new List<string>();

    // --- Коллекционные предметы (пока закомментировано) ---
    // public List<string> collectedIDs = new List<string>();

    // --- Валюта (пока закомментировано) ---
    // public int currency;

    // --- Глобальный квест (пока закомментировано) ---
    // public int questProgress;

    // --- Метаданные ---
    public DateTime saveDate;
    public string saveSlotName; // "Автосохранение", "Слот 1" и т.д.
    public int playTimeSeconds;
}