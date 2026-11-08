using UnityEngine;
using UnityEditor;

public static class GroupSelectedEditor
{
    private const string menuRoot = "Tools/Group Selected";

    [MenuItem(menuRoot + "/Into New Group")]
    public static void GroupIntoNew()
    {
        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Group Selected", "Ничего не выделено.", "ОК");
            return;
        }

        string baseName = "Group";
        CreateGroupAndParentSelected(baseName);
    }

    [MenuItem(menuRoot + "/Into 'Enemies' Group")]
    public static void GroupIntoEnemies()
    {
        CreateOrUseContainer("Enemies");
    }

    [MenuItem(menuRoot + "/Into 'Platforms' Group")]
    public static void GroupIntoPlatforms()
    {
        CreateOrUseContainer("Platforms");
    }

    [MenuItem(menuRoot + "/Into 'Environment' Group")]
    public static void GroupIntoEnvironment()
    {
        CreateOrUseContainer("Environment");
    }

    private static void CreateOrUseContainer(string containerName)
    {
        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0)
        {
            EditorUtility.DisplayDialog("Group Selected", "Ничего не выделено.", "ОК");
            return;
        }

        GameObject container = GameObject.Find(containerName);
        if (container == null)
        {
            container = new GameObject(containerName);
            Undo.RegisterCreatedObjectUndo(container, "Create " + containerName);
        }

        foreach (var t in selected)
        {
            Undo.SetTransformParent(t, container.transform, "Group into " + containerName);
        }

        Selection.activeGameObject = container;
        EditorGUIUtility.PingObject(container);
    }

    private static void CreateGroupAndParentSelected(string baseName)
    {
        var selected = Selection.transforms;
        if (selected == null || selected.Length == 0) return;

        string name = NextName(baseName + "_");
        GameObject container = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(container, "Create " + name);

        // Place container at average position of selected or at Scene view pivot
        Vector3 avg = Vector3.zero;
        foreach (var t in selected) avg += t.position;
        avg /= selected.Length;
        container.transform.position = avg;

        foreach (var t in selected)
        {
            Undo.SetTransformParent(t, container.transform, "Group into " + name);
        }

        Selection.activeGameObject = container;
        EditorGUIUtility.PingObject(container);
    }

    private static string NextName(string prefix)
    {
        int i = 1;
        while (GameObject.Find(prefix + i) != null) i++;
        return prefix + i;
    }
}