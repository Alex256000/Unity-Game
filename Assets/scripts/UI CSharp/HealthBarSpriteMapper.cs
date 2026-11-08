using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class HealthBarSpriteMapper : MonoBehaviour
{
    [HideInInspector]
    public PlayerHealth playerHealth;

    public Image barImage;

    [Tooltip("Индекс — здоровье: 0 = пусто, maxHealth = полное здоровье.")]
    public Sprite[] healthSprites;

    // Отдельная ссылка нужна для безопасной отписки,
    // даже если публичное поле playerHealth изменили извне.
    private PlayerHealth subscribedHealth;

    private void Reset()
    {
        barImage = GetComponent<Image>();
    }

    private void Awake()
    {
        if (barImage == null)
            barImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        SubscribeToPlayer();
        Refresh();
    }

    private void OnDisable()
    {
        UnsubscribeFromPlayer();
    }

    private void SubscribeToPlayer()
    {
        UnsubscribeFromPlayer();

        if (playerHealth == null)
            return;

        subscribedHealth = playerHealth;
        subscribedHealth.OnHealthChanged += UpdateSprite;
    }

    private void UnsubscribeFromPlayer()
    {
        if (subscribedHealth != null)
            subscribedHealth.OnHealthChanged -= UpdateSprite;

        subscribedHealth = null;
    }

    public void LinkToPlayer(PlayerHealth newPlayerHealth)
    {
        UnsubscribeFromPlayer();
        playerHealth = newPlayerHealth;

        if (isActiveAndEnabled)
            SubscribeToPlayer();

        Refresh();
    }

    private void Refresh()
    {
        if (playerHealth != null)
            UpdateSprite(playerHealth.currentHealth);
    }

    public void UpdateSprite(int currentHealth)
    {
        if (barImage == null)
            barImage = GetComponent<Image>();

        if (healthSprites == null || healthSprites.Length == 0)
            return;

        int index = Mathf.Clamp(
            currentHealth,
            0,
            healthSprites.Length - 1
        );

        Sprite sprite = healthSprites[index];

        if (sprite == null)
        {
            Debug.LogWarning(
                $"[HealthBarSpriteMapper] Не назначен спрайт для HP={index}.",
                this
            );

            return;
        }

        barImage.sprite = sprite;

        // Сохраняем исходное поведение.
        // Для фиксированного размера UI эту строку можно убрать отдельно.
        barImage.SetNativeSize();
    }
}
