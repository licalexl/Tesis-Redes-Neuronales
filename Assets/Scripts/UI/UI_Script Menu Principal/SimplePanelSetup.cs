using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script helper para configurar rápidamente un panel simple
/// Añade este script a cualquier panel y configura automáticamente todo lo necesario
/// </summary>
public class SimplePanelSetup : MonoBehaviour
{
    [Header("Configuración Básica")]
    public string panelID = "MyPanel";
    public Button toggleButton;

    [Header("Configuración de Pin")]
    public bool createPinButton = true;
    public Vector2 pinButtonPosition = new Vector2(-30, 30);
    public Vector2 pinButtonSize = new Vector2(30, 30);
    public Sprite pinnedIcon;
    public Sprite unpinnedIcon;

    [Header("Configuración de Arrastre")]
    public bool makeDraggable = true;
    public bool constrainToScreen = true;
    public Transform dragAreaTransform; // Opcional: área específica para arrastrar

    [Header("Configuración de Animación")]
    public float animationSpeed = 5f;
    public Vector3 closedOffset = new Vector3(-300, 0, 0);

    [Header("Auto Setup")]
    public bool autoSetupOnStart = true;

    void Start()
    {
        if (autoSetupOnStart)
        {
            SetupPanel();
        }
    }

    [ContextMenu("Setup Panel")]
    public void SetupPanel()
    {
        Debug.Log($"Configurando panel: {panelID}");

        // 1. Configurar SimplePanelController
        SetupPanelController();

        // 2. Configurar arrastre si es necesario
        if (makeDraggable)
        {
            SetupDraggable();
        }

        // 3. Crear botón de pin si es necesario
        if (createPinButton)
        {
            CreatePinButton();
        }

        Debug.Log($"Panel {panelID} configurado correctamente!");
    }

    private void SetupPanelController()
    {
        SimplePanelController controller = GetComponent<SimplePanelController>();
        if (controller == null)
        {
            controller = gameObject.AddComponent<SimplePanelController>();
        }

        // Configurar propiedades básicas
        controller.panelID = panelID;
        controller.toggleButton = toggleButton;
        controller.animationSpeed = animationSpeed;
        controller.closedPosition = GetComponent<RectTransform>().localPosition + closedOffset;

        // Asegurar que tenemos sprites para el pin
        if (pinnedIcon != null) controller.pinnedSprite = pinnedIcon;
        if (unpinnedIcon != null) controller.unpinnedSprite = unpinnedIcon;
    }

    private void SetupDraggable()
    {
        SimpleDraggablePanel draggable = GetComponent<SimpleDraggablePanel>();
        if (draggable == null)
        {
            draggable = gameObject.AddComponent<SimpleDraggablePanel>();
        }

        draggable.isDraggable = true;
        draggable.constrainToScreen = constrainToScreen;
        draggable.showDragFeedback = true;
        draggable.bringToFrontOnDrag = true;

        // Configurar área de arrastre si se especificó
        if (dragAreaTransform != null)
        {
            RectTransform dragAreaRect = dragAreaTransform.GetComponent<RectTransform>();
            if (dragAreaRect != null)
            {
                draggable.SetDragArea(dragAreaRect);
            }
        }
    }

    private void CreatePinButton()
    {
        // Buscar si ya existe un botón de pin
        Transform existingPin = transform.Find("PinButton");
        if (existingPin != null)
        {
            // Usar el existente
            Button existingButton = existingPin.GetComponent<Button>();
            if (existingButton != null)
            {
                AssignPinButton(existingButton);
                return;
            }
        }

        // Crear nuevo botón de pin
        GameObject pinButtonGO = new GameObject("PinButton");
        pinButtonGO.transform.SetParent(transform, false);

        // Añadir componentes
        Image buttonImage = pinButtonGO.AddComponent<Image>();
        Button button = pinButtonGO.AddComponent<Button>();

        // Configurar imagen
        if (unpinnedIcon != null)
        {
            buttonImage.sprite = unpinnedIcon;
        }
        else
        {
            // Crear un sprite simple si no hay ninguno
            buttonImage.color = Color.white;
        }

        // Configurar RectTransform
        RectTransform rectTransform = pinButtonGO.GetComponent<RectTransform>();
        rectTransform.sizeDelta = pinButtonSize;
        rectTransform.anchorMin = new Vector2(1, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.anchoredPosition = pinButtonPosition;

        // Asignar el botón al controller
        AssignPinButton(button);
    }

    private void AssignPinButton(Button pinButton)
    {
        SimplePanelController controller = GetComponent<SimplePanelController>();
        if (controller != null)
        {
            controller.pinButton = pinButton;
            controller.pinIcon = pinButton.GetComponent<Image>();
        }
    }

    // Métodos de utilidad para configuración rápida
    public void SetToggleButton(Button button)
    {
        toggleButton = button;
        SimplePanelController controller = GetComponent<SimplePanelController>();
        if (controller != null)
        {
            controller.toggleButton = button;
        }
    }

    public void SetDragArea(Transform dragArea)
    {
        dragAreaTransform = dragArea;
        SimpleDraggablePanel draggable = GetComponent<SimpleDraggablePanel>();
        if (draggable != null)
        {
            RectTransform dragAreaRect = dragArea.GetComponent<RectTransform>();
            if (dragAreaRect != null)
            {
                draggable.SetDragArea(dragAreaRect);
            }
        }
    }

    // Configuraciones predefinidas
    [ContextMenu("Quick Setup - Menu Panel")]
    public void QuickSetupMenu()
    {
        panelID = gameObject.name + "_Menu";
        makeDraggable = true;
        createPinButton = true;
        pinButtonSize = new Vector2(35, 35);
        animationSpeed = 6f;
        closedOffset = new Vector3(-400, 0, 0);
        SetupPanel();
    }

    [ContextMenu("Quick Setup - Info Panel")]
    public void QuickSetupInfo()
    {
        panelID = gameObject.name + "_Info";
        makeDraggable = true;
        createPinButton = true;
        pinButtonSize = new Vector2(30, 30);
        animationSpeed = 4f;
        closedOffset = new Vector3(400, 0, 0);
        SetupPanel();
    }

    [ContextMenu("Quick Setup - Toolbar")]
    public void QuickSetupToolbar()
    {
        panelID = gameObject.name + "_Toolbar";
        makeDraggable = true;
        createPinButton = false; // Los toolbars normalmente no se pin
        animationSpeed = 7f;
        closedOffset = new Vector3(0, 300, 0);
        SetupPanel();
    }

    // Debugging
    [ContextMenu("Debug Panel Setup")]
    public void DebugPanelSetup()
    {
        Debug.Log("=== PANEL SETUP DEBUG ===");
        Debug.Log($"Panel ID: {panelID}");
        Debug.Log($"Toggle Button: {(toggleButton != null ? toggleButton.name : "None")}");
        Debug.Log($"Make Draggable: {makeDraggable}");
        Debug.Log($"Create Pin Button: {createPinButton}");
        Debug.Log($"Animation Speed: {animationSpeed}");
        Debug.Log($"Closed Offset: {closedOffset}");

        SimplePanelController controller = GetComponent<SimplePanelController>();
        if (controller != null)
        {
            Debug.Log($"Controller Found - Is Open: {controller.IsOpen}, Is Pinned: {controller.IsPinned}");
        }
        else
        {
            Debug.Log("No SimplePanelController found");
        }
    }
}