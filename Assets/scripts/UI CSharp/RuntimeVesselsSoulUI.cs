using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


[RequireComponent(typeof(RectTransform))]
public class RuntimeVesselsSoulUI : MonoBehaviour
{
    [Header("Prefab / container")]
    [Tooltip("Prefab должен содержать Image компоненты (RectTransform + Image). Если null — объекты создаются автоматически.")]
    [SerializeField] private GameObject vesselPrefab;
    [SerializeField] private RectTransform vesselContainer;

    [Header("Sprites")]
    [SerializeField] private Sprite fullSoulSprite;
    [SerializeField] private Sprite emptySoulSprite;
    [SerializeField] private Sprite[] partialSoulSprites; // partialSprite[0] -> 1/Per, [1] -> 2/Per и т.д.

    [Header("Optional")]
    [SerializeField] private bool setNativeSize = true;

    private readonly List<Image> vesselImages = new List<Image>();

    private RectTransform Container
    {
        get
        {
            if (vesselContainer == null)
                vesselContainer = GetComponent<RectTransform>();
            return vesselContainer;
        }
    }

    // Убедиться, что количество слотов равно vesselCount
    private void EnsureVesselCount(int vesselCount)
    {
        // Удаляем лишние
        while (vesselImages.Count > vesselCount)
        {
            var img = vesselImages[vesselImages.Count - 1];
            if (img != null)
                Destroy(img.gameObject);
            vesselImages.RemoveAt(vesselImages.Count - 1);
        }

        // Добавляем недостающие
        for (int i = vesselImages.Count; i < vesselCount; i++)
        {
            GameObject go;
            if (vesselPrefab != null)
            {
                go = Instantiate(vesselPrefab, Container, false);
            }
            else
            {
                go = new GameObject($"Vessel_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                go.transform.SetParent(Container, false);
                var rt = go.GetComponent<RectTransform>();
                // Можно настроить базовый размер/anchor тут при необходимости
                rt.sizeDelta = new Vector2(32, 32);
            }

            var img = go.GetComponent<Image>();
            if (img == null)
                img = go.AddComponent<Image>();

            vesselImages.Add(img);
        }
    }

 
    public void UpdateSoulDisplay(int currentSoul, int vesselCount, int perVessel)
    {
        if (perVessel <= 0) perVessel = 1;
        EnsureVesselCount(vesselCount);

        for (int i = 0; i < vesselImages.Count; i++)
        {
            var img = vesselImages[i];
            if (i < vesselCount)
            {
                img.gameObject.SetActive(true);

                int soulInThis = Mathf.Clamp(currentSoul - (i * perVessel), 0, perVessel);

                if (soulInThis == 0)
                {
                    img.sprite = emptySoulSprite;
                }
                else if (soulInThis >= perVessel)
                {
                    img.sprite = fullSoulSprite;
                }
                else
                {
                    int idx = Mathf.Clamp(soulInThis - 1, 0, partialSoulSprites.Length - 1);
                    img.sprite = partialSoulSprites.Length > 0 ? partialSoulSprites[idx] : emptySoulSprite;
                }

                if (setNativeSize)
                    img.SetNativeSize();
            }
            else
            {
                img.gameObject.SetActive(false);
            }
        }
    }
}