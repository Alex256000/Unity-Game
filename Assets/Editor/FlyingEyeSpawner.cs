using UnityEditor;
using UnityEngine;
using System.IO;

public class FlyingEyeSpawnerWindow : EditorWindow
{
    private GameObject eyePrefab;
    private Vector2 spawnPosition = Vector2.zero;
    private bool showAdvanced = false;

    [MenuItem("Tools/Flying Eye Spawner")]
    public static void ShowWindow()
    {
        GetWindow<FlyingEyeSpawnerWindow>("Flying Eye Spawner");
    }

    void OnGUI()
    {
        GUILayout.Label("Spawn Flying Eye Enemy", EditorStyles.boldLabel);

        eyePrefab = (GameObject)EditorGUILayout.ObjectField("Eye Prefab", eyePrefab, typeof(GameObject), false);
        spawnPosition = EditorGUILayout.Vector2Field("Spawn Position", spawnPosition);

        EditorGUILayout.Space();

        if (GUILayout.Button("Spawn at Position", GUILayout.Height(30)))
        {
            SpawnEye();
        }

        if (GUILayout.Button("Spawn at Mouse Position", GUILayout.Height(30)))
        {
            SpawnAtMouse();
        }

        EditorGUILayout.Space();

        showAdvanced = EditorGUILayout.Foldout(showAdvanced, "Advanced Options");
        if (showAdvanced)
        {
            if (GUILayout.Button("Create New Eye Prefab", GUILayout.Height(25)))
            {
                CreateEyePrefab();
            }

            if (GUILayout.Button("Delete All Flying Eyes in Scene", GUILayout.Height(25)))
            {
                DeleteAllFlyingEyes();
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("Make sure to assign the prefab first! Or use 'Create New Eye Prefab' to generate one.", MessageType.Info);
    }

    void SpawnEye()
    {
        if (eyePrefab == null)
        {
            Debug.LogError("No eye prefab assigned! Use 'Create New Eye Prefab' first.");
            return;
        }

        GameObject newEye = Instantiate(eyePrefab, spawnPosition, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(newEye, "Spawn Flying Eye");
        Selection.activeGameObject = newEye;
        Debug.Log($"Spawned Flying Eye at {spawnPosition}");
    }

    void SpawnAtMouse()
    {
        if (eyePrefab == null)
        {
            Debug.LogError("No eye prefab assigned! Use 'Create New Eye Prefab' first.");
            return;
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        // Получаем позицию мыши в мировых координатах через HandleUtility
        Vector2 mousePos = Event.current.mousePosition;
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
        Vector3 worldPos = ray.origin;
        worldPos.z = 0;

        GameObject newEye = Instantiate(eyePrefab, worldPos, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(newEye, "Spawn Flying Eye at Mouse");
        Selection.activeGameObject = newEye;
        Debug.Log($"Spawned Flying Eye at {worldPos}");
    }

    void DeleteAllFlyingEyes()
    {
        // Используем FindObjectsByType для поиска ВСЕХ объектов
        var eyes = FindObjectsByType<FlyingEyeController>(FindObjectsSortMode.None);

        if (eyes == null || eyes.Length == 0)
        {
            Debug.Log("No Flying Eyes found in scene");
            return;
        }

        foreach (FlyingEyeController eye in eyes)
        {
            Undo.DestroyObjectImmediate(eye.gameObject);
        }

        Debug.Log($"Deleted {eyes.Length} Flying Eyes");
    }

    void CreateEyePrefab()
    {
        string prefabPath = "Assets/Prefabs";
        if (!Directory.Exists(prefabPath))
        {
            Directory.CreateDirectory(prefabPath);
            AssetDatabase.Refresh();
        }

        GameObject parent = new GameObject("FlyingEye");
        parent.transform.position = Vector3.zero;

        FlyingEyeController controller = parent.AddComponent<FlyingEyeController>();
        parent.tag = "Enemy";

        GameObject leftEyeObj = new GameObject("LeftEye");
        leftEyeObj.transform.SetParent(parent.transform);
        leftEyeObj.transform.localPosition = new Vector3(-0.5f, 0f, 0f);
        SpriteRenderer leftRenderer = leftEyeObj.AddComponent<SpriteRenderer>();
        leftRenderer.color = new Color(0.8f, 0.2f, 1f);
        leftRenderer.sortingOrder = 1;
        CircleCollider2D leftCollider = leftEyeObj.AddComponent<CircleCollider2D>();
        leftCollider.radius = 0.3f;
        EyeMovement leftMovement = leftEyeObj.AddComponent<EyeMovement>();

        GameObject rightEyeObj = new GameObject("RightEye");
        rightEyeObj.transform.SetParent(parent.transform);
        rightEyeObj.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        SpriteRenderer rightRenderer = rightEyeObj.AddComponent<SpriteRenderer>();
        rightRenderer.color = new Color(0.8f, 0.2f, 1f);
        rightRenderer.sortingOrder = 1;
        CircleCollider2D rightCollider = rightEyeObj.AddComponent<CircleCollider2D>();
        rightCollider.radius = 0.3f;
        EyeMovement rightMovement = rightEyeObj.AddComponent<EyeMovement>();

        GameObject laserObj = new GameObject("EyeLaser");
        laserObj.transform.SetParent(parent.transform);
        laserObj.transform.localPosition = Vector3.zero;
        EyeLaser laser = laserObj.AddComponent<EyeLaser>();
        LineRenderer lineRenderer = laserObj.AddComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.numCapVertices = 5;
        lineRenderer.enabled = false;

        GameObject chargeObj = new GameObject("ChargeEffect");
        chargeObj.transform.SetParent(parent.transform);
        chargeObj.transform.localPosition = Vector3.zero;
        SpriteRenderer chargeRenderer = chargeObj.AddComponent<SpriteRenderer>();
        chargeRenderer.color = Color.yellow;
        chargeRenderer.sortingOrder = 2;
        chargeObj.SetActive(false);

        Sprite eyeSprite = CreateCircleSprite(new Color(0.8f, 0.2f, 1f), 32, "eye");
        leftRenderer.sprite = eyeSprite;
        rightRenderer.sprite = eyeSprite;

        Sprite chargeSprite = CreateCircleSprite(Color.yellow, 32, "charge");
        chargeRenderer.sprite = chargeSprite;

        GameObject hitEffectObj = new GameObject("HitEffect");
        hitEffectObj.transform.SetParent(laserObj.transform);
        hitEffectObj.transform.localPosition = Vector3.zero;
        SpriteRenderer hitRenderer = hitEffectObj.AddComponent<SpriteRenderer>();
        hitRenderer.color = Color.yellow;
        hitRenderer.sortingOrder = 1;
        Sprite hitSprite = CreateCircleSprite(Color.yellow, 16, "hit");
        hitRenderer.sprite = hitSprite;
        hitEffectObj.SetActive(false);

        controller.leftEye = leftEyeObj.transform;
        controller.rightEye = rightEyeObj.transform;
        controller.leftEyeMovement = leftMovement;
        controller.rightEyeMovement = rightMovement;
        controller.eyeLaser = laser;
        controller.chargeEffect = chargeObj;

        laser.laserLine = lineRenderer;
        laser.hitEffect = hitEffectObj;

        Gradient chargeGradient = new Gradient();
        chargeGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.yellow, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(1.0f, 1.0f) }
        );
        controller.chargeGradient = chargeGradient;

        Gradient laserGradient = new Gradient();
        laserGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.cyan, 0.0f), new GradientColorKey(Color.red, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.5f, 1.0f) }
        );
        laser.laserGradient = laserGradient;

        GameObject shardPrefab = CreateGlassShardPrefab("GlassShard");
        controller.glassShardPrefab = shardPrefab;

        string path = "Assets/Prefabs/FlyingEye.prefab";

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(parent, path);
        Debug.Log($"Created prefab at {path}");
        DestroyImmediate(parent);
        eyePrefab = prefab;
    }

    Sprite CreateCircleSprite(Color color, int size, string name)
    {
        Texture2D texture = new Texture2D(size, size);
        Color[] colors = new Color[size * size];

        float radius = size * 0.4f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - size / 2f;
                float dy = y - size / 2f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);

                if (distance < radius * 0.8f)
                {
                    colors[y * size + x] = color;
                }
                else if (distance < radius)
                {
                    float alpha = 1f - (distance - radius * 0.8f) / (radius * 0.2f);
                    colors[y * size + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    colors[y * size + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(colors);
        texture.Apply();
        texture.name = name;

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        return sprite;
    }

    GameObject CreateGlassShardPrefab(string name)
    {
        GameObject shard = new GameObject(name);

        SpriteRenderer renderer = shard.AddComponent<SpriteRenderer>();
        renderer.color = new Color(0.8f, 0.2f, 1f, 0.8f);
        renderer.sortingOrder = 1;

        Texture2D shardTexture = new Texture2D(8, 8);
        Color[] shardColors = new Color[64];
        for (int i = 0; i < 64; i++)
        {
            shardColors[i] = new Color(0.8f, 0.2f, 1f, 1f);
        }
        shardTexture.SetPixels(shardColors);
        shardTexture.Apply();

        Sprite shardSprite = Sprite.Create(shardTexture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 8);
        renderer.sprite = shardSprite;

        GlassShard script = shard.AddComponent<GlassShard>();

        Rigidbody2D rb = shard.AddComponent<Rigidbody2D>();
        rb.gravityScale = 1f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // правильно для 2D

        CircleCollider2D collider = shard.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.3f;

        string prefabPath = "Assets/Prefabs/GlassShard.prefab";

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(shard, prefabPath);
        DestroyImmediate(shard);
        return prefab;
    }
}