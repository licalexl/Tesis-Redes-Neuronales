using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIHoverAlpha : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Configuración del Hover")]
    [SerializeField] private Image targetImage;
    [SerializeField] private float alphaOnHover = 1.0f; // Alpha al pasar el mouse (0-1)

    private float originalAlpha;
    private Color originalColor;

    private void Start()
    {
        // Si no se asigna una imagen, busca una en el mismo GameObject
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }

        // Verifica que tengamos una imagen asignada
        if (targetImage == null)
        {
            Debug.LogWarning("No se encontró ninguna Image. Asigna una en el inspector.");
            return;
        }

        // Guarda el color y alpha originales
        originalColor = targetImage.color;
        originalAlpha = originalColor.a;
    }

    /// <summary>
    /// Se ejecuta cuando el mouse entra en el objeto
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (targetImage != null)
        {
            Color newColor = originalColor;
            newColor.a = alphaOnHover;
            targetImage.color = newColor;
        }
    }

    /// <summary>
    /// Se ejecuta cuando el mouse sale del objeto
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (targetImage != null)
        {
            Color newColor = originalColor;
            newColor.a = originalAlpha;
            targetImage.color = newColor;
        }
    }

    /// <summary>
    /// Método para cambiar la imagen objetivo en tiempo de ejecución
    /// </summary>
    public void SetTargetImage(Image newImage)
    {
        targetImage = newImage;
        if (targetImage != null)
        {
            originalColor = targetImage.color;
            originalAlpha = originalColor.a;
        }
    }

    /// <summary>
    /// Método para cambiar el alpha de hover en tiempo de ejecución
    /// </summary>
    public void SetHoverAlpha(float newAlpha)
    {
        alphaOnHover = Mathf.Clamp01(newAlpha);
    }
}