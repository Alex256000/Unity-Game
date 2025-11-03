using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class PlayerCross : MonoBehaviour
{
    [SerializeField] private PlayerMovement _movement;
    // ...
    public void StartTransition(Vector2 moveDir)
    {
        _movement.SetInputEnabled(false);
        _movement.SetAutoMove(moveDir);
        // Здесь запускаем fade, ждём, потом возвращаем allowInput
    }
    public void EndTransition()
    {
        _movement.SetAutoMove(Vector2.zero);
        _movement.SetInputEnabled(true);
    }
}