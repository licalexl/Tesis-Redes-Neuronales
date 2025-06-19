using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public class AITrainingUI : MonoBehaviour
{
    [Header("Referencias ")]
    [SerializeField] private AITrainingSaver trainingSaver;
    [SerializeField] private AITrainingLoader trainingLoader;
    [SerializeField] private HumanModeController humanModeController;
    [SerializeField] private NPCGeneticAlgorithm geneticAlgorithm;

    [Header("Variables de UI para Guardado/Carga")]
    private string saveNameInput = "";
    private List<string> availableSaveFiles = new List<string>();
    private int selectedSaveFileIndex = 0;
    private bool showSaveLoadSection = true;
    private float timeScale = 1f;
    private float previousTimeScale = 1f;

    [Header("Configuración de Guardado/Carga")]
    [SerializeField] private bool loadFromProjectFolder = true;
    [SerializeField] private string saveFolder = "TrainingData";
    [SerializeField] private string projectSaveFolder = "SavedTrainings";

    private bool showFileSelectionDialog = false;
    private Vector2 fileSelectionScrollPosition = Vector2.zero;
    private string selectedFileName = "";

    // ID único para el panel de archivos
    private const int FILE_SELECTION_DIALOG_ID = 104;

    // Configuración de ventanas GUI
    [Header("Configuración de GUI")]
    [SerializeField] private WindowConfig mainWindowConfig;
    [SerializeField] private WindowConfig statsWindowConfig;
    [SerializeField] private WindowConfig keysWindowConfig;
    [SerializeField] private bool showKeysWindow = true;

    // Sistema de mapeo de teclas
    [Header("Mapeo de Teclas")]
    [SerializeField] private List<KeyActionMapping> keyMappings = new List<KeyActionMapping>();

    // Diccionario de acciones
    private Dictionary<string, Action> actionDictionary = new Dictionary<string, Action>();

    // Variables para la UI
    private bool isPaused = false;
    private bool showConfirmationDialog = false;
    private string confirmationMessage = "";
    private Action confirmationAction = null;

    // Variables para scroll
    private Vector2 keysScrollPosition = Vector2.zero;
    private Vector2 mainScrollPosition = Vector2.zero;
    private Vector2 statsScrollPosition = Vector2.zero;

    // Configuración de guardado/carga
    [Header("Configuración de Modelos")]
    [SerializeField] private string modelSavePath = "Assets/Models/";
   

    // IDs únicos para ventanas GUI - IMPORTANTE: Evitar conflictos
    private const int MAIN_WINDOW_ID = 100;
    private const int STATS_WINDOW_ID = 101;
    private const int KEYS_WINDOW_ID = 102;
    private const int CONFIRMATION_DIALOG_ID = 103;

    private void Awake()
    {
        if (mainWindowConfig == null)
        {
            mainWindowConfig = new WindowConfig("Entrenamiento IA", new Rect(10, 10, 350, 600), "AITraining_Main");
        }

        if (statsWindowConfig == null)
        {
            statsWindowConfig = new WindowConfig("Estadísticas", new Rect(370, 10, 300, 400), "AITraining_Stats");
        }

        if (keysWindowConfig == null)
        {
            keysWindowConfig = new WindowConfig("Controles de Teclado", new Rect(10, 620, 350, 300), "AITraining_Keys");
        }

        keysWindowConfig.enabled = showKeysWindow;
        mainWindowConfig.LoadConfig();
        statsWindowConfig.LoadConfig();
        keysWindowConfig.LoadConfig();

        SetupActionDictionary();

        if (keyMappings.Count == 0)
        {
            SetupDefaultKeyMappings();
        }

        if (!Directory.Exists(modelSavePath))
        {
            Directory.CreateDirectory(modelSavePath);
        }
    }

    private void Start()
    {
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                Debug.LogError("No se encontró NPCGeneticAlgorithm en la escena. Asigna la referencia manualmente.");
            }
        }

        if (trainingSaver == null)
            trainingSaver = FindObjectOfType<AITrainingSaver>();

        if (trainingLoader == null)
            trainingLoader = FindObjectOfType<AITrainingLoader>();

        if (humanModeController == null)
            humanModeController = FindObjectOfType<HumanModeController>();


        RefreshSaveFilesList();
    }


    /// <summary>
    /// Actualiza la lista de archivos guardados disponibles
    /// </summary>
    public void RefreshSaveFilesList()
    {
        availableSaveFiles.Clear();

        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            return;
        }

        string[] files = Directory.GetFiles(fullPath, "*.json");
        foreach (string file in files)
        {
            availableSaveFiles.Add(Path.GetFileName(file));
        }

        // Verificar si el archivo seleccionado aún existe
        if (!string.IsNullOrEmpty(selectedFileName) && !availableSaveFiles.Contains(selectedFileName))
        {
            selectedFileName = "";
            selectedSaveFileIndex = 0;
        }

        Debug.Log($"Se encontraron {availableSaveFiles.Count} archivos de guardado");
    }

    /// <summary>
    /// Guarda el entrenamiento usando AITrainingSaver
    /// </summary>
    private void SaveTrainingWithName()
    {
        if (trainingSaver == null)
        {
            Debug.LogWarning("AITrainingSaver no está asignado");
            return;
        }

        string saveName = string.IsNullOrEmpty(saveNameInput) ?
            $"Generation_{geneticAlgorithm.generation}" : saveNameInput;

        trainingSaver.SaveTraining(saveName);

        // Actualizar la selección al archivo recién guardado
        selectedFileName = $"{saveName}.json";

        RefreshSaveFilesList(); // Actualizar lista después de guardar
    }

    /// <summary>
    /// Pausa el sistema antes de cargar
    /// </summary>
    public void PauseBeforeLoad()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;
            RefreshSaveFilesList();
        }
    }

    /// <summary>
    /// Carga el entrenamiento seleccionado usando AITrainingLoader
    /// </summary>
    private void LoadSelectedTraining()
    {
        if (trainingLoader == null)
        {
            Debug.LogWarning("AITrainingLoader no está asignado");
            return;
        }

        if (string.IsNullOrEmpty(selectedFileName))
        {
            Debug.LogWarning("No hay archivo seleccionado");
            return;
        }

        Debug.Log($"Cargando archivo: {selectedFileName}");
        trainingLoader.LoadTraining(selectedFileName);
    }
    private void SetupActionDictionary()
    {
        actionDictionary.Add("IniciarEntrenamiento", IniciarEntrenamiento);
        actionDictionary.Add("DetenerEntrenamiento", DetenerEntrenamiento);
        actionDictionary.Add("PausarEntrenamiento", PausarEntrenamiento);
        actionDictionary.Add("ReanudarEntrenamiento", ReanudarEntrenamiento);
        actionDictionary.Add("GuardarModelo", GuardarModelo);
        actionDictionary.Add("CargarModelo", CargarModelo);
        actionDictionary.Add("ResetearParametros", ResetearParametros);
        actionDictionary.Add("AumentarVelocidad", AumentarVelocidad);
        actionDictionary.Add("DisminuirVelocidad", DisminuirVelocidad);
        actionDictionary.Add("SiguienteGeneracion", SiguienteGeneracion);
        actionDictionary.Add("ToggleVentanaTeclas", ToggleVentanaTeclas);
        actionDictionary.Add("GuardarConfiguracion", GuardarConfiguracion);
        actionDictionary.Add("CargarConfiguracion", CargarConfiguracion);
        actionDictionary.Add("ToggleEstadisticas", ToggleEstadisticas);
        actionDictionary.Add("AplicarAprendizajeInmediato", AplicarAprendizajeInmediato);
    }

    private void SetupDefaultKeyMappings()
    {
        keyMappings.Add(new KeyActionMapping("PausarEntrenamiento", KeyCode.P, "Pausar/Reanudar entrenamiento"));
        keyMappings.Add(new KeyActionMapping("GuardarModelo", KeyCode.F5, "Guardar modelo del mejor NPC"));
        keyMappings.Add(new KeyActionMapping("CargarModelo", KeyCode.F6, "Cargar modelo guardado"));
        keyMappings.Add(new KeyActionMapping("ResetearParametros", KeyCode.U, "Resetear población"));
        keyMappings.Add(new KeyActionMapping("AumentarVelocidad", KeyCode.KeypadPlus, "Aumentar velocidad de simulación"));
        keyMappings.Add(new KeyActionMapping("DisminuirVelocidad", KeyCode.KeypadMinus, "Disminuir velocidad de simulación"));
        keyMappings.Add(new KeyActionMapping("SiguienteGeneracion", KeyCode.N, "Forzar siguiente generación"));
        keyMappings.Add(new KeyActionMapping("ToggleVentanaTeclas", KeyCode.K, "Mostrar/Ocultar ventana de teclas"));
        keyMappings.Add(new KeyActionMapping("ToggleEstadisticas", KeyCode.T, "Mostrar/Ocultar estadísticas"));
        keyMappings.Add(new KeyActionMapping("AplicarAprendizajeInmediato", KeyCode.I, "Aplicar aprendizaje por imitación"));
    }

    private void Update()
    {
        ProcessKeyInputs();
    }

    private void ProcessKeyInputs()
    {
        foreach (var mapping in keyMappings)
        {
            if (Input.GetKeyDown(mapping.keyCode) && actionDictionary.ContainsKey(mapping.actionName))
            {
                actionDictionary[mapping.actionName].Invoke();
            }
        }
    }

    private void OnGUI()
    {
        if (Event.current == null) return;

        try
        {
            // Ventana principal
            if (mainWindowConfig.enabled)
            {
                mainWindowConfig.windowRect = GUI.Window(MAIN_WINDOW_ID, mainWindowConfig.windowRect, DrawMainWindow, mainWindowConfig.windowName);
            }

            // Ventana de estadísticas
            if (statsWindowConfig.enabled)
            {
                statsWindowConfig.windowRect = GUI.Window(STATS_WINDOW_ID, statsWindowConfig.windowRect, DrawStatsWindow, statsWindowConfig.windowName);
            }

            if (showKeysWindow && keysWindowConfig.enabled)
            {
                keysWindowConfig.windowRect = GUI.Window(KEYS_WINDOW_ID, keysWindowConfig.windowRect, DrawKeysInstructionsWindow, keysWindowConfig.windowName);
            }

            if (showConfirmationDialog)
            {
                GUI.ModalWindow(CONFIRMATION_DIALOG_ID, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 150), DrawConfirmationDialog, "Confirmar");
            }

            if (showFileSelectionDialog)
            {
                GUI.ModalWindow(FILE_SELECTION_DIALOG_ID, new Rect(Screen.width / 2 - 200, Screen.height / 2 - 250, 400, 400), DrawFileSelectionDialog, "Seleccionar Archivo");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en OnGUI de AITrainingUI: {e.Message}");
        }
    }

    /// <summary>
    /// Abre el panel de selección de archivos
    /// </summary>
    private void OpenFileSelectionDialog()
    {
        RefreshSaveFilesList();
        showFileSelectionDialog = true;
        fileSelectionScrollPosition = Vector2.zero;
    }

    /// <summary>
    /// Dibuja el panel modal de selección de archivos
    /// </summary>
    /// <param name="windowID">ID de la ventana</param>
    private void DrawFileSelectionDialog(int windowID)
    {
        GUILayout.BeginVertical();

        // Título
        GUILayout.Label("Archivos JSON Disponibles", GUI.skin.box);
        GUILayout.Space(10);

        // Botón para actualizar lista
        if (GUILayout.Button("Actualizar Lista", GUILayout.Height(25)))
        {
            RefreshSaveFilesList();
        }

        GUILayout.Space(5);

        // Lista de archivos con scroll
        if (availableSaveFiles.Count > 0)
        {
            GUILayout.Label($"Encontrados {availableSaveFiles.Count} archivos:");

            // Área de scroll para los archivos
            fileSelectionScrollPosition = GUILayout.BeginScrollView(
                fileSelectionScrollPosition,
                GUILayout.Width(380),
                GUILayout.Height(150)
            );

            // Mostrar cada archivo como un botón
            for (int i = 0; i < availableSaveFiles.Count; i++)
            {
                string fileName = availableSaveFiles[i];

                // Resaltar el archivo seleccionado
                bool isSelected = fileName == selectedFileName;

                // Cambiar el estilo si está seleccionado
                GUIStyle buttonStyle = isSelected ? GUI.skin.box : GUI.skin.button;

                if (GUILayout.Button($"{fileName} {(isSelected ? "✓" : "")}", buttonStyle, GUILayout.Height(25)))
                {
                    selectedFileName = fileName;
                    selectedSaveFileIndex = i;
                }
            }

            GUILayout.EndScrollView();
        }
        else
        {
            GUILayout.Label("No se encontraron archivos JSON", GUI.skin.box);
        }

        GUILayout.Space(10);

        // Mostrar archivo seleccionado
        if (!string.IsNullOrEmpty(selectedFileName))
        {
            GUILayout.Label($"Seleccionado: {selectedFileName}", GUI.skin.box);
        }

        GUILayout.Space(10);

        // Botones de acción
        GUILayout.BeginHorizontal();

        // Botón Cargar (solo activo si hay archivo seleccionado)
        GUI.enabled = !string.IsNullOrEmpty(selectedFileName);
        if (GUILayout.Button("Cargar", GUILayout.Height(30)))
        {
            LoadSelectedTraining();
            showFileSelectionDialog = false;
        }
        GUI.enabled = true;

        // Botón Cancelar
        if (GUILayout.Button("Cancelar", GUILayout.Height(30)))
        {
            showFileSelectionDialog = false;
        }

        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void DrawMainWindow(int windowID)
    {
        mainScrollPosition = GUILayout.BeginScrollView(mainScrollPosition, GUILayout.Width(mainWindowConfig.windowRect.width - 20), GUILayout.Height(mainWindowConfig.windowRect.height - 30));
        GUILayout.BeginVertical();

        if (geneticAlgorithm == null)
        {
            GUILayout.Label("ERROR: No hay referencia al NPCGeneticAlgorithm", GUI.skin.box);
            GUILayout.Label("Asigna la referencia en el Inspector");
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            return;
        }

        GUILayout.Label("Control de Entrenamiento", GUI.skin.box);
        GUILayout.Space(10);

        // Controles de parámetros
        GUILayout.BeginHorizontal();
        GUILayout.Label("Tamaño de población:", GUILayout.Width(150));
        geneticAlgorithm.populationSize = Mathf.RoundToInt(GUILayout.HorizontalSlider(geneticAlgorithm.populationSize, 10, 200, GUILayout.Width(100)));
        GUILayout.Label(geneticAlgorithm.populationSize.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tasa de mutación:", GUILayout.Width(150));
        geneticAlgorithm.mutationRate = GUILayout.HorizontalSlider(geneticAlgorithm.mutationRate, 0.001f, 0.1f, GUILayout.Width(100));
        GUILayout.Label(geneticAlgorithm.mutationRate.ToString("F3"), GUILayout.Width(50));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("NPCs Elite:", GUILayout.Width(150));
        geneticAlgorithm.eliteCount = Mathf.RoundToInt(GUILayout.HorizontalSlider(geneticAlgorithm.eliteCount, 1, 10, GUILayout.Width(100)));
        GUILayout.Label(geneticAlgorithm.eliteCount.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tiempo límite (seg):", GUILayout.Width(150));
        geneticAlgorithm.generationTimeLimit = GUILayout.HorizontalSlider(geneticAlgorithm.generationTimeLimit, 10f, 120f, GUILayout.Width(100));
        GUILayout.Label(geneticAlgorithm.generationTimeLimit.ToString("F0"), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Tiempo invencibilidad:", GUILayout.Width(150));
        geneticAlgorithm.invincibilityTime = GUILayout.HorizontalSlider(geneticAlgorithm.invincibilityTime, 0f, 10f, GUILayout.Width(100));
        GUILayout.Label(geneticAlgorithm.invincibilityTime.ToString("F1"), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Choques máximos:", GUILayout.Width(150));
        geneticAlgorithm.maxCollisions = Mathf.RoundToInt(GUILayout.HorizontalSlider(geneticAlgorithm.maxCollisions, 1, 5, GUILayout.Width(100)));
        GUILayout.Label(geneticAlgorithm.maxCollisions.ToString(), GUILayout.Width(30));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Estado del Sistema", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.Label($"Generación: {geneticAlgorithm.generation}");
        GUILayout.Label($"Pausado: {(geneticAlgorithm.isPaused ? "Sí" : "No")}");

        if (geneticAlgorithm.population != null)
        {
            int vivos = geneticAlgorithm.population.Count(n => n != null && !n.isDead);
            GUILayout.Label($"NPCs vivos: {vivos}/{geneticAlgorithm.population.Count}");
        }

        GUILayout.Space(5);
        GUILayout.Label("Control de Velocidad", GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad:", GUILayout.Width(70));
        float newTimeScale = GUILayout.HorizontalSlider(Time.timeScale, 0.1f, 10f, GUILayout.Width(100));
        if (!isPaused && Mathf.Abs(newTimeScale - Time.timeScale) > 0.01f)
        {
            Time.timeScale = newTimeScale;
        }
        GUILayout.Label($"{Time.timeScale:F1}x", GUILayout.Width(40));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("Acciones", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(geneticAlgorithm.isPaused ? "Reanudar" : "Pausar", GUILayout.Height(30)))
        {
            if (geneticAlgorithm.isPaused)
                ReanudarEntrenamiento();
            else
                PausarEntrenamiento();
        }

        if (GUILayout.Button("Nueva Generación", GUILayout.Height(30)))
        {
            SiguienteGeneracion();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Guardar Modelo", GUILayout.Height(25)))
        {
            GuardarModelo();
        }

        if (GUILayout.Button("Cargar Modelo", GUILayout.Height(25)))
        {
            CargarModelo();
        }
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Aplicar Aprendizaje", GUILayout.Height(25)))
        {
            AplicarAprendizajeInmediato();
        }

        if (GUILayout.Button("Resetear Población", GUILayout.Height(25)))
        {
            MostrarConfirmacion("¿Resetear toda la población?", ResetearParametros);
        }

        // SECCIÓN DE GUARDADO/CARGA ACTUALIZADA
        GUILayout.Space(10);
        GUILayout.Label("Guardado y Carga", GUI.skin.box);
        GUILayout.Space(5);

        // Campo de texto para nombre de archivo
        GUILayout.BeginHorizontal();
        GUILayout.Label("Nombre:", GUILayout.Width(60));
        saveNameInput = GUILayout.TextField(saveNameInput);
        GUILayout.EndHorizontal();

        // Botones de guardar y cargar
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Guardar", GUILayout.Height(25)))
        {
            SaveTrainingWithName();
        }

        if (GUILayout.Button("Abrir Archivos", GUILayout.Height(25)))
        {
            OpenFileSelectionDialog();
        }
        GUILayout.EndHorizontal();

        // Mostrar archivo seleccionado actualmente
        if (!string.IsNullOrEmpty(selectedFileName))
        {
            GUILayout.Label($"Último seleccionado: {selectedFileName}", GUI.skin.box);
        }
        else
        {
            GUILayout.Label("Ningún archivo seleccionado", GUI.skin.box);
        }

        GUILayout.Space(10);
        GUILayout.Label("Bloqueo de Acciones", GUI.skin.box);
        GUILayout.Space(5);

        geneticAlgorithm.lockMovement = GUILayout.Toggle(geneticAlgorithm.lockMovement, "Bloquear Movimiento");
        geneticAlgorithm.lockTurnLeft = GUILayout.Toggle(geneticAlgorithm.lockTurnLeft, "Bloquear Giro Izquierda");
        geneticAlgorithm.lockTurnRight = GUILayout.Toggle(geneticAlgorithm.lockTurnRight, "Bloquear Giro Derecha");
        geneticAlgorithm.lockJump = GUILayout.Toggle(geneticAlgorithm.lockJump, "Bloquear Salto");

        GUILayout.Space(10);
        GUILayout.Label("Configuración", GUI.skin.box);
        GUILayout.Space(5);

        bool newShowStats = GUILayout.Toggle(statsWindowConfig.enabled, "Mostrar Estadísticas");
        if (newShowStats != statsWindowConfig.enabled)
        {
            ToggleEstadisticas();
        }

        bool newShowKeys = GUILayout.Toggle(showKeysWindow, "Mostrar Teclas");
        if (newShowKeys != showKeysWindow)
        {
            ToggleVentanaTeclas();
        }
                
        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (mainWindowConfig.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, mainWindowConfig.windowRect.width, 20));
        }
    }

    private void DrawStatsWindow(int windowID)
    {
        statsScrollPosition = GUILayout.BeginScrollView(statsScrollPosition, GUILayout.Width(statsWindowConfig.windowRect.width - 20), GUILayout.Height(statsWindowConfig.windowRect.height - 30));
        GUILayout.BeginVertical();

        GUILayout.Label("Estadísticas en Tiempo Real", GUI.skin.box);
        GUILayout.Space(10);

        if (geneticAlgorithm == null)
        {
            GUILayout.Label("No hay datos disponibles");
        }
        else
        {
            GUILayout.Label($"Generación: {geneticAlgorithm.generation}");

            if (geneticAlgorithm.population != null && geneticAlgorithm.population.Count > 0)
            {
                var npcsVivos = geneticAlgorithm.population.Where(n => n != null && !n.isDead).ToList();
                var mejorNPC = geneticAlgorithm.population.Where(n => n != null).OrderByDescending(n => n.fitness).FirstOrDefault();

                GUILayout.Label($"NPCs vivos: {npcsVivos.Count}/{geneticAlgorithm.population.Count}");

                if (mejorNPC != null)
                {
                    GUILayout.Label($"Mejor fitness: {mejorNPC.fitness:F2}");
                    GUILayout.Label($"Distancia total: {mejorNPC.totalDistance:F1}");
                    GUILayout.Label($"Tiempo vivo: {mejorNPC.timeAlive:F1}s");
                    GUILayout.Label($"Saltos correctos: {mejorNPC.correctJumps}");
                    GUILayout.Label($"Saltos incorrectos: {mejorNPC.incorrectJumps}");
                    GUILayout.Label($"Áreas exploradas: {mejorNPC.uniqueAreasVisited}");
                }

                if (npcsVivos.Count > 0)
                {
                    float fitnessPromedio = npcsVivos.Average(n => n.fitness);
                    float distanciaPromedio = npcsVivos.Average(n => n.totalDistance);
                    GUILayout.Space(5);
                    GUILayout.Label($"Fitness promedio: {fitnessPromedio:F2}");
                    GUILayout.Label($"Distancia promedio: {distanciaPromedio:F1}");
                }
            }

            GUILayout.Space(5);
            GUILayout.Label($"Pausado: {(geneticAlgorithm.isPaused ? "Sí" : "No")}");
            GUILayout.Label($"Velocidad: {Time.timeScale:F1}x");
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Cerrar"))
        {
            statsWindowConfig.enabled = false;
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (statsWindowConfig.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, statsWindowConfig.windowRect.width, 20));
        }
    }

    private void DrawKeysInstructionsWindow(int windowID)
    {
        if (keyMappings == null || keyMappings.Count == 0)
        {
            GUILayout.Label("No hay teclas configuradas", GUI.skin.box);
            if (GUILayout.Button("Cerrar", GUILayout.Height(25)))
            {
                SafeCloseKeysWindow();
            }
            GUI.DragWindow();
            return;
        }

        try
        {
            float windowWidth = keysWindowConfig.windowRect.width;
            float windowHeight = keysWindowConfig.windowRect.height;

            float scrollWidth = Mathf.Max(200, windowWidth - 20);
            float scrollHeight = Mathf.Max(100, windowHeight - 80); 

            // Iniciar scroll view
            keysScrollPosition = GUILayout.BeginScrollView(
                keysScrollPosition,
                GUILayout.Width(scrollWidth),
                GUILayout.Height(scrollHeight)
            );

            GUILayout.BeginVertical();

            // Título
            GUILayout.Label("Atajos de Teclado", GUI.skin.box);
            GUILayout.Space(5);

            // Lista de teclas
            foreach (var mapping in keyMappings)
            {
                if (mapping != null) // Validación adicional
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(mapping.keyCode.ToString(), GUILayout.Width(80));
                    GUILayout.Label(mapping.description ?? "Sin descripción");
                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            // Espacio antes del botón
            GUILayout.Space(5);

            // Botón de cerrar
            if (GUILayout.Button("Cerrar", GUILayout.Height(25)))
            {
                SafeCloseKeysWindow();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en DrawKeysInstructionsWindow: {e.Message}");

            // UI de emergencia en caso de error
            GUILayout.Label("Error al mostrar teclas", GUI.skin.box);
            if (GUILayout.Button("Cerrar", GUILayout.Height(25)))
            {
                SafeCloseKeysWindow();
            }
        }

        // Permitir arrastrar la ventana
        if (keysWindowConfig.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, keysWindowConfig.windowRect.width, 20));
        }
    }

    private void SafeCloseKeysWindow()
    {
        try
        {
            showKeysWindow = false;
            keysWindowConfig.enabled = false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error al cerrar ventana de teclas: {e.Message}");
        }
    }

    private void DrawConfirmationDialog(int windowID)
    {
        GUILayout.BeginVertical();
        GUILayout.Space(20);
        GUILayout.Label(confirmationMessage, GUI.skin.box);
        GUILayout.Space(20);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Sí", GUILayout.Height(30)))
        {
            if (confirmationAction != null)
            {
                confirmationAction();
            }
            showConfirmationDialog = false;
        }

        if (GUILayout.Button("No", GUILayout.Height(30)))
        {
            showConfirmationDialog = false;
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void MostrarConfirmacion(string mensaje, Action accion)
    {
        confirmationMessage = mensaje;
        confirmationAction = accion;
        showConfirmationDialog = true;
    }

    // Métodos de acción
    private void IniciarEntrenamiento()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.InitializePopulation();
            Debug.Log("Entrenamiento iniciado");
        }
    }

    private void DetenerEntrenamiento()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;
            Debug.Log("Entrenamiento detenido");
        }
    }

    private void PausarEntrenamiento()
    {
        if (geneticAlgorithm != null)
        {
            previousTimeScale = Time.timeScale;
            geneticAlgorithm.isPaused = true;
            Time.timeScale = 0f;
            isPaused = true;
            Debug.Log("Entrenamiento pausado");
        }
    }



    private void ReanudarEntrenamiento()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = false;
            Time.timeScale = previousTimeScale;
            isPaused = false;
            Debug.Log("Entrenamiento reanudado");
        }
    }

    private void SiguienteGeneracion()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.ForceNextGeneration();
            Debug.Log("Avanzando a siguiente generación");
        }
    }

    private void ResetearParametros()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.InitializePopulation();
            geneticAlgorithm.generation = 1;
            Debug.Log("Población reseteada");
        }
    }

    private void AumentarVelocidad()
    {
        Time.timeScale = Mathf.Min(Time.timeScale * 1.5f, 10f);
        Debug.Log($"Velocidad: {Time.timeScale:F1}x");
    }

    private void DisminuirVelocidad()
    {
        Time.timeScale = Mathf.Max(Time.timeScale / 1.5f, 0.1f);
        Debug.Log($"Velocidad: {Time.timeScale:F1}x");
    }

    private void ToggleEstadisticas()
    {
        statsWindowConfig.enabled = !statsWindowConfig.enabled;
        Debug.Log($"Estadísticas: {(statsWindowConfig.enabled ? "Activadas" : "Desactivadas")}");
    }

    private void ToggleVentanaTeclas()
    {
        showKeysWindow = !showKeysWindow;
        keysWindowConfig.enabled = showKeysWindow;
        Debug.Log($"Ventana de teclas: {(showKeysWindow ? "Activada" : "Desactivada")}");
    }

    private void AplicarAprendizajeInmediato()
    {
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.ApplyImitationLearningNow();
            Debug.Log("Aplicando aprendizaje por imitación");
        }
    }

    private void GuardarModelo()
    {
        SaveTrainingWithName();
    }

    private void CargarModelo()
    {
        PauseBeforeLoad();
    }
    private void GuardarConfiguracion()
    {
        mainWindowConfig.SaveConfig();
        statsWindowConfig.SaveConfig();
        keysWindowConfig.SaveConfig();
        Debug.Log("Configuración guardada");
    }

    private void CargarConfiguracion()
    {
        mainWindowConfig.LoadConfig();
        statsWindowConfig.LoadConfig();
        keysWindowConfig.LoadConfig();
        Debug.Log("Configuración cargada");
    }

    private void OnApplicationQuit()
    {
        if (mainWindowConfig != null) mainWindowConfig.SaveConfig();
        if (statsWindowConfig != null) statsWindowConfig.SaveConfig();
        if (keysWindowConfig != null) keysWindowConfig.SaveConfig();
    }
}

[System.Serializable]
public class KeyActionMapping
{
    public string actionName;
    public KeyCode keyCode;
    public string description;

    public KeyActionMapping(string actionName, KeyCode keyCode, string description)
    {
        this.actionName = actionName;
        this.keyCode = keyCode;
        this.description = description;
    }
}