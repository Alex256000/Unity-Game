using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Наносим урон, равный текущему здоровью
                playerHealth.TakeDamage(playerHealth.currentHealth, Vector2.zero);
                // FIX: Перезагружаем уровень, если игрок погиб
                if (playerHealth.currentHealth <= 0)
                {
                    RestartLevel();
                }
            }
            else
            {
                // FIX: Проверяем на null CoinManager.Instance
                if (CoinManager.Instance != null)
                    CoinManager.Instance.ResetForDeath();
                RestartLevel();
            }
        }
    }

    public void RestartLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}