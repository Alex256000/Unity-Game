using UnityEngine;
using UnityEditor;

public static class CreateCheckpointEditor
{
    private const string menuRoot = "Tools/Create Checkpoint";

    [MenuItem(menuRoot + "/Default")]
    public static void CreateDefaultCheckpoint()
    {
        CreateCheckpoint(interactive: true, activateOnTouch: false);
    }

    [MenuItem(menuRoot + "/Interactive (press Interact)")]
    public static void CreateInteractiveCheckpoint()
    {
        CreateCheckpoint(interactive: true, activateOnTouch: false);
    }

    [MenuItem(menuRoot + "/Touch (activate on enter)")]
    public static void CreateTouchCheckpoint()
    {
        CreateCheckpoint(interactive: false, activateOnTouch: true);
    }

    private static void CreateCheckpoint(bool interactive, bool activateOnTouch)
    {
        // Create parent container if not exists
        GameObject parent = GameObject.Find("Checkpoints");
        if (parent == null)
        {
            parent = new GameObject("Checkpoints");
            Undo.RegisterCreatedObjectUndo(parent, "Create Checkpoints Parent");
        }

        // Create checkpoint root
        var checkpoint = new GameObject(NextName());
        Undo.RegisterCreatedObjectUndo(checkpoint, "Create Checkpoint");
        checkpoint.transform.SetParent(parent.transform);

        // Position at scene view pivot if available
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
            checkpoint.transform.position = sceneView.pivot;
        else
            checkpoint.transform.position = Vector3.zero;

        // SpriteRenderer
        var sr = checkpoint.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 0;
        sr.color = Color.white;
        // leave sprite null so user can assign

        // BoxCollider2D (isTrigger)
        var col = checkpoint.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(1f, 1f);

        // Add RespawnPointInteractive (or RespawnPoint if you prefer)
        var interactiveComp = checkpoint.AddComponent<RespawnPointInteractive>();

        // Default settings
        interactiveComp.playerTag = "Player";
        interactiveComp.activateOnTouch = activateOnTouch;
        interactiveComp.requireButtonPress = interactive;
        interactiveComp.interactActionName = "Interact";
        interactiveComp.activationDelay = 0.05f;
        interactiveComp.animator = null;
        interactiveComp.activationSound = null;
        interactiveComp.activationParticlesPrefab = null;
        interactiveComp.activatedSprite = null;

        // Create a small VFX placeholder child (inactive) so designer has a starting point
        var vfx = new GameObject("VFX_Placeholder");
        Undo.RegisterCreatedObjectUndo(vfx, "Create VFX Placeholder");
        vfx.transform.SetParent(checkpoint.transform, false);
        vfx.transform.localPosition = Vector3.zero;
        var ps = vfx.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.startLifetime = 0.6f;
        main.startSpeed = 1f;
        main.startSize = 0.2f;
        vfx.SetActive(false);

        // Select newly created checkpoint
        Selection.activeGameObject = checkpoint;

        // Ping in Hierarchy
        EditorGUIUtility.PingObject(checkpoint);

        Debug.Log($"Checkpoint created at {checkpoint.transform.position}. Настройте спрайт, звук и префаб частиц в инспекторе.");
    }

    private static string NextName()
    {
        int i = 1;
        string baseName = "Checkpoint_";
        while (GameObject.Find(baseName + i) != null) i++;
        return baseName + i;
    }
}