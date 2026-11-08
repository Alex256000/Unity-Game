using UnityEngine;
using UnityEngine.InputSystem;

public class Teleporter : MonoBehaviour
{
    [Header("Настройки телепорта")]
    [SerializeField] private Transform destinationPoint;
    [SerializeField] private float teleportDelay = 0f; // 0 для мгновенного телепорта
    [SerializeField] private bool preserveMomentum = false;
    
    [Header("Визуальные эффекты")]
    [SerializeField] private GameObject teleportEffect;
    [SerializeField] private float effectDuration = 0.5f;
    
    [Header("Настройки ввода")]
    [SerializeField] private InputActionReference interactAction; // Ссылка на Action из InputSystem
    
    private bool playerInRange = false;
    private GameObject currentPlayer;
    private float teleportTimer = 0f;
    private bool isTeleporting = false;
    
    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
            interactAction.action.performed += OnInteractPerformed;
        }
    }
    
    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteractPerformed;
            interactAction.action.Disable();
        }
    }
    
    private void Update()
    {
        if (isTeleporting)
        {
            teleportTimer -= Time.deltaTime;
            if (teleportTimer <= 0f)
            {
                PerformTeleport();
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            currentPlayer = other.gameObject;
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            currentPlayer = null;
            isTeleporting = false;
        }
    }
    
    private void OnInteractPerformed(InputAction.CallbackContext context)
    {
        if (playerInRange && !isTeleporting && currentPlayer != null)
        {
            StartTeleport();
        }
    }
    
    private void StartTeleport()
    {
        isTeleporting = true;
        teleportTimer = teleportDelay;
        
        // Отключаем управление игроком на время телепорта
        DisablePlayerControl(true);
        
        // Визуальный эффект начала телепорта
        if (teleportEffect != null && teleportDelay > 0)
        {
            GameObject effect = Instantiate(teleportEffect, currentPlayer.transform.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
    }
    
    private void PerformTeleport()
    {
        if (currentPlayer == null) return;
        
        // Сохраняем скорость если нужно
        Vector2 velocity = Vector2.zero;
        Rigidbody2D rb = currentPlayer.GetComponent<Rigidbody2D>();
        if (rb != null && preserveMomentum)
        {
            velocity = rb.velocity;
        }
        
        // Перемещаем игрока
        currentPlayer.transform.position = destinationPoint.position;
        
        // Восстанавливаем скорость
        if (rb != null && preserveMomentum)
        {
            rb.velocity = velocity;
        }
        
        // Визуальный эффект прибытия
        if (teleportEffect != null)
        {
            GameObject effect = Instantiate(teleportEffect, destinationPoint.position, Quaternion.identity);
            Destroy(effect, effectDuration);
        }
        
        // Включаем управление обратно
        DisablePlayerControl(false);
        
        isTeleporting = false;
    }
    
    private void DisablePlayerControl(bool disable)
    {
        if (currentPlayer == null) return;
        
        // Отключаем PlayerInput если используется
        PlayerInput playerInput = currentPlayer.GetComponent<PlayerInput>();
        if (playerInput != null)
        {
            playerInput.enabled = !disable;
        }
        
        // Замораживаем Rigidbody2D
        Rigidbody2D rb = currentPlayer.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            if (disable)
            {
                rb.velocity = Vector2.zero;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
            else
            {
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
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
}