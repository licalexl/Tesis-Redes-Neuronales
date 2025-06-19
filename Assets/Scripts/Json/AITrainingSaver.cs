using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// Clase serializable que representa una red neuronal guardada con todas sus metricas
/// </summary>
[System.Serializable]
public class SerializedNetwork
{
    #region Estructura de Red Neuronal
    [Tooltip("Arquitectura de capas de la red neuronal")]
    public int[] layers;

    [Tooltip("Todos los pesos de la red neuronal aplanados en una lista")]
    public List<float> flattenedWeights;

    [Tooltip("Puntuacion de fitness obtenida por esta red")]
    public float fitness;
    #endregion

    #region Datos de Comportamiento
    [Header("Datos de Comportamiento")]
    [Tooltip("Tiempo que sobrevivio el NPC")]
    public float timeAlive = 0f;

    [Tooltip("Distancia total recorrida por el NPC")]
    public float totalDistance = 0f;

    [Tooltip("Numero de saltos exitosos realizados")]
    public int successfulJumps = 0;

    [Tooltip("Numero de checkpoints alcanzados")]
    public int checkpointsReached = 0;

    [Tooltip("Numero de areas unicas exploradas")]
    public int uniqueAreasVisited = 0;
    #endregion

    #region Datos de Red Neuronal
    [Header("Datos de Red Neuronal")]
    [Tooltip("Numero de pesos activos (mayor a 0.01)")]
    public int activeWeightsCount = 0;

    [Tooltip("Porcentaje de pesos activos respecto al total")]
    public float weightComplexity = 0f;

    [Tooltip("Estado de bloqueos para cada salida: movimiento, giro izq, giro der, salto")]
    public bool[] outputLockStatus;
    #endregion

    #region Metricas de Calidad
    [Header("Metricas de Calidad")]
    [Tooltip("Numero de saltos realizados correctamente")]
    public int correctJumps = 0;

    [Tooltip("Numero de saltos realizados incorrectamente")]
    public int incorrectJumps = 0;

    [Tooltip("Eficiencia en la exploracion de areas")]
    public float explorationEfficiency = 0f;
    #endregion
}

/// <summary>
/// Clase serializable que contiene todos los datos de una generacion de entrenamiento
/// </summary>
[System.Serializable]
public class TrainingData
{
    #region Datos Basicos de Generacion
    [Tooltip("Numero de generacion actual")]
    public int generation;

    [Tooltip("Mejor fitness obtenido en esta generacion")]
    public float bestFitness;

    [Tooltip("Fitness promedio de la poblacion")]
    public float averageFitness;

    [Tooltip("Peor fitness obtenido en esta generacion")]
    public float worstFitness;

    [Tooltip("Lista de redes neuronales guardadas")]
    public List<SerializedNetwork> networks;

    [Tooltip("Marca de tiempo de cuando se guardo")]
    public string timestamp;
    #endregion

    #region Estadisticas de Poblacion
    [Header("Estadisticas de Poblacion")]
    [Tooltip("Tamaño total de la poblacion")]
    public int populationSize = 0;

    [Tooltip("Numero de NPCs vivos al final de la generacion")]
    public int aliveNPCs = 0;

    [Tooltip("Duracion total de la generacion en segundos")]
    public float generationDuration = 0f;
    #endregion

    #region Configuracion de Entrenamiento
    [Header("Configuracion de Entrenamiento")]
    [Tooltip("Tasa de mutacion utilizada en esta generacion")]
    public float mutationRate = 0f;

    [Tooltip("Numero de individuos elite preservados")]
    public int eliteCount = 0;

    [Tooltip("Estado global de bloqueos de salidas")]
    public bool[] globalLockStatus;
    #endregion

    #region Metricas Derivadas
    [Header("Metricas Derivadas")]
    [Tooltip("Rango de fitness (diferencia entre mejor y peor)")]
    public float fitnessRange = 0f;

    [Tooltip("Indice de diversidad de la poblacion")]
    public float diversityIndex = 0f;

    [Tooltip("Tiempo promedio de supervivencia")]
    public float averageTimeAlive = 0f;

    [Tooltip("Suma total de distancias recorridas")]
    public float totalDistanceSum = 0f;

    [Tooltip("Total de saltos exitosos en la generacion")]
    public int totalSuccessfulJumps = 0;
    #endregion

    #region Metricas Adicionales para Graficas
    [Header("Metricas de Poblacion")]
    [Tooltip("Diversidad genetica de la poblacion")]
    public float populationDiversity;

    [Tooltip("Tasa de aprendizaje de esta generacion")]
    public float learningRate;

    [Tooltip("Velocidad de convergencia de la poblacion")]
    public float convergenceRate;
    #endregion

    #region Metricas de Comportamiento
    [Header("Metricas de Comportamiento")]
    [Tooltip("Total de checkpoints alcanzados por toda la poblacion")]
    public int totalCheckpointsReached;

    [Tooltip("Distancia promedio recorrida por la poblacion")]
    public float averageDistance;

    [Tooltip("Numero de NPCs que murieron en esta generacion")]
    public int totalDeaths;
    #endregion

    #region Metricas de Progreso
    [Header("Metricas de Progreso")]
    [Tooltip("Tasa de mejora respecto a la generacion anterior")]
    public float improvementRate;

    [Tooltip("Bonus total otorgado por exploracion")]
    public float explorationBonus;

    [Tooltip("Puntuacion de eficiencia (fitness/tiempo)")]
    public float efficiencyScore;

    [Tooltip("Numero de comportamientos unicos observados")]
    public int activeBehaviors;
    #endregion

    #region Metricas del Sistema
    [Header("Metricas del Sistema")]
    [Tooltip("NPCs vivos al final de la generacion")]
    public int aliveAtEnd;

    [Tooltip("Uso estimado de memoria en MB")]
    public float memoryUsage;
    #endregion

    #region Constructor

    /// <summary>
    /// Constructor que inicializa todos los valores por defecto
    /// </summary>
    public TrainingData()
    {
        networks = new List<SerializedNetwork>();
        timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        // Inicializar metricas adicionales con valores por defecto
        populationDiversity = 0f;
        learningRate = 0f;
        fitnessRange = 0f;
        convergenceRate = 0f;
        totalCheckpointsReached = 0;
        totalSuccessfulJumps = 0;
        averageDistance = 0f;
        averageTimeAlive = 0f;
        totalDeaths = 0;
        improvementRate = 0f;
        explorationBonus = 0f;
        efficiencyScore = 0f;
        activeBehaviors = 0;
        generationDuration = 0f;
        aliveAtEnd = 0;
        memoryUsage = 0f;
    }
    #endregion
}

/// <summary>
/// Componente encargado de guardar el progreso del entrenamiento de IA en archivos JSON
/// Maneja el guardado automatico y manual con metricas completas de rendimiento
/// </summary>
public class AITrainingSaver : MonoBehaviour
{
    #region Referencias
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genetico principal")]
    public NPCGeneticAlgorithm geneticAlgorithm;
    #endregion

    #region Configuracion de Guardado
    [Header("Configuracion de Guardado")]
    [Tooltip("Si es true, guarda en una carpeta del proyecto en lugar de persistentDataPath")]
    public bool saveInProjectFolder = true;

    [Tooltip("Carpeta donde se guardaran los archivos (si saveInProjectFolder es false)")]
    public string saveFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para guardar archivos (si saveInProjectFolder es true)")]
    public string projectSaveFolder = "SavedTrainings";

    [Tooltip("Activar guardado automatico cada cierto numero de generaciones")]
    public bool autoSave = true;

    [Tooltip("Guardar automaticamente cada X generaciones")]
    public int autoSaveInterval = 5;
    #endregion

    #region Variables Privadas de Control
    /// <summary>
    /// Mejor fitness de la generacion anterior para calcular mejoras
    /// </summary>
    private float lastGenerationBestFitness = 0f;

    /// <summary>
    /// Tiempo de inicio de la generacion actual
    /// </summary>
    private float generationStartTime = 0f;

    /// <summary>
    /// Ruta del ultimo archivo guardado
    /// </summary>
    public string lastSavePath { get; private set; }
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa el componente y configura las rutas de guardado
    /// </summary>
    void Start()
    {
        // Buscamos el algoritmo genetico si no esta asignado
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
            if (geneticAlgorithm == null)
            {
                Debug.LogError("No se encontro el componente NPCGeneticAlgorithm");
                enabled = false; // Desactivamos el componente
                return;
            }
        }

        // Creamos el directorio de guardado si no existe
        string fullPath;
        if (saveInProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
            Debug.Log("Guardando en carpeta del proyecto: " + fullPath);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
            Debug.Log("Guardando en carpeta persistente: " + fullPath);
        }

        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }
    }
    #endregion

    #region Metodos de Update

    /// <summary>
    /// Verifica si debe realizar guardado automatico basado en el intervalo configurado
    /// </summary>
    void Update()
    {
        // Auto-guardar basado en el numero de generacion
        if (autoSave &&
            geneticAlgorithm.generation % autoSaveInterval == 0 &&
            geneticAlgorithm.generation != 0 &&
            GetComponent<AITrainingLoader>()?.isLoading == false)
        {
            SaveTraining($"Generation_{geneticAlgorithm.generation}");
        }
    }
    #endregion

    #region Metodos de Calculo de Metricas

    /// <summary>
    /// Calcula automaticamente todas las metricas avanzadas para el sistema de graficas
    /// </summary>
    /// <param name="data">Datos de entrenamiento donde guardar las metricas calculadas</param>
    private void CalculateAdvancedMetrics(TrainingData data)
    {
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
        {
            Debug.LogWarning("No hay poblacion para calcular metricas");
            return;
        }

        var population = geneticAlgorithm.population.Where(npc => npc != null).ToList();

        // Metricas de Poblacion
        CalculatePopulationMetrics(data, population);

        // Metricas de Comportamiento
        CalculateBehaviorMetrics(data, population);

        // Metricas de Progreso
        CalculateProgressMetrics(data, population);

        // Metricas del Sistema
        CalculateSystemMetrics(data, population);

        // Actualizar tracking para proxima generacion
        lastGenerationBestFitness = data.bestFitness;
        generationStartTime = Time.time;

        Debug.Log($"Metricas calculadas - Gen {data.generation}: " +
                 $"Diversidad={data.populationDiversity:F2}, " +
                 $"Aprendizaje={data.learningRate:F2}, " +
                 $"Checkpoints={data.totalCheckpointsReached}");
    }

    /// <summary>
    /// Calcula metricas relacionadas con la diversidad y convergencia de la poblacion
    /// </summary>
    /// <param name="data">Datos donde almacenar las metricas</param>
    /// <param name="population">Lista de NPCs de la poblacion</param>
    private void CalculatePopulationMetrics(TrainingData data, List<NPCController> population)
    {
        // Diversidad: Varianza de fitness normalizada
        float fitnessVariance = 0f;
        if (population.Count > 1)
        {
            float avgFitness = population.Average(npc => npc.fitness);
            fitnessVariance = population.Sum(npc => Mathf.Pow(npc.fitness - avgFitness, 2)) / population.Count;
            data.populationDiversity = Mathf.Sqrt(fitnessVariance) / (avgFitness + 0.1f); // Normalizada
        }

        // Tasa de aprendizaje: Mejora respecto a generacion anterior
        data.improvementRate = data.bestFitness - lastGenerationBestFitness;
        data.learningRate = data.improvementRate / Mathf.Max(lastGenerationBestFitness, 1f);

        // Rango de fitness
        data.fitnessRange = data.bestFitness - data.worstFitness;

        // Tasa de convergencia (que tan similares son los fitness)
        data.convergenceRate = 1f - (data.fitnessRange / (data.bestFitness + 0.1f));
    }

    /// <summary>
    /// Calcula metricas relacionadas con el comportamiento de los NPCs
    /// </summary>
    /// <param name="data">Datos donde almacenar las metricas</param>
    /// <param name="population">Lista de NPCs de la poblacion</param>
    private void CalculateBehaviorMetrics(TrainingData data, List<NPCController> population)
    {
        // Comportamientos observados
        data.totalCheckpointsReached = population.Sum(npc =>
            CheckpointSystem.Instance != null ? CheckpointSystem.Instance.GetCheckpointsReached(npc) : 0);

        data.totalSuccessfulJumps = population.Sum(npc => npc.successfulJumps);
        data.averageDistance = population.Average(npc => npc.totalDistance);
        data.averageTimeAlive = population.Average(npc => npc.timeAlive);
        data.totalDeaths = population.Count(npc => npc.isDead);
    }

    /// <summary>
    /// Calcula metricas relacionadas con el progreso del entrenamiento
    /// </summary>
    /// <param name="data">Datos donde almacenar las metricas</param>
    /// <param name="population">Lista de NPCs de la poblacion</param>
    private void CalculateProgressMetrics(TrainingData data, List<NPCController> population)
    {
        // Bonus por exploracion (suma de todos los NPCs)
        data.explorationBonus = population.Sum(npc => npc.fitness * 0.1f); // Estimacion

        // Eficiencia: fitness promedio / tiempo promedio
        data.efficiencyScore = data.averageFitness / Mathf.Max(data.averageTimeAlive, 1f);

        // Comportamientos unicos activos (estimacion basada en diversidad)
        data.activeBehaviors = Mathf.RoundToInt(data.populationDiversity * 10f);
    }

    /// <summary>
    /// Calcula metricas relacionadas con el rendimiento del sistema
    /// </summary>
    /// <param name="data">Datos donde almacenar las metricas</param>
    /// <param name="population">Lista de NPCs de la poblacion</param>
    private void CalculateSystemMetrics(TrainingData data, List<NPCController> population)
    {
        // Duracion de la generacion
        data.generationDuration = Time.time - generationStartTime;

        // NPCs vivos al final
        data.aliveAtEnd = population.Count(npc => !npc.isDead);

        // Uso de memoria (estimacion)
        data.memoryUsage = population.Count * 0.5f; // MB aproximado por NPC
    }
    #endregion

    #region Metodos de Procesamiento de Datos

    /// <summary>
    /// Convierte los pesos de una red neuronal 3D en una lista plana para serializar
    /// </summary>
    /// <param name="weights">Array 3D de pesos de la red neuronal</param>
    /// <returns>Lista plana con todos los pesos</returns>
    private List<float> FlattenWeights(float[][][] weights)
    {
        List<float> flatWeights = new List<float>();

        if (weights == null)
        {
            Debug.LogError("Error: Intento de aplanar pesos nulos");
            return flatWeights;
        }

        // Recorremos la estructura y añadimos cada peso a la lista
        for (int i = 0; i < weights.Length; i++)
        {
            if (weights[i] == null) continue;

            for (int j = 0; j < weights[i].Length; j++)
            {
                if (weights[i][j] == null) continue;

                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    flatWeights.Add(weights[i][j][k]);
                }
            }
        }

        return flatWeights;
    }

    /// <summary>
    /// Obtiene el numero de checkpoints alcanzados por un NPC
    /// </summary>
    /// <param name="npc">NPC del cual obtener los checkpoints</param>
    /// <returns>Numero de checkpoints alcanzados</returns>
    private int GetCheckpointsReached(NPCController npc)
    {
        // Si tienes CheckpointSystem
        if (CheckpointSystem.Instance != null)
        {
            return CheckpointSystem.Instance.GetCheckpointsReached(npc);
        }
        return 0;
    }
    #endregion

    #region Metodos de Guardado

    /// <summary>
    /// Guarda el estado actual del entrenamiento en un archivo JSON con todas las metricas
    /// </summary>
    /// <param name="saveName">Nombre personalizado para el archivo (opcional)</param>
    public void SaveTraining(string saveName = "")
    {
        // Verificamos que haya poblacion para guardar
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
        {
            Debug.LogWarning("No hay poblacion para guardar");
            return;
        }

        // Crear estructura de datos principal
        TrainingData data = CreateBaseTrainingData();

        // Agregar metricas derivadas basicas
        CalculateBasicMetrics(data);

        // Serializar las mejores redes neuronales
        SerializeBestNetworks(data);

        // Calcular metricas avanzadas
        CalculateAdvancedMetrics(data);

        // Guardar archivo
        SaveToFile(data, saveName);
    }

    /// <summary>
    /// Crea la estructura base de datos de entrenamiento con informacion basica
    /// </summary>
    /// <returns>TrainingData con datos basicos inicializados</returns>
    private TrainingData CreateBaseTrainingData()
    {
        return new TrainingData
        {
            generation = geneticAlgorithm.generation,
            bestFitness = geneticAlgorithm.population.Max(npc => npc.fitness),
            averageFitness = geneticAlgorithm.population.Average(npc => npc.fitness),
            worstFitness = geneticAlgorithm.population.Min(npc => npc.fitness),
            timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),

            populationSize = geneticAlgorithm.populationSize,
            aliveNPCs = geneticAlgorithm.population.Count(npc => !npc.isDead),
            mutationRate = geneticAlgorithm.mutationRate,
            eliteCount = geneticAlgorithm.eliteCount,
            globalLockStatus = new bool[] {
                geneticAlgorithm.lockMovement,
                geneticAlgorithm.lockTurnLeft,
                geneticAlgorithm.lockTurnRight,
                geneticAlgorithm.lockJump
            },

            networks = new List<SerializedNetwork>()
        };
    }

    /// <summary>
    /// Calcula metricas derivadas basicas para los datos de entrenamiento
    /// </summary>
    /// <param name="data">Datos de entrenamiento a completar</param>
    private void CalculateBasicMetrics(TrainingData data)
    {
        data.fitnessRange = data.bestFitness - data.worstFitness;
        data.averageTimeAlive = geneticAlgorithm.population.Average(npc => npc.timeAlive);
        data.totalDistanceSum = geneticAlgorithm.population.Sum(npc => npc.totalDistance);
        data.totalSuccessfulJumps = geneticAlgorithm.population.Sum(npc => npc.successfulJumps);

        float sumSquaredDiff = 0;
        foreach (var npc in geneticAlgorithm.population)
        {
            float diff = npc.fitness - data.averageFitness;
            sumSquaredDiff += diff * diff;
        }
        data.diversityIndex = Mathf.Sqrt(sumSquaredDiff / geneticAlgorithm.population.Count);
    }

    /// <summary>
    /// Serializa las mejores redes neuronales de la poblacion para guardar
    /// </summary>
    /// <param name="data">Datos donde agregar las redes serializadas</param>
    private void SerializeBestNetworks(TrainingData data)
    {
        // Solo guardamos las mejores redes para ahorrar espacio
        // (maximo 10 o la cantidad total de la poblacion si es menor)
        int networksToSave = Mathf.Min(10, geneticAlgorithm.population.Count);

        // Ordenamos los NPCs por fitness y tomamos los mejores
        var bestNPCs = geneticAlgorithm.population
           .OrderByDescending(npc => npc.fitness)
           .Take(networksToSave);

        // Serializamos cada red neuronal seleccionada
        foreach (var npc in bestNPCs)
        {
            if (npc.brain == null) continue;

            SerializedNetwork serializedNetwork = CreateSerializedNetwork(npc);
            data.networks.Add(serializedNetwork);
        }
    }

    /// <summary>
    /// Crea una red neuronal serializada a partir de un NPC
    /// </summary>
    /// <param name="npc">NPC del cual extraer la informacion</param>
    /// <returns>Red neuronal serializada con todas sus metricas</returns>
    private SerializedNetwork CreateSerializedNetwork(NPCController npc)
    {
        float[][][] weights = npc.brain.GetWeights();
        List<float> flattenedWeights = FlattenWeights(weights);

        // Calcular pesos activos
        int activeWeights = flattenedWeights.Count(w => Mathf.Abs(w) > 0.01f);

        return new SerializedNetwork
        {
            // Campos existentes
            layers = npc.brain.GetLayers(),
            flattenedWeights = flattenedWeights,
            fitness = npc.fitness,

            // Nuevos campos de comportamiento
            timeAlive = npc.timeAlive,
            totalDistance = npc.totalDistance,
            successfulJumps = npc.successfulJumps,
            uniqueAreasVisited = npc.uniqueAreasVisited,
            correctJumps = npc.correctJumps,
            incorrectJumps = npc.incorrectJumps,

            // Metricas de red
            activeWeightsCount = activeWeights,
            weightComplexity = flattenedWeights.Count > 0 ? (float)activeWeights / flattenedWeights.Count : 0f,

            // Estado de bloqueos
            outputLockStatus = new bool[] {
                geneticAlgorithm.lockMovement,
                geneticAlgorithm.lockTurnLeft,
                geneticAlgorithm.lockTurnRight,
                geneticAlgorithm.lockJump
            },

            // Checkpoints (si el sistema existe)
            checkpointsReached = CheckpointSystem.Instance?.GetCheckpointsReached(npc) ?? 0,

            // Eficiencia de exploracion
            explorationEfficiency = npc.totalDistance > 0 ? npc.uniqueAreasVisited / npc.totalDistance : 0f
        };
    }

    /// <summary>
    /// Guarda los datos de entrenamiento en un archivo JSON
    /// </summary>
    /// <param name="data">Datos a guardar</param>
    /// <param name="saveName">Nombre del archivo (opcional)</param>
    private void SaveToFile(TrainingData data, string saveName)
    {
        if (string.IsNullOrEmpty(saveName))
        {
            saveName = $"Training_{DateTime.Now:yyyyMMdd_HHmmss}";
        }

        string fullPath;
        if (saveInProjectFolder)
        {
            // Guarda en una carpeta en el proyecto
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
        }
        else
        {
            // Usa la ruta persistente normal
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        // Asegurate de que la carpeta exista
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
        }

        // Creamos la ruta completa del archivo
        string fileName = $"{saveName}.json";
        string filePath = Path.Combine(fullPath, fileName);

        // Convertimos los datos a formato JSON con formato legible (pretty print)
        string json = JsonUtility.ToJson(data, true);

        // Guardamos el archivo
        File.WriteAllText(filePath, json);

        // Guardamos la ruta para referencia
        lastSavePath = filePath;
        Debug.Log($"Entrenamiento guardado en: {filePath}");

        // Si guardamos en el proyecto, notificar a Unity que actualice el AssetDatabase
#if UNITY_EDITOR
        if (saveInProjectFolder)
        {
            UnityEditor.AssetDatabase.Refresh();
        }
#endif
    }

    /// <summary>
    /// Metodo publico para guardar con un nombre personalizado desde la UI
    /// </summary>
    /// <param name="customName">Nombre personalizado para el archivo</param>
    public void SaveTrainingWithCustomName(string customName)
    {
        SaveTraining(customName);
    }
    #endregion
}