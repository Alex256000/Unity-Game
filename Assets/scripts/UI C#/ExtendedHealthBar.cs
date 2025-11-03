using UnityEngine;
using UnityEngine.UI;

public class ExtendedHealthBar : MonoBehaviour
{
    [Header("Основное здоровье")]
    public Image[] mainSegments;
    public Sprite[] mainSprites; // Для уникальных стадий

    [Header("Броня")]
    public Image[] armorSegments;
    public Sprite armorFullSprite;
    public Sprite armorEmptySprite;

    private int armorCount = 0;
    private int maxArmor = 3;

    public void SetMainHealth(int value, int max)
    {
        float percent = (float)value / max;
        int active = Mathf.CeilToInt(percent * mainSegments.Length);

        for (int i = 0; i < mainSegments.Length; i++)
        {
            mainSegments[i].enabled = i < active;
            if (i < mainSprites.Length)
                mainSegments[i].sprite = mainSprites[i];
        }
    }

    public void SetArmor(int currentArmor, int maxPossibleArmor)
    {
        armorCount = currentArmor;
        maxArmor = maxPossibleArmor;

        for (int i = 0; i < armorSegments.Length; i++)
        {
            if (i < maxArmor)
            {
                armorSegments[i].enabled = true;
                armorSegments[i].sprite = (i < armorCount) ? armorFullSprite : armorEmptySprite;
            }
            else
            {
                armorSegments[i].enabled = false;
            }
        }
    }

    // Вызов при получении урона
    public void TakeDamage(ref int health, ref int armor, int damage)
    {
        if (armor > 0)
        {
            int absorbed = Mathf.Min(damage, armor);
            armor -= absorbed;
            damage -= absorbed;
            SetArmor(armor, maxArmor);
        }
        if (damage > 0)
        {
            health -= damage;
            SetMainHealth(health, mainSegments.Length);
        }
    }

    // Вызов при получении брони
    public void AddArmor(int amount)
    {
        armorCount = Mathf.Min(armorCount + amount, maxArmor);
        SetArmor(armorCount, maxArmor);
    }

    // Для апгрейда максимального бронирования
    public void UpgradeArmor(int newMax)
    {
        maxArmor = Mathf.Clamp(newMax, 0, armorSegments.Length);
        SetArmor(armorCount, maxArmor);
    }
}