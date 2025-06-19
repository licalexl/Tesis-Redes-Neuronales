using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Herramienta para crear y generar los prefabs necesarios para el sistema de visualización
/// de entrenamiento de NPCs con algoritmos genéticos. Crea automáticamente elementos UI 
/// como puntos de datos, líneas de conexión y neuronas para las gráficas del dashboard.
/// </summary>
public class VisualizationPrefabCreator : MonoBehaviour
{
    #region Variables Públicas - Prefabs Requeridos

    [Header("Prefabs Requeridos")]
    [Tooltip("Prefab del punto de datos para las gráficas. Si es null, se creará automáticamente")]
    [SerializeField] private GameObject dataPointPrefab;

    [Tooltip("Prefab para renderizar líneas en las gráficas. Si es null, se creará automáticamente")]
    [SerializeField] private GameObject lineRendererPrefab;

    [Tooltip("Prefab de neurona para la visualización de redes neuronales. Si es null, se creará automáticamente")]
    [SerializeField] private GameObject neuronPrefab;

    [Tooltip("Prefab para líneas de conexión entre neuronas. Si es null, se creará automáticamente")]
    [SerializeField] private GameObject connectionPrefab;

    #endregion

    #region Variables Públicas - Creación de Prefabs

    [Header("Creación de Prefabs")]
    [Tooltip("Botón que ejecuta la creación automática de todos los prefabs necesarios")]
    [SerializeField] private Button createPrefabsButton;

    [Tooltip("Botón para limpiar todos los objetos creados")]
    [SerializeField] private Button clearObjectsButton;

    #endregion

    #region Variables Públicas - Referencias

    [Header("Referencias")]
    [Tooltip("RectTransform del canvas donde se mostrarán los prefabs de preview")]
    [SerializeField] private RectTransform canvasRect;

    [Tooltip("Panel donde se mostrará una vista previa de los prefabs creados")]
    [SerializeField] private GameObject previewPanel;

    [Tooltip("Texto que muestra el estado actual del proceso de creación")]
    [SerializeField] private TextMeshProUGUI statusText;

    #endregion

    #region Variables Públicas - Configuración

    [Header("Configuración de Prefabs")]
    [Tooltip("Tamaño de los puntos de datos en píxeles")]
    [SerializeField] private Vector2 dataPointSize = new Vector2(12, 12);

    [Tooltip("Grosor de las líneas de gráfica")]
    [SerializeField] private float lineThickness = 2f;

    [Tooltip("Tamaño de las neuronas en píxeles")]
    [SerializeField] private Vector2 neuronSize = new Vector2(25, 25);

    [Tooltip("Grosor de las conexiones entre neuronas")]
    [SerializeField] private float connectionThickness = 1f;

    #endregion

    #region Variables Privadas

    private List<GameObject> createdObjects = new List<GameObject>();
    private List<GameObject> previewObjects = new List<GameObject>();

    #endregion

    #region Métodos de Unity

    void Start()
    {
        if (createPrefabsButton != null)
        {
            createPrefabsButton.onClick.AddListener(CreateRequiredPrefabs);
        }

        if (clearObjectsButton != null)
        {
            clearObjectsButton.onClick.AddListener(ClearCreatedObjects);
        }

        UpdateStatusText("Listo para crear prefabs del sistema de visualización.");
    }

    void OnDestroy()
    {
        ClearCreatedObjects();
    }

    #endregion

    #region Métodos Públicos - Creación Principal

    /// <summary>
    /// Método principal que crea todos los prefabs necesarios para el sistema de visualización
    /// </summary>
    public void CreateRequiredPrefabs()
    {
        ClearCreatedObjects();
        UpdateStatusText("Creando prefabs...");

        try
        {
            if (dataPointPrefab == null)
            {
                dataPointPrefab = CreateDataPointPrefab();
                UpdateStatusText("✓ Prefab de punto de datos creado");
            }

            if (lineRendererPrefab == null)
            {
                lineRendererPrefab = CreateLineRendererPrefab();
                UpdateStatusText("✓ Prefab de línea creado");
            }

            if (neuronPrefab == null)
            {
                neuronPrefab = CreateNeuronPrefab();
                UpdateStatusText("✓ Prefab de neurona creado");
            }

            if (connectionPrefab == null)
            {
                connectionPrefab = CreateConnectionPrefab();
                UpdateStatusText("✓ Prefab de conexión creado");
            }

            ShowPrefabsPreview();
            UpdateStatusText("✅ Todos los prefabs creados exitosamente!\n\nPara usarlos:\n1. Arrastra cada objeto desde la jerarquía a una carpeta de prefabs\n2. Asigna estos prefabs a los componentes TrainingVisualizer y NetworkAnalyzer");

        }
        catch (System.Exception e)
        {
            UpdateStatusText($"❌ Error al crear prefabs: {e.Message}");
            Debug.LogError($"Error en CreateRequiredPrefabs: {e.Message}");
        }
    }

    #endregion

    #region Métodos Privados - Creación de Prefabs Específicos

    /// <summary>
    /// Crea un prefab de punto de datos para usar en las gráficas de entrenamiento
    /// </summary>
    private GameObject CreateDataPointPrefab()
    {
        GameObject dataPoint = new GameObject("DataPointPrefab");
        dataPoint.transform.SetParent(transform);

        // Configurar RectTransform
        RectTransform rect = dataPoint.AddComponent<RectTransform>();
        rect.sizeDelta = dataPointSize;
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;

        // Configurar imagen
        Image image = dataPoint.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = CreateCircleSprite(32, Color.white);
        image.raycastTarget = false; // Optimización

        // Añadir componente para identificar el tipo
        dataPoint.AddComponent<VisualizationElement>().elementType = VisualizationElementType.DataPoint;

        createdObjects.Add(dataPoint);
        return dataPoint;
    }

    /// <summary>
    /// Crea un prefab de línea para conectar puntos en las gráficas
    /// </summary>
    private GameObject CreateLineRendererPrefab()
    {
        GameObject lineRenderer = new GameObject("LineRendererPrefab");
        lineRenderer.transform.SetParent(transform);

        // Configurar RectTransform
        RectTransform rect = lineRenderer.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, lineThickness);
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;

        // Configurar imagen
        Image image = lineRenderer.AddComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;

        // Añadir componente identificador
        lineRenderer.AddComponent<VisualizationElement>().elementType = VisualizationElementType.GraphLine;

        createdObjects.Add(lineRenderer);
        return lineRenderer;
    }

    /// <summary>
    /// Crea un prefab de neurona para la visualización de redes neuronales
    /// </summary>
    private GameObject CreateNeuronPrefab()
    {
        GameObject neuron = new GameObject("NeuronPrefab");
        neuron.transform.SetParent(transform);

        // Configurar RectTransform
        RectTransform rect = neuron.AddComponent<RectTransform>();
        rect.sizeDelta = neuronSize;
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;

        // Configurar imagen principal
        Image image = neuron.AddComponent<Image>();
        image.color = Color.white;
        image.sprite = CreateCircleSprite(64, Color.white);
        image.raycastTarget = false;

        // Añadir borde para mejor visualización
        GameObject border = new GameObject("Border");
        border.transform.SetParent(neuron.transform, false);

        RectTransform borderRect = border.AddComponent<RectTransform>();
        borderRect.sizeDelta = neuronSize * 1.1f;
        borderRect.anchoredPosition = Vector2.zero;

        Image borderImage = border.AddComponent<Image>();
        borderImage.color = Color.black;
        borderImage.sprite = CreateCircleSprite(64, Color.black);
        borderImage.raycastTarget = false;

        // Mover la imagen principal al frente
        neuron.transform.SetAsLastSibling();

        // Añadir componente identificador
        neuron.AddComponent<VisualizationElement>().elementType = VisualizationElementType.Neuron;

        createdObjects.Add(neuron);
        return neuron;
    }

    /// <summary>
    /// Crea un prefab de conexión para las líneas entre neuronas
    /// </summary>
    private GameObject CreateConnectionPrefab()
    {
        GameObject connection = new GameObject("ConnectionPrefab");
        connection.transform.SetParent(transform);

        // Configurar RectTransform
        RectTransform rect = connection.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(100, connectionThickness);
        rect.anchorMin = Vector2.one * 0.5f;
        rect.anchorMax = Vector2.one * 0.5f;

        // Configurar imagen
        Image image = connection.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.7f); // Semi-transparente por defecto
        image.raycastTarget = false;

        // Añadir componente identificador
        connection.AddComponent<VisualizationElement>().elementType = VisualizationElementType.Connection;

        createdObjects.Add(connection);
        return connection;
    }

    #endregion

    #region Métodos Privados - Visualización y Preview

    /// <summary>
    /// Muestra una vista previa de todos los prefabs creados
    /// </summary>
    private void ShowPrefabsPreview()
    {
        if (previewPanel == null) return;

        ClearPreviewObjects();
        previewPanel.SetActive(true);

        float spacing = 80f;
        float startX = -150f;

        // Preview del punto de datos
        if (dataPointPrefab != null)
        {
            GameObject preview = CreatePreviewObject(dataPointPrefab, "Punto de Datos",
                new Vector2(startX, 20), Color.green);
            previewObjects.Add(preview);
        }

        // Preview de la línea
        if (lineRendererPrefab != null)
        {
            GameObject preview = CreatePreviewObject(lineRendererPrefab, "Línea de Gráfica",
                new Vector2(startX + spacing, 20), Color.blue);

            // Hacer la línea más visible en el preview
            RectTransform previewRect = preview.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(60, 4);
            }

            previewObjects.Add(preview);
        }

        // Preview de la neurona
        if (neuronPrefab != null)
        {
            GameObject preview = CreatePreviewObject(neuronPrefab, "Neurona",
                new Vector2(startX + spacing * 2, 20), Color.cyan);
            previewObjects.Add(preview);
        }

        // Preview de la conexión
        if (connectionPrefab != null)
        {
            GameObject preview = CreatePreviewObject(connectionPrefab, "Conexión",
                new Vector2(startX + spacing * 3, 20), Color.yellow);

            // Hacer la conexión más visible en el preview
            RectTransform previewRect = preview.GetComponent<RectTransform>();
            if (previewRect != null)
            {
                previewRect.sizeDelta = new Vector2(50, 3);
            }

            previewObjects.Add(preview);
        }
    }

    /// <summary>
    /// Crea un objeto de preview para mostrar en el panel
    /// </summary>
    private GameObject CreatePreviewObject(GameObject original, string label, Vector2 position, Color color)
    {
        GameObject preview = Instantiate(original, previewPanel.transform);
        preview.name = $"Preview_{label}";

        RectTransform rect = preview.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = position;
        }

        Image img = preview.GetComponent<Image>();
        if (img != null)
        {
            img.color = color;
        }

        // Crear etiqueta
        GameObject labelObj = new GameObject($"Label_{label}");
        labelObj.transform.SetParent(preview.transform, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(100, 20);
        labelRect.anchoredPosition = new Vector2(0, -40);

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 10;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;

        return preview;
    }

    #endregion

    #region Métodos Privados - Utilidades

    /// <summary>
    /// Crea un sprite circular proceduralmente
    /// </summary>
    private Sprite CreateCircleSprite(int resolution, Color color)
    {
        Texture2D texture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);

        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f - 1f; // Pequeño margen para evitar pixeles cortados

        Color[] pixels = new Color[resolution * resolution];

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);

                if (distance <= radius)
                {
                    // Anti-aliasing simple en los bordes
                    float alpha = distance > radius - 1f ? (radius - distance) : 1f;
                    pixels[y * resolution + x] = new Color(color.r, color.g, color.b, alpha);
                }
                else
                {
                    pixels[y * resolution + x] = Color.clear;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        return Sprite.Create(texture, new Rect(0, 0, resolution, resolution), Vector2.one * 0.5f);
    }

    /// <summary>
    /// Actualiza el texto de estado
    /// </summary>
    private void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
        Debug.Log($"VisualizationPrefabCreator: {message}");
    }

    /// <summary>
    /// Limpia todos los objetos creados
    /// </summary>
    private void ClearCreatedObjects()
    {
        foreach (var obj in createdObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        createdObjects.Clear();

        ClearPreviewObjects();
    }

    /// <summary>
    /// Limpia los objetos de preview
    /// </summary>
    private void ClearPreviewObjects()
    {
        foreach (var obj in previewObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        previewObjects.Clear();
    }

    #endregion

    #region Métodos Públicos de Configuración

    /// <summary>
    /// Configura los tamaños de los prefabs
    /// </summary>
    public void ConfigurePrefabSizes(Vector2 newDataPointSize, float newLineThickness,
        Vector2 newNeuronSize, float newConnectionThickness)
    {
        dataPointSize = newDataPointSize;
        lineThickness = newLineThickness;
        neuronSize = newNeuronSize;
        connectionThickness = newConnectionThickness;

        UpdateStatusText("Configuración de tamaños actualizada. Recrea los prefabs para aplicar cambios.");
    }

    /// <summary>
    /// Asigna automáticamente los prefabs creados a los componentes del sistema
    /// </summary>
    public void AssignPrefabsToComponents()
    {
        TrainingVisualizer visualizer = FindObjectOfType<TrainingVisualizer>();
        NetworkAnalyzer analyzer = FindObjectOfType<NetworkAnalyzer>();

        if (visualizer != null)
        {
            // Usar reflection para asignar los prefabs privados
            var dataPointField = typeof(TrainingVisualizer).GetField("dataPointPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var lineField = typeof(TrainingVisualizer).GetField("lineRendererPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (dataPointField != null && dataPointPrefab != null)
                dataPointField.SetValue(visualizer, dataPointPrefab);

            if (lineField != null && lineRendererPrefab != null)
                lineField.SetValue(visualizer, lineRendererPrefab);

            UpdateStatusText("✓ Prefabs asignados a TrainingVisualizer");
        }

        if (analyzer != null)
        {
            var neuronField = typeof(NetworkAnalyzer).GetField("neuronPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            var connectionField = typeof(NetworkAnalyzer).GetField("connectionPrefab",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (neuronField != null && neuronPrefab != null)
                neuronField.SetValue(analyzer, neuronPrefab);

            if (connectionField != null && connectionPrefab != null)
                connectionField.SetValue(analyzer, connectionPrefab);

            UpdateStatusText("✓ Prefabs asignados a NetworkAnalyzer");
        }

        if (visualizer != null || analyzer != null)
        {
            UpdateStatusText(" Prefabs asignados automáticamente a los componentes!");
        }
        else
        {
            UpdateStatusText(" No se encontraron componentes TrainingVisualizer o NetworkAnalyzer en la escena.");
        }
    }

    #endregion
}

#region Clases de Apoyo

/// <summary>
/// Tipos de elementos de visualización
/// </summary>
public enum VisualizationElementType
{
    DataPoint,
    GraphLine,
    Neuron,
    Connection
}

/// <summary>
/// Componente para identificar elementos de visualización
/// </summary>
public class VisualizationElement : MonoBehaviour
{
    public VisualizationElementType elementType;
}

#endregion