// CameraManager.cs
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }
    
    [SerializeField] private CinemachineVirtualCamera vcam;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Подписываемся на событие загрузки сцены
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Находим игрока в новой сцене
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && vcam != null)
        {
            vcam.Follow = player.transform;
            Debug.Log($"Камера настроена на игрока в сцене: {scene.name}");
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}