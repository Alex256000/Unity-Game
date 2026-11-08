using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private ParticleSystem popParticles;
    [SerializeField] private AudioClip popSound;
    [SerializeField] private bool destroyOnTouch = true;
    [SerializeField] private bool hideVisualImmediately = true;
    [SerializeField] private bool respawn = true;
    [SerializeField] private float respawnTime = 3f;
    
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private new Collider2D collider2D;
    private bool isPopped = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        collider2D = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && !isPopped)
        {
            bool fromAbove = collision.transform.position.y > transform.position.y + 0.2f;
            if (fromAbove)
            {
                Rigidbody2D rb = collision.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 0f);
                    rb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);
                }
            }

            Pop();
        }
    }

    private void Pop()
    {
        isPopped = true;

        // Запускаем частицы (оставляем их прикрепленными к объекту)
        if (popParticles != null)
        {
            popParticles.Play();
        }

        // Проигрываем звук
        if (popSound != null)
        {
            AudioSource.PlayClipAtPoint(popSound, transform.position);
        }

        // Скрываем визуал и отключаем коллайдер
        if (hideVisualImmediately)
        {
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (collider2D != null) collider2D.enabled = false;
        }

        Debug.Log($"Balloon popped! Respawn in {respawnTime} seconds");

        // Запускаем корутину для обработки возрождения
        StartCoroutine(PopSequence());
    }

    private IEnumerator PopSequence()
    {
        // Ждем небольшое время перед следующей логикой
        yield return new WaitForSeconds(0.1f);
        
        if (respawn)
        {
            if (destroyOnTouch)
            {
                // Ждем время возрождения
                yield return new WaitForSeconds(respawnTime);
                
                // Создаем новый объект вместо старого
                CreateNewBalloon();
                
                // Уничтожаем текущий объект
                Destroy(gameObject);
            }
            else
            {
                // Ждем время возрождения
                yield return new WaitForSeconds(respawnTime);
                
                // Восстанавливаем текущий объект
                Respawn();
            }
        }
        else if (destroyOnTouch)
        {
            // Если не нужно возрождать, просто уничтожаем объект
            Destroy(gameObject);
        }
    }

    private void CreateNewBalloon()
    {
        // Создаем новый объект на той же позиции
        GameObject newBalloon = Instantiate(gameObject, transform.position, transform.rotation);
        
        // Настраиваем новый объект
        Balloon newBalloonScript = newBalloon.GetComponent<Balloon>();
        newBalloonScript.isPopped = false;
        
        // Если есть родитель, сохраняем иерархию
        if (transform.parent != null)
        {
            newBalloon.transform.SetParent(transform.parent);
        }
    }

    private void Respawn()
    {
        // Восстанавливаем визуал
        if (spriteRenderer != null) spriteRenderer.enabled = true;
        if (collider2D != null) collider2D.enabled = true;
        
        // Сбрасываем состояние
        isPopped = false;
        
        // Останавливаем и очищаем частицы, если они все еще активны
        if (popParticles != null)
        {
            popParticles.Stop();
            popParticles.Clear();
        }
        
        Debug.Log("Balloon respawned!");
    }

    public void SetBounceForce(float newForce)
    {
        bounceForce = newForce;
    }

    public void SetRespawnSettings(bool shouldRespawn, float timeToRespawn)
    {
        respawn = shouldRespawn;
        respawnTime = timeToRespawn;
    }
}