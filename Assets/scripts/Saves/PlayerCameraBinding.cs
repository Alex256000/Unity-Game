using Cinemachine;
using UnityEngine;

public static class PlayerCameraBinding
{
    /// <summary>
    /// Повторно привязать активные виртуальные камеры к игроку.
    /// После телепорта сбрасываем предыдущее состояние камеры,
    /// чтобы она не догоняла старую позицию через весь уровень.
    /// </summary>
    public static void Refresh(
        Transform player,
        string cameraBoundsTag = "CameraBounds")
    {
        if (player == null)
            return;

        Collider2D fallbackBounds = FindBounds(cameraBoundsTag);

        CinemachineVirtualCamera[] cameras =
            Object.FindObjectsByType<CinemachineVirtualCamera>(
                FindObjectsSortMode.None
            );

        foreach (CinemachineVirtualCamera camera in cameras)
        {
            camera.Follow = player;
            camera.PreviousStateIsValid = false;

            CinemachineConfiner confiner =
                camera.GetComponent<CinemachineConfiner>();

            if (confiner == null)
                continue;

            // Явно назначенные границы камеры не перезаписываем.
            if (confiner.m_BoundingShape2D == null &&
                fallbackBounds != null)
            {
                confiner.m_BoundingShape2D = fallbackBounds;
            }

            confiner.InvalidatePathCache();
        }
    }

    private static Collider2D FindBounds(string boundsTag)
    {
        if (string.IsNullOrWhiteSpace(boundsTag))
            return null;

        try
        {
            GameObject boundsObject =
                GameObject.FindGameObjectWithTag(boundsTag);

            return boundsObject != null
                ? boundsObject.GetComponent<Collider2D>()
                : null;
        }
        catch (UnityException)
        {
            // Необязательный тег может быть не добавлен в проект.
            // Камера продолжит работать с уже назначенными границами.
            Debug.LogWarning(
                $"[PlayerCameraBinding] Тег '{boundsTag}' не задан. " +
                "Автоматический поиск границ камеры пропущен."
            );

            return null;
        }
    }
}
