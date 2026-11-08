using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootLoader : MonoBehaviour
{
    [Header("Настройки загрузки")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Min(0f)]
    [SerializeField] private float delayBeforeLoad = 0.1f;

    private IEnumerator Start()
    {
        // Даём менеджерам выполнить Awake и Start.
        yield return null;

        Time.timeScale = 1f;

        bool loadFromMenu =
            PlayerPrefs.GetInt("LoadFromMenu", 0) == 1;

        string targetScene =
            PlayerPrefs.GetString("TargetScene", "");

        if (loadFromMenu &&
            !string.IsNullOrWhiteSpace(targetScene))
        {
            // Удаляем маршрут, но НЕ IsNewGame и CurrentSaveSlot.
            // Они нужны PlayerBootstrapper после загрузки уровня.
            ClearRouteFlags();

            if (CanLoad(targetScene))
            {
                yield return LoadSceneRoutine(targetScene);
                yield break;
            }

            Debug.LogError(
                $"[BootLoader] Сцена '{targetScene}' недоступна. " +
                "Возврат в главное меню."
            );
        }

        // Обычный запуск или некорректный маршрут из меню.
        ClearRouteFlags();

        PlayerPrefs.DeleteKey("IsNewGame");
        PlayerPrefs.DeleteKey("CurrentSaveSlot");
        PlayerPrefs.Save();

        SaveManager.Instance?.ClearCurrentSaveData();

        if (!CanLoad(mainMenuSceneName))
        {
            Debug.LogError(
                $"[BootLoader] Главное меню '{mainMenuSceneName}' " +
                "не добавлено в список сцен сборки."
            );

            yield break;
        }

        yield return LoadSceneRoutine(mainMenuSceneName);
    }

    private void ClearRouteFlags()
    {
        PlayerPrefs.DeleteKey("LoadFromMenu");
        PlayerPrefs.DeleteKey("TargetScene");
        PlayerPrefs.Save();
    }

    private bool CanLoad(string sceneName)
    {
        // Не допускаем случайную загрузку BootScene самой в себя.
        return !string.IsNullOrWhiteSpace(sceneName) &&
               sceneName != gameObject.scene.name &&
               Application.CanStreamedLevelBeLoaded(sceneName);
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (delayBeforeLoad > 0f)
        {
            yield return new WaitForSecondsRealtime(delayBeforeLoad);
        }

        AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);

        if (loading == null)
        {
            Debug.LogError(
                $"[BootLoader] Не удалось начать загрузку '{sceneName}'."
            );

            yield break;
        }

        while (!loading.isDone)
            yield return null;
    }
}
