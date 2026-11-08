using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class AutoTeleporter : MonoBehaviour
{
    [Header("Настройки телепорта")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float teleportDelay = 0.5f;
    [SerializeField] private bool preserveMomentum = false;
    [SerializeField] private bool oneTimeUse = false; // Одноразовый телепорт
    
    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject startTeleportEffect;
    [SerializeField] private GameObject endTeleportEffect;
    [SerializeField] private float effectDuration = 0.5f;
    
    private bool isTeleporting = false;
    private bool hasBeenUsed = false;
    private GameObject currentPlayer;
    private PlayerInput playerInput;
    private Rigidbody2D playerRb;
    private MonoBehaviour[] playerScripts;
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && !isTeleporting && (!oneTimeUse || !hasBeenUsed))
        {
            currentPlayer = other.gameObject;
            StartCoroutine(TeleportSequence(currentPlayer));
        }
    }
    
    private IEnumerator TeleportSequence(GameObject player)
    {
        isTeleporting = true;
        
        // Получаем компоненты игрока
        playerInput = player.GetComponent<PlayerInput>();
        playerRb = player.GetComponent<Rigidbody2D>();
        
        // Сохраняем скорость если нужно
        Vector2 savedVelocity = Vector2.zero;
        if (playerRb != null && preserveMomentum)
        {
            savedVelocity = playerRb.velocity;
        }
        
        // Блокируем управление и останавливаем игрока
        DisablePlayerControl(true);
        
        // Эффект начала телепорта
        if (startTeleportEffect != null)
        {
            GameObject effect = Instantiate(startTeleportEffect, player.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Ждем указанную задержку
        yield return new WaitForSeconds(teleportDelay);
        
        // Проверяем, не уничтожен ли игрок за время ожидания
        if (player == null)
        {
            isTeleporting = false;
            yield break;
        }
        
        // Перемещаем игрока
        player.transform.position = destinationPoint.position;
        
        // Восстанавливаем скорость если нужно
        if (playerRb != null && preserveMomentum)
        {
            playerRb.velocity = savedVelocity;
        }
        
        // Эффект в точке назначения
        if (endTeleportEffect != null)
        {
            GameObject effect = Instantiate(endTeleportEffect, destinationPoint.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Возвращаем управление
        DisablePlayerControl(false);
        
        if (oneTimeUse)
        {
            hasBeenUsed = true;
        }
        
        isTeleporting = false;
    }
    
    private void DisablePlayerControl(bool disable)
    {
        if (currentPlayer == null) return;
        
        // Отключаем PlayerInput
        if (playerInput != null)
        {
            playerInput.enabled = !disable;
        }
        
        // Останавливаем физику и замораживаем Rigidbody2D
        if (playerRb != null)
        {
            if (disable)
            {
                playerRb.velocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                // Возвращаем обычные ограничения (обычно только заморозка вращения)
                playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
        
        // Опционально: отключаем скрипты движения
        if (disable)
        {
            // Можно отключить конкретные скрипты управления
            // playerScripts = player.GetComponents<MonoBehaviour>();
            // foreach (var script in playerScripts)
            // {
            //     if (script != this && script.enabled && script is not AutoTeleporter)
            //     {
            //         script.enabled = false;
            //     }
            // }
        }
        else
        {
            // Включаем обратно
            // if (playerScripts != null)
            // {
            //     foreach (var script in playerScripts)
            //     {
            //         if (script != null)
            //         {
            //             script.enabled = true;
            //         }
            //     }
            // }
        }
    }
    
    // Визуализация в редакторе
    private void OnDrawGizmos()
    {
        if (destinationPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destinationPoint.position);
            Gizmos.DrawWireSphere(destinationPoint.position, 0.3f);
        }
        

    }
    
    // Сброс состояния (можно вызвать извне для повторного использования)
    public void ResetTeleporter()
    {
        hasBeenUsed = false;
        isTeleporting = false;
    }
}