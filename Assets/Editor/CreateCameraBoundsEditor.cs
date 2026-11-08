using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class CreateCameraBoundsEditor
{
    private const string menuRoot = "Tools/Create Camera Bounds";
    private const string boundsTag = "CameraBounds";

    [MenuItem(menuRoot + "/Rectangle Bounds")]
    public static void CreateRectangleBounds()
    {
        EnsureTagExists(boundsTag);
        Vector2 size = new Vector2(20f, 10f);
        CreatePolygonBoundsAtScene("CameraBounds_Rect", size);
    }

    [MenuItem(menuRoot + "/Polygon Bounds (editable)")]
    public static void CreatePolygonBounds()
    {
        EnsureTagExists(boundsTag);
        Vector2 size = new Vector2(10f, 6f);
        CreatePolygonBoundsAtScene("CameraBounds_Poly", size);
    }

    [MenuItem(menuRoot + "/From Selected Tilemap")]
    public static void CreateBoundsFromSelectedTilemap()
    {
        EnsureTagExists(boundsTag);

        var go = Selection.activeGameObject;
        if (go == null)
        {
            EditorUtility.DisplayDialog("Create Camera Bounds", "Выберите в иерархии объект с Tilemap.", "OK");
            return;
        }

        // Проверяем наличие Tilemap (UnityEngine.Tilemaps)
        var tilemap = go.GetComponentInChildren<UnityEngine.Tilemaps.Tilemap>();
        if (tilemap == null)
        {
            EditorUtility.DisplayDialog("Create Camera Bounds", "Выбранный объект не содержит Tilemap.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(go, "Add Camera Bounds Components");

        // Добавляем TilemapCollider2D (если нет)
        var tilemapCollider = go.GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>();
        if (tilemapCollider == null)
            tilemapCollider = Undo.AddComponent<UnityEngine.Tilemaps.TilemapCollider2D>(go);

        // Настраиваем для использования Composite (UPDATED для Unity 2023)
        tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        // Rigidbody2D (static) обязателен для CompositeCollider2D
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb == null)
            rb = Undo.AddComponent<Rigidbody2D>(go);
        rb.bodyType = RigidbodyType2D.Static;
        rb.simulated = true;

        // CompositeCollider2D
        var composite = go.GetComponent<CompositeCollider2D>();
        if (composite == null)
            composite = Undo.AddComponent<CompositeCollider2D>(go);
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons;
        composite.generationType = CompositeCollider2D.GenerationType.Synchronous;

        // Устанавливаем тег
        go.tag = boundsTag;

        EditorSceneManager.MarkSceneDirty(go.scene);
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);

        Debug.Log($"[CreateCameraBoundsEditor] Added Composite bounds to Tilemap object '{go.name}'. Tag set to '{boundsTag}'.");
    }

    // === Implementation ===

    private static void CreatePolygonBoundsAtScene(string baseName, Vector2 size)
    {
        // Создаём корневой объект
        string name = NextName(baseName);
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        // Расположим в позиции SceneView pivot или (0,0,0)
        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null)
            go.transform.position = sceneView.pivot;
        else
            go.transform.position = Vector3.zero;

        // PolygonCollider2D и дефолтный прямоугольный контур
        var poly = Undo.AddComponent<PolygonCollider2D>(go);
        Vector2 half = size * 0.5f;
        Vector2[] rect = new Vector2[]
        {
            new Vector2(-half.x, -half.y),
            new Vector2(-half.x, half.y),
            new Vector2(half.x, half.y),
            new Vector2(half.x, -half.y)
        };
        poly.points = rect;
        poly.isTrigger = false;

        // Rigidbody2D static (нужно для некоторых конфигов и чтобы confiner корректно работал с Composite)
        var rb = Undo.AddComponent<Rigidbody2D>(go);
        rb.bodyType = RigidbodyType2D.Static;

        // Устанавливаем тег
        EnsureTagExists(boundsTag);
        go.tag = boundsTag;

        // Select and ping, mark scene dirty
        Selection.activeGameObject = go;
        EditorSceneManager.MarkSceneDirty(go.scene);
        EditorGUIUtility.PingObject(go);

        Debug.Log($"[CreateCameraBoundsEditor] Created polygon bounds '{name}' at {go.transform.position} with tag '{boundsTag}'.");
    }

    private static string NextName(string prefix)
    {
        int i = 1;
        while (GameObject.Find(prefix + i) != null) i++;
        return prefix + i;
    }

    // Добавляет тег в TagManager если его нет
    private static void EnsureTagExists(string tag)
    {
        if (string.IsNullOrEmpty(tag)) return;

        // Получаем TagManager asset
        var manager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tagsProp = manager.FindProperty("tags");

        // Проверяем наличие
        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            var t = tagsProp.GetArrayElementAtIndex(i).stringValue;
            if (t == tag) return;
        }

        // Добавляем новый тег
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
        manager.ApplyModifiedProperties();
        Debug.Log($"[CreateCameraBoundsEditor] Tag '{tag}' added to TagManager.");
    }
}