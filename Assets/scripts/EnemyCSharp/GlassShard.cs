using UnityEngine;

public class GlassShard : MonoBehaviour
{
    [Header("Settings")]
    public float lifetime = 5f;
    public float damageRadius = 0.3f;
    public int damage = 1;
    public AudioSource shatterSound;
    public GameObject visualEffect;
    
    [Header("Physics")]
    public float gravityScale = 1f;
    public float initialSpreadForce = 5f;
    
    private float spawnTime;
    private bool isActive = true;
    private Rigidbody2D rb;
    private CircleCollider2D damageCollider;
    private float damageCooldown = 0f;
    private const float DAMAGE_INTERVAL = 0.2f; // Защита от множественного урона за один кадр
    
    void Start()
    {
        spawnTime = Time.time;
        
        // Получаем или добавляем компоненты
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            // В 2023 API правильное значение для блокировки вращения в 2D:
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            rb.gravityScale = gravityScale;
        }
        else
        {
            // Если Rigidbody уже есть — убедимся, что gravityScale установлен
            rb.gravityScale = gravityScale;
        }
        
        damageCollider = GetComponent<CircleCollider2D>();
        if (damageCollider == null)
        {
            damageCollider = gameObject.AddComponent<CircleCollider2D>();
            damageCollider.isTrigger = true;
            damageCollider.radius = damageRadius;
        }
        else
        {
            damageCollider.isTrigger = true;
            damageCollider.radius = damageRadius;
        }
        
        // Добавляем начальную скорость разброса
        if (rb != null)
        {
            Vector2 randomDirection = Random.insideUnitCircle.normalized;
            rb.velocity = randomDirection * initialSpreadForce;
        }
        
        // Автоматическое уничтожение через lifetime
        Destroy(gameObject, lifetime);
        
        // Проигрываем звук
        if (shatterSound != null)
        {
            shatterSound.Play();
        }
    }
    
    void OnTriggerStay2D(Collider2D other)
    {
        if (!isActive) return;
        
        if (other.CompareTag("Player"))
        {
            damageCooldown -= Time.deltaTime;
            
            if (damageCooldown <= 0f)
            {
                PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    // Наносим урон, игнорируя неуязвимость
                    playerHealth.TakeDamageIgnoreInvulnerability(damage);
                    damageCooldown = DAMAGE_INTERVAL;
                }
            }
        }
    }
    
    public void Initialize(float newLifetime, int newDamage)
    {
        lifetime = newLifetime;
        damage = newDamage;
    }
    
    public void SetSoundEnabled(bool enabled)
    {
        if (shatterSound != null)
        {
            shatterSound.enabled = enabled;
        }
    }
    
    // Метод для отключения осколков (например, по звуку)
    public void Deactivate()
    {
        isActive = false;
        if (visualEffect != null)
        {
            visualEffect.SetActive(false);
        }
        if (shatterSound != null)
        {
            shatterSound.Stop();
        }
        Destroy(gameObject, 0.5f);
    }
}