using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData
{
    // --- Сцена и позиция ---
    public string sceneName;
    public float posX, posY, posZ;
    // --- Последний активированный чекпоинт сцены сохранения ---
    // Эти координаты хранятся отдельно от позиции появления игрока.
    public bool hasActivatedCheckpoint;
    public string activatedCheckpointScene;
    public float activatedCheckpointX;
    public float activatedCheckpointY;
    public float activatedCheckpointZ;


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