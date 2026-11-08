using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerSoulSystem))]
public class PlayerSaveHandler : MonoBehaviour
{
    private PlayerHealth playerHealth;
    private PlayerSoulSystem playerSoul;
    private PlayerMovement playerMovement;
    private PlayerHealInput playerHeal;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (playerHealth == null)
            playerHealth = GetComponent<PlayerHealth>();

        if (playerSoul == null)
            playerSoul = GetComponent<PlayerSoulSystem>();

        if (playerMovement == null)
            playerMovement = GetComponent<PlayerMovement>();

        if (playerHeal == null)
            playerHeal = GetComponent<PlayerHealInput>();
    }

    /// <summary>
    /// Применяет сохранённые данные к игроку.
    /// Сменой сцены занимается загрузчик, не этот компонент.
    /// </summary>
    public void ApplyLoadedData(SaveData data)
    {
        if (data == null)
        {
            Debug.LogError(
                "[PlayerSaveHandler] Получены пустые данные.",
                this
            );

            return;
        }

        CacheComponents();

        // Не применяем координаты сохранения к посторонней сцене.
        if (!string.IsNullOrEmpty(data.sceneName) &&
            data.sceneName != SceneManager.GetActiveScene().name)
        {
            Debug.LogWarning(
                "[PlayerSaveHandler] Сначала нужно загрузить сцену сохранения.",
                this
            );

            return;
        }

        // Не переносим незавершённое лечение в загруженное состояние.
        if (playerHeal != null)
            playerHeal.CancelHeal();

        if (playerMovement != null)
            playerMovement.ResetMovementState();

        // Сцена и позиция.
        Vector3 savedPosition = new Vector3(
            data.posX,
            data.posY,
            data.posZ
        );

        transform.position = savedPosition;

        Rigidbody2D body = GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.position = savedPosition;
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        // Единственный источник здоровья — PlayerHealth.
        playerHealth.SetHealth(data.health);

        // Восстанавливаем точное значение, а не только положительную разницу.
        playerSoul.SetSoul(data.souls);

        // Восстанавливаем отдельно от координат появления:
        // позиция сохранения не обязательно является чекпоинтом.
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance
                .RestoreActivatedCheckpointFromSave(data);
        }


        // --- Инвентарь: заготовка для будущей системы ---
        // if (InventoryManager.Instance != null)
        // {
        //     InventoryManager.Instance.Clear();
        //     foreach (string item in data.inventoryItems)
        //         InventoryManager.Instance.AddItem(item);
        // }

        // --- Коллекционные предметы: пока не подключены ---
        // CollectibleTracker.Instance?.LoadFromSave(data.collectedIDs);

        // --- Валюта: пока не подключена к SaveData ---
        // Wallet.Instance?.SetCurrency(data.currency);

        // --- Квесты: пока не подключены ---
        // QuestManager.Instance?.SetProgress(data.questProgress);
    }

    /// <summary>
    /// Собирает состояние игрока.
    /// Запись на диск выполняет SaveManager.
    /// </summary>
    public SaveData CollectSaveData()
    {
        CacheComponents();

        SaveData data = new SaveData
        {
            sceneName = SceneManager.GetActiveScene().name,
            posX = transform.position.x,
            posY = transform.position.y,
            posZ = transform.position.z,
            health = playerHealth.currentHealth,
            souls = playerSoul.GetCurrentSoul()
        };

        // Запоминаем только настоящий активированный чекпоинт
        // текущей локации, а не произвольную позицию игрока.
        RespawnManager respawns = RespawnManager.Instance;

        if (respawns != null &&
            respawns.TryGetActivatedCheckpointInActiveScene(
                out Vector3 checkpoint))
        {
            data.hasActivatedCheckpoint = true;
            data.activatedCheckpointScene = data.sceneName;
            data.activatedCheckpointX = checkpoint.x;
            data.activatedCheckpointY = checkpoint.y;
            data.activatedCheckpointZ = checkpoint.z;
        }


        // --- Инвентарь: заготовка для будущей системы ---
        // data.inventoryItems = InventoryManager.Instance?.GetAllItems()
        //     ?? new System.Collections.Generic.List<string>();

        // --- Коллекционные предметы: пока не подключены ---
        // data.collectedIDs = CollectibleTracker.Instance?.GetCollectedIDs()
        //     ?? new System.Collections.Generic.List<string>();

        // --- Валюта: пока не подключена ---
        // data.currency = Wallet.Instance?.GetCurrency() ?? 0;

        // --- Квесты: пока не подключены ---
        // data.questProgress = QuestManager.Instance?.GetProgress() ?? 0;

        return data;
    }
}
