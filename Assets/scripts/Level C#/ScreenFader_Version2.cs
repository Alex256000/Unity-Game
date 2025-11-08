using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public CanvasGroup fadeGroup;
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        // Сделаем фейдер персистентным, чтобы он переживал смену сцен
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FadeOut()
    {
        if (fadeGroup == null) yield break;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 1f;
    }

    public IEnumerator FadeIn()
    {
        if (fadeGroup == null) yield break;
        float t = fadeDuration;
        while (t > 0f)
        {
            t -= Time.deltaTime;
            fadeGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        fadeGroup.alpha = 0f;
    }
}