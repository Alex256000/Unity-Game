using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class AutoTeleporterAdvanced : MonoBehaviour
{
    [Header("Настройки телепорта")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float teleportDelay = 0.5f;
    [SerializeField] private bool preserveMomentum = false;
    [SerializeField] private bool oneTimeUse = false;
    
    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject startTeleportEffect;
    [SerializeField] private GameObject endTeleportEffect;
    [SerializeField] private float effectDuration = 0.5f;
    [SerializeField] private string teleportStartAnimation = "TeleportStart";
    [SerializeField] private string teleportEndAnimation = "TeleportEnd";
    
    private bool isTeleporting = false;
    private bool hasBeenUsed = false;
    private GameObject currentPlayer;
    private PlayerInput playerInput;
    private Rigidbody2D playerRb;
    private Animator playerAnimator;
    
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
        
        // Получаем компоненты
        playerInput = player.GetComponent<PlayerInput>();
        playerRb = player.GetComponent<Rigidbody2D>();
        playerAnimator = player.GetComponent<Animator>();
        
        Vector2 savedVelocity = Vector2.zero;
        if (playerRb != null && preserveMomentum)
        {
            savedVelocity = playerRb.velocity;
        }
        
        // Блокируем управление
        DisablePlayerControl(true);
        
        // Запускаем анимацию начала телепорта
        if (playerAnimator != null && !string.IsNullOrEmpty(teleportStartAnimation))
        {
            playerAnimator.SetTrigger(teleportStartAnimation);
        }
        
        // Эффект начала телепорта
        if (startTeleportEffect != null)
        {
            GameObject effect = Instantiate(startTeleportEffect, player.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        yield return new WaitForSeconds(teleportDelay);
        
        if (player == null)
        {
            isTeleporting = false;
            yield break;
        }
        
        // Перемещаем игрока
        player.transform.position = destinationPoint.position;
        
        if (playerRb != null && preserveMomentum)
        {
            playerRb.velocity = savedVelocity;
        }
        
        // Анимация появления
        if (playerAnimator != null && !string.IsNullOrEmpty(teleportEndAnimation))
        {
            playerAnimator.SetTrigger(teleportEndAnimation);
        }
        
        // Эффект в точке назначения
        if (endTeleportEffect != null)
        {
            GameObject effect = Instantiate(endTeleportEffect, destinationPoint.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Небольшая задержка перед возвратом управления
        yield return new WaitForSeconds(0.1f);
        
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
        
        if (playerInput != null)
        {
            playerInput.enabled = !disable;
        }
        
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
                playerRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }
    }
    
    private void OnDrawGizmos()
    {
        if (destinationPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, destinationPoint.position);
            Gizmos.DrawWireSphere(destinationPoint.position, 0.3f);
        }
    }
    
    public void ResetTeleporter()
    {
        hasBeenUsed = false;
        isTeleporting = false;
    }
}