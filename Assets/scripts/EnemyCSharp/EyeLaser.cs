using UnityEngine;
using System.Collections;

public class EyeLaser : MonoBehaviour
{
    [Header("Laser Settings")]
    public float laserDuration = 2f;
    public int ticksPerSecond = 10;
    public int damagePerTick = 1;
    public LayerMask wallLayer;
    public float laserFollowSpeed = 5f; // Скорость поворота луча к игроку
    
    [Header("Visuals")]
    public LineRenderer laserLine;
    public Gradient laserGradient;
    public GameObject hitEffect;
    
    private Transform currentTarget;
    private bool isFiring = false;
    private float elapsedTime;
    private Vector2 lastKnownTargetPos;
    
    void Start()
    {
        if (laserLine == null)
        {
            laserLine = GetComponent<LineRenderer>();
            if (laserLine == null)
            {
                laserLine = gameObject.AddComponent<LineRenderer>();
                laserLine.startWidth = 0.1f;
                laserLine.endWidth = 0.1f;
                laserLine.material = new Material(Shader.Find("Sprites/Default"));
                laserLine.numCapVertices = 5;
            }
        }
        
        if (laserLine != null)
            laserLine.enabled = false;
    }
    
    public void Fire(Transform target, LayerMask walls)
    {
        if (isFiring) return;
        
        currentTarget = target;
        wallLayer = walls;
        lastKnownTargetPos = target.position;
        StartCoroutine(FireLaserCoroutine());
    }
    
    IEnumerator FireLaserCoroutine()
    {
        isFiring = true;
        if (laserLine != null)
            laserLine.enabled = true;
        elapsedTime = 0f;
        
        float tickInterval = 1f / Mathf.Max(1, ticksPerSecond);
        float nextTick = Time.time;
        
        while (elapsedTime < laserDuration)
        {
            elapsedTime += Time.deltaTime;
            
            // Плавное слежение за игроком
            if (currentTarget != null)
            {
                lastKnownTargetPos = Vector2.Lerp(lastKnownTargetPos, (Vector2)currentTarget.position, laserFollowSpeed * Time.deltaTime);
            }
            
            UpdateLaserVisuals();
            
            if (Time.time >= nextTick)
            {
                nextTick = Time.time + tickInterval;
                DealTickDamage();
            }
            
            yield return null;
        }
        
        if (laserLine != null)
            laserLine.enabled = false;
        isFiring = false;
    }
    
    void UpdateLaserVisuals()
    {
        if (laserLine == null)
            return;
        
        if (currentTarget == null)
        {
            laserLine.enabled = false;
            if (hitEffect != null) hitEffect.SetActive(false);
            return;
        }
        
        Vector3 origin = transform.position;
        Vector3 direction = ((Vector3)lastKnownTargetPos - origin).normalized;
        float distance = Vector3.Distance(origin, (Vector3)lastKnownTargetPos);
        
        // Проверяем стену (явно передаём слой как int)
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, wallLayer.value);
        
        Vector3 endPoint;
        if (hit.collider != null)
        {
            endPoint = hit.point;
        }
        else
        {
            endPoint = origin + direction * distance;
        }
        
        laserLine.SetPosition(0, origin);
        laserLine.SetPosition(1, endPoint);
        
        // Градиент
        if (laserGradient != null)
        {
            float t = Mathf.Clamp01(elapsedTime / laserDuration);
            Color col = laserGradient.Evaluate(t);
            // Если используется ColorOverLifetime style — устанавливаем оба конца
            laserLine.startColor = col;
            laserLine.endColor = col;
        }
        
        // Эффект попадания (показываем только если попали по игроку)
        if (hitEffect != null && hit.collider != null && hit.collider.CompareTag("Player"))
        {
            hitEffect.transform.position = endPoint;
            hitEffect.SetActive(true);
        }
        else if (hitEffect != null)
        {
            hitEffect.SetActive(false);
        }
    }
    
    void DealTickDamage()
    {
        if (currentTarget == null) return;
        
        // Проверяем, виден ли игрок
        Vector2 direction = (Vector2)currentTarget.position - (Vector2)transform.position;
        float distance = direction.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, distance, wallLayer.value);
        
        // Если луч попал по игроку (в зависимости от mask) — наносим урон
        if (hit.collider != null && hit.collider.CompareTag("Player"))
        {
            PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                // Наносим урон, игнорируя неуязвимость
                playerHealth.TakeDamageIgnoreInvulnerability(damagePerTick);
            }
        }
    }
}