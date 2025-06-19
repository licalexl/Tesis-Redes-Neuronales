using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Define un esquema de colores personalizable para la interfaz de usuario.
/// Permite configurar colores para fondo, elementos primarios, secundarios, de acento, texto y notificaciones.
/// </summary>
[System.Serializable]
public class ColorScheme
{
    [Tooltip("Color de fondo principal de la interfaz.")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);

    [Tooltip("Color primario para elementos importantes de la UI.")]
    public Color primaryColor = new Color(0.2f, 0.6f, 1f);

    [Tooltip("Color secundario para elementos complementarios de la UI.")]
    public Color secondaryColor = new Color(0.2f, 0.8f, 0.2f);

    [Tooltip("Color de acento para resaltar informacion critica o seleccionada.")]
    public Color accentColor = new Color(1f, 0.6f, 0.2f);

    [Tooltip("Color del texto general en toda la interfaz.")]
    public Color textColor = Color.white;

    [Tooltip("Color utilizado para mostrar advertencias.")]
    public Color warningColor = new Color(1f, 0.6f, 0f);

    [Tooltip("Color utilizado para mostrar errores criticos.")]
    public Color errorColor = new Color(1f, 0.2f, 0.2f);
}

/// <summary>
/// Administrador central de configuracion del sistema de visualizacion de entrenamiento.
/// Controla la apariencia visual, configuracion de carga y preferencias del usuario.
/// Implementa el patron Singleton para un acceso global desde cualquier componente del sistema.
/// </summary>
public class TrainingVisualizationSettings : MonoBehaviour
{
    #region Variables de Configuracion Visual

    [Header("Configuracion Visual")]
    [Tooltip("Esquema de colores activo para toda la interfaz de usuario.")]
    public ColorScheme colorScheme = new ColorScheme();

    [Tooltip("Si esta activado, muestra los valores numericos sobre los puntos de datos en las graficas.")]
    public bool showDataLabels = true;

    [Tooltip("Limite maximo de puntos a mostrar en las graficas. Un valor de 0 significa que no hay limite.")]
    public int maxDataPoints = 0;

    [Tooltip("Tamaño de la fuente para las etiquetas de datos en las graficas.")]
    public int labelFontSize = 12;

    [Tooltip("Grosor de las lineas utilizadas para dibujar las graficas.")]
    public float lineThickness = 2f;

    [Tooltip("Tamaño de los puntos que representan datos en las graficas.")]
    public float dataPointSize = 10f;

    #endregion

    #region Variables de Configuracion de Carga

    [Header("Configuracion de Carga")]
    [Tooltip("Carpeta por defecto donde se buscaran los archivos de datos de entrenamiento.")]
    public string loadFolder = "TrainingData";

    [Tooltip("Si esta activado, carga desde la carpeta del proyecto (Assets). Si no, carga desde persistentDataPath.")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Si esta activado, ordena los archivos de datos por fecha de modificacion.")]
    public bool sortFilesByDate = true;

    [Tooltip("Si esta activado, carga automaticamente archivos relacionados que pertenecen a la misma sesion de entrenamiento.")]
    public bool autoLoadRelatedFiles = true;

    #endregion

    #region Variables de UI Settings Panel

    [Header("UI Settings Panel")]
    [Tooltip("Referencia al panel principal que contiene todos los controles de configuracion.")]
    public GameObject settingsPanel;

    [Tooltip("Control de tipo Toggle para activar o desactivar las etiquetas de datos en tiempo real.")]
    public Toggle showLabelsToggle;

    [Tooltip("Control de tipo Slider para ajustar el grosor de las lineas de las graficas.")]
    public Slider lineThicknessSlider;

    [Tooltip("Control de tipo Slider para ajustar el tamaño de los puntos de datos en las graficas.")]
    public Slider pointSizeSlider;

    [Tooltip("Control de tipo Slider para ajustar el tamaño de la fuente de las etiquetas de datos.")]
    public Slider fontSizeSlider;

    [Tooltip("Control de tipo Toggle para activar o desactivar la carga automatica de archivos relacionados.")]
    public Toggle autoLoadToggle;

    #endregion

    #region Variables Privadas y Referencias

    /// <summary>
    /// Referencia al componente principal encargado de visualizar las graficas.
    /// </summary>
    private TrainingVisualizer trainingVisualizer;

    /// <summary>
    /// Referencia al componente que analiza la estructura y datos de la red neuronal.
    /// </summary>
    private NetworkAnalyzer networkAnalyzer;

    /// <summary>
    /// Referencia al panel de control principal (dashboard) de la interfaz.
    /// </summary>
    private TrainingDashboard dashboard;

    /// <summary>
    /// Instancia estatica para implementar el patron Singleton, permitiendo acceso global.
    /// </summary>
    public static TrainingVisualizationSettings Instance { get; private set; }

    #endregion

    #region Clase de Configuracion Guardada

    /// <summary>
    /// Estructura de datos interna utilizada para persistir la configuracion del usuario en un archivo JSON.
    /// </summary>
    [System.Serializable]
    private class SavedSettings
    {
        public ColorScheme colorScheme;
        public bool showDataLabels;
        public int maxDataPoints;
        public float lineThickness;
        public float dataPointSize;
        public int labelFontSize;
        public string loadFolder;
        public bool loadFromProjectFolder;
        public bool autoLoadRelatedFiles;
    }

    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Metodo llamado por Unity al cargar el objeto.
    /// Configura el patron Singleton, busca referencias a otros componentes y carga la configuracion guardada.
    /// </summary>
    void Awake()
    {
        // Configuracion del Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Encontrar referencias a componentes criticos
        trainingVisualizer = FindObjectOfType<TrainingVisualizer>();
        networkAnalyzer = FindObjectOfType<NetworkAnalyzer>();
        dashboard = FindObjectOfType<TrainingDashboard>();

        // Cargar configuracion guardada previamente por el usuario
        LoadSettings();

        // Inicializar los elementos del panel de configuracion
        InitializeSettingsPanel();
    }

    /// <summary>
    /// Metodo llamado por Unity despues de Awake, en el primer frame.
    /// Oculta el panel de configuracion al inicio y aplica la configuracion cargada a todos los componentes relevantes.
    /// </summary>
    void Start()
    {
        // Ocultar el panel de configuracion al iniciar la aplicacion
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }

        // Aplicar la configuracion a los componentes visuales
        ApplySettings();
    }

    #endregion

    #region Metodos de Gestion del Panel de Configuracion

    /// <summary>
    /// Alterna la visibilidad del panel de configuracion.
    /// Si el panel se hace visible, actualiza los controles de la UI con los valores actuales.
    /// </summary>
    public void ToggleSettingsPanel()
    {
        if (settingsPanel != null)
        {
            bool isActive = !settingsPanel.activeSelf;
            settingsPanel.SetActive(isActive);

            // Actualizar la UI con los valores actuales si el panel se acaba de activar
            if (isActive)
            {
                UpdateSettingsPanelUI();
            }
        }
    }

    /// <summary>
    /// Inicializa los controles del panel de configuracion (sliders, toggles) con sus valores actuales
    /// y asigna los listeners para responder a los cambios del usuario.
    /// </summary>
    private void InitializeSettingsPanel()
    {
        if (settingsPanel == null) return;

        // Configurar Toggles y sus listeners
        if (showLabelsToggle != null)
        {
            showLabelsToggle.isOn = showDataLabels;
            showLabelsToggle.onValueChanged.AddListener(value => {
                showDataLabels = value;
                ApplySettings();
            });
        }

        if (autoLoadToggle != null)
        {
            autoLoadToggle.isOn = autoLoadRelatedFiles;
            autoLoadToggle.onValueChanged.AddListener(value => {
                autoLoadRelatedFiles = value;
                ApplySettings();
            });
        }

        // Configurar Sliders y sus listeners
        if (lineThicknessSlider != null)
        {
            lineThicknessSlider.value = lineThickness;
            lineThicknessSlider.onValueChanged.AddListener(value => {
                lineThickness = value;
                ApplySettings();
            });
        }

        if (pointSizeSlider != null)
        {
            pointSizeSlider.value = dataPointSize;
            pointSizeSlider.onValueChanged.AddListener(value => {
                dataPointSize = value;
                ApplySettings();
            });
        }

        if (fontSizeSlider != null)
        {
            fontSizeSlider.value = labelFontSize;
            fontSizeSlider.onValueChanged.AddListener(value => {
                labelFontSize = Mathf.RoundToInt(value);
                ApplySettings();
            });
        }
    }

    /// <summary>
    /// Actualiza los valores de los controles en el panel de configuracion (UI) para que reflejen
    /// el estado actual de las variables de configuracion.
    /// </summary>
    private void UpdateSettingsPanelUI()
    {
        if (showLabelsToggle != null) showLabelsToggle.isOn = showDataLabels;
        if (autoLoadToggle != null) autoLoadToggle.isOn = autoLoadRelatedFiles;
        if (lineThicknessSlider != null) lineThicknessSlider.value = lineThickness;
        if (pointSizeSlider != null) pointSizeSlider.value = dataPointSize;
        if (fontSizeSlider != null) fontSizeSlider.value = labelFontSize;
    }

    #endregion

    #region Metodos de Aplicacion de Configuracion

    /// <summary>
    /// Aplica la configuracion actual a todos los componentes relevantes del sistema,
    /// como el visualizador de entrenamiento y el analizador de red. Ademas, guarda la configuracion.
    /// </summary>
    private void ApplySettings()
    {
        // Aplicar configuracion al visualizador de entrenamiento
        if (trainingVisualizer != null)
        {
            trainingVisualizer.showValueLabels = showDataLabels;
            trainingVisualizer.loadFolder = loadFolder;
            trainingVisualizer.loadFromProjectFolder = loadFromProjectFolder;
            trainingVisualizer.UpdateGraph(); // Forzar actualizacion visual de la grafica
        }

        // Aplicar configuracion al analizador de redes
        if (networkAnalyzer != null)
        {
            networkAnalyzer.showNeuronLabels = showDataLabels;
        }

        // Guardar la configuracion para persistencia
        SaveSettings();
    }

    #endregion

    #region Metodos de Persistencia de Configuracion

    /// <summary>
    /// Guarda la configuracion actual en un archivo JSON en la carpeta de datos persistente de la aplicacion.
    /// </summary>
    public void SaveSettings()
    {
        SavedSettings settings = new SavedSettings
        {
            colorScheme = colorScheme,
            showDataLabels = showDataLabels,
            maxDataPoints = maxDataPoints,
            lineThickness = lineThickness,
            dataPointSize = dataPointSize,
            labelFontSize = labelFontSize,
            loadFolder = loadFolder,
            loadFromProjectFolder = loadFromProjectFolder,
            autoLoadRelatedFiles = autoLoadRelatedFiles
        };

        string json = JsonUtility.ToJson(settings, true);
        string path = Path.Combine(Application.persistentDataPath, "VisualizationSettings.json");

        try
        {
            File.WriteAllText(path, json);
            Debug.Log("Configuracion guardada en: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("Error al guardar la configuracion: " + e.Message);
        }
    }

    /// <summary>
    /// Carga la configuracion desde un archivo JSON ubicado en la carpeta de datos persistente.
    /// Si el archivo no existe, utiliza los valores por defecto.
    /// </summary>
    public void LoadSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, "VisualizationSettings.json");

        if (File.Exists(path))
        {
            try
            {
                string json = File.ReadAllText(path);
                SavedSettings settings = JsonUtility.FromJson<SavedSettings>(json);

                if (settings != null)
                {
                    colorScheme = settings.colorScheme;
                    showDataLabels = settings.showDataLabels;
                    maxDataPoints = settings.maxDataPoints;
                    lineThickness = settings.lineThickness;
                    dataPointSize = settings.dataPointSize;
                    labelFontSize = settings.labelFontSize;
                    loadFolder = settings.loadFolder;
                    loadFromProjectFolder = settings.loadFromProjectFolder;
                    autoLoadRelatedFiles = settings.autoLoadRelatedFiles;

                    Debug.Log("Configuracion cargada desde: " + path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error al cargar la configuracion: " + e.Message);
            }
        }
        else
        {
            Debug.Log("No se encontro archivo de configuracion. Usando valores por defecto.");
        }
    }

    #endregion

    #region Metodos de Utilidad

    /// <summary>
    /// Restaura todos los valores de configuracion a su estado predeterminado y aplica los cambios.
    /// </summary>
    public void ResetToDefaults()
    {
        colorScheme = new ColorScheme();
        showDataLabels = true;
        maxDataPoints = 0;
        labelFontSize = 12;
        lineThickness = 2f;
        dataPointSize = 10f;
        loadFolder = "TrainingData";
        loadFromProjectFolder = true;
        autoLoadRelatedFiles = true;

        UpdateSettingsPanelUI();
        ApplySettings();
    }

    /// <summary>
    /// Obtiene la ruta completa para cargar archivos de datos, basada en la configuracion actual.
    /// </summary>
    /// <returns>La ruta completa del directorio de datos.</returns>
    public string GetLoadPath()
    {
        return loadFromProjectFolder ?
            Path.Combine(Application.dataPath, loadFolder) :
            Path.Combine(Application.persistentDataPath, loadFolder);
    }

    #endregion

    #region Metodos de Aplicacion de Esquema de Color

    /// <summary>
    /// Aplica el esquema de colores configurado a un elemento de UI especifico, segun su tipo.
    /// </summary>
    /// <param name="uiElement">El GameObject del elemento de UI al que se le aplicara el color.</param>
    /// <param name="elementType">El tipo de elemento (ej: "background", "primary", "text").</param>
    public void ApplyColorScheme(GameObject uiElement, string elementType)
    {
        if (uiElement == null) return;

        Image image = uiElement.GetComponent<Image>();
        TextMeshProUGUI text = uiElement.GetComponent<TextMeshProUGUI>();
        Button button = uiElement.GetComponent<Button>();

        switch (elementType.ToLower())
        {
            case "background":
                if (image != null) image.color = colorScheme.backgroundColor;
                break;
            case "primary":
                if (image != null) image.color = colorScheme.primaryColor;
                if (text != null) text.color = colorScheme.textColor;
                break;
            case "secondary":
                if (image != null) image.color = colorScheme.secondaryColor;
                if (text != null) text.color = colorScheme.textColor;
                break;
            case "accent":
                if (image != null) image.color = colorScheme.accentColor;
                if (text != null) text.color = colorScheme.textColor;
                break;
            case "button":
                if (button != null)
                {
                    ColorBlock colors = button.colors;
                    colors.normalColor = colorScheme.primaryColor;
                    colors.highlightedColor = Color.Lerp(colorScheme.primaryColor, Color.white, 0.2f);
                    colors.pressedColor = Color.Lerp(colorScheme.primaryColor, Color.black, 0.2f);
                    colors.selectedColor = colorScheme.accentColor;
                    button.colors = colors;
                }
                break;
            case "text":
                if (text != null) text.color = colorScheme.textColor;
                break;
            case "warning":
                if (image != null) image.color = colorScheme.warningColor;
                if (text != null) text.color = colorScheme.warningColor;
                break;
            case "error":
                if (image != null) image.color = colorScheme.errorColor;
                if (text != null) text.color = colorScheme.errorColor;
                break;
        }
    }

    /// <summary>
    /// Aplica el esquema de colores a toda la interfaz de usuario principal, utilizando las referencias del Dashboard.
    /// </summary>
    public void ApplyColorSchemeToUI()
    {
        if (dashboard == null) return;

        // Aplicar color a paneles principales
        if (dashboard.generationsPanel != null) ApplyColorScheme(dashboard.generationsPanel, "background");
        if (dashboard.networksPanel != null) ApplyColorScheme(dashboard.networksPanel, "background");
        if (dashboard.comparisonPanel != null) ApplyColorScheme(dashboard.comparisonPanel, "background");
        if (dashboard.infoPanel != null) ApplyColorScheme(dashboard.infoPanel, "background");

        // Aplicar color a botones de pestañas
        if (dashboard.tabButtons != null)
        {
            foreach (var tabButton in dashboard.tabButtons)
            {
                ApplyColorScheme(tabButton, "button");
            }
        }

        // Aplicar color a textos principales
        if (dashboard.currentFileText != null) ApplyColorScheme(dashboard.currentFileText.gameObject, "text");
        if (dashboard.networkStatsText != null) ApplyColorScheme(dashboard.networkStatsText.gameObject, "text");
        if (dashboard.selectedFilesText != null) ApplyColorScheme(dashboard.selectedFilesText.gameObject, "text");
    }

    #endregion

    #region Metodos Publicos de Configuracion Rapida

    /// <summary>
    /// Aplica una configuracion rapida para un tema visual oscuro.
    /// </summary>
    public void SetDarkMode()
    {
        colorScheme.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        colorScheme.textColor = Color.white;
        ApplyColorSchemeToUI();
    }

    /// <summary>
    /// Aplica una configuracion rapida para un tema visual claro.
    /// </summary>
    public void SetLightMode()
    {
        colorScheme.backgroundColor = new Color(0.9f, 0.9f, 0.9f, 0.9f);
        colorScheme.textColor = Color.black;
        ApplyColorSchemeToUI();
    }

    /// <summary>
    /// Aplica una configuracion optimizada para visualizar una alta densidad de datos (lineas y puntos mas pequeños).
    /// </summary>
    public void SetHighDensityMode()
    {
        lineThickness = 1f;
        dataPointSize = 5f;
        labelFontSize = 10;
        showDataLabels = false;

        UpdateSettingsPanelUI();
        ApplySettings();
    }

    /// <summary>
    /// Aplica una configuracion optimizada para presentaciones (elementos visuales mas grandes y claros).
    /// </summary>
    public void SetPresentationMode()
    {
        lineThickness = 3f;
        dataPointSize = 15f;
        labelFontSize = 14;
        showDataLabels = true;

        UpdateSettingsPanelUI();
        ApplySettings();
    }

    #endregion
}