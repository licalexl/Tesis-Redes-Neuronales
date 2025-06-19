using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;


/// <summary>
/// Interfaz de usuario basica para el sistema de aprendizaje por imitacion.
/// Proporciona controles simples para aplicar el aprendizaje y monitorear el estado.
/// </summary>
public class ImitationLearningUI : MonoBehaviour
{


    #region Referencias del Sistema (mantener igual)
    [Header("Referencias del Sistema")]
    [Tooltip("Referencia al aplicador de aprendizaje por imitacion")]
    public ImitationLearningApplier imitationApplier;

    [Tooltip("Referencia al gestor de demostraciones")]
    public DemonstrationManager demonstrationManager;

    [Tooltip("Referencia al algoritmo genetico")]
    public NPCGeneticAlgorithm geneticAlgorithm;
    #endregion

    #region Configuración de Ventanas OnGUI (NUEVO)
    [Header("Configuración de Ventanas GUI")]
    [SerializeField] private WindowConfig mainImitationWindow;
    [SerializeField] private WindowConfig advancedImitationWindow;
    [SerializeField] private bool showAdvancedWindow = false;
    #endregion

    #region Variables de OnGUI (NUEVO)
    [Header("Configuración de Controles")]
    private float imitationStrengthValue = 0.5f;
    private float applicationIntervalValue = 10f;
    private float validationThresholdValue = 1.0f;
    private float curriculumAdvancementValue = 0.8f;
    private bool enableValidation = true;
    private bool enableCurriculum = false;
    private bool enableMultiLayer = false;

    private Vector2 mainScrollPosition = Vector2.zero;
    private Vector2 advancedScrollPosition = Vector2.zero;
    #endregion

    // IDs únicos para las ventanas
    private const int IMITATION_MAIN_WINDOW_ID = 300;
    private const int IMITATION_ADVANCED_WINDOW_ID = 301;

      
    #region Controles de UI
    [Header("Controles de Interfaz")]
    [Tooltip("Boton para aplicar manualmente el aprendizaje por imitacion")]
    public Button applyImitationButton;

    [Tooltip("Boton para recargar las demostraciones")]
    public Button reloadDemosButton;

    [Tooltip("Deslizador para ajustar la fuerza de imitacion")]
    public Slider imitationStrengthSlider;

    [Tooltip("Deslizador para ajustar el intervalo de aplicacion")]
    public Slider applicationIntervalSlider;
    #endregion

    #region Elementos de Visualizacion
    [Header("Elementos de Visualizacion")]
    [Tooltip("Texto para mostrar estadisticas del aprendizaje por imitacion")]
    public TextMeshProUGUI imitationStatsText;

    [Tooltip("Texto para mostrar informacion de demostraciones")]
    public TextMeshProUGUI demonstrationInfoText;

    [Tooltip("Texto para mostrar el valor de fuerza de imitacion")]
    public TextMeshProUGUI imitationStrengthText;

    [Tooltip("Texto para mostrar el valor del intervalo de aplicacion")]
    public TextMeshProUGUI applicationIntervalText;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa la interfaz de usuario y encuentra las referencias necesarias
    /// </summary>
    void Start()
    {
        // Buscar referencias si no están asignadas (mantener igual)
        if (imitationApplier == null)
            imitationApplier = FindObjectOfType<ImitationLearningApplier>();

        if (demonstrationManager == null)
            demonstrationManager = FindObjectOfType<DemonstrationManager>();

        if (geneticAlgorithm == null)
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();

        LoadInitialValues();
    }

    /// <summary>
    /// Carga los valores iniciales desde los componentes
    /// </summary>
    private void LoadInitialValues()
    {
        if (imitationApplier != null)
        {
            imitationStrengthValue = imitationApplier.imitationStrength;
            applicationIntervalValue = imitationApplier.applicationInterval;
            enableValidation = imitationApplier.enableValidation;
            enableCurriculum = imitationApplier.enableCurriculumLearning;
            enableMultiLayer = imitationApplier.enableMultiLayerLearning;
            validationThresholdValue = imitationApplier.minimumImprovementThreshold;
            curriculumAdvancementValue = imitationApplier.advancementFitnessPercentile;
        }
    }

    /// <summary>
    /// Configura los controles de la interfaz de usuario y sus eventos
    /// </summary>
    void SetupControls()
    {
        // Configurar botones
        if (applyImitationButton != null)
        {
            applyImitationButton.onClick.AddListener(ApplyImitationManually);
        }

        if (reloadDemosButton != null)
        {
            reloadDemosButton.onClick.AddListener(ReloadDemonstrations);
        }

        // Configurar deslizadores
        if (imitationStrengthSlider != null && imitationApplier != null)
        {
            imitationStrengthSlider.value = imitationApplier.imitationStrength;
            imitationStrengthSlider.onValueChanged.AddListener(OnImitationStrengthChanged);
        }

        if (applicationIntervalSlider != null && imitationApplier != null)
        {
            applicationIntervalSlider.value = imitationApplier.applicationInterval;
            applicationIntervalSlider.onValueChanged.AddListener(OnApplicationIntervalChanged);
        }
    }
    #endregion

    #region Metodos de Actualizacion

    /// <summary>
    /// Actualiza la interfaz de usuario cada frame
    /// </summary>
    void Update()
    {
        UpdateUI();
    }

    /// <summary>
    /// Actualiza todos los elementos de la interfaz de usuario
    /// </summary>
    void UpdateUI()
    {
        // Actualizar estadisticas del aprendizaje por imitacion
        if (imitationStatsText != null && imitationApplier != null)
        {
            int nextApplication = CalculateNextApplicationGeneration();

            imitationStatsText.text = $"Estadisticas de Aprendizaje por Imitacion:\n" +
                                     $"Aplicaciones Totales: {imitationApplier.totalApplications}\n" +
                                     $"Ultima Mejora: +{imitationApplier.lastApplicationImprovement:F1}\n" +
                                     $"Proxima Aplicacion: Gen {nextApplication}\n" +
                                     $"NPCs Objetivo: {imitationApplier.targetNPCCount}";
        }

        // Actualizar informacion de demostraciones
        if (demonstrationInfoText != null && demonstrationManager != null)
        {
            int totalFrames = demonstrationManager.GetTotalFramesAvailable();
            var bestDemo = demonstrationManager.GetBestDemonstration();
            float bestFitness = bestDemo != null ? bestDemo.totalFitness : 0f;

            demonstrationInfoText.text = $"Datos de Demostracion:\n" +
                                        $"Demos Cargadas: {demonstrationManager.loadedDemonstrations.Count}\n" +
                                        $"Frames Totales: {totalFrames}\n" +
                                        $"Mejor Demo Fitness: {bestFitness:F1}\n" +
                                        $"Estado: {(totalFrames > 0 ? "LISTO" : "SIN DATOS")}";
        }

        // Actualizar visualizacion de valores de deslizadores
        if (imitationStrengthText != null && imitationApplier != null)
        {
            imitationStrengthText.text = $"Fuerza de Imitacion: {imitationApplier.imitationStrength:F2}";
        }

        if (applicationIntervalText != null && imitationApplier != null)
        {
            applicationIntervalText.text = $"Intervalo de Aplicacion: {imitationApplier.applicationInterval} gens";
        }
    }
    #endregion

    #region Metodos de Calculo

    /// <summary>
    /// Calcula la proxima generacion en la que se aplicara el aprendizaje por imitacion
    /// </summary>
    /// <returns>Numero de la proxima generacion de aplicacion</returns>
    int CalculateNextApplicationGeneration()
    {
        if (imitationApplier == null || geneticAlgorithm == null) return 0;

        // Calcular cuando ocurrira la proxima aplicacion basada en la generacion actual y el intervalo
        int currentGen = geneticAlgorithm.generation;
        int interval = imitationApplier.applicationInterval;

        return ((currentGen / interval) + 1) * interval;
    }
    #endregion

    #region Metodos de Control Manual

    /// <summary>
    /// Aplica manualmente el aprendizaje por imitacion
    /// </summary>
    void ApplyImitationManually()
    {
        if (imitationApplier == null)
        {
            Debug.LogWarning("No se encontro el Aplicador de Aprendizaje por Imitacion");
            return;
        }

        if (demonstrationManager == null || demonstrationManager.loadedDemonstrations.Count == 0)
        {
            Debug.LogWarning("No hay demostraciones disponibles para el aprendizaje por imitacion");
            return;
        }

        imitationApplier.ForceApplyImitationLearning();
        Debug.Log("Aplicacion manual del aprendizaje por imitacion activada");
    }

    /// <summary>
    /// Recarga todas las demostraciones desde el disco
    /// </summary>
    void ReloadDemonstrations()
    {
        if (demonstrationManager != null)
        {
            demonstrationManager.LoadAllDemonstrations();
            Debug.Log("Demostraciones recargadas");
        }

        if (imitationApplier != null)
        {
            imitationApplier.ReloadDemonstrations();
        }
    }
    #endregion

    #region Manejadores de Eventos

    /// <summary>
    /// Maneja el cambio en el deslizador de fuerza de imitacion
    /// </summary>
    /// <param name="newValue">Nuevo valor del deslizador</param>
    void OnImitationStrengthChanged(float newValue)
    {
        if (imitationApplier != null)
        {
            imitationApplier.imitationStrength = newValue;
        }
    }

    /// <summary>
    /// Maneja el cambio en el deslizador de intervalo de aplicacion
    /// </summary>
    /// <param name="newValue">Nuevo valor del deslizador</param>
    void OnApplicationIntervalChanged(float newValue)
    {
        if (imitationApplier != null)
        {
            imitationApplier.applicationInterval = Mathf.RoundToInt(newValue);
        }
    }
    #endregion

    #region Metodos Publicos para Triggers Externos

    /// <summary>
    /// Establece la fuerza de imitacion desde codigo externo
    /// </summary>
    /// <param name="strength">Valor de fuerza entre 0 y 1</param>
    public void SetImitationStrength(float strength)
    {
        if (imitationApplier != null)
        {
            imitationApplier.imitationStrength = Mathf.Clamp01(strength);

            if (imitationStrengthSlider != null)
            {
                imitationStrengthSlider.value = imitationApplier.imitationStrength;
            }
        }
    }

    /// <summary>
    /// Establece el intervalo de aplicacion desde codigo externo
    /// </summary>
    /// <param name="interval">Numero de generaciones entre aplicaciones</param>
    public void SetApplicationInterval(int interval)
    {
        if (imitationApplier != null)
        {
            imitationApplier.applicationInterval = Mathf.Max(1, interval);

            if (applicationIntervalSlider != null)
            {
                applicationIntervalSlider.value = imitationApplier.applicationInterval;
            }
        }
    }
    #endregion

    #region Interfaz de Emergencia OnGUI

    /// <summary>
    /// Proporciona una interfaz de emergencia si los componentes UI no estan disponibles
    /// </summary>
    /// 

    void Awake()
    {
        // Configurar ventanas
        if (mainImitationWindow == null)
        {
            mainImitationWindow = new WindowConfig("Aprendizaje por Imitación", new Rect(680, 10, 400, 500), "Imitation_Main");
        }

        if (advancedImitationWindow == null)
        {
            advancedImitationWindow = new WindowConfig("Controles Avanzados", new Rect(1090, 10, 350, 400), "Imitation_Advanced");
        }

        mainImitationWindow.LoadConfig();
        advancedImitationWindow.LoadConfig();

        // Mantener la ventana de info existente
        if (imitationInfoWindow != null)
        {
            imitationInfoWindow.LoadConfig();
        }
    }


    [Header("Configuración de Ventanas GUI")]
    public WindowConfig imitationInfoWindow = new WindowConfig("Imitación", new Rect(10, 220, 300, 70));
    void OnGUI()
    {
        if (Event.current == null) return;

        try
        {
            // Ventana principal de imitación
            if (mainImitationWindow.enabled)
            {
                mainImitationWindow.windowRect = GUI.Window(IMITATION_MAIN_WINDOW_ID, mainImitationWindow.windowRect, DrawMainImitationWindow, mainImitationWindow.windowName);
            }

            // Ventana avanzada
            if (showAdvancedWindow && advancedImitationWindow.enabled)
            {
                advancedImitationWindow.windowRect = GUI.Window(IMITATION_ADVANCED_WINDOW_ID, advancedImitationWindow.windowRect, DrawAdvancedImitationWindow, advancedImitationWindow.windowName);
            }

            if (imitationStatsText == null && imitationApplier != null && imitationInfoWindow != null && imitationInfoWindow.enabled)
            {
                imitationInfoWindow.windowRect = GUI.Window(200, imitationInfoWindow.windowRect, DrawImitationInfoWindow, imitationInfoWindow.windowName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en OnGUI de ImitationLearningUI: {e.Message}");
        }
    }

    private void DrawMainImitationWindow(int windowID)
    {
        mainScrollPosition = GUILayout.BeginScrollView(mainScrollPosition, GUILayout.Width(mainImitationWindow.windowRect.width - 20), GUILayout.Height(mainImitationWindow.windowRect.height - 30));
        GUILayout.BeginVertical();

        if (imitationApplier == null)
        {
            GUILayout.Label("ERROR: No hay referencia al ImitationLearningApplier", GUI.skin.box);
            GUILayout.Label("Asigna la referencia en el Inspector");
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            return;
        }

        GUILayout.Label("Control de Aprendizaje por Imitación", GUI.skin.box);
        GUILayout.Space(10);

        // Estadísticas principales
        GUILayout.Label("Estadísticas", GUI.skin.box);
        GUILayout.Space(5);

        int nextApplication = CalculateNextApplicationGeneration();
        GUILayout.Label($"Aplicaciones Totales: {imitationApplier.totalApplications}");
        GUILayout.Label($"Última Mejora: +{imitationApplier.lastApplicationImprovement:F1}");
        GUILayout.Label($"Próxima Aplicación: Gen {nextApplication}");
        GUILayout.Label($"NPCs Objetivo: {imitationApplier.targetNPCCount}");

        GUILayout.Space(10);

        // Información de demostraciones
        GUILayout.Label("Demostraciones", GUI.skin.box);
        GUILayout.Space(5);

        if (demonstrationManager != null)
        {
            int totalFrames = demonstrationManager.GetTotalFramesAvailable();
            var bestDemo = demonstrationManager.GetBestDemonstration();
            float bestFitness = bestDemo != null ? bestDemo.totalFitness : 0f;

            GUILayout.Label($"Demos Cargadas: {demonstrationManager.loadedDemonstrations.Count}");
            GUILayout.Label($"Frames Totales: {totalFrames}");
            GUILayout.Label($"Mejor Demo Fitness: {bestFitness:F1}");
            GUILayout.Label($"Estado: {(totalFrames > 0 ? "LISTO" : "SIN DATOS")}");
        }
        else
        {
            GUILayout.Label("DemonstrationManager no encontrado", GUI.skin.box);
        }

        GUILayout.Space(10);

        // Controles principales
        GUILayout.Label("Controles Principales", GUI.skin.box);
        GUILayout.Space(5);

        // Fuerza de imitación
        GUILayout.BeginHorizontal();
        GUILayout.Label("Fuerza de Imitación:", GUILayout.Width(150));
        float newImitationStrength = GUILayout.HorizontalSlider(imitationStrengthValue, 0f, 1f, GUILayout.Width(100));
        if (Mathf.Abs(newImitationStrength - imitationStrengthValue) > 0.01f)
        {
            imitationStrengthValue = newImitationStrength;
            OnImitationStrengthChanged(imitationStrengthValue);
        }
        GUILayout.Label(imitationStrengthValue.ToString("F2"), GUILayout.Width(40));
        GUILayout.EndHorizontal();

        // Intervalo de aplicación
        GUILayout.BeginHorizontal();
        GUILayout.Label("Intervalo Aplicación:", GUILayout.Width(150));
        float newApplicationInterval = GUILayout.HorizontalSlider(applicationIntervalValue, 1f, 50f, GUILayout.Width(100));
        if (Mathf.Abs(newApplicationInterval - applicationIntervalValue) > 0.1f)
        {
            applicationIntervalValue = Mathf.Round(newApplicationInterval);
            OnApplicationIntervalChanged(applicationIntervalValue);
        }
        GUILayout.Label(applicationIntervalValue.ToString("F0"), GUILayout.Width(40));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Botones de acción
        GUILayout.Label("Acciones", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Aplicar Manualmente", GUILayout.Height(30)))
        {
            ApplyImitationManually();
        }

        if (GUILayout.Button("Recargar Demos", GUILayout.Height(30)))
        {
            ReloadDemonstrations();
        }
        GUILayout.EndHorizontal();

        // Toggles de configuración
        GUILayout.Space(10);
        GUILayout.Label("Configuración", GUI.skin.box);
        GUILayout.Space(5);

        bool newEnableValidation = GUILayout.Toggle(enableValidation, "Habilitar Validación");
        if (newEnableValidation != enableValidation)
        {
            enableValidation = newEnableValidation;
            OnValidationToggleChanged(enableValidation);
        }

        bool newEnableCurriculum = GUILayout.Toggle(enableCurriculum, "Habilitar Curriculum");
        if (newEnableCurriculum != enableCurriculum)
        {
            enableCurriculum = newEnableCurriculum;
            OnCurriculumToggleChanged(enableCurriculum);
        }

        bool newEnableMultiLayer = GUILayout.Toggle(enableMultiLayer, "Habilitar Multi-Capa");
        if (newEnableMultiLayer != enableMultiLayer)
        {
            enableMultiLayer = newEnableMultiLayer;
            OnMultiLayerToggleChanged(enableMultiLayer);
        }

        GUILayout.Space(10);

        // Toggle para ventana avanzada
        bool newShowAdvanced = GUILayout.Toggle(showAdvancedWindow, "Mostrar Controles Avanzados");
        if (newShowAdvanced != showAdvancedWindow)
        {
            showAdvancedWindow = newShowAdvanced;
            advancedImitationWindow.enabled = showAdvancedWindow;
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (mainImitationWindow.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, mainImitationWindow.windowRect.width, 20));
        }
    }

    /// <summary>
    /// Maneja el cambio en el toggle de validación
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    private void OnValidationToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableValidation = enabled;
    }

    /// <summary>
    /// Maneja el cambio en el toggle de curriculum
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    private void OnCurriculumToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableCurriculumLearning = enabled;
    }

    /// <summary>
    /// Maneja el cambio en el toggle de multi-capa
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    private void OnMultiLayerToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableMultiLayerLearning = enabled;
    }
    private void DrawAdvancedImitationWindow(int windowID)
    {
        advancedScrollPosition = GUILayout.BeginScrollView(advancedScrollPosition, GUILayout.Width(advancedImitationWindow.windowRect.width - 20), GUILayout.Height(advancedImitationWindow.windowRect.height - 30));
        GUILayout.BeginVertical();

        GUILayout.Label("Controles Avanzados", GUI.skin.box);
        GUILayout.Space(10);

        if (imitationApplier != null)
        {
            // Validación
            if (enableValidation)
            {
                GUILayout.Label("Sistema de Validación", GUI.skin.box);
                GUILayout.Space(5);

                GUILayout.Label($"Exitosas: {imitationApplier.successfulValidations}");
                GUILayout.Label($"Fallidas: {imitationApplier.failedValidations}");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Umbral Validación:", GUILayout.Width(120));
                float newValidationThreshold = GUILayout.HorizontalSlider(validationThresholdValue, 0f, 5f, GUILayout.Width(80));
                if (Mathf.Abs(newValidationThreshold - validationThresholdValue) > 0.01f)
                {
                    validationThresholdValue = newValidationThreshold;
                    OnValidationThresholdChanged(validationThresholdValue);
                }
                GUILayout.Label(validationThresholdValue.ToString("F2"), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Forzar Validación", GUILayout.Height(25)))
                {
                    ForceValidation();
                }

                GUILayout.Space(10);
            }

            // Curriculum
            if (enableCurriculum)
            {
                GUILayout.Label("Aprendizaje Curricular", GUI.skin.box);
                GUILayout.Space(5);

                GUILayout.Label($"Etapa Actual: {imitationApplier.currentCurriculumStage}/5");
                GUILayout.Label($"Progreso: {imitationApplier.stageProgress * 100:F0}%");
                GUILayout.Label($"Avances: {imitationApplier.stageAdvancementsCount}");

                GUILayout.BeginHorizontal();
                GUILayout.Label("Umbral Avance:", GUILayout.Width(120));
                float newCurriculumThreshold = GUILayout.HorizontalSlider(curriculumAdvancementValue, 0f, 1f, GUILayout.Width(80));
                if (Mathf.Abs(newCurriculumThreshold - curriculumAdvancementValue) > 0.01f)
                {
                    curriculumAdvancementValue = newCurriculumThreshold;
                    OnCurriculumThresholdChanged(curriculumAdvancementValue);
                }
                GUILayout.Label(curriculumAdvancementValue.ToString("F2"), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                if (GUILayout.Button("Avanzar Curriculum", GUILayout.Height(25)))
                {
                    AdvanceCurriculumManually();
                }

                GUILayout.Space(10);
            }

            // Métricas adicionales
            GUILayout.Label("Métricas de Rendimiento", GUI.skin.box);
            GUILayout.Space(5);

            GUILayout.Label($"Mejora Promedio: +{imitationApplier.averageImprovement:F2}");
            GUILayout.Label($"Última Validación: +{imitationApplier.lastValidationImprovement:F2}");

            if (geneticAlgorithm != null && geneticAlgorithm.population != null)
            {
                float avgFitness = geneticAlgorithm.population.Average(npc => npc.fitness);
                float bestFitness = geneticAlgorithm.population.Max(npc => npc.fitness);
                GUILayout.Label($"Fitness Promedio: {avgFitness:F1}");
                GUILayout.Label($"Mejor Fitness: {bestFitness:F1}");
            }
        }
        else
        {
            GUILayout.Label("ImitationLearningApplier no encontrado", GUI.skin.box);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Cerrar", GUILayout.Height(25)))
        {
            showAdvancedWindow = false;
            advancedImitationWindow.enabled = false;
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (advancedImitationWindow.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, advancedImitationWindow.windowRect.width, 20));
        }
    }

    private void OnValidationThresholdChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.minimumImprovementThreshold = value;
    }

    private void OnCurriculumThresholdChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.advancementFitnessPercentile = value;
    }

    private void ForceValidation()
    {
        if (imitationApplier != null)
        {
            imitationApplier.ForceValidation();
            Debug.Log("Validación manual activada");
        }
    }

    private void AdvanceCurriculumManually()
    {
        if (imitationApplier != null)
        {
            imitationApplier.ForceAdvanceCurriculumStage();
            Debug.Log("Avance manual del curriculum");
        }
    }

    private void OnApplicationQuit()
    {
        if (mainImitationWindow != null) mainImitationWindow.SaveConfig();
        if (advancedImitationWindow != null) advancedImitationWindow.SaveConfig();
        if (imitationInfoWindow != null) imitationInfoWindow.SaveConfig();
    }
    void DrawImitationInfoWindow(int windowID)
    {
        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, 25, 280, 20), $"Aprendizaje por Imitación: {imitationApplier.totalApplications} aplicaciones");

        if (demonstrationManager != null)
        {
            GUI.Label(new Rect(10, 45, 280, 20), $"Demostraciones: {demonstrationManager.loadedDemonstrations.Count} cargadas");
        }

        GUI.color = Color.white;

        // Resizable y draggable
        if (imitationInfoWindow.isResizable)
        {
            // Área de redimensionamiento en la esquina inferior derecha
            GUI.Box(new Rect(imitationInfoWindow.windowRect.width - 15, imitationInfoWindow.windowRect.height - 15, 10, 10), "");

            // Lógica de redimensionamiento
            Rect resizeRect = new Rect(imitationInfoWindow.windowRect.width - 15, imitationInfoWindow.windowRect.height - 15, 15, 15);
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.Box(resizeRect, "");
            GUI.color = Color.white;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeRect.Contains(currentEvent.mousePosition))
            {
                imitationInfoWindow.isResizing = true;
            }

            if (imitationInfoWindow.isResizing && currentEvent.type == EventType.MouseDrag)
            {
                imitationInfoWindow.windowRect.width = Mathf.Clamp(currentEvent.mousePosition.x, imitationInfoWindow.minSize.x, imitationInfoWindow.maxSize.x);
                imitationInfoWindow.windowRect.height = Mathf.Clamp(currentEvent.mousePosition.y, imitationInfoWindow.minSize.y, imitationInfoWindow.maxSize.y);
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                imitationInfoWindow.isResizing = false;
            }
        }

        // Área para arrastrar
        if (imitationInfoWindow.isDraggable)
        {
            GUI.DragWindow();
        }
    }

    // Agregar en OnDestroy o crear si no existe:
    void OnDestroy()
    {
        // Guardar configuración de ventanas
        imitationInfoWindow.SaveConfig();
    }
    #endregion
}

/// <summary>
/// Interfaz de usuario avanzada para el sistema de aprendizaje por imitacion.
/// Proporciona controles completos, graficos en tiempo real y diagnosticos detallados.
/// </summary>
public class EnhancedImitationUI : MonoBehaviour
{
    #region Referencias del Sistema
    [Header("Referencias del Sistema")]
    [Tooltip("Referencia al aplicador de aprendizaje por imitacion")]
    public ImitationLearningApplier imitationApplier;

    [Tooltip("Referencia al gestor de demostraciones")]
    public DemonstrationManager demonstrationManager;

    [Tooltip("Referencia al algoritmo genetico")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Tooltip("Referencia al controlador de modo humano")]
    public HumanModeController humanModeController;
    #endregion

    #region Panel de Control Principal
    [Header("Panel de Control Principal")]
    [Tooltip("Canvas principal de la interfaz")]
    public Canvas mainCanvas;

    [Tooltip("Panel de control principal")]
    public GameObject controlPanel;

    [Tooltip("Boton para mostrar/ocultar el panel")]
    public Button togglePanelButton;

    [Tooltip("Texto del boton de alternar panel")]
    public TextMeshProUGUI toggleButtonText;
    #endregion

    #region Controles de Aprendizaje por Imitacion
    [Header("Controles de Aprendizaje por Imitacion")]
    [Tooltip("Boton para aplicar imitacion manualmente")]
    public Button applyImitationButton;

    [Tooltip("Boton para recargar demostraciones")]
    public Button reloadDemosButton;

    [Tooltip("Boton para forzar validacion")]
    public Button forceValidationButton;

    [Tooltip("Deslizador de fuerza de imitacion")]
    public Slider imitationStrengthSlider;

    [Tooltip("Deslizador de intervalo de aplicacion")]
    public Slider applicationIntervalSlider;

    [Tooltip("Toggle para habilitar validacion")]
    public Toggle enableValidationToggle;

    [Tooltip("Toggle para habilitar aprendizaje curricular")]
    public Toggle enableCurriculumToggle;

    [Tooltip("Toggle para habilitar aprendizaje multi-capa")]
    public Toggle enableMultiLayerToggle;
    #endregion

    #region Paneles de Visualizacion
    [Header("Paneles de Visualizacion")]
    [Tooltip("Texto de estado del sistema")]
    public TextMeshProUGUI systemStatusText;

    [Tooltip("Texto de metricas de rendimiento")]
    public TextMeshProUGUI metricsDisplayText;

    [Tooltip("Texto de visualizacion del curriculum")]
    public TextMeshProUGUI curriculumDisplayText;

    [Tooltip("Texto de visualizacion de validacion")]
    public TextMeshProUGUI validationDisplayText;

    [Tooltip("Texto de visualizacion de tiempos")]
    public TextMeshProUGUI timingDisplayText;

    [Tooltip("Texto de visualizacion de demostraciones")]
    public TextMeshProUGUI demonstrationDisplayText;
    #endregion

    #region Graficos en Tiempo Real (Opcional)
    [Header("Graficos en Tiempo Real")]
    [Tooltip("Contenedor padre para el grafico de fitness")]
    public RectTransform fitnessGraphParent;

    [Tooltip("Contenedor padre para el grafico de mejoras")]
    public RectTransform improvementGraphParent;

    [Tooltip("Prefab para las lineas de los graficos")]
    public GameObject graphLinePrefab;
    #endregion

    #region Controles Avanzados
    [Header("Controles Avanzados")]
    [Tooltip("Boton para avanzar curriculum manualmente")]
    public Button advanceCurriculumButton;

    [Tooltip("Boton para resetear el sistema de tiempos")]
    public Button resetTimingButton;

    [Tooltip("Boton para exportar datos a archivo")]
    public Button exportDataButton;

    [Tooltip("Deslizador del umbral de validacion")]
    public Slider validationThresholdSlider;

    [Tooltip("Deslizador de avance del curriculum")]
    public Slider curriculumAdvancementSlider;
    #endregion

    #region Codificacion de Colores
    [Header("Codificacion de Colores")]
    [Tooltip("Color para indicar exito")]
    public Color successColor = Color.green;

    [Tooltip("Color para indicar advertencia")]
    public Color warningColor = Color.yellow;

    [Tooltip("Color para indicar error")]
    public Color errorColor = Color.red;

    [Tooltip("Color para informacion general")]
    public Color infoColor = Color.cyan;
    #endregion

    #region Estado Interno
    /// <summary>
    /// Indica si el panel esta abierto o cerrado
    /// </summary>
    private bool isPanelOpen = true;

    /// <summary>
    /// Tiempo de la ultima actualizacion de la interfaz
    /// </summary>
    private float lastUpdateTime = 0f;

    /// <summary>
    /// Intervalo de actualizacion de la interfaz en segundos
    /// </summary>
    private float updateInterval = 0.5f; // Actualizar cada 0.5 segundos

    /// <summary>
    /// Historial de valores de fitness para graficos
    /// </summary>
    private System.Collections.Generic.Queue<float> fitnessHistory = new System.Collections.Generic.Queue<float>();

    /// <summary>
    /// Historial de valores de mejora para graficos
    /// </summary>
    private System.Collections.Generic.Queue<float> improvementHistory = new System.Collections.Generic.Queue<float>();

    /// <summary>
    /// Numero maximo de puntos a mostrar en los graficos
    /// </summary>
    private int maxGraphPoints = 50;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa la interfaz de usuario avanzada
    /// </summary>
    void Start()
    {
        FindReferencesIfMissing();
        SetupUI();
        SetupEventListeners();
        UpdateAllDisplays();

        Debug.Log("Interfaz Avanzada de Aprendizaje por Imitacion inicializada");
    }

    /// <summary>
    /// Busca las referencias faltantes en la escena
    /// </summary>
    void FindReferencesIfMissing()
    {
        if (imitationApplier == null)
            imitationApplier = FindObjectOfType<ImitationLearningApplier>();

        if (demonstrationManager == null)
            demonstrationManager = FindObjectOfType<DemonstrationManager>();

        if (geneticAlgorithm == null)
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();

        if (humanModeController == null)
            humanModeController = FindObjectOfType<HumanModeController>();
    }

    /// <summary>
    /// Configura el estado inicial de la interfaz de usuario
    /// </summary>
    void SetupUI()
    {
        // Configurar estado inicial de la UI
        if (controlPanel != null)
        {
            controlPanel.SetActive(isPanelOpen);
        }

        // Configurar deslizadores con valores actuales
        if (imitationStrengthSlider != null && imitationApplier != null)
        {
            imitationStrengthSlider.value = imitationApplier.imitationStrength;
        }

        if (applicationIntervalSlider != null && imitationApplier != null)
        {
            applicationIntervalSlider.value = imitationApplier.applicationInterval;
        }

        // Configurar toggles
        if (enableValidationToggle != null && imitationApplier != null)
        {
            enableValidationToggle.isOn = imitationApplier.enableValidation;
        }

        if (enableCurriculumToggle != null && imitationApplier != null)
        {
            enableCurriculumToggle.isOn = imitationApplier.enableCurriculumLearning;
        }

        if (enableMultiLayerToggle != null && imitationApplier != null)
        {
            enableMultiLayerToggle.isOn = imitationApplier.enableMultiLayerLearning;
        }
    }

    /// <summary>
    /// Configura todos los event listeners de la interfaz
    /// </summary>
    void SetupEventListeners()
    {
        // Controles principales
        if (togglePanelButton != null)
            togglePanelButton.onClick.AddListener(TogglePanel);

        if (applyImitationButton != null)
            applyImitationButton.onClick.AddListener(ApplyImitationManually);

        if (reloadDemosButton != null)
            reloadDemosButton.onClick.AddListener(ReloadDemonstrations);

        if (forceValidationButton != null)
            forceValidationButton.onClick.AddListener(ForceValidation);

        // Deslizadores
        if (imitationStrengthSlider != null)
            imitationStrengthSlider.onValueChanged.AddListener(OnImitationStrengthChanged);

        if (applicationIntervalSlider != null)
            applicationIntervalSlider.onValueChanged.AddListener(OnApplicationIntervalChanged);

        if (validationThresholdSlider != null)
            validationThresholdSlider.onValueChanged.AddListener(OnValidationThresholdChanged);

        if (curriculumAdvancementSlider != null)
            curriculumAdvancementSlider.onValueChanged.AddListener(OnCurriculumThresholdChanged);

        // Toggles
        if (enableValidationToggle != null)
            enableValidationToggle.onValueChanged.AddListener(OnValidationToggleChanged);

        if (enableCurriculumToggle != null)
            enableCurriculumToggle.onValueChanged.AddListener(OnCurriculumToggleChanged);

        if (enableMultiLayerToggle != null)
            enableMultiLayerToggle.onValueChanged.AddListener(OnMultiLayerToggleChanged);

        // Controles avanzados
        if (advanceCurriculumButton != null)
            advanceCurriculumButton.onClick.AddListener(AdvanceCurriculumManually);

        if (resetTimingButton != null)
            resetTimingButton.onClick.AddListener(ResetTimingSystem);

        if (exportDataButton != null)
            exportDataButton.onClick.AddListener(ExportDataToFile);
    }
    #endregion

    #region Metodos de Actualizacion

    /// <summary>
    /// Actualiza la interfaz periodicamente y maneja atajos de teclado
    /// </summary>
    void Update()
    {
        // Actualizar displays periodicamente
        if (Time.time - lastUpdateTime > updateInterval)
        {
            UpdateAllDisplays();
            UpdateGraphs();
            lastUpdateTime = Time.time;
        }

        // Manejar atajos de teclado
        HandleKeyboardShortcuts();
    }

    /// <summary>
    /// Maneja los atajos de teclado para controles rapidos
    /// </summary>
    void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            TogglePanel();
        }

        if (Input.GetKeyDown(KeyCode.F1) && imitationApplier != null)
        {
            ApplyImitationManually();
        }

        if (Input.GetKeyDown(KeyCode.F2) && demonstrationManager != null)
        {
            ReloadDemonstrations();
        }
    }

    /// <summary>
    /// Actualiza todos los displays de la interfaz
    /// </summary>
    void UpdateAllDisplays()
    {
        UpdateSystemStatus();
        UpdateMetricsDisplay();
        UpdateCurriculumDisplay();
        UpdateValidationDisplay();
        UpdateTimingDisplay();
        UpdateDemonstrationDisplay();
        UpdateButtonStates();
    }

    /// <summary>
    /// Actualiza el display de estado del sistema
    /// </summary>
    void UpdateSystemStatus()
    {
        if (systemStatusText == null) return;

        string status = "ESTADO DEL SISTEMA DE APRENDIZAJE POR IMITACION\n";

        // Salud general del sistema
        bool isHealthy = true;
        string healthIcon = "CORRECTO";

        if (imitationApplier == null)
        {
            status += $"{errorColor.ToHex()} ERROR: Aplicador de Imitacion: NO ENCONTRADO\n";
            isHealthy = false;
        }
        else
        {
            status += $"{successColor.ToHex()} CORRECTO: Aplicador de Imitacion: ACTIVO\n";
        }

        if (demonstrationManager == null || demonstrationManager.loadedDemonstrations.Count == 0)
        {
            status += $"{warningColor.ToHex()} ADVERTENCIA: Demostraciones: SIN DATOS\n";
            isHealthy = false;
        }
        else
        {
            status += $"{successColor.ToHex()} CORRECTO: Demostraciones: {demonstrationManager.loadedDemonstrations.Count} CARGADAS\n";
        }

        if (geneticAlgorithm == null || geneticAlgorithm.population == null)
        {
            status += $"{errorColor.ToHex()} ERROR: Algoritmo Genetico: NO LISTO\n";
            isHealthy = false;
        }
        else
        {
            status += $"{successColor.ToHex()} CORRECTO: Poblacion: {geneticAlgorithm.population.Count} NPCs\n";
        }

        // Estado del modo humano
        if (humanModeController != null && humanModeController.isActive)
        {
            status += $"{infoColor.ToHex()} INFO: Modo Humano: GRABANDO\n";
        }

        healthIcon = isHealthy ? "BUENA" : "PROBLEMAS";
        status = $"SALUD DEL SISTEMA: {(isHealthy ? "BUENA" : "PROBLEMAS")}\n" + status;

        systemStatusText.text = status;
    }

    /// <summary>
    /// Actualiza el display de metricas de rendimiento
    /// </summary>
    void UpdateMetricsDisplay()
    {
        if (metricsDisplayText == null || imitationApplier == null) return;

        string metrics = "METRICAS DE RENDIMIENTO\n";

        metrics += $"Aplicaciones: {imitationApplier.totalApplications}\n";
        metrics += $"Tasa de Exito: {CalculateSuccessRate():F1}%\n";
        metrics += $"Mejora Promedio: +{imitationApplier.averageImprovement:F2}\n";
        metrics += $"Ultima Mejora: +{imitationApplier.lastValidationImprovement:F2}\n";

        if (geneticAlgorithm != null && geneticAlgorithm.population != null)
        {
            float avgFitness = geneticAlgorithm.population.Average(npc => npc.fitness);
            float bestFitness = geneticAlgorithm.population.Max(npc => npc.fitness);
            metrics += $"Fitness Promedio Actual: {avgFitness:F1}\n";
            metrics += $"Mejor Fitness Actual: {bestFitness:F1}\n";
        }

        metricsDisplayText.text = metrics;
    }

    /// <summary>
    /// Actualiza el display del aprendizaje curricular
    /// </summary>
    void UpdateCurriculumDisplay()
    {
        if (curriculumDisplayText == null || imitationApplier == null) return;

        string curriculum = "APRENDIZAJE CURRICULAR\n";

        if (imitationApplier.enableCurriculumLearning)
        {
            curriculum += $"Estado: {successColor.ToHex()}HABILITADO\n";
            curriculum += $"Etapa Actual: {imitationApplier.currentCurriculumStage}/5\n";
            curriculum += $"Etapa: {imitationApplier.currentStageName}\n";
            curriculum += $"Progreso: {imitationApplier.stageProgress * 100:F0}%\n";
            curriculum += $"Avances: {imitationApplier.stageAdvancementsCount}\n";
        }
        else
        {
            curriculum += $"Estado: {warningColor.ToHex()}DESHABILITADO\n";
            curriculum += "Habilitar para usar aprendizaje progresivo\n";
        }

        curriculumDisplayText.text = curriculum;
    }

    /// <summary>
    /// Actualiza el display del sistema de validacion
    /// </summary>
    void UpdateValidationDisplay()
    {
        if (validationDisplayText == null || imitationApplier == null) return;

        string validation = "SISTEMA DE VALIDACION\n";

        if (imitationApplier.enableValidation)
        {
            validation += $"Estado: {successColor.ToHex()}HABILITADO\n";
            validation += $"Exitosas: {imitationApplier.successfulValidations}\n";
            validation += $"Fallidas: {imitationApplier.failedValidations}\n";
            validation += $"Ultimo Resultado: {(imitationApplier.lastValidationImprovement >= imitationApplier.minimumImprovementThreshold ? successColor.ToHex() + "PASO" : errorColor.ToHex() + "FALLO")}\n";
            validation += $"Umbral: {imitationApplier.minimumImprovementThreshold:F3}\n";
        }
        else
        {
            validation += $"Estado: {warningColor.ToHex()}DESHABILITADO\n";
            validation += "Habilitar para control automatico de calidad\n";
        }

        validationDisplayText.text = validation;
    }

    /// <summary>
    /// Actualiza el display del sistema de tiempos
    /// </summary>
    void UpdateTimingDisplay()
    {
        if (timingDisplayText == null || imitationApplier == null) return;

        string timing = "TIEMPOS ADAPTATIVOS\n";

        string diagnostics = imitationApplier.GetTimingDiagnostics();
        if (!string.IsNullOrEmpty(diagnostics))
        {
            timing += diagnostics.Replace("\n", "\n");
        }
        else
        {
            timing += "Sistema de tiempos no disponible\n";
            timing += $"Proxima Aplicacion: Gen {CalculateNextApplication()}\n";
        }

        timingDisplayText.text = timing;
    }

    /// <summary>
    /// Actualiza el display de informacion de demostraciones
    /// </summary>
    void UpdateDemonstrationDisplay()
    {
        if (demonstrationDisplayText == null) return;

        string demos = "DATOS DE DEMOSTRACION\n";

        if (demonstrationManager != null)
        {
            demos += $"Demos Cargadas: {demonstrationManager.loadedDemonstrations.Count}\n";
            demos += $"Frames Totales: {demonstrationManager.GetTotalFramesAvailable()}\n";

            var bestDemo = demonstrationManager.GetBestDemonstration();
            if (bestDemo != null)
            {
                demos += $"Fitness Mejor Demo: {bestDemo.totalFitness:F1}\n";
                demos += $"Frames Mejor Demo: {bestDemo.frames.Count}\n";
            }

            if (humanModeController != null)
            {
                string recordingStatus = humanModeController.isActive ? $"{errorColor.ToHex()}GRABANDO" : $"{infoColor.ToHex()}EN ESPERA";
                demos += $"Grabacion: {recordingStatus}\n";
            }
        }
        else
        {
            demos += $"{errorColor.ToHex()}Gestor de Demostraciones no encontrado\n";
        }

        demonstrationDisplayText.text = demos;
    }

    /// <summary>
    /// Actualiza el estado de interactividad de los botones
    /// </summary>
    void UpdateButtonStates()
    {
        // Actualizar interactividad de botones basada en el estado del sistema
        if (applyImitationButton != null)
        {
            bool canApply = imitationApplier != null &&
                           demonstrationManager != null &&
                           demonstrationManager.loadedDemonstrations.Count > 0;
            applyImitationButton.interactable = canApply;
        }

        if (forceValidationButton != null)
        {
            bool canValidate = imitationApplier != null && imitationApplier.enableValidation;
            forceValidationButton.interactable = canValidate;
        }

        if (advanceCurriculumButton != null)
        {
            bool canAdvance = imitationApplier != null && imitationApplier.enableCurriculumLearning;
            advanceCurriculumButton.interactable = canAdvance;
        }
    }

    /// <summary>
    /// Actualiza los graficos en tiempo real
    /// </summary>
    void UpdateGraphs()
    {
        if (geneticAlgorithm == null || geneticAlgorithm.population == null) return;

        // Actualizar historial de fitness
        float currentAvgFitness = geneticAlgorithm.population.Average(npc => npc.fitness);
        fitnessHistory.Enqueue(currentAvgFitness);

        if (fitnessHistory.Count > maxGraphPoints)
            fitnessHistory.Dequeue();

        // Actualizar historial de mejoras
        improvementHistory.Enqueue(imitationApplier?.lastValidationImprovement ?? 0f);

        if (improvementHistory.Count > maxGraphPoints)
            improvementHistory.Dequeue();

        // TODO: Implementar renderizado real de graficos si los prefabs estan disponibles
        // DrawGraph(fitnessGraphParent, fitnessHistory);
        // DrawGraph(improvementGraphParent, improvementHistory);
    }
    #endregion

    #region Manejadores de Eventos

    /// <summary>
    /// Alterna la visibilidad del panel de control
    /// </summary>
    void TogglePanel()
    {
        isPanelOpen = !isPanelOpen;
        if (controlPanel != null)
            controlPanel.SetActive(isPanelOpen);

        if (toggleButtonText != null)
            toggleButtonText.text = isPanelOpen ? "Ocultar Panel [Tab]" : "Mostrar Panel [Tab]";
    }

    /// <summary>
    /// Aplica manualmente el aprendizaje por imitacion
    /// </summary>
    void ApplyImitationManually()
    {
        if (imitationApplier != null)
        {
            imitationApplier.ForceApplyImitationLearning();
            Debug.Log("Aprendizaje por imitacion manual activado via UI");
        }
    }

    /// <summary>
    /// Recarga las demostraciones desde el disco
    /// </summary>
    void ReloadDemonstrations()
    {
        if (demonstrationManager != null)
        {
            demonstrationManager.LoadAllDemonstrations();
            Debug.Log("Demostraciones recargadas via UI");
        }
    }

    /// <summary>
    /// Fuerza una validacion manual del sistema
    /// </summary>
    void ForceValidation()
    {
        if (imitationApplier != null)
        {
            imitationApplier.ForceValidation();
            Debug.Log("Validacion manual activada via UI");
        }
    }

    /// <summary>
    /// Avanza manualmente el curriculum a la siguiente etapa
    /// </summary>
    void AdvanceCurriculumManually()
    {
        if (imitationApplier != null)
        {
            imitationApplier.ForceAdvanceCurriculumStage();
            Debug.Log("Avance manual del curriculum via UI");
        }
    }

    /// <summary>
    /// Reinicia el sistema de tiempos y contadores
    /// </summary>
    void ResetTimingSystem()
    {
        // Reiniciar contadores relacionados con tiempos
        if (imitationApplier != null)
        {
            // Reiniciar contadores de triggers (necesitaria agregarse a ImitationLearningApplier)
            Debug.Log("Sistema de tiempos reiniciado via UI");
        }
    }

    /// <summary>
    /// Exporta los datos actuales a un archivo JSON
    /// </summary>
    void ExportDataToFile()
    {
        // Exportar metricas actuales a archivo JSON
        string exportData = GenerateExportData();
        string fileName = $"ImitationLearning_Export_{System.DateTime.Now:yyyyMMdd_HHmmss}.json";
        string path = System.IO.Path.Combine(Application.persistentDataPath, fileName);

        try
        {
            System.IO.File.WriteAllText(path, exportData);
            Debug.Log($"Datos exportados a: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fallo al exportar datos: {e.Message}");
        }
    }

    /// <summary>
    /// Maneja el cambio en el deslizador de fuerza de imitacion
    /// </summary>
    /// <param name="value">Nuevo valor del deslizador</param>
    void OnImitationStrengthChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.imitationStrength = value;
    }

    /// <summary>
    /// Maneja el cambio en el deslizador de intervalo de aplicacion
    /// </summary>
    /// <param name="value">Nuevo valor del deslizador</param>
    void OnApplicationIntervalChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.applicationInterval = Mathf.RoundToInt(value);
    }

    /// <summary>
    /// Maneja el cambio en el deslizador de umbral de validacion
    /// </summary>
    /// <param name="value">Nuevo valor del deslizador</param>
    void OnValidationThresholdChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.minimumImprovementThreshold = value;
    }

    /// <summary>
    /// Maneja el cambio en el deslizador de umbral de curriculum
    /// </summary>
    /// <param name="value">Nuevo valor del deslizador</param>
    void OnCurriculumThresholdChanged(float value)
    {
        if (imitationApplier != null)
            imitationApplier.advancementFitnessPercentile = value;
    }

    /// <summary>
    /// Maneja el cambio en el toggle de validacion
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    void OnValidationToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableValidation = enabled;
    }

    /// <summary>
    /// Maneja el cambio en el toggle de curriculum
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    void OnCurriculumToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableCurriculumLearning = enabled;
    }

    /// <summary>
    /// Maneja el cambio en el toggle de multi-capa
    /// </summary>
    /// <param name="enabled">Estado del toggle</param>
    void OnMultiLayerToggleChanged(bool enabled)
    {
        if (imitationApplier != null)
            imitationApplier.enableMultiLayerLearning = enabled;
    }
    #endregion

    #region Metodos de Ayuda

    /// <summary>
    /// Calcula la tasa de exito de las validaciones
    /// </summary>
    /// <returns>Porcentaje de exito de las validaciones</returns>
    float CalculateSuccessRate()
    {
        if (imitationApplier == null) return 0f;

        int total = imitationApplier.successfulValidations + imitationApplier.failedValidations;
        if (total == 0) return 0f;

        return (float)imitationApplier.successfulValidations / total * 100f;
    }

    /// <summary>
    /// Calcula la proxima generacion de aplicacion
    /// </summary>
    /// <returns>Numero de la proxima generacion</returns>
    int CalculateNextApplication()
    {
        if (imitationApplier == null || geneticAlgorithm == null) return 0;

        int currentGen = geneticAlgorithm.generation;
        int interval = imitationApplier.applicationInterval;

        return ((currentGen / interval) + 1) * interval;
    }

    /// <summary>
    /// Genera los datos de exportacion en formato JSON
    /// </summary>
    /// <returns>String JSON con los datos del sistema</returns>
    string GenerateExportData()
    {
        var exportObject = new
        {
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            generation = geneticAlgorithm?.generation ?? 0,
            totalApplications = imitationApplier?.totalApplications ?? 0,
            successfulValidations = imitationApplier?.successfulValidations ?? 0,
            failedValidations = imitationApplier?.failedValidations ?? 0,
            averageImprovement = imitationApplier?.averageImprovement ?? 0f,
            currentStage = imitationApplier?.currentCurriculumStage ?? 0,
            loadedDemonstrations = demonstrationManager?.loadedDemonstrations.Count ?? 0,
            fitnessHistory = fitnessHistory.ToArray(),
            improvementHistory = improvementHistory.ToArray()
        };

        return JsonUtility.ToJson(exportObject, true);
    }
    #endregion
}

/// <summary>
/// Metodos de extension para conversion de colores a formato hexadecimal
/// </summary>
public static class ColorExtensions
{
    /// <summary>
    /// Convierte un color Unity a formato hexadecimal para UI de texto
    /// </summary>
    /// <param name="color">Color a convertir</param>
    /// <returns>String con el codigo hexadecimal del color</returns>
    public static string ToHex(this Color color)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>";
    }
}