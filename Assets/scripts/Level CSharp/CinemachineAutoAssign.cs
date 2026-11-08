using UnityEngine;
using Cinemachine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineVirtualCamera))]
public class CinemachineAutoAssign : MonoBehaviour
{
    [Tooltip("Если задан — будет использован как Follow при старте. Иначе будет использован PlayerController.Instance")]
    public Transform fallbackFollow;

    [Tooltip("Если в сцене есть объект с этим тегом и Collider2D — он будет назначен в Confiner (опционально)")]
    public string cameraBoundsTag = "CameraBounds";

    private CinemachineVirtualCamera vcam;

    private void Awake()
    {
        vcam = GetComponent<CinemachineVirtualCamera>();
    }

    private void Start()
    {
        TryAssignFollow();
        TryAssignConfiner();
    }

    // Also try again when enabled (useful after scene load)
    private void OnEnable()
    {
        TryAssignFollow();
        TryAssignConfiner();
    }

    private void TryAssignFollow()
    {
        if (vcam == null) return;

        // If already set and not missing — nothing to do
        if (vcam.Follow != null) return;

        // Prefer explicit fallback, otherwise use PlayerController.Instance
        if (fallbackFollow != null)
        {
            vcam.Follow = fallbackFollow;
            Debug.Log($"[CinemachineAutoAssign] Assigned fallbackFollow to vcam '{vcam.name}'");
            return;
        }

        if (PlayerController.Instance != null)
        {
            vcam.Follow = PlayerController.Instance.transform;
            Debug.Log($"[CinemachineAutoAssign] Assigned PlayerController.Instance to vcam '{vcam.name}'");
        }
    }

    private void TryAssignConfiner()
    {
        var confiner = GetComponent<CinemachineConfiner>();
        if (confiner == null) return;

        if (confiner.m_BoundingShape2D != null)
        {
            confiner.InvalidatePathCache();
            return;
        }

        if (!string.IsNullOrEmpty(cameraBoundsTag))
        {
            var go = GameObject.FindWithTag(cameraBoundsTag);
            if (go != null)
            {
                var col = go.GetComponent<Collider2D>();
                if (col != null)
                {
                    confiner.m_BoundingShape2D = col;
                    confiner.InvalidatePathCache();
                    Debug.Log($"[CinemachineAutoAssign] Assigned bounding shape '{go.name}' to confiner on '{vcam.name}'");
                }
            }
        }
    }
}