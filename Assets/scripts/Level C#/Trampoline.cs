using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField] private float bounceForce = 15f;
    [SerializeField] private ParticleSystem bounceParticles;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Проверяем, что объект сверху (игрок падает на батут)
        if (collision.CompareTag("Player") &&
            collision.transform.position.y > transform.position.y)
        {
            BouncePlayer(collision.GetComponent<Rigidbody2D>());
        }
    }

    private void BouncePlayer(Rigidbody2D playerRb)
    {
        // Обнуляем вертикальную скорость перед прыжком
        playerRb.velocity = new Vector2(playerRb.velocity.x, 0f);
        // Применяем силу вверх
        playerRb.AddForce(Vector2.up * bounceForce, ForceMode2D.Impulse);

       
    
        if (bounceParticles != null)
            bounceParticles.Play();
    }
}