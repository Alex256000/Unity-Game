using UnityEngine;
using UnityEngine.Events;

public class HealthSystem : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    public UnityEvent<int> OnHealthChanged;
    public UnityEvent OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public int GetHealth() => currentHealth;

    public void SetHealth(int value)
    {
        currentHealth = Mathf.Clamp(value, 0, maxHealth);
        OnHealthChanged?.Invoke(currentHealth);

        if (currentHealth <= 0)
            OnDeath?.Invoke();
    }

    public void TakeDamage(int damage)
    {
        SetHealth(currentHealth - damage);
    }

    public void Heal(int amount)
    {
        SetHealth(currentHealth + amount);
    }
}