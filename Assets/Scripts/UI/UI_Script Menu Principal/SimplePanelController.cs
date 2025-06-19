using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimplePanelController : MonoBehaviour
{
    [Header("Configuración Básica")]
    public string panelID;
    public Button toggleButton;
    public Button pinButton;

    [Header("Configuración de Pin")]
    public Image pinIcon;
    public Sprite pinnedSprite;
    public Sprite unpinnedSprite;
    public Color pinnedColor = Color.yellow;
    public Color unpinnedColor = Color.white;

    [Header("Animación")]
    public float animationSpeed = 5f;
    public Vector3 closedPosition = new Vector3(-300, 0, 0); // Posición cuando está cerrado

    // Estado
    private bool isOpen = false;
    private bool isPinned = false;
    private bool isAnimating = false;

    // Componentes
    private RectTransform rectTransform;
    private Vector3 openPosition;

    // Propiedades públicas
    public bool IsOpen => isOpen;
    public bool IsPinned => isPinned;
    public bool IsAnimating => isAnimating;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        openPosition = rectTransform.localPosition;
    }

    void Start()
    {
        // Registrar en el manager
        SimplePanelManager.Instance.RegisterPanel(this);

        // Configurar botones
        if (toggleButton != null)
            toggleButton.onClick.AddListener(TogglePanel);

        if (pinButton != null)
            pinButton.onClick.AddListener(TogglePin);

        // Configurar estado inicial
        SetToClosedPosition();
        UpdatePinVisuals();
    }

    void OnDestroy()
    {
        if (SimplePanelManager.Instance != null)
            SimplePanelManager.Instance.UnregisterPanel(this);
    }

    public void TogglePanel()
    {
        if (isOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (isAnimating) return;

        // Notificar al manager que vamos a abrir este panel
        SimplePanelManager.Instance.OnPanelOpening(this);

        StartCoroutine(AnimateToPosition(openPosition, true));
    }

    public void ClosePanel()
    {
        if (isAnimating || isPinned) return;

        StartCoroutine(AnimateToPosition(closedPosition, false));
    }

    public void ForceClose()
    {
        if (isAnimating) return;

        StartCoroutine(AnimateToPosition(closedPosition, false));
    }

    private IEnumerator AnimateToPosition(Vector3 targetPosition, bool willBeOpen)
    {
        isAnimating = true;
        Vector3 startPosition = rectTransform.localPosition;
        float journey = 0f;

        while (journey <= 1f)
        {
            journey += Time.deltaTime * animationSpeed;
            rectTransform.localPosition = Vector3.Lerp(startPosition, targetPosition, EaseInOut(journey));
            yield return null;
        }

        rectTransform.localPosition = targetPosition;
        isOpen = willBeOpen;
        isAnimating = false;
    }

    private float EaseInOut(float t)
    {
        return t * t * (3f - 2f * t);
    }

    public void TogglePin()
    {
        isPinned = !isPinned;
        UpdatePinVisuals();
        Debug.Log($"Panel {panelID} {(isPinned ? "pinned" : "unpinned")}");
    }

    public void SetPinned(bool pinned)
    {
        isPinned = pinned;
        UpdatePinVisuals();
    }

    private void UpdatePinVisuals()
    {
        if (pinIcon != null)
        {
            pinIcon.sprite = isPinned ? pinnedSprite : unpinnedSprite;
            pinIcon.color = isPinned ? pinnedColor : unpinnedColor;
        }
    }

    private void SetToClosedPosition()
    {
        rectTransform.localPosition = closedPosition;
        isOpen = false;
    }

    // Métodos de utilidad
    public void SetOpenPosition(Vector3 position)
    {
        openPosition = position;
    }

    public void SetClosedPosition(Vector3 position)
    {
        closedPosition = position;
    }

    // Debug
    [ContextMenu("Test Open")]
    public void TestOpen() => OpenPanel();

    [ContextMenu("Test Close")]
    public void TestClose() => ClosePanel();

    [ContextMenu("Toggle Pin")]
    public void TestTogglePin() => TogglePin();
}