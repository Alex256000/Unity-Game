using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using Cinemachine;

[RequireComponent(typeof(Collider2D))]
public class SceneTransition : MonoBehaviour
{
    [Header("Transition Settings")]
    public string nextSceneName;
    public string spawnPointName = "";
    public Vector2 exitDirection = Vector2.right;
    
    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // Сохраняем здоровье игрока
        var playerHealth = other.GetComponent<PlayerHealth>();
        int health = (playerHealth != null) ? playerHealth.currentHealth : 0;
        
        // Вызываем менеджер перехода
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.StartTransition(
                nextSceneName, 
                spawnPointName, 
                exitDirection, 
                health
            );
        }
        else
        {
            Debug.LogError("SceneTransitionManager не найден! Создайте GameObject с этим скриптом в начальной сцене.");
        }
    }
}