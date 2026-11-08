using System.Collections;
using UnityEngine;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Затемнение")]
    public CanvasGroup fadeGroup;

    [Min(0f)]
    public float fadeDuration = 0.5f;

    // Новая операция отменяет изменение прозрачности предыдущей.
    private int operationVersion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (fadeGroup == null)
            fadeGroup = GetComponent<CanvasGroup>();

        SetInstant(0f);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public IEnumerator FadeOut()
    {
        return FadeTo(1f);
    }

    public IEnumerator FadeIn()
    {
        return FadeTo(0f);
    }

    /// <summary>
    /// Немедленно установить прозрачность:
    /// 0 — экран открыт, 1 — экран полностью закрыт.
    /// </summary>
    public void SetInstant(float alpha)
    {
        operationVersion++;
        ApplyAlpha(alpha);
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        int version = ++operationVersion;

        if (fadeGroup == null)
            yield break;

        float startAlpha = fadeGroup.alpha;
        float duration = Mathf.Max(0f, fadeDuration);

        if (duration <= 0f)
        {
            ApplyAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            // Другая операция уже начала управлять этим затемнением.
            if (version != operationVersion || fadeGroup == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float progress = Mathf.Clamp01(elapsed / duration);

            ApplyAlpha(
                Mathf.Lerp(startAlpha, targetAlpha, progress)
            );

            yield return null;
        }

        if (version == operationVersion && fadeGroup != null)
            ApplyAlpha(targetAlpha);
    }

    private void ApplyAlpha(float alpha)
    {
        if (fadeGroup == null)
            return;

        fadeGroup.alpha = Mathf.Clamp01(alpha);

        // Затемнение перекрывает UI только пока оно видно.
        fadeGroup.blocksRaycasts = fadeGroup.alpha > 0.001f;
        fadeGroup.interactable = false;
    }
}
