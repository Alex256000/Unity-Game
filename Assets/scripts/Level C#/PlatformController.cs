using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformController : MonoBehaviour
{
    // ==================================================
    // PLATFORM MOVEMENT TYPE
    // ==================================================
    public enum MovementType
    {
        Static,     // Does not move
        Horizontal, // Left-Right
        Vertical,   // Up-Down
        Circular    // Circular movement
    }

    [Header("Main Settings")]
    [Tooltip("Select the platform movement type")]
    public MovementType movementType = MovementType.Static;

    [Tooltip("If enabled, the platform moves back and forth. If disabled — one-way movement")]
    public bool pingPong = true;

    [Tooltip("If enabled, the movement will be smooth (with inertia)")]
    public bool useSmoothDamp = false;

    // ==================================================
    // MOVEMENT SETTINGS
    // ==================================================
    [Header("Horizontal Movement")]
    [Tooltip("Movement distance to the right from the initial point")]
    public float horizontalDistance = 5f;

    [Tooltip("Movement speed (linear)")]
    public float horizontalSpeed = 1f;

    [Header("Vertical Movement")]
    [Tooltip("Movement distance upwards from the initial point")]
    public float verticalDistance = 3f;

    [Tooltip("Movement speed (linear)")]
    public float verticalSpeed = 1f;

    [Header("Circular Movement")]
    [Tooltip("Radius of the circle")]
    public float circleRadius = 2f;

    [Tooltip("Speed of circular movement")]
    public float circleSpeed = 1f;

    [Header("Smooth Movement (SmoothDamp)")]
    [Tooltip("Acceleration/deceleration time (the bigger, the smoother)")]
    public float smoothTime = 0.3f;

    [Tooltip("Maximum speed (limits acceleration)")]
    public float maxSpeed = Mathf.Infinity;

    // ==================================================
    // SERVICE VARIABLES
    // ==================================================
    private Vector3 startPosition;       // Initial platform position
    private Vector3 lastPosition;

    private float angle = 0f;           // Angle for circular movement
    private Vector3 currentVelocity;    // Current velocity (for SmoothDamp)

    // ==================================================
    // INITIALIZATION
    // ==================================================
    private void Start()
    {
        startPosition = transform.position; // Store the starting position
        lastPosition = transform.position;
    }

    // ==================================================
    // FRAME UPDATE
    // ==================================================
    private void Update()
    {
        switch (movementType)
        {
            case MovementType.Static:
                // Do nothing — platform is static
                break;

            case MovementType.Horizontal:
                MoveHorizontal();
                break;

            case MovementType.Vertical:
                MoveVertical();
                break;

            case MovementType.Circular:
                MoveCircular();
                break;
        }
        lastPosition = transform.position;
    }
    public Vector3 DeltaPosition => transform.position - lastPosition;
    // ==================================================
    // HORIZONTAL MOVEMENT (LEFT-RIGHT)
    // ==================================================
    private void MoveHorizontal()
    {
        float targetX;

        if (pingPong)
        {
            // Back and forth movement (0 → distance → 0)
            targetX = Mathf.PingPong(Time.time * horizontalSpeed, horizontalDistance);
        }
        else
        {
            // One-way movement (0 → distance → teleport → 0)
            targetX = Mathf.Repeat(Time.time * horizontalSpeed, horizontalDistance);
        }

        // Target position
        Vector3 targetPosition = startPosition + new Vector3(targetX, 0, 0);

        // Apply movement (smooth or linear)
        ApplyMovement(targetPosition);
    }

    // ==================================================
    // VERTICAL MOVEMENT (UP-DOWN)
    // ==================================================
    private void MoveVertical()
    {
        float targetY;

        if (pingPong)
        {
            // Up and down movement (0 → distance → 0)
            targetY = Mathf.PingPong(Time.time * verticalSpeed, verticalDistance);
        }
        else
        {
            // One-way movement (0 → distance → teleport → 0)
            targetY = Mathf.Repeat(Time.time * verticalSpeed, verticalDistance);
        }

        // Target position
        Vector3 targetPosition = startPosition + new Vector3(0, targetY, 0);

        // Apply movement
        ApplyMovement(targetPosition);
    }

    // ==================================================
    // CIRCULAR MOVEMENT
    // ==================================================
    private void MoveCircular()
    {
        // Increase angle over time
        angle += circleSpeed * Time.deltaTime;

        // Calculate position on the circle
        float x = Mathf.Cos(angle) * circleRadius;
        float y = Mathf.Sin(angle) * circleRadius;

        // Target position
        Vector3 targetPosition = startPosition + new Vector3(x, y, 0);

        // Apply movement
        ApplyMovement(targetPosition);
    }

    // ==================================================
    // APPLY MOVEMENT (LINEAR OR SMOOTH)
    // ==================================================
    private void ApplyMovement(Vector3 targetPosition)
    {
        if (useSmoothDamp)
        {
            // Smooth movement with inertia (acceleration/deceleration)
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                smoothTime,
                maxSpeed
            );
        }
        else
        {
            // Simple linear movement
            transform.position = targetPosition;
        }
    }
}