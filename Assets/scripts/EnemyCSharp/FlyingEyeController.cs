using UnityEngine;
using System.Collections;

public class FlyingEyeController : MonoBehaviour
{
    [Header("Eye References")]
    public Transform leftEye;
    public Transform rightEye;
    public EyeMovement leftEyeMovement;
    public EyeMovement rightEyeMovement;
    
    [Header("Zones")]
    public float detectionRadius = 8f;
    public float attackRadius = 4f;
    public LayerMask wallLayer;
    public Transform player;
    
    [Header("Movement")]
    public float hoverAmplitudeIdle = 0.5f;
    public float hoverAmplitudeAlert = 0.2f;
    public float hoverSpeedIdleLeft = 1.5f;
    public float hoverSpeedIdleRight = 1.8f;
    public float hoverSpeedAlertLeft = 1.2f;
    public float hoverSpeedAlertRight = 1.4f;
    
    [Header("Eye Tracking")]
    public float eyeTrackingSpeed = 5f; // Скорость поворота глаз к игроку
    
    [Header("Attack")]
    public float chargeTime = 3f;
    public float dischargeTime = 3f;
    public float mergeSpeed = 2f;
    public float attackCooldown = 7f;
    public EyeLaser eyeLaser;
    
    [Header("Death")]
    public GameObject glassShardPrefab;
    public int shardCount = 5;
    public float shardLifetime = 5f;
    public int shardDamage = 1;
    
    [Header("Visuals")]
    public GameObject chargeEffect;
    public Gradient chargeGradient;
    
    // Состояния
    public enum EnemyState { Idle, Alert, Charging, Attacking }
    public EnemyState currentState = EnemyState.Idle;
    
    // Внутренние переменные
    private Vector3 leftEyeStartPos;
    private Vector3 rightEyeStartPos;
    private Vector3 leftEyeOriginalLocalPos;
    private Vector3 rightEyeOriginalLocalPos;
    private bool eyesMerged = false;
    private float currentCharge = 0f;
    private bool isDischarging = false;
    private bool playerInDetectionZone = false;
    private bool playerInAttackZone = false;
    private Coroutine attackCoroutine;
    private float attackCooldownTimer = 0f;
    
    void Start()
    {
        // Поиск игрока
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) player = playerObj.transform;
        }
        
        // Сохраняем начальные локальные позиции глаз
        if (leftEye != null)
        {
            leftEyeOriginalLocalPos = leftEye.localPosition;
            leftEyeStartPos = leftEye.position;
        }
        if (rightEye != null)
        {
            rightEyeOriginalLocalPos = rightEye.localPosition;
            rightEyeStartPos = rightEye.position;
        }
        
        // Настройка движения глаз
        if (leftEyeMovement != null)
        {
            leftEyeMovement.SetParameters(hoverAmplitudeIdle, hoverSpeedIdleLeft);
        }
        if (rightEyeMovement != null)
        {
            rightEyeMovement.SetParameters(hoverAmplitudeIdle, hoverSpeedIdleRight);
        }
        
        // Скрываем эффект зарядки
        if (chargeEffect != null) chargeEffect.SetActive(false);
        
        // Запускаем проверку зон
        StartCoroutine(CheckZonesRoutine());
    }
    
    void Update()
    {
        if (player == null) return;
        
        // Обновляем кулдаун атаки
        if (attackCooldownTimer > 0f)
        {
            attackCooldownTimer -= Time.deltaTime;
        }
        
        // Обновление состояния
        switch (currentState)
        {
            case EnemyState.Idle:
                UpdateIdle();
                break;
            case EnemyState.Alert:
                UpdateAlert();
                break;
            case EnemyState.Charging:
                UpdateCharging();
                break;
            case EnemyState.Attacking:
                // В атаке ничего не обновляем, ждем завершения
                break;
        }
    }
    
    void UpdateIdle()
    {
        // Амплитуда уже установлена в Start, просто следим за зонами
    }
    
    void UpdateAlert()
    {
        // Плавный поворот глаз к игроку
        LookAtPlayerSmooth(leftEye);
        LookAtPlayerSmooth(rightEye);
    }
    
    void UpdateCharging()
    {
        if (!eyesMerged)
        {
            // Стягиваем глаза к центру
            MoveEyesToCenter();
            return;
        }
        
        // Обработка заряда
        if (playerInAttackZone && !isDischarging && CanSeePlayer())
        {
            // Заряжаемся
            currentCharge += Time.deltaTime / chargeTime;
            currentCharge = Mathf.Clamp01(currentCharge);
            UpdateChargeVisuals(currentCharge);
            
            if (currentCharge >= 1f)
            {
                // Заряд полный — атакуем
                StartAttack();
            }
        }
        else if (playerInAttackZone && !isDischarging && !CanSeePlayer())
        {
            // Если игрок за стеной, разряжаемся
            StartDischarge();
        }
        else if (!playerInAttackZone && currentCharge > 0)
        {
            // Игрок вышел из зоны атаки — разряжаемся
            StartDischarge();
        }
        
        if (isDischarging)
        {
            currentCharge -= Time.deltaTime / dischargeTime;
            currentCharge = Mathf.Max(0, currentCharge);
            UpdateChargeVisuals(currentCharge);
            
            if (currentCharge <= 0)
            {
                // Разрядились полностью, возвращаемся в Alert
                StopDischarge();
                ReturnToAlert();
            }
        }
    }
    
    void MoveEyesToCenter()
    {
        // Центр — это позиция контроллера
        Vector3 centerPos = transform.position;
        
        if (leftEye != null)
        {
            leftEye.position = Vector3.MoveTowards(leftEye.position, centerPos, mergeSpeed * Time.deltaTime);
        }
        if (rightEye != null)
        {
            rightEye.position = Vector3.MoveTowards(rightEye.position, centerPos, mergeSpeed * Time.deltaTime);
        }
        
        // Проверяем, сошлись ли глаза
        if (leftEye != null && rightEye != null)
        {
            if (Vector3.Distance(leftEye.position, centerPos) < 0.05f &&
                Vector3.Distance(rightEye.position, centerPos) < 0.05f)
            {
                eyesMerged = true;
                // Объединяем визуально — можно сделать один большой спрайт
                if (chargeEffect != null) chargeEffect.SetActive(true);
            }
        }
    }
    
    void UpdateChargeVisuals(float charge)
    {
        if (chargeEffect != null && chargeGradient != null)
        {
            var renderer = chargeEffect.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = chargeGradient.Evaluate(charge);
                renderer.transform.localScale = Vector3.one * (0.5f + charge * 0.5f);
            }
        }
    }
    
    void StartDischarge()
    {
        isDischarging = true;
    }
    
    void StopDischarge()
    {
        isDischarging = false;
    }
    
    void StartAttack()
    {
        if (attackCooldownTimer > 0f) return; // Кулдаун активен
        
        if (attackCoroutine != null) StopCoroutine(attackCoroutine);
        attackCoroutine = StartCoroutine(AttackCoroutine());
    }
    
    IEnumerator AttackCoroutine()
    {
        currentState = EnemyState.Attacking;
        
        // Запускаем лазер
        if (eyeLaser != null)
        {
            eyeLaser.Fire(player, wallLayer);
        }
        
        // Ждем окончания атаки
        float waitTime = (eyeLaser != null) ? eyeLaser.laserDuration : 0.5f;
        yield return new WaitForSeconds(waitTime);
        
        // Устанавливаем кулдаун
        attackCooldownTimer = attackCooldown;
        
        // Возвращаемся в Alert после атаки
        ReturnToAlert();
    }
    
    void ReturnToAlert()
    {
        currentState = EnemyState.Alert;
        currentCharge = 0f;
        isDischarging = false;
        eyesMerged = false;
        
        // Возвращаем глаза на исходные позиции
        StartCoroutine(ReturnEyesToStart());
        
        if (chargeEffect != null) chargeEffect.SetActive(false);
        
        // Восстанавливаем амплитуду для Alert
        if (leftEyeMovement != null)
            leftEyeMovement.SetParameters(hoverAmplitudeAlert, hoverSpeedAlertLeft);
        if (rightEyeMovement != null)
            rightEyeMovement.SetParameters(hoverAmplitudeAlert, hoverSpeedAlertRight);
    }
    
    IEnumerator ReturnEyesToStart()
    {
        float duration = 0.5f;
        float elapsed = 0f;
        Vector3 leftStart = leftEye.localPosition;
        Vector3 rightStart = rightEye.localPosition;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            if (leftEye != null)
                leftEye.localPosition = Vector3.Lerp(leftStart, leftEyeOriginalLocalPos, t);
            if (rightEye != null)
                rightEye.localPosition = Vector3.Lerp(rightStart, rightEyeOriginalLocalPos, t);
            
            yield return null;
        }
        
        if (leftEye != null)
            leftEye.localPosition = leftEyeOriginalLocalPos;
        if (rightEye != null)
            rightEye.localPosition = rightEyeOriginalLocalPos;
    }
    
    void LookAtPlayerSmooth(Transform eye)
    {
        if (eye == null || player == null) return;
        
        Vector2 direction = (player.position - eye.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        
        float currentAngle = eye.eulerAngles.z;
        // Нормализуем углы для правильной интерполяции
        if (currentAngle > 180f) currentAngle -= 360f;
        if (angle > 180f) angle -= 360f;
        
        float targetAngle = Mathf.LerpAngle(currentAngle, angle, eyeTrackingSpeed * Time.deltaTime);
        eye.rotation = Quaternion.Euler(0, 0, targetAngle);
    }
    
    IEnumerator CheckZonesRoutine()
    {
        while (true)
        {
            if (player != null)
            {
                float distance = Vector2.Distance(transform.position, player.position);
                
                playerInDetectionZone = distance <= detectionRadius;
                // playerInAttackZone требует видимости — используем явную маску
                playerInAttackZone = distance <= attackRadius && CanSeePlayer();
                
                // Обработка переходов
                if (currentState == EnemyState.Idle && playerInDetectionZone)
                {
                    EnterAlertState();
                }
                else if (currentState == EnemyState.Alert && !playerInDetectionZone)
                {
                    ExitAlertState();
                }
                else if (currentState == EnemyState.Alert && playerInAttackZone && !eyesMerged && attackCooldownTimer <= 0f)
                {
                    EnterChargingState();
                }
                else if (currentState == EnemyState.Charging && !playerInAttackZone && currentCharge < 0.1f)
                {
                    ReturnToAlert();
                }
            }
            
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    void EnterAlertState()
    {
        currentState = EnemyState.Alert;
        
        // Уменьшаем амплитуду колебаний
        if (leftEyeMovement != null)
            leftEyeMovement.SetParameters(hoverAmplitudeAlert, hoverSpeedAlertLeft);
        if (rightEyeMovement != null)
            rightEyeMovement.SetParameters(hoverAmplitudeAlert, hoverSpeedAlertRight);
    }
    
    void ExitAlertState()
    {
        StartCoroutine(LookAroundAndReturnToIdle());
    }
    
    IEnumerator LookAroundAndReturnToIdle()
    {
        // Быстрый осмотр (вверх-вниз)
        float originalAngle = leftEye.eulerAngles.z;
        
        for (int i = 0; i < 2; i++)
        {
            // Поворот вверх
            float targetAngleUp = originalAngle + 30f;
            float elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                float angle = Mathf.Lerp(originalAngle, targetAngleUp, t);
                leftEye.rotation = Quaternion.Euler(0, 0, angle);
                rightEye.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            
            // Поворот вниз
            float targetAngleDown = originalAngle - 30f;
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                float angle = Mathf.Lerp(targetAngleUp, targetAngleDown, t);
                leftEye.rotation = Quaternion.Euler(0, 0, angle);
                rightEye.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
            
            // Возврат в исходное положение
            elapsed = 0f;
            while (elapsed < 0.1f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.1f;
                float angle = Mathf.Lerp(targetAngleDown, originalAngle, t);
                leftEye.rotation = Quaternion.Euler(0, 0, angle);
                rightEye.rotation = Quaternion.Euler(0, 0, angle);
                yield return null;
            }
        }
        
        // Возвращаемся в Idle
        currentState = EnemyState.Idle;
        
        // Восстанавливаем амплитуду
        if (leftEyeMovement != null)
            leftEyeMovement.SetParameters(hoverAmplitudeIdle, hoverSpeedIdleLeft);
        if (rightEyeMovement != null)
            rightEyeMovement.SetParameters(hoverAmplitudeIdle, hoverSpeedIdleRight);
        
        // Поворачиваем глаза прямо
        leftEye.rotation = Quaternion.identity;
        rightEye.rotation = Quaternion.identity;
    }
    
    void EnterChargingState()
    {
        currentState = EnemyState.Charging;
        currentCharge = 0f;
        eyesMerged = false;
        isDischarging = false;
    }
    
    bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector2 direction = player.position - transform.position;
        float distance = direction.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction.normalized, distance, wallLayer.value);
        
        return hit.collider == null;
    }
    
    public void TakeDamage(int damage)
    {
        // Стеклянный — умирает от любого урона
        Die();
    }
    
    void Die()
    {
        // Создаем осколки
        if (glassShardPrefab != null)
        {
            for (int i = 0; i < shardCount; i++)
            {
                Vector2 randomOffset = Random.insideUnitCircle * 0.5f;
                GameObject shard = Instantiate(glassShardPrefab, transform.position + (Vector3)randomOffset, Quaternion.identity);
                GlassShard glassShard = shard.GetComponent<GlassShard>();
                if (glassShard != null)
                {
                    glassShard.Initialize(shardLifetime, shardDamage);
                }
            }
        }
        
        // Уничтожаем противника
        Destroy(gameObject);
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}