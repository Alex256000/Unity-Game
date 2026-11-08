using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PlayerMovement))]
public class PlayerAnimation : MonoBehaviour
{
    private Rigidbody2D rb;
    private Animator anim;
    private PlayerMovement movement;

    private static readonly int SpeedHash =
        Animator.StringToHash("Speed");

    private static readonly int IsGroundedHash =
        Animator.StringToHash("IsGrounded");

    private static readonly int VerticalVelocityHash =
        Animator.StringToHash("VerticalVelocity");

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        movement = GetComponent<PlayerMovement>();

        // Положение игрока меняет только PlayerMovement.
        // Анимации не должны дополнительно смещать корневой объект.
        anim.applyRootMotion = false;
    }

    private void Update()
    {
        // Speed остаётся в привычном диапазоне 0–1.
        // В отличие от старого скрипта, значение берётся из движения,
        // а не из отдельного чтения старого Input Manager.
        bool isMoving = Mathf.Abs(rb.velocity.x) > 0.05f;

        anim.SetFloat(SpeedHash, isMoving ? 1f : 0f);
        anim.SetBool(IsGroundedHash, movement.IsGrounded());
        anim.SetFloat(VerticalVelocityHash, rb.velocity.y);
    }
}
