using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Controlador especializado para NPCs en modo gameplay final.
/// Permite cargar cerebros entrenados y usar NPCs sin la lógica de entrenamiento.
/// Versión OnGUI con WindowConfig.
/// </summary>
public class GameplayNPCController : MonoBehaviour
{
    #region Referencias
    [Header("Referencias Requeridas")]
    [Tooltip("Referencia al GameplayNPC")]
    public GameplayNPC npcController;
    #endregion

    #region Configuración de Archivos
    [Header("Configuración de Archivos")]
    [Tooltip("Cargar desde carpeta del proyecto")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta donde están los entrenamientos")]
    public string saveFolder = "SavedTrainings";
    #endregion

    #region Configuración de GUI
    [Header("Configuración de GUI")]
    [SerializeField] private WindowConfig mainWindowConfig;
    [SerializeField] private WindowConfig statsWindowConfig;
    [SerializeField] private WindowConfig advancedWindowConfig;
    [SerializeField] private bool showStatsWindow = true;
    [SerializeField] private bool showAdvancedWindow = false;
    #endregion

    #region Variables de Estado
    [Header("Estado (Solo Lectura)")]
    [SerializeField] private bool brainLoaded = false;
    [SerializeField] private string currentFileName = "";
    [SerializeField] private int currentNetworkIndex = 0;
    [SerializeField] private string loadedBrainInfo = "";
    [SerializeField] private List<string> availableFiles = new List<string>();
    #endregion

    #region Variables GUI
    private Vector2 mainScrollPosition = Vector2.zero;
    private Vector2 statsScrollPosition = Vector2.zero;
    private Vector2 advancedScrollPosition = Vector2.zero;
    private Vector2 fileListScrollPosition = Vector2.zero;

    // Variables para controles GUI
    private int selectedFileIndex = 0;
    private int selectedStrategyIndex = 0;
    private float speedMultiplier = 1f;
    private float rotationMultiplier = 1f;
    private string customFileName = "";
    private bool showSensors = false;

    // Variables para dropdown simulado de archivos
    private bool showFileDropdown = false;

    // Variables para dropdown de estrategia
    private bool showStrategyDropdown = false;
    private string[] strategyOptions = { "Mejor (0)", "Segundo Mejor (1)", "Tercero (2)", "Aleatorio", "Peor" };
    #endregion

    #region Variables Privadas
    private TrainingData currentTrainingData;
    private float statsUpdateTimer = 0f;
    private const float STATS_UPDATE_INTERVAL = 0.5f;

    // IDs únicos para ventanas
    private const int MAIN_WINDOW_ID = 200;
    private const int STATS_WINDOW_ID = 201;
    private const int ADVANCED_WINDOW_ID = 202;
    #endregion

    #region Inicialización
    void Awake()
    {
        if (mainWindowConfig == null)
        {
            mainWindowConfig = new WindowConfig("Control de NPC", new Rect(10, 10, 400, 500), "GameplayNPC_Main");
        }

        if (statsWindowConfig == null)
        {
            statsWindowConfig = new WindowConfig("Estadísticas", new Rect(420, 10, 300, 300), "GameplayNPC_Stats");
        }

        if (advancedWindowConfig == null)
        {
            advancedWindowConfig = new WindowConfig("Controles Avanzados", new Rect(730, 10, 350, 400), "GameplayNPC_Advanced");
        }

        statsWindowConfig.enabled = showStatsWindow;
        advancedWindowConfig.enabled = showAdvancedWindow;

        mainWindowConfig.LoadConfig();
        statsWindowConfig.LoadConfig();
        advancedWindowConfig.LoadConfig();
    }

    void Start()
    {
        
        if (npcController == null)
        {
            npcController = GetComponent<GameplayNPC>();
            if (npcController == null)
            {
                Debug.LogError($"[{gameObject.name}] No se encontró GameplayNPC");
                enabled = false;
                return;
            }
        }

        RefreshAvailableFiles();
    }
    #endregion

    #region Update
    void Update()
    {
        // Actualizar estadísticas
        statsUpdateTimer += Time.deltaTime;
        if (statsUpdateTimer >= STATS_UPDATE_INTERVAL)
        {
            statsUpdateTimer = 0f;
        }

        // Teclas rápidas para testing
        HandleHotkeys();
    }

    void HandleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetNPCPosition();
        }

        if (Input.GetKeyDown(KeyCode.L))
        {
            LoadSelectedBrain();
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            RefreshAvailableFiles();
        }

        // Cambiar índice de red con teclas numéricas
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                SetNetworkIndex(i);
            }
        }

        // Toggle ventanas con teclas
        if (Input.GetKeyDown(KeyCode.T))
        {
            ToggleStatsWindow();
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            ToggleAdvancedWindow();
        }
    }
    #endregion

    #region OnGUI
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

            // Ventana avanzada
            if (advancedWindowConfig.enabled)
            {
                advancedWindowConfig.windowRect = GUI.Window(ADVANCED_WINDOW_ID, advancedWindowConfig.windowRect, DrawAdvancedWindow, advancedWindowConfig.windowName);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error en OnGUI de GameplayNPCController: {e.Message}");
        }
    }

    private void DrawMainWindow(int windowID)
    {
        mainScrollPosition = GUILayout.BeginScrollView(mainScrollPosition, GUILayout.Width(mainWindowConfig.windowRect.width - 20), GUILayout.Height(mainWindowConfig.windowRect.height - 30));
        GUILayout.BeginVertical();

        if (npcController == null)
        {
            GUILayout.Label("ERROR: No hay referencia al GameplayNPC", GUI.skin.box);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            return;
        }

        // Información del cerebro cargado
        GUILayout.Label("Estado del Cerebro", GUI.skin.box);
        if (brainLoaded)
        {
            GUI.color = Color.green;
            GUILayout.Label(" Cerebro Cargado", GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.Label(loadedBrainInfo, GUI.skin.box);
        }
        else
        {
            GUI.color = Color.red;
            GUILayout.Label(" Sin cerebro cargado", GUI.skin.box);
            GUI.color = Color.white;
            GUILayout.Label("Selecciona archivo y presiona Cargar", GUI.skin.box);
        }

        GUILayout.Space(10);

        // Selección de archivo
        GUILayout.Label("Selección de Archivo", GUI.skin.box);
        GUILayout.Space(5);

        // Dropdown simulado para archivos
        if (availableFiles.Count > 0)
        {
            selectedFileIndex = Mathf.Clamp(selectedFileIndex, 0, availableFiles.Count - 1);
            currentFileName = availableFiles[selectedFileIndex];

            GUILayout.Label($"Archivo: {currentFileName}", GUI.skin.box);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Anterior") && selectedFileIndex > 0)
            {
                selectedFileIndex--;
                OnTrainingFileSelected(selectedFileIndex);
            }

            GUILayout.Label($"{selectedFileIndex + 1}/{availableFiles.Count}", GUILayout.Width(60));

            if (GUILayout.Button("Siguiente ▶") && selectedFileIndex < availableFiles.Count - 1)
            {
                selectedFileIndex++;
                OnTrainingFileSelected(selectedFileIndex);
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("No hay archivos disponibles", GUI.skin.box);
        }

        // Botón refrescar
        if (GUILayout.Button("Refrescar Archivos", GUILayout.Height(25)))
        {
            RefreshAvailableFiles();
        }

        GUILayout.Space(10);

        // Índice de red neuronal
        GUILayout.Label("Índice de Red Neuronal", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Red:", GUILayout.Width(40));
        int maxNetworks = currentTrainingData?.networks != null ? currentTrainingData.networks.Count - 1 : 9;
        currentNetworkIndex = Mathf.RoundToInt(GUILayout.HorizontalSlider(currentNetworkIndex, 0, maxNetworks, GUILayout.Width(150)));
        string maxText = currentTrainingData?.networks != null ? $"/{currentTrainingData.networks.Count}" : "/10";
        GUILayout.Label($"{currentNetworkIndex}{maxText}", GUILayout.Width(60));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Botones principales
        GUILayout.Label("Acciones", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cargar Cerebro", GUILayout.Height(30)))
        {
            LoadSelectedBrain();
        }

        if (GUILayout.Button("Reset Posición", GUILayout.Height(30)))
        {
            ResetNPCPosition();
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cargar Aleatorio", GUILayout.Height(25)))
        {
            LoadRandomBrain();
        }

        if (GUILayout.Button("Cargar Mejor", GUILayout.Height(25)))
        {
            SetNetworkIndex(0);
            LoadSelectedBrain();
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Archivo personalizado
        GUILayout.Label("Archivo Personalizado", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Nombre:", GUILayout.Width(60));
        customFileName = GUILayout.TextField(customFileName);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Cargar Personalizado", GUILayout.Height(25)))
        {
            LoadCustomFile();
        }

        GUILayout.Space(10);

        // Configuración de ventanas
        GUILayout.Label("Ventanas", GUI.skin.box);
        GUILayout.Space(5);

        bool newShowStats = GUILayout.Toggle(statsWindowConfig.enabled, "Mostrar Estadísticas");
        if (newShowStats != statsWindowConfig.enabled)
        {
            ToggleStatsWindow();
        }

        bool newShowAdvanced = GUILayout.Toggle(advancedWindowConfig.enabled, "Mostrar Controles Avanzados");
        if (newShowAdvanced != advancedWindowConfig.enabled)
        {
            ToggleAdvancedWindow();
        }

        GUILayout.Space(10);

        // Teclas rápidas
        GUILayout.Label("Teclas Rápidas", GUI.skin.box);
        GUILayout.Space(2);
        GUILayout.Label("R - Reset Posición", GUI.skin.label);
        GUILayout.Label("L - Cargar Cerebro", GUI.skin.label);
        GUILayout.Label("F - Refrescar Archivos", GUI.skin.label);
        GUILayout.Label("0-9 - Cambiar Índice", GUI.skin.label);
        GUILayout.Label("T - Toggle Estadísticas", GUI.skin.label);
        GUILayout.Label("A - Toggle Avanzado", GUI.skin.label);

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

        if (npcController != null)
        {
            // Mostrar estadísticas del NPC
            string stats = npcController.GetStats();
            GUILayout.Label(stats, GUI.skin.box);
        }
        else
        {
            GUILayout.Label("No hay datos disponibles", GUI.skin.box);
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

    private void DrawAdvancedWindow(int windowID)
    {
        advancedScrollPosition = GUILayout.BeginScrollView(advancedScrollPosition, GUILayout.Width(advancedWindowConfig.windowRect.width - 20), GUILayout.Height(advancedWindowConfig.windowRect.height - 30));
        GUILayout.BeginVertical();

        GUILayout.Label("Controles Avanzados", GUI.skin.box);
        GUILayout.Space(10);

        // Estrategia de selección
        GUILayout.Label("Estrategia de Selección", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.Label($"Actual: {strategyOptions[selectedStrategyIndex]}", GUI.skin.box);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ Anterior") && selectedStrategyIndex > 0)
        {
            selectedStrategyIndex--;
            OnStrategyChanged(selectedStrategyIndex);
        }

        GUILayout.Label($"{selectedStrategyIndex + 1}/{strategyOptions.Length}", GUILayout.Width(60));

        if (GUILayout.Button("Siguiente ▶") && selectedStrategyIndex < strategyOptions.Length - 1)
        {
            selectedStrategyIndex++;
            OnStrategyChanged(selectedStrategyIndex);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Multiplicadores
        GUILayout.Label("Multiplicadores", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        GUILayout.Label("Velocidad:", GUILayout.Width(80));
        float newSpeedMult = GUILayout.HorizontalSlider(speedMultiplier, 0.1f, 3f, GUILayout.Width(150));
        if (Mathf.Abs(newSpeedMult - speedMultiplier) > 0.01f)
        {
            speedMultiplier = newSpeedMult;
            OnSpeedMultiplierChanged(speedMultiplier);
        }
        GUILayout.Label($"{speedMultiplier:F1}x", GUILayout.Width(40));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Rotación:", GUILayout.Width(80));
        float newRotMult = GUILayout.HorizontalSlider(rotationMultiplier, 0.1f, 3f, GUILayout.Width(150));
        if (Mathf.Abs(newRotMult - rotationMultiplier) > 0.01f)
        {
            rotationMultiplier = newRotMult;
            OnRotationMultiplierChanged(rotationMultiplier);
        }
        GUILayout.Label($"{rotationMultiplier:F1}x", GUILayout.Width(40));
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Presets
        GUILayout.Label("Presets", GUI.skin.box);
        GUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Rápido", GUILayout.Height(25)))
        {
            ApplyPreset("fast");
        }

        if (GUILayout.Button("Lento", GUILayout.Height(25)))
        {
            ApplyPreset("slow");
        }

        if (GUILayout.Button("Normal", GUILayout.Height(25)))
        {
            ApplyPreset("normal");
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        // Opciones adicionales
        GUILayout.Label("Opciones", GUI.skin.box);
        GUILayout.Space(5);

        bool newShowSensors = GUILayout.Toggle(showSensors, "Mostrar Sensores");
        if (newShowSensors != showSensors)
        {
            showSensors = newShowSensors;
            OnShowSensorsToggled(showSensors);
        }

        GUILayout.Space(10);
        if (GUILayout.Button("Cerrar"))
        {
            advancedWindowConfig.enabled = false;
        }

        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (advancedWindowConfig.isDraggable)
        {
            GUI.DragWindow(new Rect(0, 0, advancedWindowConfig.windowRect.width, 20));
        }
    }
    #endregion

    #region Métodos de Toggle de Ventanas
    public void ToggleStatsWindow()
    {
        statsWindowConfig.enabled = !statsWindowConfig.enabled;
        showStatsWindow = statsWindowConfig.enabled;
    }

    public void ToggleAdvancedWindow()
    {
        advancedWindowConfig.enabled = !advancedWindowConfig.enabled;
        showAdvancedWindow = advancedWindowConfig.enabled;
    }
    #endregion

    #region Manejo de Archivos
    public void RefreshAvailableFiles()
    {
        availableFiles.Clear();

        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        if (!Directory.Exists(folderPath))
        {
            Debug.LogWarning($"[{gameObject.name}] Carpeta no encontrada: {folderPath}");
            return;
        }

        string[] files = Directory.GetFiles(folderPath, "*.json");
        foreach (string file in files)
        {
            availableFiles.Add(Path.GetFileName(file));
        }

        // Ajustar índice si es necesario
        if (selectedFileIndex >= availableFiles.Count)
        {
            selectedFileIndex = Mathf.Max(0, availableFiles.Count - 1);
        }

        if (availableFiles.Count > 0 && string.IsNullOrEmpty(currentFileName))
        {
            currentFileName = availableFiles[0];
        }

        Debug.Log($"[{gameObject.name}] {availableFiles.Count} archivos encontrados");
    }
    #endregion

    #region Carga de Cerebros
    public void LoadSelectedBrain()
    {
        if (string.IsNullOrEmpty(currentFileName))
        {
            Debug.LogWarning($"[{gameObject.name}] No hay archivo seleccionado");
            return;
        }

        LoadBrainFromFile(currentFileName, currentNetworkIndex);
    }

    public void LoadBrainFromFile(string fileName, int networkIndex)
    {
        string fullPath = GetFilePath(fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[{gameObject.name}] Archivo no encontrado: {fullPath}");
            return;
        }

        try
        {
            string json = File.ReadAllText(fullPath);
            currentTrainingData = JsonUtility.FromJson<TrainingData>(json);

            if (currentTrainingData?.networks == null || currentTrainingData.networks.Count == 0)
            {
                Debug.LogError($"[{gameObject.name}] Archivo sin redes válidas");
                return;
            }

            // Validar índice
            if (networkIndex >= currentTrainingData.networks.Count)
            {
                Debug.LogWarning($"[{gameObject.name}] Índice {networkIndex} fuera de rango, usando 0");
                networkIndex = 0;
            }

            SerializedNetwork selectedNetwork = currentTrainingData.networks[networkIndex];

            if (CreateBrainFromNetwork(selectedNetwork))
            {
                brainLoaded = true;
                currentFileName = fileName;
                currentNetworkIndex = networkIndex;

                loadedBrainInfo = $"Gen:{currentTrainingData.generation} " +
                                $"Red:{networkIndex + 1}/{currentTrainingData.networks.Count} " +
                                $"Fitness:{selectedNetwork.fitness:F1} " +
                                $"Saltos:{selectedNetwork.successfulJumps}";

                Debug.Log($"[{gameObject.name}] Cerebro cargado: {loadedBrainInfo}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error cargando cerebro: {e.Message}");
        }
    }

    bool CreateBrainFromNetwork(SerializedNetwork network)
    {
        if (network.layers == null || network.layers.Length < 2)
        {
            Debug.LogError("Estructura de red inválida");
            return false;
        }

        try
        {
            var newBrain = new NeuralNetwork(network.layers);

            if (network.flattenedWeights != null && network.flattenedWeights.Count > 0)
            {
                var weights = RebuildWeights(network.flattenedWeights, network.layers);
                if (weights != null)
                {
                    newBrain.SetWeights(weights);
                }
            }

            npcController.SetBrain(newBrain);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creando cerebro: {e.Message}");
            return false;
        }
    }

    float[][][] RebuildWeights(List<float> flatWeights, int[] layers)
    {
        var weights = new float[layers.Length - 1][][];
        int index = 0;

        for (int i = 0; i < layers.Length - 1; i++)
        {
            weights[i] = new float[layers[i]][];
            for (int j = 0; j < layers[i]; j++)
            {
                weights[i][j] = new float[layers[i + 1]];
                for (int k = 0; k < layers[i + 1]; k++)
                {
                    if (index < flatWeights.Count)
                        weights[i][j][k] = flatWeights[index++];
                    else
                        weights[i][j][k] = Random.Range(-1f, 1f);
                }
            }
        }

        return weights;
    }
    #endregion

    #region Eventos de UI
    void OnTrainingFileSelected(int index)
    {
        if (index >= 0 && index < availableFiles.Count)
        {
            currentFileName = availableFiles[index];
            Debug.Log($"[{gameObject.name}] Archivo seleccionado: {currentFileName}");
        }
    }

    void OnStrategyChanged(int strategyIndex)
    {
        if (currentTrainingData?.networks == null) return;

        int newIndex = 0;
        switch (strategyIndex)
        {
            case 0: newIndex = 0; break; // Mejor
            case 1: newIndex = Mathf.Min(1, currentTrainingData.networks.Count - 1); break; // Segundo
            case 2: newIndex = Mathf.Min(2, currentTrainingData.networks.Count - 1); break; // Tercero
            case 3: newIndex = Random.Range(0, currentTrainingData.networks.Count); break; // Aleatorio
            case 4: newIndex = currentTrainingData.networks.Count - 1; break; // Peor
        }

        SetNetworkIndex(newIndex);
    }

    void OnSpeedMultiplierChanged(float value)
    {
        var modifier = npcController.GetComponent<NPCGameplayExtension>();
        if (modifier != null)
        {
            modifier.SetSpeedMultiplier(value);
        }
        else
        {
            // Aplicar directamente si no hay modificador
            npcController.moveSpeed = npcController.moveSpeed * value;
        }
    }

    void OnRotationMultiplierChanged(float value)
    {
        var modifier = npcController.GetComponent<NPCGameplayExtension>();
        if (modifier != null)
        {
            modifier.SetRotationMultiplier(value);
        }
        else
        {
            npcController.rotationSpeed = npcController.rotationSpeed * value;
        }
    }

    void OnShowSensorsToggled(bool show)
    {
        // Toggle para mostrar/ocultar visualización de sensores
        Debug.Log($"[{gameObject.name}] Sensores: {(show ? "Mostrar" : "Ocultar")}");
    }
    #endregion

    #region Métodos Públicos de Control
    public void SetNetworkIndex(int index)
    {
        currentNetworkIndex = index;
    }

    public void ResetNPCPosition()
    {
        if (npcController != null)
        {
            npcController.ResetPosition();
            Debug.Log($"[{gameObject.name}] Posición del NPC reseteada");
        }
    }

    public void ApplyPreset(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "fast":
                speedMultiplier = 2f;
                rotationMultiplier = 1.5f;
                OnSpeedMultiplierChanged(speedMultiplier);
                OnRotationMultiplierChanged(rotationMultiplier);
                break;
            case "slow":
                speedMultiplier = 0.5f;
                rotationMultiplier = 0.7f;
                OnSpeedMultiplierChanged(speedMultiplier);
                OnRotationMultiplierChanged(rotationMultiplier);
                break;
            case "normal":
                speedMultiplier = 1f;
                rotationMultiplier = 1f;
                OnSpeedMultiplierChanged(speedMultiplier);
                OnRotationMultiplierChanged(rotationMultiplier);
                break;
        }
    }

    public void LoadCustomFile()
    {
        if (!string.IsNullOrEmpty(customFileName))
        {
            LoadBrainFromFile(customFileName, currentNetworkIndex);
        }
    }

    public void LoadRandomBrain()
    {
        if (currentTrainingData?.networks != null && currentTrainingData.networks.Count > 0)
        {
            int randomIndex = Random.Range(0, currentTrainingData.networks.Count);
            SetNetworkIndex(randomIndex);
            LoadSelectedBrain();
        }
    }
    #endregion

    #region Utilidades
    string GetFilePath(string fileName)
    {
        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        return Path.Combine(folderPath, fileName);
    }

    public bool HasBrainLoaded() => brainLoaded;
    public string GetCurrentFileName() => currentFileName;
    public int GetCurrentNetworkIndex() => currentNetworkIndex;
    public string GetBrainInfo() => loadedBrainInfo;
    #endregion

    #region Métodos de Context Menu para Debug
    [ContextMenu("Refrescar Archivos")]
    void ContextRefreshFiles() => RefreshAvailableFiles();

    [ContextMenu("Cargar Mejor Cerebro")]
    void ContextLoadBest()
    {
        SetNetworkIndex(0);
        LoadSelectedBrain();
    }

    [ContextMenu("Cargar Cerebro Aleatorio")]
    void ContextLoadRandom() => LoadRandomBrain();

    [ContextMenu("Reset Posición")]
    void ContextResetPosition() => ResetNPCPosition();

    [ContextMenu("Mostrar Info")]
    void ContextShowInfo()
    {
        Debug.Log($"Archivo: {currentFileName}");
        Debug.Log($"Índice: {currentNetworkIndex}");
        Debug.Log($"Cerebro cargado: {brainLoaded}");
        Debug.Log($"Info: {loadedBrainInfo}");
    }
    #endregion

    #region Limpieza
    private void OnApplicationQuit()
    {
        if (mainWindowConfig != null) mainWindowConfig.SaveConfig();
        if (statsWindowConfig != null) statsWindowConfig.SaveConfig();
        if (advancedWindowConfig != null) advancedWindowConfig.SaveConfig();
    }
    #endregion
}