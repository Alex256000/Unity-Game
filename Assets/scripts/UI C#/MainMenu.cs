using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public Button startButton;
    public Button continueButton;
    public Button exitButton;

    void Start()
    {
        // Кнопка "Продолжить" неактивна, так как нет системы сохранения
        continueButton.interactable = false;

        // Привязываем методы к кнопкам
        startButton.onClick.AddListener(StartGame);
        continueButton.onClick.AddListener(ContinueGame);
        exitButton.onClick.AddListener(ExitGame);
    }

    public void StartGame()
    {
        // Здесь загрузите вашу игровую сцену (например, с индексом 1)
        SceneManager.LoadScene("TestLevel"); // Замените на имя или индекс вашей игровой сцены
    }

    public void ContinueGame()
    {
        // Пока не реализовано
    }

    public void ExitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // Для выхода из режима Play в редакторе
        #endif
    }
}