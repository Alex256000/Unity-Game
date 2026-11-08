#if UNITY_EDITOR

using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

public static class MainMenuCreator
{
    private const string RootName = "GeneratedMainMenu";
    private const string UndoName = "Create Main Menu";

    private static readonly Color BackgroundColor =
        new Color(0.055f, 0.065f, 0.085f, 1f);

    private static readonly Color ButtonColor =
        new Color(0.17f, 0.21f, 0.28f, 1f);

    private static readonly Color FrameColor =
        new Color(0.45f, 0.49f, 0.56f, 1f);

    [MenuItem("Tools/Create Main Menu")]
    public static void CreateMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Создание меню",
                "Сначала выйди из Play Mode.",
                "Хорошо"
            );

            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            EditorUtility.DisplayDialog(
                "Создание меню",
                "Сначала открой сцену главного меню.",
                "Хорошо"
            );

            return;
        }

        // Не создаём второе меню поверх уже существующего.
        if (FindInScene<MainMenuManager>(scene) != null)
        {
            EditorUtility.DisplayDialog(
                "Меню уже существует",
                "В этой сцене уже есть MainMenuManager.\n\n" +
                "Сохрани резервную копию сцены и удали старые " +
                "объекты меню перед созданием новых.\n\n" +
                "Скрипты и служебные менеджеры удалять не нужно.",
                "Хорошо"
            );

            return;
        }

        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            if (sceneRoot.name != RootName)
                continue;

            EditorUtility.DisplayDialog(
                "Объект уже существует",
                $"В сцене уже есть объект {RootName}.\n\n" +
                "Удали или переименуй его перед созданием меню.",
                "Хорошо"
            );

            return;
        }

        if (TMP_Settings.defaultFontAsset == null)
        {
            EditorUtility.DisplayDialog(
                "Нужны ресурсы TextMeshPro",
                "Не найден шрифт TextMeshPro по умолчанию.\n\n" +
                "Импортируй TMP Essential Resources через " +
                "Window → TextMeshPro и повтори создание меню.",
                "Хорошо"
            );

            return;
        }

        GameObject root = null;
        GameObject createdEventSystem = null;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName(UndoName);

        try
        {
            root = new GameObject(RootName);
            SceneManager.MoveGameObjectToScene(root, scene);

            // Менеджер находится вне скрываемого интерфейса.
            GameObject managerObject = new GameObject("MenuManager");
            managerObject.transform.SetParent(root.transform, false);

            MainMenuManager manager =
                managerObject.AddComponent<MainMenuManager>();

            // Canvas.
            GameObject canvasObject =
                CreateUI("MainMenuCanvas", root.transform);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler =
                canvasObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode =
                CanvasScaler.ScaleMode.ScaleWithScreenSize;

            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode =
                CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            // Фон остаётся видимым при подтверждении удаления.
            Image background = CreateImage(
                "Background",
                canvasObject.transform,
                BackgroundColor,
                false
            );

            Stretch(background.rectTransform);

            // Всё внутри этого контейнера скрывается
            // при открытии окна подтверждения.
            GameObject menuInterface =
                CreateUI("MenuInterface", canvasObject.transform);

            Stretch(Rect(menuInterface));

            TextMeshProUGUI title = CreateText(
                "Title",
                "Главное меню",
                menuInterface.transform,
                48f
            );

            Place(
                title.rectTransform,
                new Vector2(1000f, 90f),
                new Vector2(0f, 320f)
            );

            // Основные кнопки.
            GameObject mainButtons = CreateUI(
                "MainButtonsContainer",
                menuInterface.transform
            );

            Stretch(Rect(mainButtons));

            Button playButton = CreateButton(
                "PlayButton",
                "Играть",
                mainButtons.transform,
                new Vector2(320f, 70f),
                new Vector2(0f, 100f),
                ButtonColor
            );

            Button settingsButton = CreateButton(
                "SettingsButton",
                "Настройки",
                mainButtons.transform,
                new Vector2(320f, 70f),
                Vector2.zero,
                ButtonColor
            );

            Button exitButton = CreateButton(
                "ExitButton",
                "Выход",
                mainButtons.transform,
                new Vector2(320f, 70f),
                new Vector2(0f, -100f),
                ButtonColor
            );

            // Экран выбора ячеек.
            GameObject slotsContainer = CreateUI(
                "SlotsContainer",
                menuInterface.transform
            );

            Stretch(Rect(slotsContainer));

            TextMeshProUGUI slotsTitle = CreateText(
                "SlotsTitle",
                "Выберите ячейку сохранения",
                slotsContainer.transform,
                28f
            );

            Place(
                slotsTitle.rectTransform,
                new Vector2(1000f, 55f),
                new Vector2(0f, 220f)
            );

            SaveSlotView[] slotViews = new SaveSlotView[3];

            for (int i = 0; i < slotViews.Length; i++)
            {
                float x = (i - 1) * 340f;

                slotViews[i] = CreateSlot(
                    i + 1,
                    slotsContainer.transform,
                    new Vector2(x, 30f)
                );
            }

            Button backButton = CreateButton(
                "BackButton",
                "Назад",
                slotsContainer.transform,
                new Vector2(280f, 65f),
                new Vector2(0f, -200f),
                ButtonColor
            );

            TextMeshProUGUI statusText = CreateText(
                "StatusText",
                string.Empty,
                menuInterface.transform,
                22f
            );

            statusText.color = new Color(1f, 0.8f, 0.55f, 1f);

            Place(
                statusText.rectTransform,
                new Vector2(1100f, 100f),
                new Vector2(0f, -390f)
            );

            // Подтверждение — сосед MenuInterface,
            // а не его дочерний объект.
            Image confirmationBackground = CreateImage(
                "DeleteConfirmationPanel",
                canvasObject.transform,
                new Color(0f, 0f, 0f, 0.55f),
                true
            );

            Stretch(confirmationBackground.rectTransform);

            Image dialog = CreateImage(
                "Dialog",
                confirmationBackground.transform,
                new Color(0.11f, 0.13f, 0.18f, 1f),
                true
            );

            Place(
                dialog.rectTransform,
                new Vector2(680f, 340f),
                Vector2.zero
            );

            TextMeshProUGUI confirmationText = CreateText(
                "ConfirmationText",
                "Вы уверены, что хотите очистить " +
                "этот слот сохранения?",
                dialog.transform,
                27f
            );

            Place(
                confirmationText.rectTransform,
                new Vector2(620f, 200f),
                new Vector2(0f, 45f)
            );

            Button yesButton = CreateButton(
                "YesButton",
                "Да",
                dialog.transform,
                new Vector2(220f, 65f),
                new Vector2(-135f, -115f),
                new Color(0.42f, 0.18f, 0.19f, 1f)
            );

            Button noButton = CreateButton(
                "NoButton",
                "Нет",
                dialog.transform,
                new Vector2(220f, 65f),
                new Vector2(135f, -115f),
                ButtonColor
            );

            // Автоматическое назначение полей менеджера.
            SerializedObject serializedManager =
                new SerializedObject(manager);

            serializedManager.Update();

            SetReference(
                serializedManager,
                "menuInterface",
                menuInterface
            );

            SetReference(
                serializedManager,
                "mainButtonsContainer",
                mainButtons
            );

            SetReference(
                serializedManager,
                "slotsContainer",
                slotsContainer
            );

            SetReference(serializedManager, "playButton", playButton);
            SetReference(
                serializedManager,
                "settingsButton",
                settingsButton
            );
            SetReference(serializedManager, "exitButton", exitButton);
            SetReference(serializedManager, "backButton", backButton);

            SetReference(
                serializedManager,
                "deleteConfirmationPanel",
                confirmationBackground.gameObject
            );

            SetReference(
                serializedManager,
                "confirmationText",
                confirmationText
            );

            SetReference(serializedManager, "yesButton", yesButton);
            SetReference(serializedManager, "noButton", noButton);
            SetReference(serializedManager, "statusText", statusText);

            SerializedProperty slotsProperty =
                RequireProperty(serializedManager, "slots");

            if (!slotsProperty.isArray)
            {
                throw new InvalidOperationException(
                    "Поле slots должно быть массивом SaveSlotView."
                );
            }

            slotsProperty.arraySize = slotViews.Length;

            for (int i = 0; i < slotViews.Length; i++)
            {
                slotsProperty.GetArrayElementAtIndex(i)
                    .objectReferenceValue = slotViews[i];
            }

            RequireProperty(
                serializedManager,
                "firstLevelSceneName"
            ).stringValue = "Level_01";

            RequireProperty(
                serializedManager,
                "bootSceneName"
            ).stringValue = "PreLoadScene";

            serializedManager.ApplyModifiedPropertiesWithoutUndo();

            // Существующую систему ввода не меняем.
            if (FindInScene<EventSystem>(scene) == null)
            {
                createdEventSystem = new GameObject("EventSystem");
                SceneManager.MoveGameObjectToScene(
                    createdEventSystem,
                    scene
                );

                createdEventSystem.AddComponent<EventSystem>();

#if ENABLE_INPUT_SYSTEM
                InputSystemUIInputModule inputModule =
                    createdEventSystem
                        .AddComponent<InputSystemUIInputModule>();

                inputModule.AssignDefaultActions();
#else
                createdEventSystem
                    .AddComponent<StandaloneInputModule>();
#endif
            }

            // Начальное состояние.
            mainButtons.SetActive(true);
            slotsContainer.SetActive(false);
            confirmationBackground.gameObject.SetActive(false);

            // Созданное меню можно отменить одним Undo.
            Undo.RegisterCreatedObjectUndo(root, UndoName);

            if (createdEventSystem != null)
            {
                Undo.RegisterCreatedObjectUndo(
                    createdEventSystem,
                    UndoName
                );
            }

            Undo.CollapseUndoOperations(undoGroup);

            EditorSceneManager.MarkSceneDirty(scene);
            Selection.activeGameObject = managerObject;

            Debug.Log(
                "[MainMenuCreator] Меню создано, ссылки назначены. " +
                "Проверь имена сцен в MenuManager и сохрани сцену.",
                manager
            );
        }
        catch (Exception exception)
        {
            // При несовпадении версий скриптов не оставляем
            // в сцене наполовину созданный интерфейс.
            if (root != null)
                UnityEngine.Object.DestroyImmediate(root);

            if (createdEventSystem != null)
            {
                UnityEngine.Object.DestroyImmediate(
                    createdEventSystem
                );
            }

            Debug.LogException(exception);

            EditorUtility.DisplayDialog(
                "Меню не создано",
                "Создание прервано:\n\n" +
                exception.Message +
                "\n\nПроверь, что установлены последние версии " +
                "MainMenuManager и SaveSlotView из инструкции.",
                "Хорошо"
            );
        }
    }
    private static SaveSlotView CreateSlot(
        int number,
        Transform parent,
        Vector2 position)
    {
        // Общая область наведения — сама ячейка.
        Image frame = CreateImage(
            $"Slot{number}",
            parent,
            FrameColor,
            true
        );

        Place(
            frame.rectTransform,
            new Vector2(300f, 200f),
            position
        );

        // Основная кнопка и крестик — соседние объекты.
        Button contentButton = CreateButton(
            "ContentButton",
            null,
            frame.transform,
            Vector2.zero,
            Vector2.zero,
            new Color(0.08f, 0.095f, 0.13f, 1f)
        );

        Stretch(Rect(contentButton.gameObject), 3f);
        contentButton.transition = Selectable.Transition.None;
        // Пустая ячейка.
        GameObject emptyContent = CreateUI(
            "EmptyContent",
            contentButton.transform);

        Stretch(Rect(emptyContent));

        TextMeshProUGUI plus = CreateText(
            "PlusText",
            "+",
            emptyContent.transform,
            68f);

        Place(
            plus.rectTransform,
            new Vector2(140f, 95f),
            new Vector2(0f, 20f));

        TextMeshProUGUI hint = CreateText(
            "HintText",
            "Новая игра",
            emptyContent.transform,
            23f);

        Place(
            hint.rectTransform,
            new Vector2(270f, 45f),
            new Vector2(0f, -58f));

        // Заполненная ячейка.
        GameObject filledContent = CreateUI(
            "FilledContent",
            contentButton.transform);

        Stretch(Rect(filledContent));

        Image preview = CreateImage(
            "PreviewImage",
            filledContent.transform,
            Color.white,
            false);

        Stretch(preview.rectTransform);
        preview.preserveAspect = true;
        // Затемнение под подписью не мешает кликам.
        Image textShade = CreateImage(
            "TextShade",
            filledContent.transform,
            new Color(0f, 0f, 0f, 0.7f),
            false
        );

        RectTransform shadeRect = textShade.rectTransform;
        shadeRect.anchorMin = new Vector2(0f, 0f);
        shadeRect.anchorMax = new Vector2(1f, 0f);
        shadeRect.pivot = new Vector2(0.5f, 0f);
        shadeRect.sizeDelta = new Vector2(0f, 78f);
        shadeRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI infoText = CreateText(
            "InfoText",
            $"Слот {number}\n0 ч 00 мин",
            filledContent.transform,
            24f
        );

        Place(
            infoText.rectTransform,
            new Vector2(275f, 70f),
            new Vector2(0f, -58f)
        );
        // Крестик создаётся последним: он поверх ContentButton.
        Button deleteButton = CreateButton(
            "DeleteButton",
            "×",
            frame.transform,
            new Vector2(36f, 36f),
            Vector2.zero,
            new Color(0.5f, 0.17f, 0.18f, 1f)
        );

        RectTransform deleteRect = Rect(deleteButton.gameObject);
        deleteRect.anchorMin = Vector2.one;
        deleteRect.anchorMax = Vector2.one;
        deleteRect.pivot = Vector2.one;
        deleteRect.anchoredPosition = new Vector2(-8f, -8f);

        Navigation navigation = deleteButton.navigation;
        navigation.mode = Navigation.Mode.None;
        deleteButton.navigation = navigation;

        TextMeshProUGUI cross =
            deleteButton.GetComponentInChildren<TextMeshProUGUI>();

        cross.fontSize = 30f;

        // Скрипт получает все ссылки автоматически.
        SaveSlotView view =
            frame.gameObject.AddComponent<SaveSlotView>();

        SerializedObject serializedView =
            new SerializedObject(view);

        serializedView.Update();

        SetReference(serializedView, "openButton", contentButton);
        SetReference(serializedView, "deleteButton", deleteButton);
        SetReference(serializedView, "emptyContent", emptyContent);
        SetReference(serializedView, "filledContent", filledContent);
        SetReference(serializedView, "infoText", infoText);
        SetReference(serializedView, "previewImage", preview);

        RequireProperty(
            serializedView,
            "normalColor"
        ).colorValue = new Color(0.65f, 0.65f, 0.65f, 1f);

        RequireProperty(
            serializedView,
            "hoverColor"
        ).colorValue = Color.white;

        serializedView.ApplyModifiedPropertiesWithoutUndo();

        emptyContent.SetActive(true);
        filledContent.SetActive(false);
        deleteButton.gameObject.SetActive(false);

        return view;
    }
    private static Button CreateButton(
        string name,
        string caption,
        Transform parent,
        Vector2 size,
        Vector2 position,
        Color color)
    {
        Image image = CreateImage(name, parent, color, true);

        Place(image.rectTransform, size, position);

        Button button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor =
            new Color(1.18f, 1.18f, 1.18f, 1f);
        colors.pressedColor =
            new Color(0.75f, 0.75f, 0.75f, 1f);
        colors.selectedColor = Color.white;
        colors.fadeDuration = 0.1f;

        button.colors = colors;

        if (!string.IsNullOrEmpty(caption))
        {
            TextMeshProUGUI label = CreateText(
                "Text",
                caption,
                image.transform,
                28f
            );

            Stretch(label.rectTransform, 4f);
        }

        return button;
    }
    private static TextMeshProUGUI CreateText(
        string name,
        string text,
        Transform parent,
        float fontSize)
    {
        GameObject go = CreateUI(name, parent);

        TextMeshProUGUI label =
            go.AddComponent<TextMeshProUGUI>();

        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;

        // Надписи не перекрывают кнопки.
        label.raycastTarget = false;

        return label;
    }
    private static Image CreateImage(
        string name,
        Transform parent,
        Color color,
        bool raycastTarget)
    {
        GameObject go = CreateUI(name, parent);
        Image image = go.AddComponent<Image>();

        image.color = color;
        image.raycastTarget = raycastTarget;

        return image;
    }
    private static GameObject CreateUI(
        string name,
        Transform parent)
    {
        GameObject go = new GameObject(
            name,
            typeof(RectTransform)
        );

        go.transform.SetParent(parent, false);

        int uiLayer = LayerMask.NameToLayer("UI");

        if (uiLayer >= 0)
            go.layer = uiLayer;

        return go;
    }
    private static RectTransform Rect(GameObject go)
    {
        return go.GetComponent<RectTransform>();
    }
    private static void Stretch(
        RectTransform rect,
        float inset = 0f)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }
    private static void Place(
        RectTransform rect,
        Vector2 size,
        Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }
    private static void SetReference(
        SerializedObject target,
        string propertyName,
        UnityEngine.Object value)
    {
        RequireProperty(
            target,
            propertyName
        ).objectReferenceValue = value;
    }
    private static SerializedProperty RequireProperty(
        SerializedObject target,
        string propertyName)
    {
        SerializedProperty property =
            target.FindProperty(propertyName);

        if (property == null)
        {
            throw new InvalidOperationException(
                $"В {target.targetObject.GetType().Name} " +
                $"не найдено поле '{propertyName}'. " +
                "Версия скрипта не совпадает с генератором."
            );
        }

        return property;
    }
    private static T FindInScene<T>(Scene scene)
        where T : Component
    {
        // Проверяем также выключенные объекты.
        foreach (GameObject sceneRoot in scene.GetRootGameObjects())
        {
            T component =
                sceneRoot.GetComponentInChildren<T>(true);

            if (component != null)
                return component;
        }

        return null;
    }
}
#endif