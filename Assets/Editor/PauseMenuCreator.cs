#if UNITY_EDITOR

using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class PauseMenuCreator
{
    [MenuItem("Tools/Create Pause Menu")]
    public static void CreatePauseMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Меню паузы",
                "Сначала выйди из Play Mode.",
                "Хорошо"
            );

            return;
        }

        GameObject selected = Selection.activeGameObject;

        PlayerUIController ui = selected != null
            ? selected.GetComponent<PlayerUIController>()
            : null;

        if (ui == null || !selected.scene.IsValid())
        {
            EditorUtility.DisplayDialog(
                "Выбери игровой интерфейс",
                "Открой префаб PlayerUI_Persistent " +
                "в режиме редактирования префаба " +
                "и выбери его корневой объект " +
                "с PlayerUIController.",
                "Хорошо"
            );

            return;
        }

        if (ui.GetComponentInChildren<PauseMenuController>(true) != null)
        {
            EditorUtility.DisplayDialog(
                "Меню уже существует",
                "У этого интерфейса уже есть PauseMenuController.",
                "Хорошо"
            );

            return;
        }

        // Используем шрифт существующих надписей, если он есть.
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;

        foreach (TextMeshProUGUI text in
                 ui.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (text.font == null)
                continue;

            font = text.font;
            break;
        }

        if (font == null)
        {
            EditorUtility.DisplayDialog(
                "Нужен шрифт",
                "Сначала настрой ресурсы TextMeshPro.",
                "Хорошо"
            );

            return;
        }

        GameObject root = null;

        try
        {
            root = CreateUI("PauseMenu", ui.transform);
            Stretch(Rect(root));

            // Вложенный Canvas наследует экранное пространство
            // родительского Canvas, но рисуется поверх игровых шкал.
            Canvas canvas = root.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 2000;

            root.AddComponent<GraphicRaycaster>();

            PauseMenuController controller =
                root.AddComponent<PauseMenuController>();

            Image shade = CreateImage(
                "PauseWindow",
                root.transform,
                new Color(0f, 0f, 0f, 0.72f)
            );

            Stretch(shade.rectTransform);

            Image main = CreateImage(
                "MainPanel",
                shade.transform,
                new Color(0.10f, 0.12f, 0.17f, 1f)
            );

            Place(main.rectTransform, 620f, 500f, 0f, 0f);

            TextMeshProUGUI title =
                CreateText("Title", "Пауза", main.transform, font, 42f);

            Place(title.rectTransform, 540f, 70f, 0f, 170f);

            Button resume = CreateButton(
                "ResumeButton", "Продолжить",
                main.transform, font, 75f
            );

            Button menu = CreateButton(
                "MainMenuButton", "В главное меню",
                main.transform, font, -15f
            );

            Button quit = CreateButton(
                "QuitButton", "Выйти из игры",
                main.transform, font, -105f
            );

            Image confirmation = CreateImage(
                "ConfirmationPanel",
                shade.transform,
                new Color(0.10f, 0.12f, 0.17f, 1f)
            );

            Place(confirmation.rectTransform, 760f, 430f, 0f, 0f);

            TextMeshProUGUI question = CreateText(
                "ConfirmationText", string.Empty,
                confirmation.transform, font, 27f
            );

            Place(question.rectTransform, 680f, 210f, 0f, 65f);

            Button yes = CreateButton(
                "YesButton", "Да",
                confirmation.transform, font, -125f
            );

            Button no = CreateButton(
                "NoButton", "Нет",
                confirmation.transform, font, -125f
            );

            Place(Rect(yes.gameObject), 240f, 65f, -145f, -125f);
            Place(Rect(no.gameObject), 240f, 65f, 145f, -125f);

            TextMeshProUGUI status = CreateText(
                "StatusText", string.Empty,
                shade.transform, font, 22f
            );

            status.color = new Color(1f, 0.75f, 0.55f, 1f);

            Place(status.rectTransform, 1050f, 120f, 0f, -350f);

            SerializedObject data = new SerializedObject(controller);
            data.Update();

            Set(data, "pauseWindow", shade.gameObject);
            Set(data, "mainPanel", main.gameObject);
            Set(data, "confirmationPanel", confirmation.gameObject);

            Set(data, "resumeButton", resume);
            Set(data, "mainMenuButton", menu);
            Set(data, "quitButton", quit);

            Set(data, "confirmationText", question);
            Set(data, "yesButton", yes);
            Set(data, "noButton", no);
            Set(data, "statusText", status);

            data.ApplyModifiedPropertiesWithoutUndo();

            confirmation.gameObject.SetActive(false);
            shade.gameObject.SetActive(false);

            Undo.RegisterCreatedObjectUndo(root, "Create Pause Menu");
            EditorSceneManager.MarkSceneDirty(root.scene);

            Selection.activeGameObject = root;

            Debug.Log(
                "Меню паузы создано. Проверь имена сцен, " +
                "режим сохранения и сохрани префаб.",
                root
            );
        }
        catch (Exception exception)
        {
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);

            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Меню не создано",
                exception.Message,
                "Хорошо"
            );
        }
    }

    private static GameObject CreateUI(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.layer = parent.gameObject.layer;
        return go;
    }

    private static Image CreateImage(
        string name, Transform parent, Color color)
    {
        GameObject go = CreateUI(name, parent);
        Image image = go.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = true;

        return image;
    }

    private static TextMeshProUGUI CreateText(
        string name,
        string value,
        Transform parent,
        TMP_FontAsset font,
        float size)
    {
        GameObject go = CreateUI(name, parent);

        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(
        string name,
        string label,
        Transform parent,
        TMP_FontAsset font,
        float y)
    {
        Image image = CreateImage(
            name, parent,
            new Color(0.20f, 0.25f, 0.33f, 1f)
        );

        Place(image.rectTransform, 400f, 65f, 0f, y);

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        // Без анимаций, зависящих от остановленного времени.
        button.transition = Selectable.Transition.ColorTint;

        TextMeshProUGUI caption =
            CreateText("Text", label, image.transform, font, 27f);

        Stretch(caption.rectTransform);
        return button;
    }

    private static RectTransform Rect(GameObject go)
    {
        return go.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(
        RectTransform rect,
        float width,
        float height,
        float x,
        float y)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private static void Set(
        SerializedObject data,
        string field,
        UnityEngine.Object value)
    {
        SerializedProperty property = data.FindProperty(field);

        if (property == null)
            throw new InvalidOperationException(
                "Версия PauseMenuController не совпадает " +
                "с генератором. Не найдено поле: " + field
            );

        property.objectReferenceValue = value;
    }
}

#endif
