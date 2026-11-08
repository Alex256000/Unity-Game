using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Поддерживаем случай, когда коллайдер находится на дочернем объекте.
        PlayerHealth playerHealth =
            collision.GetComponentInParent<PlayerHealth>();

        bool isPlayer =
            collision.CompareTag("Player") ||
            (playerHealth != null && playerHealth.CompareTag("Player"));

        if (!isPlayer)
            return;

        if (playerHealth != null)
        {
            // Падение за границу уровня смертельно даже во время неуязвимости.
            // Возрождение запускает сам PlayerHealth.
            playerHealth.TakeDamageIgnoreInvulnerability(
                playerHealth.currentHealth
            );

            return;
        }

        // Запасной вариант для тестового игрока без компонента здоровья.
        RestartLevel();
    }

    public void RestartLevel()
    {
        if (CoinManager.Instance != null)
            CoinManager.Instance.ResetForDeath();

        // Перезапуск из паузы не должен оставлять игру замороженной.
        Time.timeScale = 1f;

        // Если игрок переживает загрузку сцены, не оставляем его с нулём HP.
        PlayerController player = PlayerController.Instance;

        if (player != null)
        {
            if (player.HealInput != null)
                player.HealInput.CancelHeal();

            if (player.Movement != null)
            {
                player.Movement.ResetMovementState();
                player.Movement.SetInputEnabled(true);
            }

            if (player.Health != null)
                player.Health.SetHealth(player.Health.maxHealth);
        }

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}
