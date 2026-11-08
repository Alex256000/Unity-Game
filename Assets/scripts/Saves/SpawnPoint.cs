using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnPoint : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private bool isDefaultSpawn = true;
    [SerializeField] private Color gizmoColor = Color.green;

    public bool IsDefaultSpawn => isDefaultSpawn;

    public Vector3 GetSpawnPosition()
    {
        return transform.position;
    }

    /// <summary>
    /// Находит единственную точку появления по умолчанию
    /// в активной сцене.
    ///
    /// Именованные точки переходов могут иметь isDefaultSpawn = false.
    /// </summary>
    public static SpawnPoint FindDefaultInActiveScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        SpawnPoint[] points = Object.FindObjectsByType<SpawnPoint>(
            FindObjectsSortMode.None
        );

        SpawnPoint result = null;

        foreach (SpawnPoint point in points)
        {
            if (point.gameObject.scene != activeScene ||
                !point.isDefaultSpawn)
            {
                continue;
            }

            if (result != null)
            {
                Debug.LogError(
                    "[SpawnPoint] В активной сцене несколько точек " +
                    "по умолчанию. Оставьте isDefaultSpawn только у одной."
                );

                return null;
            }

            result = point;
        }

        return result;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Короткая линия помогает видеть направление вверх от точки.
        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.up
        );
    }
}
