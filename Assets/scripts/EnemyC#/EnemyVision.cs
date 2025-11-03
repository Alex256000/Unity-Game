using UnityEngine;

public class EnemyVision : MonoBehaviour
{
    public Vector2 visionBoxSize = new Vector2(5, 2); // ширина и высота зоны видимости
    public Vector2 visionBoxOffset = new Vector2(0, 0); // смещение относительно врага
    public string playerTag = "Player";
    public bool PlayerInSight { get; private set; }
    public Vector2 lastSeenPosition;

    void Update()
    {
        Vector2 center = (Vector2)transform.position + visionBoxOffset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, visionBoxSize, 0);
        PlayerInSight = false;
        foreach (var hit in hits)
        {
            if (hit.CompareTag(playerTag))
            {
                PlayerInSight = true;
                lastSeenPosition = hit.transform.position;
                break;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector2 center = (Vector2)transform.position + visionBoxOffset;
        Gizmos.DrawWireCube(center, visionBoxSize);
    }
}