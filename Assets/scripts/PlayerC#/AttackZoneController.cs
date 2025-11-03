using UnityEngine;

public class AttackZoneController : MonoBehaviour
{
    [SerializeField] private GameObject attackZoneSprite; // Сюда назначь объект со SpriteRenderer

    // Показываем зону атаки
    public void ShowZone()
    {
        attackZoneSprite.SetActive(true);
    }

    // Скрываем зону атаки
    public void HideZone()
    {
        attackZoneSprite.SetActive(false);
    }
}