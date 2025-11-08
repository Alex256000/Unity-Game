using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class SceneExitTrigger : MonoBehaviour
{
    [Tooltip("Имя сцены, в которую переходим")]
    public string nextSceneName;

    [Tooltip("Если задан — объект спавна в новой сцене (имя GameObject). Если пусто, будет использован тег PlayerSpawn.")]
    public string spawnPointName = "";

    [Tooltip("Направление автоматического движения персонажа при переходе (локально по X).")]
    public Vector2 exitDirection = Vector2.right;

    [Tooltip("Время автодвижения после загрузки сцены")]
    public float autoMoveTime = 0.7f; // Сколько времени персонаж идет сам после перехода

    [Tooltip("Screen fader (можно назначить CanvasGroup wrapper). Может быть null.")]
    public ScreenFader fader;

    private bool triggered = false;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (triggered) return;
        if (!other.CompareTag("Player")) return;

        // Предпочтительно — работать через PlayerController фасад
        var pc = other.GetComponent<PlayerController>();
        if (pc != null)
        {
            triggered = true;
            StartCoroutine(HandleTransitionWithController(pc));
            return;
        }

        // Если нет PlayerController (маловероятно, но на всякий случай)
        var pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            triggered = true;
            StartCoroutine(HandleTransitionDirect(pm));
            return;
        }

        Debug.LogWarning("SceneExitTrigger: на игроке нет PlayerController и нет PlayerMovement.");
    }

    private IEnumerator HandleTransitionWithController(PlayerController pc)
    {
        if (pc == null) yield break;

        // 1) Запрещаем ввод и назначаем авто-движение
        pc.SetInputEnabled(false);
        pc.SetAutoMove(exitDirection);

        // 2) Fade out (если есть)
        if (fader != null)
            yield return fader.FadeOut();
        else
            yield return null;

        // 3) Синхронная загрузка сцены (LoadScene)
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("SceneExitTrigger: nextSceneName не задан.");

        // 4) Ждём один кадр, чтобы новая сцена и её объекты инициализировались
        yield return null;

        // 5) Телепортируем персистентного игрока на spawn-point в новой сцене (если найден)
        Transform spawn = null;

        if (!string.IsNullOrEmpty(spawnPointName))
        {
            var go = GameObject.Find(spawnPointName);
            if (go != null) spawn = go.transform;
        }
        else
        {
            var spGO = GameObject.FindWithTag("PlayerSpawn");
            if (spGO != null) spawn = spGO.transform;
        }

        // Получаем текущий (персистентный) игрок
        var player = PlayerController.Instance;
        if (player != null)
        {
            var playerTransform = player.transform;
            var rb = player.GetComponent<Rigidbody2D>();

            if (spawn != null)
            {
                // Перемещаем игрока и сбрасываем скорость
                if (rb != null)
                {
                    rb.position = spawn.position;
                    rb.velocity = Vector2.zero;
                }
                else
                {
                    playerTransform.position = spawn.position;
                }
            }
            else
            {
                Debug.LogWarning("SceneExitTrigger: spawn point не найден. Назначьте spawnPointName или пометьте объект тегом PlayerSpawn.");
            }

            // 6) Продолжаем авто-движение небольшое время, затем возвращаем контроль
            yield return new WaitForSeconds(autoMoveTime);
            player.SetAutoMove(Vector2.zero);
            player.SetInputEnabled(true);
        }
        else
        {
            Debug.LogWarning("SceneExitTrigger: PlayerController.Instance == null после загрузки сцены.");
        }

        // 7) Fade in (если есть)
        if (fader != null)
            yield return fader.FadeIn();
        else
            yield return null;

        triggered = false;
    }

    private IEnumerator HandleTransitionDirect(PlayerMovement pm)
    {
        if (pm == null) yield break;

        pm.SetInputEnabled(false);
        pm.SetAutoMove(exitDirection);

        if (fader != null)
            yield return fader.FadeOut();
        else
            yield return null;

        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
        else
            Debug.LogWarning("SceneExitTrigger: nextSceneName не задан.");

        yield return null;

        Transform spawn = null;
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            var go = GameObject.Find(spawnPointName);
            if (go != null) spawn = go.transform;
        }
        else
        {
            var spGO = GameObject.FindWithTag("PlayerSpawn");
            if (spGO != null) spawn = spGO.transform;
        }

        // Если игрок персистентный — pm будет тем же компонентом
        var newPm = FindObjectOfType<PlayerMovement>();
        if (newPm != null)
        {
            var rb = newPm.GetComponent<Rigidbody2D>();
            if (spawn != null)
            {
                if (rb != null)
                {
                    rb.position = spawn.position;
                    rb.velocity = Vector2.zero;
                }
                else
                {
                    newPm.transform.position = spawn.position;
                }
            }
            else
            {
                Debug.LogWarning("SceneExitTrigger: spawn point не найден.");
            }

            yield return new WaitForSeconds(autoMoveTime);
            newPm.SetAutoMove(Vector2.zero);
            newPm.SetInputEnabled(true);
        }
        else
        {
            Debug.LogWarning("SceneExitTrigger: PlayerMovement не найден после загрузки сцены.");
        }

        if (fader != null)
            yield return fader.FadeIn();
        else
            yield return null;

        triggered = false;
    }
}