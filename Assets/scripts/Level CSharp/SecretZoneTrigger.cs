using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SecretZoneTrigger : MonoBehaviour
{
    public SecretWallTransparency wallScript;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            wallScript.SetTransparent();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            wallScript.SetOpaque();
        }
    }
}