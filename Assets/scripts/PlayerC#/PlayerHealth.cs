using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 3;
    public int currentHealth;

    [Header("Invulnerability Settings")]
    public float invulTime = 0.6f;
    private bool isInvul = false;
    private float invulTimer = 0f;

    public event System.Action OnPlayerDeath;
    private void Start()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth); // FIX: корректируем стартовое значение
        if (currentHealth == 0) currentHealth = maxHealth; // По умолчанию устанавливаем maxHealth
    }

    private void FixedUpdate()
    {
        if (isInvul)
        {
            invulTimer -= Time.fixedDeltaTime;
            if (invulTimer <= 0f)
            {
                isInvul = false;
            }
        }
    }

    public void TakeDamage(int damage, Vector2 hitDirection)
    {
        if (isInvul || currentHealth <= 0)
            return;

        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);
        isInvul = true;
        invulTimer = invulTime;

        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
            movement.ApplyKnockback(hitDirection);

        if (currentHealth <= 0)
            Die();
    }

    public void TakeDamage(int damage)
    {
        TakeDamage(damage, Vector2.zero);
    }

    public bool IsInvulnerable()
    {
        return isInvul;
    }

    private void Die()
    {
        isInvul = false; // FIX: Сброс неуязвимости при смерти
        var deadZone = FindObjectOfType<DeadZone>();
        if (deadZone != null)
        {
            deadZone.RestartLevel();
        }
        else
        {
            Debug.LogError("DeadZone not found in scene!");
        }

        OnPlayerDeath?.Invoke();
        Debug.Log("Player died!");
    }
}