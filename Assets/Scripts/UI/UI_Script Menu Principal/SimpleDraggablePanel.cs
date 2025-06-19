using UnityEngine;
using UnityEngine.EventSystems;

public class SimpleDraggablePanel : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Configuración de Arrastre")]
    public bool isDraggable = true;
    public bool constrainToScreen = true;
    public RectTransform dragArea; // Si es null, usa todo el panel

    [Header("Efectos Visuales")]
    public bool showDragFeedback = true;
    public float dragOpacity = 0.8f;
    public bool bringToFrontOnDrag = true;

    // Componentes
    private RectTransform rectTransform;
    private Canvas parentCanvas;
    private CanvasGroup canvasGroup;

    // Estado de arrastre
    private bool isDragging = false;
    private Vector2 dragOffset;
    private float originalOpacity;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        parentCanvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Si no hay área de arrastre especificada, usar el panel completo
        if (dragArea == null)
        {
            dragArea = rectTransform;
        }

        // Guardar valores originales
        originalOpacity = canvasGroup.alpha;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!isDraggable) return;

        // Verificar si el arrastre comenzó en el área válida
        if (!IsPointerInDragArea(eventData)) return;

        isDragging = true;

        // Calcular offset del mouse relativo al panel
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        dragOffset = rectTransform.anchoredPosition - localPointerPosition;

        // Aplicar efectos visuales
        if (showDragFeedback)
        {
            canvasGroup.alpha = dragOpacity;
        }

        // Traer al frente si está habilitado
        if (bringToFrontOnDrag)
        {
            BringToFront();
        }

        Debug.Log($"Comenzando arrastre del panel: {gameObject.name}");
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDraggable || !isDragging) return;

        // Convertir posición del mouse a coordenadas locales
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPointerPosition
        );

        // Calcular nueva posición
        Vector2 newPosition = localPointerPosition + dragOffset;

        // Aplicar restricciones si están habilitadas
        if (constrainToScreen)
        {
            newPosition = ConstrainToScreen(newPosition);
        }

        // Actualizar posición
        rectTransform.anchoredPosition = newPosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging) return;

        isDragging = false;

        // Restaurar efectos visuales
        if (showDragFeedback)
        {
            canvasGroup.alpha = originalOpacity;
        }

        Debug.Log($"Arrastre terminado del panel: {gameObject.name}");
    }

    private bool IsPointerInDragArea(PointerEventData eventData)
    {
        if (dragArea == null) return true;

        return RectTransformUtility.RectangleContainsScreenPoint(
            dragArea,
            eventData.position,
            eventData.pressEventCamera
        );
    }

    private Vector2 ConstrainToScreen(Vector2 position)
    {
        if (parentCanvas == null) return position;

        // Obtener límites del canvas
        RectTransform canvasRect = parentCanvas.GetComponent<RectTransform>();
        if (canvasRect == null) return position;

        // Obtener el tamaño del panel
        Vector2 panelSize = rectTransform.sizeDelta;

        // Calcular límites considerando el tamaño del panel
        float halfWidth = panelSize.x * 0.5f;
        float halfHeight = panelSize.y * 0.5f;

        Vector2 canvasSize = canvasRect.sizeDelta;
        float maxX = (canvasSize.x * 0.5f) - halfWidth;
        float minX = -(canvasSize.x * 0.5f) + halfWidth;
        float maxY = (canvasSize.y * 0.5f) - halfHeight;
        float minY = -(canvasSize.y * 0.5f) + halfHeight;

        // Restringir posición
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);

        return position;
    }

    private void BringToFront()
    {
        if (parentCanvas != null)
        {
            transform.SetAsLastSibling();
        }
    }

    // Métodos públicos para control externo
    public void SetDraggable(bool draggable)
    {
        isDraggable = draggable;
    }

    public bool IsDragging()
    {
        return isDragging;
    }

    public void SetDragArea(RectTransform newDragArea)
    {
        dragArea = newDragArea;
    }

    // Método para debugging
    [ContextMenu("Test Drag Settings")]
    public void TestDragSettings()
    {
        Debug.Log($"Panel: {gameObject.name}");
        Debug.Log($"Draggable: {isDraggable}");
        Debug.Log($"Constrain to Screen: {constrainToScreen}");
        Debug.Log($"Current Position: {rectTransform.anchoredPosition}");
    }
}