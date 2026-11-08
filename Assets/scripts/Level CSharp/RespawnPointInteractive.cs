using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class RespawnPointInteractive : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("Тег объекта игрока")]
    public string playerTag = "Player";

    [Header("Activation")]
    [Tooltip("Если true — активируется автоматически при входе в триггер")]
    public bool activateOnTouch = false;

    [Tooltip("Если true — для активации требуется нажать кнопку Interact (в зоне триггера)")]
    public bool requireButtonPress = true;

    [Tooltip("Имя Action в Input Actions, используемое для взаимодействия.")]
    public string interactActionName = "Interact";

    [Header("Save Settings")]
    [Tooltip("В какой слот сохранять игру при активации чекпоинта")]
    [Range(1, 3)]
    public int saveSlot = 1;  // ВАЖНО: Выбор слота для сохранения!

    [Header("Visuals / Audio")]
    public Sprite activatedSprite;
    public GameObject activationParticlesPrefab;
    public AudioClip activationSound;
    public float activationDelay = 0.05f;

    [Header("Optional")]
    public Animator animator;
    public string animatorTriggerName = "Activate";
    public UnityEvent OnActivated;

    private SpriteRenderer sr;
    private bool activated = false;
    private bool playerInRange = false;
    private PlayerInput cachedPlayerInput = null;
    private InputAction interactAction = null;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (activated) return;
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        cachedPlayerInput = other.GetComponent<PlayerInput>();
        if (cachedPlayerInput != null && requireButtonPress)
        {
            interactAction = cachedPlayerInput.actions.FindAction(interactActionName);
            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }
            else
            {
                Debug.LogWarning($"[RespawnPointInteractive] Interact action '{interactActionName}' not found.");
            }
        }

        if (activateOnTouch && !requireButtonPress)
        {
            ActivateSequence();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;
        UnsubscribeInteract();
    }

    private void Update()
    {
        if (activated) return;

        if (playerInRange && requireButtonPress && interactAction == null)
        {
            if (Keyboard.current != null)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    ActivateSequence();
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.E))
                    ActivateSequence();
            }
        }
    }

    private void OnInteractPerformed(InputAction.CallbackContext ctx)
    {
        ActivateSequence();
    }

    private void UnsubscribeInteract()
    {
        if (interactAction != null)
        {
            interactAction.performed -= OnInteractPerformed;
            interactAction = null;
        }
        cachedPlayerInput = null;
    }

    private void ActivateSequence()
    {
        if (activated || Time.timeScale <= 0f) return;
        activated = true;

        UnsubscribeInteract();

        // Визуальные эффекты
        if (activationParticlesPrefab != null)
        {
            var p = Instantiate(activationParticlesPrefab, transform.position, Quaternion.identity);
            Destroy(p, 3f);
        }
        if (activationSound != null)
        {
            AudioSource.PlayClipAtPoint(activationSound, transform.position);
        }

        if (animator != null && !string.IsNullOrEmpty(animatorTriggerName))
        {
            animator.SetTrigger(animatorTriggerName);
        }

        if (sr != null && activatedSprite != null)
        {
            sr.sprite = activatedSprite;
        }

        if (activationDelay > 0f)
            Invoke(nameof(FinishActivate), activationDelay);
        else
            FinishActivate();
    }

    private void FinishActivate()
    {
        // 1. Сохраняем чекпоинт в RespawnManager (для возрождения после смерти)
        if (RespawnManager.Instance != null)
        {
            RespawnManager.Instance.RegisterActivatedCheckpoint(transform.position);
            Debug.Log($"[RespawnPointInteractive] Чекпоинт установлен в RespawnManager: {transform.position}");
        }
        else
        {
            Debug.LogWarning("[RespawnPointInteractive] RespawnManager не найден!");
        }

        // 2. Сохраняем игру в выбранный слот через SaveManager
        SaveGameToSlot();

        OnActivated?.Invoke();

        var col = GetComponent<Collider2D>();
        if (col != null) col.enabled = false;

        Debug.Log($"[RespawnPointInteractive] Чекпоинт активирован и игра сохранена в слот {saveSlot}");
    }

    private void SaveGameToSlot()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogError("[RespawnPointInteractive] SaveManager не найден!");
            return;
        }

        PlayerSaveHandler saveHandler = FindAnyObjectByType<PlayerSaveHandler>();
        if (saveHandler == null)
        {
            Debug.LogError("[RespawnPointInteractive] PlayerSaveHandler не найден!");
            return;
        }

        // Получаем текущий слот из PlayerPrefs (если есть) или используем saveSlot по умолчанию
        int currentSlot = PlayerPrefs.GetInt("CurrentSaveSlot", saveSlot);
    
        SaveData data = saveHandler.CollectSaveData();

        if (data == null)
        {
            Debug.LogError(
                "[RespawnPointInteractive] Не удалось собрать " +
                "данные для сохранения."
            );

            return;
        }

        // При обычном сохранении на чекпоинте
        // появляемся в точке чекпоинта, а не на краю его триггера.
        Vector3 checkpoint = transform.position;

        data.posX = checkpoint.x;
        data.posY = checkpoint.y;
        data.posZ = checkpoint.z;

        SaveManager.Instance.SaveGame(currentSlot, data);


        Debug.Log($"[RespawnPointInteractive] Игра сохранена в слот {currentSlot}.");
    }

    private void OnDisable()
    {
        UnsubscribeInteract();
    }

    public void ActivateNow()
    {
        if (!activated) ActivateSequence();
    }
}