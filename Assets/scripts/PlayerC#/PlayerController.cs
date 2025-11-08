using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAttack))]
public class PlayerController : MonoBehaviour 
{
    private PlayerMovement _movement;
    private PlayerAttack _attack;
    private PlayerHealth _health;

    // Singleton instance, чтобы не создавать дубликаты при DontDestroyOnLoad
    public static PlayerController Instance { get; private set; }

    private void Awake()
    {
        // Простая логика singleton: если уже есть Instance — уничтожаем дубликат
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
        _health = GetComponent<PlayerHealth>();
    }

    // --- Фасад API для внешних систем ---
    public void SetInputEnabled(bool enabled)
    {
        if (_movement != null)
            _movement.SetInputEnabled(enabled);
    }

    public void SetAutoMove(Vector2 dir)
    {
        if (_movement != null)
            _movement.SetAutoMove(dir);
    }

    // Можно добавить другие фасадные методы (BlockAttack, Die, и т.д.)
}