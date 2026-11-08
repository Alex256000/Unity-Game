using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public static class CreatePlayerUIEditor
{
    [MenuItem("Tools/Create Player Persistent UI")]
    public static void CreatePlayerUI()
    {
        // Если уже есть объект с таким именем — выберем его
        var existing = GameObject.Find("PlayerUI_Persistent");
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            Debug.Log("PlayerUI_Persistent уже существует в сцене. Выбран существующий объект.");
            return;
        }

        // Root (будет DontDestroyOnLoad через PlayerUIController)
        var root = new GameObject("PlayerUI_Persistent");
        Undo.RegisterCreatedObjectUndo(root, "Create PlayerUI_Persistent");

        // Canvas
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>();
        root.AddComponent<GraphicRaycaster>();

        // PlayerUIController (скрипт должен быть в проекте)
        var playerUI = root.AddComponent<PlayerUIController>();

        // --- Health bar setup ---
        var healthContainer = new GameObject("HealthBarContainer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(healthContainer, "Create HealthBarContainer");
        healthContainer.transform.SetParent(root.transform, false);
        var hcRect = healthContainer.GetComponent<RectTransform>();
        hcRect.anchorMin = new Vector2(0.5f, 1f);
        hcRect.anchorMax = new Vector2(0.5f, 1f);
        hcRect.pivot = new Vector2(0.5f, 1f);
        hcRect.anchoredPosition = new Vector2(0f, -10f);
        hcRect.sizeDelta = new Vector2(300f, 48f);

        // Health image (single Image that will be swapped via HealthBarSpriteMapper)
        var healthImageGO = new GameObject("HealthBarImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(healthImageGO, "Create HealthBarImage");
        healthImageGO.transform.SetParent(healthContainer.transform, false);
        var hiRect = healthImageGO.GetComponent<RectTransform>();
        hiRect.anchorMin = new Vector2(0f, 0f);
        hiRect.anchorMax = new Vector2(1f, 1f);
        hiRect.offsetMin = Vector2.zero;
        hiRect.offsetMax = Vector2.zero;

        var healthMapper = healthImageGO.AddComponent<HealthBarSpriteMapper>();
        // Ensure barImage reference
        healthMapper.barImage = healthImageGO.GetComponent<Image>();

        // --- Soul display setup ---
        var soulContainer = new GameObject("SoulDisplayContainer", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(soulContainer, "Create SoulDisplayContainer");
        soulContainer.transform.SetParent(root.transform, false);
        var scRect = soulContainer.GetComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0.5f, 1f);
        scRect.anchorMax = new Vector2(0.5f, 1f);
        scRect.pivot = new Vector2(0.5f, 1f);
        scRect.anchoredPosition = new Vector2(0f, -70f);
        scRect.sizeDelta = new Vector2(240f, 48f);

        // Add layout group so vessels are laid out horizontally
        var hlg = soulContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 6f;
        hlg.childControlHeight = false;
        hlg.childControlWidth = false;

        // RuntimeVesselsSoulUI on soul container
        var soulUI = soulContainer.AddComponent<RuntimeVesselsSoulUI>();

        // Create a hidden child to serve as "vessel prefab" (RuntimeVesselsSoulUI will Instantiate this)
        var vesselPrefab = new GameObject("VesselPrefab", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        Undo.RegisterCreatedObjectUndo(vesselPrefab, "Create VesselPrefab");
        vesselPrefab.transform.SetParent(soulContainer.transform, false);
        var vpRect = vesselPrefab.GetComponent<RectTransform>();
        vpRect.sizeDelta = new Vector2(32f, 32f);
        var vesselImage = vesselPrefab.GetComponent<Image>();
        vesselImage.raycastTarget = false;
        // Hide the prototype in hierarchy at runtime; we keep it inactive so runtime clones look correct
        vesselPrefab.SetActive(false);

        // --- Wire references into PlayerUIController (private serialized fields) using SerializedObject ---
        var so = new SerializedObject(playerUI);
        so.FindProperty("persistBetweenScenes").boolValue = true;
        so.FindProperty("debugLog").boolValue = false;
        so.FindProperty("healthBarContainer").objectReferenceValue = healthContainer;
        so.FindProperty("healthBar").objectReferenceValue = healthMapper;
        so.FindProperty("soulDisplayContainer").objectReferenceValue = soulContainer;
        so.FindProperty("soulUIBehaviour").objectReferenceValue = soulUI;
        so.ApplyModifiedProperties();

        // Wire vesselPrefab into RuntimeVesselsSoulUI (private serialized field)
        var rso = new SerializedObject(soulUI);
        rso.FindProperty("vesselPrefab").objectReferenceValue = vesselPrefab;
        // Set a couple of sensible runtime defaults
        rso.FindProperty("setNativeSize").boolValue = true;
        rso.ApplyModifiedProperties();

        // Select result
        Selection.activeGameObject = root;

        Debug.Log("Player UI created: PlayerUI_Persistent. Назначьте спрайты для HealthBarSpriteMapper.healthSprites (length = maxHealth + 1) и для RuntimeVesselsSoulUI (full/empty/partial).");
    }
}