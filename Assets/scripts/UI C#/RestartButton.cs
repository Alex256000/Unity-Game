using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    // Этот метод будет вызван при нажатии на кнопку
    public void RestartLevel()
    {
        // Получаем имя текущей сцены и перезагружаем её
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        
        // Если хотите сделать сброс времени (если у вас есть пауза при смерти)
        Time.timeScale = 1f;
    }
}