using UnityEngine;
using System.Collections;

public class AnimatedTeleporter : MonoBehaviour
{
    [Header("Настройки телепорта")]
    [SerializeField] private Transform destination;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;
    
    [Header("Анимация")]
    [SerializeField] private Animator animator;
    [SerializeField] private string activationTrigger = "Activate";
    [SerializeField] private SpriteRenderer portalSprite;
    
    [Header("Звуки")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip activationSound;
    [SerializeField] private AudioClip teleportSound;
    
    private string playerTag = "Player";
    private bool isActivated = false;
    
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag) && !isActivated)
        {
            StartCoroutine(ActivateAndTeleport(other.gameObject));
        }
    }
    
    private IEnumerator ActivateAndTeleport(GameObject player)
    {
        isActivated = true;
        
        // Активируем анимацию портала
        if (animator != null)
        {
            animator.SetTrigger(activationTrigger);
        }
        
        // Звук активации
        if (activationSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(activationSound);
        }
        
        // Эффект исчезновения игрока
        yield return StartCoroutine(FadePlayer(player, 1f, 0f, fadeOutDuration));
        
        // Звук телепортации
        if (teleportSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(teleportSound);
        }
        
        // Перемещаем игрока
        if (destination != null)
        {
            player.transform.position = destination.position;
        }
        
        // Небольшая пауза
        yield return new WaitForSeconds(0.1f);
        
        // Эффект появления игрока
        yield return StartCoroutine(FadePlayer(player, 0f, 1f, fadeInDuration));
        
        // Сбрасываем состояние
        yield return new WaitForSeconds(0.5f);
        isActivated = false;
    }
    
    private IEnumerator FadePlayer(GameObject player, float startAlpha, float endAlpha, float duration)
    {
        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        if (playerSprite == null) yield break;
        
        float elapsed = 0f;
        Color color = playerSprite.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            playerSprite.color = color;
            yield return null;
        }
        
        color.a = endAlpha;
        playerSprite.color = color;
    }
}