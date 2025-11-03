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
    
    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _attack = GetComponent<PlayerAttack>();
        _health = GetComponent<PlayerHealth>();
        
    }
}