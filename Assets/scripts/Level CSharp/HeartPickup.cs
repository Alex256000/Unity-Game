using UnityEngine;

public class HeartPickup : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int healAmount = 1; // Сколько здоровья восстанавливает
    [SerializeField] private bool destroyOnPickup = true;
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem pickupEffect;
    [SerializeField] private AudioClip pickupSound;
    
    [Header("Visual Feedback")]
    [SerializeField] private float floatAmplitude = 0.2f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float rotationSpeed = 50f;
    
    private Vector3 startPosition;
    private Transform visualTransform;
    
    private void Start()
    {
        // Сохраняем начальную позицию для парения
        startPosition = transform.position;
        
        // Находим визуальную часть (если есть дочерний объект с моделью)
        if (transform.childCount > 0)
        {
            visualTransform = transform.GetChild(0);
        }
        else
        {
            visualTransform = transform;
        }
    }
    
    private void Update()
    {
        // Анимация парения
        float yOffset = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        visualTransform.position = startPosition + new Vector3(0, yOffset, 0);
        
        // Вращение
        visualTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Проверяем, что это игрок
        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Восстанавливаем здоровье
                playerHealth.Heal(healAmount);
                
                // Эффекты
                PlayPickupEffects();
                
                // Уничтожаем предмет
                if (destroyOnPickup)
                {
                    Destroy(gameObject);
                }
                else
                {
                    // Или просто деактивируем для возможного повторного использования
                    gameObject.SetActive(false);
                }
            }
        }
    }
    
    private void PlayPickupEffects()
    {
        // Проигрываем звук
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Создаем эффекты частиц
        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }
    }
    
    // Метод для настройки количества здоровья (можно вызывать из инспектора)
    public void SetHealAmount(int amount)
    {
        healAmount = Mathf.Max(1, amount);
    }
}