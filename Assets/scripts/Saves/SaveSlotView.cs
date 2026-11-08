using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaveSlotView : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Кнопки")]
    [SerializeField] private Button openButton;
    [SerializeField] private Button deleteButton;

    [Header("Состояния ячейки")]
    [SerializeField] private GameObject emptyContent;
    [SerializeField] private GameObject filledContent;
    [SerializeField] private TextMeshProUGUI infoText;

    [Header("Изображение занятой ячейки")]
    [SerializeField] private Image previewImage;

    [SerializeField] private Color normalColor =
        new Color(0.65f, 0.65f, 0.65f, 1f);

    [SerializeField] private Color hoverColor = Color.white;

    private MainMenuManager menu;
    private int slotIndex;

    private bool hasSave;
    private bool pointerInside;

    public bool IsConfigured =>
        openButton != null &&
        deleteButton != null &&
        emptyContent != null &&
        filledContent != null &&
        infoText != null;

    /// <summary>
    /// Номер ячейки назначает менеджер:
    /// первый элемент массива — слот 1 и так далее.
    /// </summary>
    public void Initialize(
        MainMenuManager owner,
        int index)
    {
        menu = owner;
        slotIndex = index;

        // Повторная инициализация не создаёт дубли обработчиков.
        openButton.onClick.RemoveListener(OpenSlot);
        deleteButton.onClick.RemoveListener(RequestDelete);

        openButton.onClick.AddListener(OpenSlot);
        deleteButton.onClick.AddListener(RequestDelete);

        pointerInside = false;
        UpdateVisuals();
    }

    public void SetState(bool occupied, string description)
    {
        hasSave = occupied;

        infoText.text = description;

        // После обновления меню крестик появится
        // при следующем наведении.
        pointerInside = false;
        UpdateVisuals();
    }

    private void OnEnable()
    {
        pointerInside = false;
        UpdateVisuals();
    }

    private void OnDisable()
    {
        pointerInside = false;

        if (deleteButton != null)
            deleteButton.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OpenSlot);

        if (deleteButton != null)
            deleteButton.onClick.RemoveListener(RequestDelete);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        UpdateVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (emptyContent != null)
            emptyContent.SetActive(!hasSave);

        if (filledContent != null)
            filledContent.SetActive(hasSave);

        if (deleteButton != null)
        {
            deleteButton.gameObject.SetActive(
                hasSave && pointerInside
            );
        }

        if (previewImage != null)
        {
            previewImage.color =
                hasSave && pointerInside
                    ? hoverColor
                    : normalColor;
        }
    }

    private void OpenSlot()
    {
        if (menu != null)
            menu.OpenSlot(slotIndex);
    }

    private void RequestDelete()
    {
        if (!hasSave || menu == null)
            return;

        menu.DeleteSlot(slotIndex);
    }
}
