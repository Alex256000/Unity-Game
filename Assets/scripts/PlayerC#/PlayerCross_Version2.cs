using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class PlayerCross_Version2 : MonoBehaviour
{
    [Tooltip("Имя сцены, в которую переходим")]
    public string nextSceneName;

    [Tooltip("Направление автоматического движения персонажа при переходе (локально по X).")]
    public Vector2 exitDirection = Vector2.right;

    [Tooltip("Время автодвижения после загрузки сцены")]
    public float autoMoveTime = 0.7f;

    [Tooltip("Screen fader (можно назначить CanvasGroup wrapper)")]
    public ScreenFader screenFader;

    private bool triggered = false;

    private void Reset()
    {
        // Если триггер, помечаем как IsTrigger в инспекторе
        var col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        // Получаем компонент PlayerMovement на объекте игрока
        var pm = other.GetComponent<PlayerMovement>();
        if (pm == null)
        {
            Debug.LogWarning("PlayerCross: PlayerMovement не найден на Player!");
            return;
        }

        triggered = true;
        StartCoroutine(HandleTransition(pm, other.gameObject));
    }

    private IEnumerator HandleTransition(PlayerMovement pm, GameObject playerGO)
    {
        // 1) Блокируем ввод и устанавливаем автодвижение в сторону выхода
        pm.SetInputEnabled(false);
        pm.SetAutoMove(exitDirection);

        // 2) Fade out (если есть)
        if (screenFader != null)
            yield return screenFader.FadeOut();
        else
            yield return null;

        // 3) Загружаем сцену (здесь простая LoadScene, можно заменить на additive и смещение позиции)
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("PlayerCross: nextSceneName не задан.");

        // Ждём один кадр, чтобы объекты инициализировались в новой сцене
        yield return null;

        // 4) В новой сцене возможно нужно снова найти PlayerMovement (если объект DontDestroyOnLoad)
        // Если игрок не переносится через DontDestroyOnLoad, нужно разместить точку спавна и т.д.
        // Для простоты: попробуем получить PlayerMovement заново из playerGO (если он сохранился)
        var newPm = playerGO.GetComponent<PlayerMovement>();
        if (newPm == null)
        {
            // Попытка найти любой PlayerMovement в сцене
            newPm = FindObjectOfType<PlayerMovement>();
        }

        // 5) Продолжаем авто-движение небольшое время, затем снимаем авто-режим и возвращаем ввод
        if (newPm != null)
        {
            yield return new WaitForSeconds(autoMoveTime);
            newPm.SetAutoMove(Vector2.zero);
            newPm.SetInputEnabled(true);
        }
        else
        {
            Debug.LogWarning("PlayerCross: не удалось найти PlayerMovement после загрузки сцены.");
        }

        // 6) Fade in
        if (screenFader != null)
            yield return screenFader.FadeIn();

        // Сброс состояния триггера, если нужно (или уничтожить компонент)
        // trigger позволяет повторные переходы при возврате
        triggered = false;
    }
}