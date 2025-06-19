using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

/// <summary>
/// Representa un frame individual de demostracion humana con sensores y acciones
/// </summary>
[System.Serializable]
public class DemonstrationFrame
{
    /// <summary>
    /// Los 8 inputs de sensores capturados en este frame
    /// </summary>
    public float[] sensorInputs = new float[8];

    /// <summary>
    /// Los 4 outputs de acciones que hizo el humano
    /// </summary>
    public float[] humanActions = new float[4];

    /// <summary>
    /// Calidad de este frame (que tan bueno fue)
    /// </summary>
    public float frameQuality = 1.0f;

    /// <summary>
    /// Timestamp para analisis temporal
    /// </summary>
    public float timestamp;

    /// <summary>
    /// Constructor por defecto que inicializa arrays vacios
    /// </summary>
    public DemonstrationFrame()
    {
        sensorInputs = new float[8];
        humanActions = new float[4];
        timestamp = Time.time;
    }

    /// <summary>
    /// Constructor con parametros para crear un frame completo
    /// </summary>
    /// <param name="sensors">Array de valores de sensores</param>
    /// <param name="actions">Array de acciones realizadas</param>
    /// <param name="quality">Calidad del frame (por defecto 1.0)</param>
    public DemonstrationFrame(float[] sensors, float[] actions, float quality = 1.0f)
    {
        sensorInputs = new float[8];
        humanActions = new float[4];

        Array.Copy(sensors, sensorInputs, Mathf.Min(sensors.Length, 8));
        Array.Copy(actions, humanActions, Mathf.Min(actions.Length, 4));

        frameQuality = quality;
        timestamp = Time.time;
    }
}

/// <summary>
/// Contiene todos los datos de una sesion completa de demostracion
/// </summary>
[System.Serializable]
public class DemonstrationData
{
    /// <summary>
    /// Lista de todos los frames grabados en esta sesion
    /// </summary>
    public List<DemonstrationFrame> frames = new List<DemonstrationFrame>();

    /// <summary>
    /// Nombre identificador de la sesion
    /// </summary>
    public string sessionName;

    /// <summary>
    /// Fitness total alcanzado en esta demostracion
    /// </summary>
    public float totalFitness;

    /// <summary>
    /// Duracion total de la sesion de grabacion
    /// </summary>
    public float sessionDuration;

    /// <summary>
    /// Timestamp de cuando se creo la sesion
    /// </summary>
    public string timestamp;

    /// <summary>
    /// Constructor que inicializa una nueva sesion de demostracion
    /// </summary>
    /// <param name="name">Nombre para identificar la sesion</param>
    public DemonstrationData(string name)
    {
        sessionName = name;
        frames = new List<DemonstrationFrame>();
        timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}

/// <summary>
/// Graba y gestiona las demostraciones de juego humano para entrenar la IA
/// Captura inputs, sensores y evalua la calidad de las acciones realizadas
/// </summary>
public class HumanDemonstrationRecorder : MonoBehaviour
{
    #region Configuracion de Grabacion
    [Header("Configuracion de Grabacion")]
    [Tooltip("Frames por segundo de grabacion (menor = menos datos, mejor rendimiento)")]
    [Range(1, 30)]
    public int recordingFPS = 10;

    [Tooltip("Velocidad minima de movimiento para grabar (filtra frames inactivos)")]
    public float minMovementThreshold = 0.1f;

    [Tooltip("Cambio minimo de input para grabar (filtra frames duplicados)")]
    public float minInputChangeThreshold = 0.05f;

    [Tooltip("Auto-guardar demostracion cada X frames")]
    public int autoSaveInterval = 100;
    #endregion

    #region Filtros de Calidad
    [Header("Filtros de Calidad")]
    [Tooltip("Bonus de calidad cerca de checkpoints")]
    public float checkpointBonus = 2.0f;

    [Tooltip("Bonus de calidad cuando se salta exitosamente")]
    public float jumpBonus = 1.5f;

    [Tooltip("Reduccion de calidad cuando se choca con obstaculos")]
    public float collisionPenalty = 0.5f;
    #endregion

    #region Gestion de Archivos
    [Header("Gestion de Archivos")]
    [Tooltip("Guardar demostraciones en carpeta del proyecto")]
    public bool saveInProjectFolder = true;

    [Tooltip("Nombre de la carpeta para las demostraciones")]
    public string demonstrationFolder = "Demonstrations";
    #endregion

    #region Configuracion de Limite de Demos
    [Header("Configuracion de Limite de Demos")]
    [Tooltip("Maximo de demostraciones a mantener (las mas viejas/peores se eliminan)")]
    public int maxDemonstrationsToKeep = 3;

    [Tooltip("Auto-eliminar demos viejas cuando se alcanza el limite")]
    public bool autoManageDemoLimit = true;
    #endregion

    #region Variables de Estado de Grabacion
    /// <summary>
    /// Indica si actualmente se esta grabando una demostracion
    /// </summary>
    private bool isRecording = false;

    /// <summary>
    /// Datos de la sesion actual de grabacion
    /// </summary>
    private DemonstrationData currentSession;

    /// <summary>
    /// Tiempo de la ultima grabacion de frame
    /// </summary>
    private float lastRecordTime = 0f;

    /// <summary>
    /// Intervalo entre grabaciones de frames
    /// </summary>
    private float recordInterval;

    /// <summary>
    /// Ultima posicion registrada del NPC
    /// </summary>
    private Vector3 lastPosition;

    /// <summary>
    /// Ultimos inputs de sensores registrados
    /// </summary>
    private float[] lastInputs = new float[8];

    /// <summary>
    /// Ultimas acciones registradas
    /// </summary>
    private float[] lastActions = new float[4];
    #endregion

    #region Referencias
    /// <summary>
    /// Controlador del modo humano
    /// </summary>
    private HumanModeController humanController;

    /// <summary>
    /// NPC actualmente controlado por el humano
    /// </summary>
    private NPCController currentNPC;
    #endregion

    #region Estadisticas
    /// <summary>
    /// Total de frames grabados en todas las sesiones
    /// </summary>
    public int totalFramesRecorded = 0;

    /// <summary>
    /// Frames grabados en la sesion actual
    /// </summary>
    public int currentSessionFrames = 0;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa el grabador de demostraciones y configura los parametros basicos
    /// </summary>
    void Start()
    {
        humanController = GetComponent<HumanModeController>();
        recordInterval = 1.0f / recordingFPS;

        // Crear carpeta de demostraciones
        CreateDemonstrationFolder();

        Debug.Log($"Grabador de Demostraciones inicializado. Grabando a {recordingFPS} FPS");
    }
    #endregion

    #region Metodos de Update

    /// <summary>
    /// Controla el estado de grabacion y registra frames segun el intervalo configurado
    /// </summary>
    void Update()
    {
        // Verificar si el modo humano esta activo y tenemos un NPC controlado
        if (humanController != null && humanController.isActive)
        {
            if (!isRecording)
            {
                StartRecording();
            }

            // Grabar frames en el intervalo especificado
            if (Time.time - lastRecordTime >= recordInterval)
            {
                RecordCurrentFrame();
                lastRecordTime = Time.time;
            }
        }
        else if (isRecording)
        {
            StopRecording();
        }
    }
    #endregion

    #region Metodos de Gestion de Limite de Demostraciones

    /// <summary>
    /// Gestiona el limite de demostraciones eliminando las peores cuando se supera el maximo
    /// </summary>
    /// <param name="newDemo">Nueva demostracion a evaluar</param>
    private void ManageDemonstrationLimit(DemonstrationData newDemo)
    {
        if (!autoManageDemoLimit) return;

        string demosPath = saveInProjectFolder ?
            Path.Combine(Application.dataPath, demonstrationFolder) :
            Path.Combine(Application.persistentDataPath, demonstrationFolder);

        if (!Directory.Exists(demosPath)) return;

        // Obtener todos los archivos de demo existentes
        string[] existingFiles = Directory.GetFiles(demosPath, "*.json");

        if (existingFiles.Length < maxDemonstrationsToKeep) return; // No es necesario limpiar aun

        // Cargar y evaluar todas las demos existentes
        List<(string filePath, float fitness)> demoFitnessList = new List<(string, float)>();

        foreach (string file in existingFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                DemonstrationData demo = JsonUtility.FromJson<DemonstrationData>(json);

                if (demo != null)
                {
                    demoFitnessList.Add((file, demo.totalFitness));
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Fallo al leer archivo de demo {file}: {e.Message}");
            }
        }

        // Agregar la nueva demo a la comparacion
        demoFitnessList.Add(("NEW_DEMO", newDemo.totalFitness));

        // Ordenar por fitness (mejores primero)
        demoFitnessList = demoFitnessList.OrderByDescending(x => x.fitness).ToList();

        // Mantener solo las mejores y eliminar el resto
        var demosToKeep = demoFitnessList.Take(maxDemonstrationsToKeep).ToList();
        var demosToDelete = demoFitnessList.Skip(maxDemonstrationsToKeep).ToList();

        // Verificar si la nueva demo llego a la lista de las mejores
        bool newDemoAccepted = demosToKeep.Any(x => x.filePath == "NEW_DEMO");

        if (!newDemoAccepted)
        {
            Debug.Log($"Nueva demo rechazada (fitness: {newDemo.totalFitness:F1}) - No es mejor que las {maxDemonstrationsToKeep} mejores existentes");
            return; // No guardar la nueva demo
        }

        // Eliminar archivos de demos viejas/peores
        foreach (var (filePath, fitness) in demosToDelete)
        {
            if (filePath != "NEW_DEMO" && File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                    Debug.Log($"Demo antigua eliminada: {Path.GetFileName(filePath)} (fitness: {fitness:F1})");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Fallo al eliminar archivo de demo {filePath}: {e.Message}");
                }
            }
        }

        Debug.Log($"Limite de demos gestionado - Manteniendo las {maxDemonstrationsToKeep} mejores. Nueva demo aceptada (fitness: {newDemo.totalFitness:F1})");
    }
    #endregion

    #region Metodos de Control de Grabacion

    /// <summary>
    /// Inicia la grabacion de una nueva sesion de demostracion
    /// </summary>
    void StartRecording()
    {
        currentNPC = GetCurrentControlledNPC();
        if (currentNPC == null) return;

        isRecording = true;
        currentSession = new DemonstrationData($"Demo_{DateTime.Now:yyyyMMdd_HHmmss}");
        lastPosition = currentNPC.transform.position;
        currentSessionFrames = 0;

        Debug.Log($"Inicio de grabacion de demostracion: {currentSession.sessionName}");
    }

    /// <summary>
    /// Detiene la grabacion actual y guarda la sesion
    /// </summary>
    void StopRecording()
    {
        if (!isRecording || currentSession == null) return;

        isRecording = false;

        // Calcular estadisticas de la sesion
        if (currentNPC != null)
        {
            currentSession.totalFitness = currentNPC.fitness;
        }
        currentSession.sessionDuration = Time.time - (currentSession.frames.Count > 0 ? currentSession.frames[0].timestamp : Time.time);

        // Auto-guardar la sesion
        SaveDemonstration(currentSession);

        Debug.Log($"Grabacion detenida. Frames grabados: {currentSessionFrames}, Fitness total: {currentSession.totalFitness:F1}");

        currentSession = null;
        currentNPC = null;
    }

    /// <summary>
    /// Graba el frame actual si cumple con los filtros de calidad
    /// </summary>
    void RecordCurrentFrame()
    {
        if (currentNPC == null || currentSession == null) return;

        // Obtener inputs de sensores actuales del NPC
        float[] sensorInputs = GetCurrentSensorInputs();

        // Convertir input humano a salidas de red neuronal
        float[] humanActions = ConvertHumanInputToActions();

        // Calcular calidad del frame
        float quality = CalculateFrameQuality(sensorInputs, humanActions);

        // Aplicar filtros para decidir si debemos grabar este frame
        if (ShouldRecordFrame(sensorInputs, humanActions, quality))
        {
            DemonstrationFrame frame = new DemonstrationFrame(sensorInputs, humanActions, quality);
            currentSession.frames.Add(frame);

            currentSessionFrames++;
            totalFramesRecorded++;

            // Almacenar para comparacion del siguiente frame
            Array.Copy(sensorInputs, lastInputs, 8);
            Array.Copy(humanActions, lastActions, 4);
            lastPosition = currentNPC.transform.position;

            // Verificar auto-guardado
            if (currentSessionFrames % autoSaveInterval == 0)
            {
                Debug.Log($"Checkpoint de auto-guardado: {currentSessionFrames} frames grabados");
            }
        }
    }
    #endregion

    #region Metodos de Captura de Datos

    /// <summary>
    /// Obtiene los inputs de sensores actuales del NPC controlado
    /// </summary>
    /// <returns>Array con los 8 valores de sensores</returns>
    float[] GetCurrentSensorInputs()
    {
        if (currentNPC == null || currentNPC.inputs == null)
            return new float[8];

        float[] inputs = new float[8];
        Array.Copy(currentNPC.inputs, inputs, Mathf.Min(currentNPC.inputs.Length, 8));
        return inputs;
    }

    /// <summary>
    /// Convierte el input humano (WASD) a formato de salidas de red neuronal
    /// </summary>
    /// <returns>Array con las 4 acciones convertidas</returns>
    float[] ConvertHumanInputToActions()
    {
        float[] actions = new float[4];

        // Convertir input WASD a salidas de red neuronal
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        bool jump = Input.GetKey(KeyCode.Space);

        // Convertir a formato de salida de red neuronal
        actions[0] = Mathf.Clamp01(vertical);           // Movimiento adelante (0-1)
        actions[1] = Mathf.Clamp01(-horizontal);        // Girar izquierda (0-1) 
        actions[2] = Mathf.Clamp01(horizontal);         // Girar derecha (0-1)
        actions[3] = jump ? 1.0f : 0.0f;               // Saltar (0 o 1)

        return actions;
    }
    #endregion

    #region Metodos de Evaluacion de Calidad

    /// <summary>
    /// Calcula la calidad de un frame basado en sensores y acciones
    /// </summary>
    /// <param name="sensors">Valores de sensores</param>
    /// <param name="actions">Acciones realizadas</param>
    /// <returns>Valor de calidad del frame</returns>
    float CalculateFrameQuality(float[] sensors, float[] actions)
    {
        float quality = 1.0f;

        // Aumentar calidad si esta cerca de obstaculos y toma decisiones inteligentes
        bool nearObstacle = false;
        for (int i = 0; i < 5; i++) // Verificar primeros 5 sensores
        {
            if (sensors[i] > 0.3f) nearObstacle = true;
        }

        if (nearObstacle)
        {
            // Recompensar navegacion inteligente cerca de obstaculos
            quality += 0.5f;

            // Bonus por saltar sobre obstaculos bajos
            if (actions[3] > 0.5f && sensors[5] > 0.3f && sensors[6] < 0.3f)
            {
                quality += jumpBonus;
            }
        }

        // Aumentar calidad cerca de checkpoints
        if (CheckpointSystem.Instance != null)
        {
            // Verificacion simple de proximidad - en implementacion real verificarias distancia al checkpoint mas cercano
            quality += checkpointBonus * 0.1f; // Pequeno bonus constante por ahora
        }

        // Penalizar si se mueve hacia obstaculos
        if (sensors[2] > 0.7f && actions[0] > 0.5f) // Sensor frontal alto, aun moviendose adelante
        {
            quality *= collisionPenalty;
        }

        return Mathf.Clamp(quality, 0.1f, 5.0f);
    }

    /// <summary>
    /// Determina si un frame debe ser grabado basado en filtros de calidad
    /// </summary>
    /// <param name="sensors">Valores de sensores</param>
    /// <param name="actions">Acciones realizadas</param>
    /// <param name="quality">Calidad calculada del frame</param>
    /// <returns>True si el frame debe grabarse</returns>
    bool ShouldRecordFrame(float[] sensors, float[] actions, float quality)
    {
        // Filtro 1: Umbral minimo de calidad
        if (quality < 0.3f) return false;

        // Filtro 2: Movimiento minimo
        float distanceMoved = Vector3.Distance(currentNPC.transform.position, lastPosition);
        if (distanceMoved < minMovementThreshold && actions[3] < 0.5f) // Permitir saltos aunque no se mueva
            return false;

        // Filtro 3: Umbral de cambio de input (evitar frames duplicados)
        float inputChange = 0f;
        for (int i = 0; i < 8; i++)
        {
            inputChange += Mathf.Abs(sensors[i] - lastInputs[i]);
        }
        for (int i = 0; i < 4; i++)
        {
            inputChange += Mathf.Abs(actions[i] - lastActions[i]);
        }

        if (inputChange < minInputChangeThreshold) return false;

        return true;
    }
    #endregion

    #region Metodos de Utilidad

    /// <summary>
    /// Obtiene el NPC actualmente controlado por el humano
    /// </summary>
    /// <returns>NPCController del NPC controlado o null si no hay ninguno</returns>
    NPCController GetCurrentControlledNPC()
    {
        // Acceder al NPC controlado a traves de reflection o hacerlo publico en HumanModeController
        // Por ahora, lo encontraremos buscando el NPC deshabilitado (el que esta siendo controlado)
        if (humanController.geneticAlgorithm?.population == null) return null;

        foreach (var npc in humanController.geneticAlgorithm.population)
        {
            if (npc != null && !npc.enabled && !npc.isDead)
            {
                return npc;
            }
        }

        return null;
    }

    /// <summary>
    /// Crea la carpeta de demostraciones si no existe
    /// </summary>
    void CreateDemonstrationFolder()
    {
        string fullPath;
        if (saveInProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, demonstrationFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, demonstrationFolder);
        }

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            Debug.Log($"Carpeta de demostraciones creada: {fullPath}");
        }
    }
    #endregion

    #region Metodos de Guardado

    /// <summary>
    /// Guarda una demostracion en disco como archivo JSON
    /// </summary>
    /// <param name="demo">Datos de la demostracion a guardar</param>
    public void SaveDemonstration(DemonstrationData demo)
    {
        if (demo == null || demo.frames.Count == 0)
        {
            Debug.LogWarning("No se puede guardar demostracion vacia");
            return;
        }

        // NUEVO: Verificar limite ANTES de guardar
        if (autoManageDemoLimit)
        {
            ManageDemonstrationLimit(demo);

            // Re-verificar si el demo fue rechazado
            string tempPath = saveInProjectFolder ?
                Path.Combine(Application.dataPath, demonstrationFolder) :
                Path.Combine(Application.persistentDataPath, demonstrationFolder);

            string[] existingFiles = Directory.GetFiles(tempPath, "*.json");

            if (existingFiles.Length >= maxDemonstrationsToKeep)
            {
                // Verificar si este demo es mejor que el peor existente
                float worstFitness = float.MaxValue;
                foreach (string file in existingFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(file);
                        DemonstrationData existingDemo = JsonUtility.FromJson<DemonstrationData>(json);
                        if (existingDemo != null && existingDemo.totalFitness < worstFitness)
                        {
                            worstFitness = existingDemo.totalFitness;
                        }
                    }
                    catch { }
                }

                if (demo.totalFitness <= worstFitness)
                {
                    Debug.Log($"Demo no guardado - fitness {demo.totalFitness:F1} no mejor que el peor existente {worstFitness:F1}");
                    return; // No guardar
                }
            }
        }

        // Codigo original de guardado continua aqui...
        string fullPath;
        if (saveInProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, demonstrationFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, demonstrationFolder);
        }

        string fileName = $"{demo.sessionName}.json";
        string filePath = Path.Combine(fullPath, fileName);

        try
        {
            string json = JsonUtility.ToJson(demo, true);
            File.WriteAllText(filePath, json);

            Debug.Log($"Demo guardado: {filePath} ({demo.frames.Count} frames, fitness: {demo.totalFitness:F1})");

#if UNITY_EDITOR
            if (saveInProjectFolder)
            {
                UnityEditor.AssetDatabase.Refresh();
            }
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"Fallo al guardar demostracion: {e.Message}");
        }
    }

    /// <summary>
    /// Calcula la calidad promedio de una demostracion
    /// </summary>
    /// <param name="demo">Demostracion a evaluar</param>
    /// <returns>Calidad promedio de todos los frames</returns>
    float CalculateAverageQuality(DemonstrationData demo)
    {
        if (demo.frames.Count == 0) return 0f;

        float totalQuality = 0f;
        foreach (var frame in demo.frames)
        {
            totalQuality += frame.frameQuality;
        }

        return totalQuality / demo.frames.Count;
    }
    #endregion

    #region Metodos de UI

    /// <summary>
    /// Muestra informacion de grabacion en la interfaz de usuario
    /// </summary>
    /// 
    [Header("Configuración de Ventanas GUI")]
    public WindowConfig recordInfoWindow = new WindowConfig("Grabación", new Rect(10, 130, 300, 70));

    void Awake()
    {
        recordInfoWindow.LoadConfig();
    }
    void OnGUI()
    {
        if (isRecording && recordInfoWindow.enabled)
        {
            recordInfoWindow.windowRect = GUI.Window(2, recordInfoWindow.windowRect, DrawRecordInfoWindow, recordInfoWindow.windowName);
        }
    }

    void DrawRecordInfoWindow(int windowID)
    {
        GUI.color = Color.red;
        GUI.Label(new Rect(10, 25, 280, 20), $"GRABANDO DEMO - Frames: {currentSessionFrames}");
        GUI.Label(new Rect(10, 45, 280, 20), $"Total Grabado: {totalFramesRecorded}");
        GUI.color = Color.white;

        // Resizable y draggable
        if (recordInfoWindow.isResizable)
        {
            // Área de redimensionamiento en la esquina inferior derecha
            GUI.Box(new Rect(recordInfoWindow.windowRect.width - 15, recordInfoWindow.windowRect.height - 15, 10, 10), "");

            // Lógica de redimensionamiento
            Rect resizeRect = new Rect(recordInfoWindow.windowRect.width - 15, recordInfoWindow.windowRect.height - 15, 15, 15);
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.Box(resizeRect, "");
            GUI.color = Color.white;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeRect.Contains(currentEvent.mousePosition))
            {
                recordInfoWindow.isResizing = true;
            }

            if (recordInfoWindow.isResizing && currentEvent.type == EventType.MouseDrag)
            {
                recordInfoWindow.windowRect.width = Mathf.Clamp(currentEvent.mousePosition.x, recordInfoWindow.minSize.x, recordInfoWindow.maxSize.x);
                recordInfoWindow.windowRect.height = Mathf.Clamp(currentEvent.mousePosition.y, recordInfoWindow.minSize.y, recordInfoWindow.maxSize.y);
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                recordInfoWindow.isResizing = false;
            }
        }

        // Área para arrastrar
        if (recordInfoWindow.isDraggable)
        {
            GUI.DragWindow();
        }
    }
    void OnDestroy()
    {
        // Guardar configuración de ventanas
        recordInfoWindow.SaveConfig();
    }
    #endregion
}