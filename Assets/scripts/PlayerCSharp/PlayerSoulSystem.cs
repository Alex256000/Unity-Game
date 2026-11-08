using UnityEngine;
using UnityEngine.Events;

public class PlayerSoulSystem : MonoBehaviour
{
    [Header("Soul Settings")]
    [Tooltip("Количество сосудов души.")]
    [Min(1)]
    [SerializeField] private int maxSoul = 3;

    [Tooltip("Количество единиц души в одном сосуде.")]
    [Min(1)]
    [SerializeField] private int soulPerVessel = 3;

    [Header("UI Reference (опционально)")]
    [SerializeField] private MonoBehaviour soulUIBehaviour;

    [Header("Events")]
    public UnityEvent<int> OnSoulChanged = new UnityEvent<int>();

    private int currentSoul;
    private bool initialized;

    public int SoulPerVessel => soulPerVessel;

    private void Awake()
    {
        InitializeIfNeeded();
    }

    private void Start()
    {
        NotifyChanged();
    }

    private void InitializeIfNeeded()
    {
        if (initialized)
            return;

        initialized = true;
        maxSoul = Mathf.Max(1, maxSoul);
        soulPerVessel = Mathf.Max(1, soulPerVessel);

        // Сохраняем исходное правило: новый игрок начинает с полной душой.
        currentSoul = GetMaxSoul();
    }

    public void AddSoul(int amount)
    {
        InitializeIfNeeded();

        if (amount <= 0)
            return;

        int actualAmount = Mathf.Min(
            amount,
            GetMaxSoul() - currentSoul
        );

        if (actualAmount <= 0)
            return;

        currentSoul += actualAmount;
        NotifyChanged();
    }

    /// <summary>
    /// Точное восстановление запаса из сохранения.
    /// Поддерживает уменьшение и нулевое значение.
    /// </summary>
    public void SetSoul(int amount)
    {
        InitializeIfNeeded();

        currentSoul = Mathf.Clamp(amount, 0, GetMaxSoul());
        NotifyChanged();
    }

    public bool TrySpendSoul(int cost)
    {
        InitializeIfNeeded();

        if (cost < 0 || currentSoul < cost)
            return false;

        if (cost == 0)
            return true;

        currentSoul -= cost;
        NotifyChanged();

        return true;
    }

    public bool HasEnoughSoul(int amount)
    {
        InitializeIfNeeded();

        return amount >= 0 && currentSoul >= amount;
    }

    public int GetCurrentSoul()
    {
        InitializeIfNeeded();
        return currentSoul;
    }

    public int GetMaxSoul()
    {
        return maxSoul * soulPerVessel;
    }

    public int GetVesselCount()
    {
        return maxSoul;
    }

    public void IncreaseMaxSoul(int additionalVessels = 1)
    {
        InitializeIfNeeded();

        if (additionalVessels <= 0)
            return;

        // Новые сосуды увеличивают вместимость, но не заполняются бесплатно.
        maxSoul += additionalVessels;
        NotifyChanged();
    }

    private void NotifyChanged()
    {
        UpdateUI();
        OnSoulChanged?.Invoke(currentSoul);
    }

    private void UpdateUI()
    {
        if (soulUIBehaviour == null)
            return;

        // Пока сохраняем совместимость с существующим способом
        // подключения UI через MonoBehaviour.
        // Ищем именно метод с тремя целочисленными параметрами.
        var method = soulUIBehaviour.GetType().GetMethod(
            "UpdateSoulDisplay",
            new[] { typeof(int), typeof(int), typeof(int) }
        );

        if (method != null)
        {
            method.Invoke(
                soulUIBehaviour,
                new object[]
                {
                    currentSoul,
                    GetVesselCount(),
                    soulPerVessel
                }
            );
        }
    }
}
