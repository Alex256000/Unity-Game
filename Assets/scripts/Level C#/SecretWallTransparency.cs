using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretWallTransparency : MonoBehaviour
{
    public SpriteRenderer wallRenderer;
    public float transparentAlpha = 0.3f;
    public float opaqueAlpha = 1f;

    void Start()
    {
        if (wallRenderer == null)
            wallRenderer = GetComponent<SpriteRenderer>();
        SetOpaque();
    }

    public void SetTransparent()
    {
        Color c = wallRenderer.color;
        c.a = transparentAlpha;
        wallRenderer.color = c;
    }

    public void SetOpaque()
    {
        Color c = wallRenderer.color;
        c.a = opaqueAlpha;
        wallRenderer.color = c;
    }
}