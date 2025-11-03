using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneExitTrigger : MonoBehaviour
{
    public string nextSceneName;
    public Vector2 exitDirection = Vector2.right;
    public float autoMoveTime = 0.7f; // Сколько времени персонаж идет сам после перехода
    public ScreenFader fader;

    private bool triggered = false;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (other.CompareTag("Player"))
        {
            triggered = true;
            StartCoroutine(HandleTransition(other.GetComponent<PlayerController>()));
        }
    }

    IEnumerator HandleTransition(PlayerController player)
    {
        player.enabled = false; // Отключаем управление
        player.SetAutoMove(exitDirection); // Включаем автоматическое движение

        yield return fader.FadeOut();

        // Загрузка новой сцены (или подгрузка, если используется addititve)
        SceneManager.LoadScene(nextSceneName);

        // После загрузки сцены:
        // yield return null; // Ждем фрейм для инициализации
        // player.SetAutoMove(exitDirection); // Повторяем автодвижение, если надо

        yield return new WaitForSeconds(autoMoveTime);

        player.SetAutoMove(Vector2.zero); // Останавливаем автодвижение
        player.enabled = true; // Возвращаем управление
        yield return fader.FadeIn();
    }
}