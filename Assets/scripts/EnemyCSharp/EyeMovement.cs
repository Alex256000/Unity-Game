using UnityEngine;

public class EyeMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float amplitude = 0.5f;
    public float speed = 1.5f;
    public bool useRandomOffset = true;
    
    private Vector3 startPosition;
    private float offset;
    
    void Start()
    {
        startPosition = transform.localPosition;
        
        if (useRandomOffset)
        {
            offset = Random.Range(0f, Mathf.PI * 2f);
        }
        else
        {
            offset = 0f;
        }
    }
    
    void Update()
    {
        // Колебания по локальной оси Y (вверх-вниз)
        float newY = startPosition.y + Mathf.Sin(Time.time * speed + offset) * amplitude;
        transform.localPosition = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    public void SetParameters(float newAmplitude, float newSpeed)
    {
        amplitude = newAmplitude;
        speed = newSpeed;
    }
}