using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using static FitnessTracker;


#region Clases de Soporte
[System.Serializable]

/// <summary>
/// Sistema de seguimiento y analisis de fitness para detectar estancamiento
/// </summary>
public class FitnessTracker
{
    #region Variables Publicas
    /// <summary>
    /// Cola de valores de fitness recientes
    /// </summary>
    public Queue<float> recentFitnessValues;

    /// <summary>
    /// Mejor fitness registrado hasta el momento
    /// </summary>
    public float lastBestFitness;

    /// <summary>
    /// Generaciones consecutivas sin mejora significativa
    /// </summary>
    public int generationsWithoutImprovement;

    /// <summary>
    /// Tasa de mejora actual del fitness
    /// </summary>
    public float improvementRate;

    /// <summary>
    /// Puntuacion de estancamiento (0-1, donde 1 es estancamiento completo)
    /// </summary>
    public float stagnationScore;
    #endregion

    #region Variables Privadas
    /// <summary>
    /// Tamaño maximo del historial de fitness
    /// </summary>
    private int maxHistorySize;
    #endregion

    #region Enumeraciones
    /// <summary>
    /// Niveles de complejidad para el curriculum de aprendizaje
    /// </summary>
    [System.Serializable]
    public enum BehaviorComplexity
    {
        Basic = 0,      // Movimiento basico hacia adelante
        Turning = 1,    // Giros simples
        Navigation = 2, // Navegacion con obstaculos
        Jumping = 3,    // Saltos basicos
        Advanced = 4    // Saltos inteligentes + navegacion compleja
    }
    #endregion

    #region Constructor
    /// <summary>
    /// Inicializa el tracker de fitness con el tamaño de historial especificado
    /// </summary>
    /// <param name="historySize">Tamaño del historial de fitness a mantener</param>
    public FitnessTracker(int historySize = 10)
    {
        maxHistorySize = historySize;
        recentFitnessValues = new Queue<float>();
        lastBestFitness = 0f;
        generationsWithoutImprovement = 0;
        improvementRate = 0f;
        stagnationScore = 0f;
    }
    #endregion

    #region Metodos Publicos
    /// <summary>
    /// Actualiza el tracker con un nuevo valor de fitness
    /// </summary>
    /// <param name="currentBestFitness">Mejor fitness de la generacion actual</param>
    public void UpdateFitness(float currentBestFitness)
    {
        // Añadir nuevo valor
        recentFitnessValues.Enqueue(currentBestFitness);

        // Mantener tamaño maximo
        while (recentFitnessValues.Count > maxHistorySize)
        {
            recentFitnessValues.Dequeue();
        }

        // Verificar mejora
        bool hasImproved = currentBestFitness > lastBestFitness + 0.5f; // Threshold minimo

        if (hasImproved)
        {
            generationsWithoutImprovement = 0;
            improvementRate = (currentBestFitness - lastBestFitness) / Mathf.Max(lastBestFitness, 1f);
        }
        else
        {
            generationsWithoutImprovement++;
            improvementRate = 0f;
        }

        lastBestFitness = Mathf.Max(lastBestFitness, currentBestFitness);

        // Calcular score de estancamiento
        CalculateStagnationScore();
    }

    /// <summary>
    /// Verifica si el sistema esta en estancamiento segun el threshold especificado
    /// </summary>
    /// <param name="threshold">Umbral de estancamiento (0-1)</param>
    /// <returns>True si esta estancado</returns>
    public bool IsStagnating(float threshold = 0.6f)
    {
        return stagnationScore > threshold;
    }

    /// <summary>
    /// Verifica si hay mejora significativa en el fitness
    /// </summary>
    /// <param name="threshold">Umbral minimo de mejora</param>
    /// <returns>True si hay mejora significativa</returns>
    public bool HasSignificantImprovement(float threshold = 0.05f)
    {
        return improvementRate > threshold;
    }
    #endregion

    #region Metodos Privados
    /// <summary>
    /// Calcula la puntuacion de estancamiento basada en multiples factores
    /// </summary>
    void CalculateStagnationScore()
    {
        if (recentFitnessValues.Count < 3)
        {
            stagnationScore = 0f;
            return;
        }

        // Factor 1: Generaciones sin mejora
        float stagnationFactor = Mathf.Min(generationsWithoutImprovement / 10f, 1f);

        // Factor 2: Variabilidad en fitness reciente
        float[] recentArray = recentFitnessValues.ToArray();
        float mean = recentArray.Average();
        float variance = recentArray.Sum(x => (x - mean) * (x - mean)) / recentArray.Length;
        float variabilityFactor = 1f - Mathf.Min(variance / (mean * mean), 0.5f);

        // Factor 3: Tendencia descendente
        float trendFactor = 0f;
        if (recentArray.Length >= 5)
        {
            float recentAvg = recentArray.Skip(recentArray.Length - 3).Average();
            float olderAvg = recentArray.Take(3).Average();
            trendFactor = Mathf.Max(0f, (olderAvg - recentAvg) / Mathf.Max(olderAvg, 1f));
        }

        // Combinar factores
        stagnationScore = (stagnationFactor * 0.5f + variabilityFactor * 0.3f + trendFactor * 0.2f);
        stagnationScore = Mathf.Clamp01(stagnationScore);
    }
    #endregion
}

/// <summary>
/// Representa una etapa del curriculum de aprendizaje
/// </summary>
[System.Serializable]
public class CurriculumStage
{
    #region Variables Publicas
    /// <summary>
    /// Nivel de complejidad de la etapa
    /// </summary>
    public BehaviorComplexity complexity;

    /// <summary>
    /// Nombre descriptivo de la etapa
    /// </summary>
    public string stageName;

    /// <summary>
    /// Pesos para cada comportamiento [forward, turnLeft, turnRight, jump]
    /// </summary>
    public float[] behaviorWeights;

    /// <summary>
    /// Umbral de calidad minima para demos de esta etapa
    /// </summary>
    public float qualityThreshold;

    /// <summary>
    /// Minimo de generaciones en esta etapa
    /// </summary>
    public int minGenerationsAtStage;

    /// <summary>
    /// Maximo de generaciones en esta etapa
    /// </summary>
    public int maxGenerationsAtStage;

    /// <summary>
    /// Umbral de fitness para avanzar a la siguiente etapa
    /// </summary>
    public float masteryThreshold;
    #endregion

    #region Constructor
    /// <summary>
    /// Inicializa una nueva etapa del curriculum
    /// </summary>
    /// <param name="comp">Complejidad del comportamiento</param>
    /// <param name="name">Nombre de la etapa</param>
    /// <param name="weights">Pesos de comportamiento</param>
    /// <param name="quality">Umbral de calidad</param>
    /// <param name="minGens">Generaciones minimas</param>
    /// <param name="maxGens">Generaciones maximas</param>
    /// <param name="mastery">Umbral de maestria</param>
    public CurriculumStage(BehaviorComplexity comp, string name, float[] weights,
                          float quality, int minGens, int maxGens, float mastery)
    {
        complexity = comp;
        stageName = name;
        behaviorWeights = weights ?? new float[] { 1f, 1f, 1f, 1f };
        qualityThreshold = quality;
        minGenerationsAtStage = minGens;
        maxGenerationsAtStage = maxGens;
        masteryThreshold = mastery;
    }
    #endregion
}

/// <summary>
/// Administrador del curriculum de aprendizaje progresivo
/// </summary>
[System.Serializable]
public class CurriculumManager
{
    #region Variables Publicas
    /// <summary>
    /// Lista de etapas del curriculum
    /// </summary>
    public List<CurriculumStage> stages;

    /// <summary>
    /// Indice de la etapa actual
    /// </summary>
    public int currentStageIndex;

    /// <summary>
    /// Generaciones transcurridas en la etapa actual
    /// </summary>
    public int generationsInCurrentStage;

    /// <summary>
    /// Indica si se ha completado todo el curriculum
    /// </summary>
    public bool hasCompletedCurriculum;

    /// <summary>
    /// Nivel de maestria en la etapa actual
    /// </summary>
    public float currentStageMastery;
    #endregion

    #region Constructor
    /// <summary>
    /// Inicializa el administrador de curriculum con etapas predeterminadas
    /// </summary>
    public CurriculumManager()
    {
        InitializeDefaultCurriculum();
        currentStageIndex = 0;
        generationsInCurrentStage = 0;
        hasCompletedCurriculum = false;
        currentStageMastery = 0f;
    }
    #endregion

    #region Metodos Publicos
    /// <summary>
    /// Obtiene la etapa actual del curriculum
    /// </summary>
    /// <returns>Etapa actual o null si se ha completado</returns>
    public CurriculumStage GetCurrentStage()
    {
        if (currentStageIndex >= stages.Count) return null;
        return stages[currentStageIndex];
    }

    /// <summary>
    /// Verifica si se debe avanzar a la siguiente etapa
    /// </summary>
    /// <param name="currentPopulationFitness">Fitness actual de la poblacion</param>
    /// <returns>True si se debe avanzar</returns>
    public bool ShouldAdvanceStage(float currentPopulationFitness)
    {
        var currentStage = GetCurrentStage();
        if (currentStage == null) return false;

        bool hasMinGenerations = generationsInCurrentStage >= currentStage.minGenerationsAtStage;
        bool hasMastery = currentPopulationFitness >= currentStage.masteryThreshold;
        bool hasMaxGenerations = generationsInCurrentStage >= currentStage.maxGenerationsAtStage;

        return (hasMinGenerations && hasMastery) || hasMaxGenerations;
    }

    /// <summary>
    /// Avanza a la siguiente etapa del curriculum
    /// </summary>
    /// <returns>True si avanzo exitosamente, False si ya se completo</returns>
    public bool AdvanceToNextStage()
    {
        currentStageIndex++;
        generationsInCurrentStage = 0;

        if (currentStageIndex >= stages.Count)
        {
            hasCompletedCurriculum = true;
            currentStageIndex = stages.Count - 1; // Stay at final stage
            return false; // No more stages
        }

        return true; // Successfully advanced
    }

    /// <summary>
    /// Incrementa el contador de generaciones en la etapa actual
    /// </summary>
    public void IncrementGeneration()
    {
        generationsInCurrentStage++;
    }

    /// <summary>
    /// Calcula el progreso en la etapa actual (0-1)
    /// </summary>
    /// <returns>Progreso como porcentaje</returns>
    public float GetStageProgress()
    {
        var currentStage = GetCurrentStage();
        if (currentStage == null) return 1f;

        return (float)generationsInCurrentStage / currentStage.maxGenerationsAtStage;
    }
    #endregion

    #region Metodos Privados
    /// <summary>
    /// Inicializa el curriculum por defecto con 5 etapas progresivas
    /// </summary>
    void InitializeDefaultCurriculum()
    {
        stages = new List<CurriculumStage>();

        // Stage 1: Movimiento basico
        stages.Add(new CurriculumStage(
            BehaviorComplexity.Basic,
            "Basic Movement",
            new float[] { 1.0f, 0.3f, 0.3f, 0.1f }, // Priorizar movimiento hacia adelante
            0.8f,   // Solo demos de alta calidad
            5,      // Minimo 5 generaciones
            15,     // Maximo 15 generaciones
            25f     // Fitness threshold para avanzar
        ));

        // Stage 2: Giros y navegacion basica
        stages.Add(new CurriculumStage(
            BehaviorComplexity.Turning,
            "Turning & Basic Navigation",
            new float[] { 0.8f, 1.0f, 1.0f, 0.2f }, // Incluir giros
            0.7f,
            4,
            12,
            40f
        ));

        // Stage 3: Navegacion con obstaculos
        stages.Add(new CurriculumStage(
            BehaviorComplexity.Navigation,
            "Obstacle Navigation",
            new float[] { 0.9f, 0.9f, 0.9f, 0.6f }, // Incluir algo de salto
            0.6f,
            3,
            10,
            60f
        ));

        // Stage 4: Saltos basicos
        stages.Add(new CurriculumStage(
            BehaviorComplexity.Jumping,
            "Basic Jumping",
            new float[] { 0.8f, 0.7f, 0.7f, 1.0f }, // Priorizar saltos
            0.9f,   // Solo saltos de muy alta calidad
            4,
            8,
            80f
        ));

        // Stage 5: Comportamiento avanzado completo
        stages.Add(new CurriculumStage(
            BehaviorComplexity.Advanced,
            "Advanced Behavior",
            new float[] { 1.0f, 1.0f, 1.0f, 1.0f }, // Todo igual
            0.5f,   // Cualquier demo valida
            2,
            5,
            100f
        ));
    }
    #endregion
}

/// <summary>
/// Configuracion para el sistema de timing adaptativo
/// </summary>
[System.Serializable]
public class AdaptiveTimingSettings
{
    #region Deteccion de Estancamiento
    [Header("Deteccion de Estancamiento")]
    [Tooltip("Puntuacion minima de estancamiento para activar aprendizaje por imitacion")]
    [Range(0.3f, 0.9f)]
    public float stagnationThreshold = 0.6f;

    [Tooltip("Generaciones minimas entre aplicaciones (cooldown de seguridad)")]
    [Range(2, 10)]
    public int minimumCooldown = 3;

    [Tooltip("Generaciones maximas entre aplicaciones forzadas")]
    [Range(10, 50)]
    public int maximumInterval = 20;
    #endregion

    #region Factores de Urgencia
    [Header("Factores de Urgencia")]
    [Tooltip("Aplicar inmediatamente si el fitness promedio baja")]
    public bool applyOnFitnessDecline = true;

    [Tooltip("Aplicar si la diversidad poblacional es muy baja")]
    public bool applyOnLowDiversity = true;

    [Tooltip("Umbral de diversidad (0.1 = muy baja diversidad)")]
    [Range(0.05f, 0.3f)]
    public float diversityThreshold = 0.15f;
    #endregion

    #region Control de Calidad de Demos
    [Header("Control de Calidad de Demos")]
    [Tooltip("Solo aplicar si la calidad del demo supera a la poblacion actual")]
    public bool requireDemoSuperiorQuality = true;

    [Tooltip("El fitness del demo debe ser X veces mejor que el promedio actual")]
    [Range(1.1f, 2.0f)]
    public float demoQualityMultiplier = 1.3f;
    #endregion
}

/// <summary>
/// Metricas para validar la efectividad del aprendizaje por imitacion
/// </summary>
[System.Serializable]
public class ValidationMetrics
{
    #region Variables Publicas
    /// <summary>
    /// Fitness promedio de la poblacion
    /// </summary>
    public float averageFitness;

    /// <summary>
    /// Mejor fitness de la poblacion
    /// </summary>
    public float bestFitness;

    /// <summary>
    /// Peor fitness de la poblacion
    /// </summary>
    public float worstFitness;

    /// <summary>
    /// Eficiencia promedio de saltos
    /// </summary>
    public float jumpEfficiency;

    /// <summary>
    /// Tasa promedio de exploracion
    /// </summary>
    public float explorationRate;

    /// <summary>
    /// Generacion cuando se capturaron estas metricas
    /// </summary>
    public int generation;

    /// <summary>
    /// Timestamp de cuando se capturaron las metricas
    /// </summary>
    public float timestamp;
    #endregion

    #region Constructor
    /// <summary>
    /// Captura metricas de una poblacion en una generacion especifica
    /// </summary>
    /// <param name="population">Poblacion de NPCs</param>
    /// <param name="currentGeneration">Generacion actual</param>
    public ValidationMetrics(List<NPCController> population, int currentGeneration)
    {
        if (population == null || population.Count == 0)
        {
            averageFitness = bestFitness = worstFitness = jumpEfficiency = explorationRate = 0f;
            return;
        }

        averageFitness = population.Average(npc => npc.fitness);
        bestFitness = population.Max(npc => npc.fitness);
        worstFitness = population.Min(npc => npc.fitness);

        // Calcular eficiencia de salto promedio
        int totalCorrectJumps = population.Sum(npc => npc.successfulJumps);
        int totalNPCs = population.Count;
        jumpEfficiency = totalNPCs > 0 ? (float)totalCorrectJumps / totalNPCs : 0f;

        // Calcular tasa de exploracion promedio (areas unicas visitadas)
        float totalExploration = population.Sum(npc => npc.GetComponent<NPCController>()?.fitness ?? 0f);
        explorationRate = totalExploration / totalNPCs;

        generation = currentGeneration;
        timestamp = Time.time;
    }
    #endregion

    #region Metodos Publicos
    /// <summary>
    /// Calcula la puntuacion de mejora comparada con una linea base
    /// </summary>
    /// <param name="baseline">Metricas de referencia</param>
    /// <returns>Puntuacion de mejora</returns>
    public float CalculateImprovementScore(ValidationMetrics baseline)
    {
        if (baseline == null) return 0f;

        float fitnessImprovement = (averageFitness - baseline.averageFitness) / Mathf.Max(baseline.averageFitness, 1f);
        float jumpImprovement = jumpEfficiency - baseline.jumpEfficiency;
        float explorationImprovement = (explorationRate - baseline.explorationRate) / Mathf.Max(baseline.explorationRate, 1f);

        // Peso ponderado de diferentes metricas
        return fitnessImprovement * 0.6f + jumpImprovement * 0.3f + explorationImprovement * 0.1f;
    }
    #endregion
}

/// <summary>
/// Respaldo de un NPC para poder restaurar su estado anterior
/// </summary>
[System.Serializable]
public class NPCBackup
{
    #region Variables Publicas
    /// <summary>
    /// Referencia al NPC respaldado
    /// </summary>
    public NPCController npc;

    /// <summary>
    /// Pesos originales de la red neuronal
    /// </summary>
    public float[][][] originalWeights;

    /// <summary>
    /// Fitness original del NPC
    /// </summary>
    public float originalFitness;
    #endregion

    #region Constructor
    /// <summary>
    /// Crea un respaldo completo del estado de un NPC
    /// </summary>
    /// <param name="target">NPC objetivo para respaldar</param>
    public NPCBackup(NPCController target)
    {
        npc = target;
        originalFitness = target.fitness;

        if (target.brain != null && target.brain.GetWeights() != null)
        {
            // Hacer copia profunda de los pesos
            var sourceWeights = target.brain.GetWeights();
            originalWeights = DeepCopyWeights(sourceWeights);
        }
    }
    #endregion

    #region Metodos Publicos
    /// <summary>
    /// Restaura los pesos originales del NPC
    /// </summary>
    public void RestoreWeights()
    {
        if (npc != null && npc.brain != null && originalWeights != null)
        {
            npc.brain.SetWeights(originalWeights);
            Debug.Log($"Restaurados los pesos para NPC {npc.name}");
        }
    }
    #endregion

    #region Metodos Privados
    /// <summary>
    /// Realiza una copia profunda de los pesos de la red neuronal
    /// </summary>
    /// <param name="source">Pesos fuente</param>
    /// <returns>Copia profunda de los pesos</returns>
    private float[][][] DeepCopyWeights(float[][][] source)
    {
        if (source == null) return null;

        float[][][] copy = new float[source.Length][][];
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] == null) continue;
            copy[i] = new float[source[i].Length][];

            for (int j = 0; j < source[i].Length; j++)
            {
                if (source[i][j] == null) continue;
                copy[i][j] = new float[source[i][j].Length];
                Array.Copy(source[i][j], copy[i][j], source[i][j].Length);
            }
        }
        return copy;
    }
    #endregion
}

#endregion

/// <summary>
/// Sistema principal para aplicar aprendizaje por imitacion a NPCs.
/// Incluye curriculum learning, timing adaptativo y validacion automatica.
/// </summary>
public class ImitationLearningApplier : MonoBehaviour
{
    #region Configuracion de Curriculum Learning
    [Header("Curriculum Learning")]
    [Tooltip("Habilitar progresion automatica de curriculum learning")]
    public bool enableCurriculumLearning = true;

    [Tooltip("Avanzar automaticamente las etapas del curriculum basado en rendimiento")]
    public bool autoAdvanceCurriculum = true;

    [Tooltip("Percentil minimo de fitness poblacional para considerar avance de etapa")]
    [Range(0.3f, 0.8f)]
    public float advancementFitnessPercentile = 0.6f;
    #endregion

    #region Variables Privadas de Curriculum
    /// <summary>
    /// Administrador del curriculum de aprendizaje
    /// </summary>
    private CurriculumManager curriculumManager;

    /// <summary>
    /// Indica si el curriculum ha sido inicializado
    /// </summary>
    private bool hasCurriculumInitialized = false;

    /// <summary>
    /// Ultima generacion verificada para curriculum
    /// </summary>
    private int lastCheckedGeneration = 0;
    #endregion

    #region Estadisticas de Curriculum
    /// <summary>
    /// Etapa actual del curriculum (para display)
    /// </summary>
    public int currentCurriculumStage = 0;

    /// <summary>
    /// Nombre de la etapa actual
    /// </summary>
    public string currentStageName = "None";

    /// <summary>
    /// Progreso en la etapa actual (0-1)
    /// </summary>
    public float stageProgress = 0f;

    /// <summary>
    /// Contador de avances de etapa realizados
    /// </summary>
    public int stageAdvancementsCount = 0;
    #endregion

    #region Configuracion de Timing Adaptativo
    [Header("Timing Adaptativo")]
    /// <summary>
    /// Configuracion para el sistema de timing adaptativo
    /// </summary>
    public AdaptiveTimingSettings adaptiveSettings;
    #endregion

    #region Variables Privadas de Timing
    /// <summary>
    /// Tracker de fitness para detectar estancamiento
    /// </summary>
    private FitnessTracker fitnessTracker;

    /// <summary>
    /// Ultimo fitness promedio registrado
    /// </summary>
    private float lastAverageFitness = 0f;

    /// <summary>
    /// Indica si el tracker ha sido inicializado
    /// </summary>
    private bool hasInitializedTracker = false;
    #endregion

    #region Estadisticas de Timing
    /// <summary>
    /// Aplicaciones activadas por estancamiento
    /// </summary>
    public int stagnationTriggeredApplications = 0;

    /// <summary>
    /// Aplicaciones activadas por urgencia
    /// </summary>
    public int urgencyTriggeredApplications = 0;

    /// <summary>
    /// Aplicaciones programadas por intervalo
    /// </summary>
    public int scheduledApplications = 0;

    /// <summary>
    /// Razon del ultimo trigger de aplicacion
    /// </summary>
    public string lastTriggerReason = "None";
    #endregion

    #region Configuracion de Validacion
    [Header("Configuracion de Validacion")]
    [Tooltip("Habilitar validacion automatica de efectividad del aprendizaje por imitacion")]
    public bool enableValidation = true;

    [Tooltip("Generaciones a esperar antes de validar efectividad")]
    [Range(1, 5)]
    public int validationGenerations = 2;

    [Tooltip("Puntuacion minima de mejora para considerar exitoso (0.05 = 5% mejora)")]
    [Range(0.01f, 0.2f)]
    public float minimumImprovementThreshold = 0.05f;

    [Tooltip("Rollback automatico si la mejora esta por debajo del umbral")]
    public bool autoRollbackOnFailure = true;

    [Tooltip("Ajustar automaticamente la fuerza de imitacion basado en resultados")]
    public bool autoAdjustStrength = true;
    #endregion

    #region Referencias
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genetico")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Tooltip("Referencia al administrador de demostraciones")]
    public DemonstrationManager demonstrationManager;
    #endregion

    #region Configuracion de Aprendizaje
    [Header("Configuracion de Aprendizaje")]
    [Tooltip("Cuanto influyen las demostraciones vs pesos geneticos (0-1)")]
    [Range(0f, 1f)]
    public float imitationStrength = 0.3f;

    [Tooltip("Numero de NPCs a los que aplicar aprendizaje por imitacion")]
    [Range(1, 10)]
    public int targetNPCCount = 5;

    [Tooltip("Percentil minimo de fitness para NPCs objetivo")]
    [Range(0f, 1f)]
    public float minFitnessPercentile = 0.3f;

    [Tooltip("Percentil maximo de fitness para NPCs objetivo")]
    [Range(0f, 1f)]
    public float maxFitnessPercentile = 0.8f;
    #endregion

    #region Estrategia de Aplicacion
    [Header("Estrategia de Aplicacion")]
    [Tooltip("Aplicar aprendizaje por imitacion cada X generaciones")]
    public int applicationInterval = 3;

    [Tooltip("Demostraciones minimas requeridas para aplicar aprendizaje")]
    public int minDemonstrationsRequired = 1;

    [Tooltip("Usar solo las mejores demostraciones para aprendizaje")]
    public bool useOnlyBestDemos = true;

    [Tooltip("Demostraciones maximas a usar simultaneamente")]
    public int maxDemosToUse = 3;
    #endregion

    #region Configuracion de Aprendizaje Avanzado
    [Header("Configuracion de Aprendizaje Avanzado")]
    [Tooltip("Habilitar aprendizaje multi-capa (no solo capa de salida)")]
    public bool enableMultiLayerLearning = true;

    [Tooltip("Decaimiento de tasa de aprendizaje por capa (capas profundas = tasa menor)")]
    [Range(0.1f, 0.9f)]
    public float layerLearningDecay = 0.6f;

    [Tooltip("Capas minimas a modificar (1 = solo salida, 2 = salida + anterior, etc.)")]
    [Range(1, 3)]
    public int minLayersToModify = 2;
    #endregion

    #region Variables Privadas de Aprendizaje
    /// <summary>
    /// Pesos aprendidos para multiples capas
    /// </summary>
    private List<float[][][]> multiLayerLearnedWeights;

    /// <summary>
    /// Indica si hay pesos multi-capa validos
    /// </summary>
    private bool hasValidMultiLayerWeights = false;

    /// <summary>
    /// Ultima generacion en que se aplico aprendizaje
    /// </summary>
    private int lastApplicationGeneration = 0;

    /// <summary>
    /// Pesos aprendidos para una sola capa
    /// </summary>
    private List<float[][]> learnedWeights;

    /// <summary>
    /// Indica si hay pesos aprendidos validos
    /// </summary>
    private bool hasValidLearnedWeights = false;
    #endregion

    #region Estadisticas
    /// <summary>
    /// Total de aplicaciones realizadas
    /// </summary>
    public int totalApplications = 0;

    /// <summary>
    /// Mejora de la ultima aplicacion
    /// </summary>
    public float lastApplicationImprovement = 0f;
    #endregion

    #region Variables Privadas de Validacion
    /// <summary>
    /// Metricas baseline para comparacion
    /// </summary>
    private ValidationMetrics baselineMetrics;

    /// <summary>
    /// Respaldos de NPCs para rollback
    /// </summary>
    private List<NPCBackup> npcBackups;

    /// <summary>
    /// Indica si hay validacion activa
    /// </summary>
    private bool isValidationActive = false;

    /// <summary>
    /// Generacion cuando inicio la validacion
    /// </summary>
    private int validationStartGeneration = 0;
    #endregion

    #region Estadisticas de Validacion
    /// <summary>
    /// Mejora de la ultima validacion
    /// </summary>
    public float lastValidationImprovement = 0f;

    /// <summary>
    /// Validaciones exitosas realizadas
    /// </summary>
    public int successfulValidations = 0;

    /// <summary>
    /// Validaciones fallidas realizadas
    /// </summary>
    public int failedValidations = 0;

    /// <summary>
    /// Mejora promedio de todas las validaciones
    /// </summary>
    public float averageImprovement = 0f;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa el sistema de aprendizaje por imitacion con todas sus configuraciones
    /// </summary>
    void Start()
    {
        // Buscar referencias si no estan asignadas
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }

        if (demonstrationManager == null)
        {
            demonstrationManager = FindObjectOfType<DemonstrationManager>();
        }

        fitnessTracker = new FitnessTracker(15); // Historial de 15 generaciones

        if (adaptiveSettings == null)
        {
            adaptiveSettings = new AdaptiveTimingSettings();
        }

        if (enableCurriculumLearning)
        {
            curriculumManager = new CurriculumManager();
            hasCurriculumInitialized = true;
            UpdateCurriculumDisplay();
            Debug.Log("Curriculum Learning inicializado con 5 etapas");
        }

        Debug.Log("Sistema de Aprendizaje por Imitacion inicializado");
    }
    #endregion

    #region Metodos de Update

    /// <summary>
    /// Actualiza el sistema cada frame verificando condiciones de aplicacion y validacion
    /// </summary>
    void Update()
    {
        // Verificar si debemos aplicar aprendizaje por imitacion
        if (ShouldApplyImitationLearning())
        {
            ApplyImitationLearning();
        }

        if (isValidationActive && ShouldPerformValidation())
        {
            PerformValidation();
        }

        if (enableCurriculumLearning && hasCurriculumInitialized)
        {
            UpdateCurriculumProgression();
        }
    }
    #endregion

    #region Metodos de Curriculum

    /// <summary>
    /// Actualiza la progresion del curriculum learning
    /// </summary>
    void UpdateCurriculumProgression()
    {
        if (curriculumManager == null) return;

        if (geneticAlgorithm.generation > lastCheckedGeneration)
        {
            curriculumManager.IncrementGeneration();
            lastCheckedGeneration = geneticAlgorithm.generation;
        }

        if (autoAdvanceCurriculum && ShouldCheckStageAdvancement())
        {
            CheckAndAdvanceStage();
        }

        UpdateCurriculumDisplay();
    }

    /// <summary>
    /// Verifica si se debe comprobar el avance de etapa
    /// </summary>
    /// <returns>True si se debe verificar</returns>
    bool ShouldCheckStageAdvancement()
    {
        if (curriculumManager.hasCompletedCurriculum) return false;
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0) return false;

        // Solo verificar cuando la generacion ha terminado
        return geneticAlgorithm.population.All(npc => npc.isDead);
    }

    /// <summary>
    /// Verifica y avanza la etapa del curriculum si se cumplen las condiciones
    /// </summary>
    void CheckAndAdvanceStage()
    {
        // Calcular fitness de percentil especificado
        var sortedFitness = geneticAlgorithm.population
            .Select(npc => npc.fitness)
            .OrderByDescending(f => f)
            .ToArray();

        int percentileIndex = Mathf.FloorToInt(sortedFitness.Length * advancementFitnessPercentile);
        float percentileFitness = sortedFitness[percentileIndex];

        if (curriculumManager.ShouldAdvanceStage(percentileFitness))
        {
            var currentStage = curriculumManager.GetCurrentStage();
            bool advanced = curriculumManager.AdvanceToNextStage();

            if (advanced)
            {
                stageAdvancementsCount++;
                var newStage = curriculumManager.GetCurrentStage();

                Debug.Log($"CURRICULUM AVANZADO! " +
                         $"De '{currentStage.stageName}' a '{newStage.stageName}' " +
                         $"(Fitness percentil poblacional: {percentileFitness:F1})");
            }
            else
            {
                Debug.Log($"CURRICULUM COMPLETADO! Todas las etapas dominadas.");
            }

            UpdateCurriculumDisplay();
        }
    }

    /// <summary>
    /// Actualiza las variables de display del curriculum
    /// </summary>
    void UpdateCurriculumDisplay()
    {
        if (curriculumManager == null) return;

        currentCurriculumStage = curriculumManager.currentStageIndex + 1;
        stageProgress = curriculumManager.GetStageProgress();

        var currentStage = curriculumManager.GetCurrentStage();
        currentStageName = currentStage?.stageName ?? "Completed";
    }

    /// <summary>
    /// Fuerza el avance del curriculum a la siguiente etapa (para uso manual)
    /// </summary>
    public void ForceAdvanceCurriculumStage()
    {
        if (!hasCurriculumInitialized) return;

        bool advanced = curriculumManager.AdvanceToNextStage();
        if (advanced)
        {
            stageAdvancementsCount++;
            UpdateCurriculumDisplay();
            Debug.Log($"Avance manual de curriculum a etapa: {currentStageName}");
        }
        else
        {
            Debug.Log("Curriculum ya completado o en etapa final");
        }
    }
    #endregion

    #region Metodos de Condiciones de Aplicacion

    /// <summary>
    /// Determina si se debe aplicar aprendizaje por imitacion basado en multiples factores
    /// </summary>
    /// <returns>True si se debe aplicar</returns>
    bool ShouldApplyImitationLearning()
    {
        // Verificaciones basicas originales
        if (geneticAlgorithm == null || demonstrationManager == null) return false;
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0) return false;
        if (demonstrationManager.loadedDemonstrations.Count < minDemonstrationsRequired) return false;
        if (!geneticAlgorithm.population.All(npc => npc.isDead)) return false;

        // Actualizar tracker de fitness
        UpdateFitnessTracking();

        // Verificar cooldown minimo
        int generationsSinceLastApplication = geneticAlgorithm.generation - lastApplicationGeneration;
        if (generationsSinceLastApplication < adaptiveSettings.minimumCooldown)
        {
            return false;
        }

        // Verificar calidad de demostraciones
        if (adaptiveSettings.requireDemoSuperiorQuality && !DemosAreGoodEnough())
        {
            lastTriggerReason = "Calidad de demo insuficiente";
            return false;
        }

        // Factor 1: Estancamiento detectado
        if (fitnessTracker.IsStagnating(adaptiveSettings.stagnationThreshold))
        {
            lastTriggerReason = $"Estancamiento detectado (score: {fitnessTracker.stagnationScore:F2})";
            stagnationTriggeredApplications++;
            return true;
        }

        // Factor 2: Urgencia por declive de fitness
        if (adaptiveSettings.applyOnFitnessDecline && DetectFitnessDecline())
        {
            lastTriggerReason = "Declive de fitness detectado";
            urgencyTriggeredApplications++;
            return true;
        }

        // Factor 3: Baja diversidad poblacional
        if (adaptiveSettings.applyOnLowDiversity && DetectLowDiversity())
        {
            lastTriggerReason = "Baja diversidad poblacional";
            urgencyTriggeredApplications++;
            return true;
        }

        // Factor 4: Intervalo maximo alcanzado (aplicacion forzada)
        if (generationsSinceLastApplication >= adaptiveSettings.maximumInterval)
        {
            lastTriggerReason = $"Intervalo maximo alcanzado ({adaptiveSettings.maximumInterval} generaciones)";
            scheduledApplications++;
            return true;
        }

        lastTriggerReason = "No se cumplieron condiciones de trigger";
        return false;
    }

    /// <summary>
    /// Actualiza el tracking de fitness para detectar estancamiento
    /// </summary>
    void UpdateFitnessTracking()
    {
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0) return;

        float currentBestFitness = geneticAlgorithm.population.Max(npc => npc.fitness);
        float currentAverageFitness = geneticAlgorithm.population.Average(npc => npc.fitness);

        fitnessTracker.UpdateFitness(currentBestFitness);
        lastAverageFitness = currentAverageFitness;
        hasInitializedTracker = true;

        if (Time.frameCount % 300 == 0) // Debug cada 5 segundos aprox
        {
            Debug.Log($"Tracking de Fitness - Mejor: {currentBestFitness:F1}, " +
                     $"Estancamiento: {fitnessTracker.stagnationScore:F2}, " +
                     $"Gens sin mejora: {fitnessTracker.generationsWithoutImprovement}");
        }
    }

    /// <summary>
    /// Verifica si las demostraciones tienen calidad suficiente
    /// </summary>
    /// <returns>True si son de buena calidad</returns>
    bool DemosAreGoodEnough()
    {
        if (!hasInitializedTracker) return true; // Si no hay historial, asumir que si

        var bestDemo = demonstrationManager.GetBestDemonstration();
        if (bestDemo == null) return false;

        float requiredFitness = lastAverageFitness * adaptiveSettings.demoQualityMultiplier;
        bool isGoodEnough = bestDemo.totalFitness >= requiredFitness;

        if (!isGoodEnough)
        {
            Debug.Log($"Verificacion de calidad de demo fallo: Mejor demo fitness {bestDemo.totalFitness:F1} < requerido {requiredFitness:F1}");
        }

        return isGoodEnough;
    }

    /// <summary>
    /// Detecta si hay un declive significativo en el fitness
    /// </summary>
    /// <returns>True si se detecta declive</returns>
    bool DetectFitnessDecline()
    {
        if (!hasInitializedTracker || fitnessTracker.recentFitnessValues.Count < 5) return false;

        var recent = fitnessTracker.recentFitnessValues.ToArray();
        int recentCount = Mathf.Min(3, recent.Length);
        int olderCount = Mathf.Min(3, recent.Length - recentCount);

        if (olderCount < 2) return false;

        float recentAvg = recent.Skip(recent.Length - recentCount).Average();
        float olderAvg = recent.Take(olderCount).Average();

        float declineThreshold = 0.05f; // 5% decline
        bool hasDeclined = (olderAvg - recentAvg) / Mathf.Max(olderAvg, 1f) > declineThreshold;

        if (hasDeclined)
        {
            Debug.Log($"Declive de fitness detectado: {olderAvg:F2} → {recentAvg:F2} ({((olderAvg - recentAvg) / olderAvg * 100):F1}% declive)");
        }

        return hasDeclined;
    }

    /// <summary>
    /// Detecta si la diversidad poblacional es baja
    /// </summary>
    /// <returns>True si la diversidad es baja</returns>
    bool DetectLowDiversity()
    {
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count < 5) return false;

        // Calcular diversidad basada en varianza de fitness
        var fitnessValues = geneticAlgorithm.population.Select(npc => npc.fitness).ToArray();
        float mean = fitnessValues.Average();
        float variance = fitnessValues.Sum(f => (f - mean) * (f - mean)) / fitnessValues.Length;
        float stdDev = Mathf.Sqrt(variance);

        // Normalizar por la media para obtener coeficiente de variacion
        float diversityScore = stdDev / Mathf.Max(mean, 1f);

        bool isLowDiversity = diversityScore < adaptiveSettings.diversityThreshold;

        if (isLowDiversity)
        {
            Debug.Log($"Baja diversidad detectada: Puntuacion de diversidad {diversityScore:F3} < umbral {adaptiveSettings.diversityThreshold:F3}");
        }

        return isLowDiversity;
    }

    /// <summary>
    /// Obtiene diagnosticos detallados del sistema de timing adaptativo
    /// </summary>
    /// <returns>String con diagnosticos</returns>
    public string GetTimingDiagnostics()
    {
        if (!hasInitializedTracker) return "Sistema de timing no inicializado";

        string diagnosis = $"DIAGNOSTICOS DE TIMING ADAPTATIVO\n";
        diagnosis += $"Puntuacion de Estancamiento: {fitnessTracker.stagnationScore:F2} (umbral: {adaptiveSettings.stagnationThreshold:F2})\n";
        diagnosis += $"Generaciones sin mejora: {fitnessTracker.generationsWithoutImprovement}\n";
        diagnosis += $"Ultima razon de trigger: {lastTriggerReason}\n";
        diagnosis += $"Aplicaciones - Estancamiento: {stagnationTriggeredApplications}, Urgencia: {urgencyTriggeredApplications}, Programadas: {scheduledApplications}";

        return diagnosis;
    }
    #endregion

    #region Metodos de Validacion

    /// <summary>
    /// Verifica si se debe realizar validacion
    /// </summary>
    /// <returns>True si se debe validar</returns>
    bool ShouldPerformValidation()
    {
        if (!isValidationActive || baselineMetrics == null) return false;

        int generationsPassed = geneticAlgorithm.generation - validationStartGeneration;
        return generationsPassed >= validationGenerations;
    }

    /// <summary>
    /// Realiza la validacion de efectividad del aprendizaje por imitacion
    /// </summary>
    void PerformValidation()
    {
        isValidationActive = false;

        // Capturar metricas actuales
        ValidationMetrics currentMetrics = new ValidationMetrics(geneticAlgorithm.population, geneticAlgorithm.generation);

        // Calcular mejora
        float improvementScore = currentMetrics.CalculateImprovementScore(baselineMetrics);
        lastValidationImprovement = improvementScore;

        // Actualizar estadisticas
        UpdateValidationStatistics(improvementScore);

        // Decidir si mantener o revertir los cambios
        bool isSuccessful = improvementScore >= minimumImprovementThreshold;

        if (isSuccessful)
        {
            HandleSuccessfulValidation(improvementScore);
        }
        else
        {
            HandleFailedValidation(improvementScore);
        }

        // Limpiar recursos
        CleanupValidation();
    }

    /// <summary>
    /// Maneja una validacion exitosa
    /// </summary>
    /// <param name="improvementScore">Puntuacion de mejora obtenida</param>
    void HandleSuccessfulValidation(float improvementScore)
    {
        Debug.Log($"VALIDACION EXITOSA! Mejora: {improvementScore:F3} (>{minimumImprovementThreshold:F3})");

        // Auto-ajustar strength al alza si esta habilitado
        if (autoAdjustStrength && improvementScore > minimumImprovementThreshold * 2f)
        {
            float newStrength = Mathf.Min(imitationStrength * 1.1f, 0.8f);
            Debug.Log($"Auto-ajustando fuerza de imitacion: {imitationStrength:F2} → {newStrength:F2}");
            imitationStrength = newStrength;
        }

        // Los cambios se mantienen (no se hace rollback)
        Debug.Log("Cambios de aprendizaje por imitacion MANTENIDOS");
    }

    /// <summary>
    /// Actualiza las estadisticas de validacion
    /// </summary>
    /// <param name="improvementScore">Puntuacion de mejora</param>
    void UpdateValidationStatistics(float improvementScore)
    {
        if (improvementScore >= minimumImprovementThreshold)
        {
            successfulValidations++;
        }
        else
        {
            failedValidations++;
        }

        // Actualizar promedio movil
        int totalValidations = successfulValidations + failedValidations;
        if (totalValidations > 0)
        {
            averageImprovement = (averageImprovement * (totalValidations - 1) + improvementScore) / totalValidations;
        }
    }

    /// <summary>
    /// Maneja una validacion fallida
    /// </summary>
    /// <param name="improvementScore">Puntuacion de mejora obtenida</param>
    void HandleFailedValidation(float improvementScore)
    {
        Debug.LogWarning($"VALIDACION FALLIDA! Mejora: {improvementScore:F3} (<{minimumImprovementThreshold:F3})");

        failedValidations++;

        // Rollback automatico si esta habilitado
        if (autoRollbackOnFailure && npcBackups != null)
        {
            foreach (var backup in npcBackups)
            {
                backup.RestoreWeights();
            }
            Debug.Log("ROLLBACK REALIZADO - Pesos originales restaurados");
        }

        // Auto-ajustar strength a la baja si esta habilitado
        if (autoAdjustStrength)
        {
            float newStrength = Mathf.Max(imitationStrength * 0.8f, 0.1f);
            Debug.Log($"Auto-ajustando fuerza de imitacion: {imitationStrength:F2} → {newStrength:F2}");
            imitationStrength = newStrength;
        }
    }

    /// <summary>
    /// Limpia los recursos del proceso de validacion
    /// </summary>
    void CleanupValidation()
    {
        baselineMetrics = null;
        npcBackups = null;
        validationStartGeneration = 0;

        Debug.Log("Proceso de validacion completado y limpiado");
    }

    /// <summary>
    /// Fuerza la validacion inmediata (para uso manual)
    /// </summary>
    public void ForceValidation()
    {
        if (isValidationActive && baselineMetrics != null)
        {
            PerformValidation();
        }
        else
        {
            Debug.LogWarning("No hay proceso de validacion activo para realizar");
        }
    }
    #endregion

    #region Metodos de Aplicacion Principal

    /// <summary>
    /// Aplica el aprendizaje por imitacion a NPCs seleccionados
    /// </summary>
    public void ApplyImitationLearning()
    {
        Debug.Log($"Aplicando aprendizaje por imitacion en generacion {geneticAlgorithm.generation}");

        // Capturar metricas baseline antes de aplicar
        if (enableValidation)
        {
            CaptureBaselineMetrics();
        }

        // Procesar demostraciones
        if (!ProcessDemonstrationsIntoWeights())
        {
            Debug.LogWarning("Fallo al procesar demostraciones en pesos");
            return;
        }

        // Seleccionar NPCs objetivo
        List<NPCController> targetNPCs = SelectTargetNPCs();
        if (targetNPCs.Count == 0)
        {
            Debug.LogWarning("No se encontraron NPCs objetivo adecuados para aprendizaje por imitacion");
            return;
        }

        // Crear backups si la validacion esta habilitada
        if (enableValidation)
        {
            CreateNPCBackups(targetNPCs);
        }

        // Aplicar pesos aprendidos
        foreach (var npc in targetNPCs)
        {
            ApplyLearnedWeightsToNPC(npc);
        }

        // Iniciar proceso de validacion
        if (enableValidation)
        {
            StartValidationProcess();
        }

        // Actualizar estadisticas
        lastApplicationGeneration = geneticAlgorithm.generation;
        totalApplications++;

        Debug.Log($"Aplicado aprendizaje por imitacion a {targetNPCs.Count} NPCs. Validacion: {(enableValidation ? "ACTIVA" : "DESHABILITADA")}");
    }

    /// <summary>
    /// Captura metricas baseline antes de aplicar aprendizaje
    /// </summary>
    void CaptureBaselineMetrics()
    {
        baselineMetrics = new ValidationMetrics(geneticAlgorithm.population, geneticAlgorithm.generation);
        Debug.Log($"Baseline capturado: Fitness Promedio = {baselineMetrics.averageFitness:F2}, Eficiencia de Salto = {baselineMetrics.jumpEfficiency:F2}");
    }

    /// <summary>
    /// Crea respaldos de los NPCs objetivo para posible rollback
    /// </summary>
    /// <param name="targetNPCs">Lista de NPCs a respaldar</param>
    void CreateNPCBackups(List<NPCController> targetNPCs)
    {
        npcBackups = new List<NPCBackup>();

        foreach (var npc in targetNPCs)
        {
            NPCBackup backup = new NPCBackup(npc);
            npcBackups.Add(backup);
        }

        Debug.Log($"Creados respaldos para {npcBackups.Count} NPCs");
    }

    /// <summary>
    /// Inicia el proceso de validacion
    /// </summary>
    void StartValidationProcess()
    {
        isValidationActive = true;
        validationStartGeneration = geneticAlgorithm.generation;
        Debug.Log($"Proceso de validacion iniciado. Evaluara en {validationGenerations} generaciones.");
    }

    /// <summary>
    /// Fuerza la aplicacion de aprendizaje por imitacion (para uso manual)
    /// </summary>
    public void ForceApplyImitationLearning()
    {
        lastApplicationGeneration = geneticAlgorithm.generation - applicationInterval; // Reset timer
        ApplyImitationLearning();
    }

    /// <summary>
    /// Recarga todas las demostraciones disponibles
    /// </summary>
    public void ReloadDemonstrations()
    {
        if (demonstrationManager != null)
        {
            demonstrationManager.LoadAllDemonstrations();
            hasValidLearnedWeights = false; // Forzar reprocesamiento
        }
    }
    #endregion

    #region Metodos de Procesamiento de Demostraciones

    /// <summary>
    /// Procesa las demostraciones disponibles en pesos utilizables
    /// </summary>
    /// <returns>True si el procesamiento fue exitoso</returns>
    bool ProcessDemonstrationsIntoWeights()
    {
        List<DemonstrationData> demosToUse = GetDemonstrationsForLearning();
        if (demosToUse.Count == 0) return false;

        InitializeLearnedWeights();

        // Filtrar y ponderar frames segun curriculum actual
        int totalFramesProcessed = 0;
        foreach (var demo in demosToUse)
        {
            totalFramesProcessed += ProcessDemonstrationWithCurriculum(demo);
        }

        if (totalFramesProcessed == 0) return false;

        NormalizeLearnedWeights(totalFramesProcessed);
        hasValidLearnedWeights = true;

        Debug.Log($"Procesadas {demosToUse.Count} demostraciones ({totalFramesProcessed} frames) con etapa de curriculum: {currentStageName}");
        return true;
    }

    /// <summary>
    /// Procesa una demostracion aplicando filtros del curriculum actual
    /// </summary>
    /// <param name="demo">Demostracion a procesar</param>
    /// <returns>Numero de frames procesados</returns>
    int ProcessDemonstrationWithCurriculum(DemonstrationData demo)
    {
        if (!enableCurriculumLearning || !hasCurriculumInitialized)
        {
            return ProcessSingleDemonstration(demo); // Fallback al metodo original
        }

        var currentStage = curriculumManager.GetCurrentStage();
        if (currentStage == null) return ProcessSingleDemonstration(demo);

        int framesProcessed = 0;

        foreach (var frame in demo.frames)
        {
            // Filtrar por calidad minima del stage
            if (frame.frameQuality < currentStage.qualityThreshold) continue;

            // Verificar si el frame es relevante para el stage actual
            if (!IsFrameRelevantForStage(frame, currentStage)) continue;

            // Aplicar ponderacion por curriculum
            UpdateWeightsFromFrameWithCurriculum(frame, currentStage);
            framesProcessed++;
        }

        return framesProcessed;
    }

    /// <summary>
    /// Verifica si un frame es relevante para la etapa actual del curriculum
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="stage">Etapa actual del curriculum</param>
    /// <returns>True si el frame es relevante</returns>
    bool IsFrameRelevantForStage(DemonstrationFrame frame, CurriculumStage stage)
    {
        switch (stage.complexity)
        {
            case BehaviorComplexity.Basic:
                // Solo movimiento hacia adelante
                return frame.humanActions[0] > 0.3f && frame.humanActions[3] < 0.5f;

            case BehaviorComplexity.Turning:
                // Incluir giros
                return frame.humanActions[1] > 0.3f || frame.humanActions[2] > 0.3f;

            case BehaviorComplexity.Navigation:
                // Navegacion con algunos obstaculos
                bool hasObstacles = false;
                for (int i = 0; i < 5 && i < frame.sensorInputs.Length; i++)
                {
                    if (frame.sensorInputs[i] > 0.2f) hasObstacles = true;
                }
                return hasObstacles;

            case BehaviorComplexity.Jumping:
                // Solo frames con saltos
                return frame.humanActions[3] > 0.5f;

            case BehaviorComplexity.Advanced:
                // Todo es valido
                return true;

            default:
                return true;
        }
    }

    /// <summary>
    /// Actualiza pesos usando un frame con ponderacion del curriculum
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="stage">Etapa actual del curriculum</param>
    void UpdateWeightsFromFrameWithCurriculum(DemonstrationFrame frame, CurriculumStage stage)
    {
        if (enableMultiLayerLearning && multiLayerLearnedWeights.Count > 0)
        {
            UpdateMultiLayerWeightsWithCurriculum(frame, stage);
        }
        else if (learnedWeights.Count > 0)
        {
            UpdateSingleLayerWeightsWithCurriculum(frame, stage);
        }
    }

    /// <summary>
    /// Actualiza pesos de una sola capa con ponderacion del curriculum
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="stage">Etapa del curriculum</param>
    void UpdateSingleLayerWeightsWithCurriculum(DemonstrationFrame frame, CurriculumStage stage)
    {
        var lastLayerWeights = learnedWeights[0];
        float baseLearningRate = 0.01f * frame.frameQuality;

        for (int neuronIdx = 0; neuronIdx < lastLayerWeights.Length; neuronIdx++)
        {
            for (int outputIdx = 0; outputIdx < lastLayerWeights[neuronIdx].Length; outputIdx++)
            {
                // Aplicar ponderacion del curriculum
                float behaviorWeight = outputIdx < stage.behaviorWeights.Length ?
                                      stage.behaviorWeights[outputIdx] : 1f;

                float adjustedLearningRate = baseLearningRate * behaviorWeight;

                float sensorValue = neuronIdx < frame.sensorInputs.Length ? frame.sensorInputs[neuronIdx] : 0f;
                float targetAction = outputIdx < frame.humanActions.Length ? frame.humanActions[outputIdx] : 0f;

                float deltaWeight = adjustedLearningRate * sensorValue * targetAction;
                lastLayerWeights[neuronIdx][outputIdx] += deltaWeight;
            }
        }
    }

    /// <summary>
    /// Actualiza pesos multi-capa con ponderacion del curriculum
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="stage">Etapa del curriculum</param>
    void UpdateMultiLayerWeightsWithCurriculum(DemonstrationFrame frame, CurriculumStage stage)
    {
        // Similar al metodo multi-layer original pero con ponderacion de curriculum
        float baseLearningRate = 0.01f * frame.frameQuality;
        float[] hiddenActivations = CalculateHiddenActivations(frame.sensorInputs);

        for (int layerLevel = 0; layerLevel < multiLayerLearnedWeights.Count; layerLevel++)
        {
            float currentLearningRate = baseLearningRate * Mathf.Pow(layerLearningDecay, layerLevel);
            var layerWeights = multiLayerLearnedWeights[layerLevel][0];

            for (int neuronIdx = 0; neuronIdx < layerWeights.Length; neuronIdx++)
            {
                for (int outputIdx = 0; outputIdx < layerWeights[neuronIdx].Length; outputIdx++)
                {
                    // Aplicar ponderacion del curriculum
                    float behaviorWeight = 1f;
                    if (layerLevel == multiLayerLearnedWeights.Count - 1 && outputIdx < stage.behaviorWeights.Length)
                    {
                        behaviorWeight = stage.behaviorWeights[outputIdx];
                    }

                    float adjustedLearningRate = currentLearningRate * behaviorWeight;

                    float inputValue = GetInputForLayer(frame, layerLevel, neuronIdx, hiddenActivations);
                    float targetValue = GetTargetForLayer(frame, layerLevel, outputIdx);

                    float deltaWeight = adjustedLearningRate * inputValue * targetValue;
                    layerWeights[neuronIdx][outputIdx] += deltaWeight;
                }
            }
        }
    }

    /// <summary>
    /// Obtiene las demostraciones a usar para aprendizaje
    /// </summary>
    /// <returns>Lista de demostraciones seleccionadas</returns>
    List<DemonstrationData> GetDemonstrationsForLearning()
    {
        var availableDemos = demonstrationManager.loadedDemonstrations;

        if (useOnlyBestDemos)
        {
            // Usar solo las mejores demostraciones
            return availableDemos
                .OrderByDescending(d => d.totalFitness)
                .Take(maxDemosToUse)
                .ToList();
        }
        else
        {
            // Usar todas las demostraciones disponibles
            return availableDemos.Take(maxDemosToUse).ToList();
        }
    }

    /// <summary>
    /// Inicializa las estructuras de pesos aprendidos
    /// </summary>
    void InitializeLearnedWeights()
    {
        var sampleNPC = geneticAlgorithm.population.FirstOrDefault(npc => npc != null && npc.brain != null);
        if (sampleNPC == null) return;

        var weights = sampleNPC.brain.GetWeights();
        if (weights == null) return;

        if (enableMultiLayerLearning)
        {
            InitializeMultiLayerWeights(weights);
        }
        else
        {
            InitializeSingleLayerWeights(weights);
        }
    }

    /// <summary>
    /// Inicializa pesos para aprendizaje multi-capa
    /// </summary>
    /// <param name="weights">Pesos de referencia</param>
    void InitializeMultiLayerWeights(float[][][] weights)
    {
        multiLayerLearnedWeights = new List<float[][][]>();

        int layersToModify = Mathf.Min(minLayersToModify, weights.Length);
        int startLayer = weights.Length - layersToModify;

        Debug.Log($"Inicializando aprendizaje multi-capa para {layersToModify} capas (empezando desde capa {startLayer})");

        for (int layerIdx = startLayer; layerIdx < weights.Length; layerIdx++)
        {
            var currentLayer = weights[layerIdx];
            float[][] learnedLayer = new float[currentLayer.Length][];

            for (int i = 0; i < currentLayer.Length; i++)
            {
                learnedLayer[i] = new float[currentLayer[i].Length];
                Array.Clear(learnedLayer[i], 0, learnedLayer[i].Length);
            }

            multiLayerLearnedWeights.Add(new float[][][] { learnedLayer });
        }
    }

    /// <summary>
    /// Inicializa pesos para aprendizaje de una sola capa
    /// </summary>
    /// <param name="weights">Pesos de referencia</param>
    void InitializeSingleLayerWeights(float[][][] weights)
    {
        // Metodo original mantenido para compatibilidad
        learnedWeights = new List<float[][]>();
        int lastLayerIndex = weights.Length - 1;
        var lastLayerWeights = weights[lastLayerIndex];

        float[][] learnedLastLayer = new float[lastLayerWeights.Length][];
        for (int i = 0; i < lastLayerWeights.Length; i++)
        {
            learnedLastLayer[i] = new float[lastLayerWeights[i].Length];
            Array.Clear(learnedLastLayer[i], 0, learnedLastLayer[i].Length);
        }

        learnedWeights.Add(learnedLastLayer);
    }

    /// <summary>
    /// Procesa una demostracion individual sin filtros de curriculum
    /// </summary>
    /// <param name="demo">Demostracion a procesar</param>
    /// <returns>Numero de frames procesados</returns>
    int ProcessSingleDemonstration(DemonstrationData demo)
    {
        int framesProcessed = 0;

        foreach (var frame in demo.frames)
        {
            // Saltar frames de baja calidad
            if (frame.frameQuality < 0.5f) continue;

            // Aplicar actualizacion tipo gradiente a pesos aprendidos
            UpdateWeightsFromFrame(frame);
            framesProcessed++;
        }

        return framesProcessed;
    }

    /// <summary>
    /// Actualiza pesos basado en un frame de demostracion
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    void UpdateWeightsFromFrame(DemonstrationFrame frame)
    {
        if (enableMultiLayerLearning && multiLayerLearnedWeights.Count > 0)
        {
            UpdateMultiLayerWeightsFromFrame(frame);
        }
        else if (learnedWeights.Count > 0)
        {
            UpdateSingleLayerWeightsFromFrame(frame);
        }
    }

    /// <summary>
    /// Actualiza pesos multi-capa desde un frame
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    void UpdateMultiLayerWeightsFromFrame(DemonstrationFrame frame)
    {
        float baseLearningRate = 0.01f * frame.frameQuality;

        // Calcular activaciones intermedias (simuladas)
        float[] hiddenActivations = CalculateHiddenActivations(frame.sensorInputs);

        for (int layerLevel = 0; layerLevel < multiLayerLearnedWeights.Count; layerLevel++)
        {
            // Learning rate decreciente para capas mas profundas
            float currentLearningRate = baseLearningRate * Mathf.Pow(layerLearningDecay, layerLevel);

            var layerWeights = multiLayerLearnedWeights[layerLevel][0];

            for (int neuronIdx = 0; neuronIdx < layerWeights.Length; neuronIdx++)
            {
                for (int outputIdx = 0; outputIdx < layerWeights[neuronIdx].Length; outputIdx++)
                {
                    float inputValue = GetInputForLayer(frame, layerLevel, neuronIdx, hiddenActivations);
                    float targetValue = GetTargetForLayer(frame, layerLevel, outputIdx);

                    // Regla de aprendizaje mejorada con momentum
                    float deltaWeight = currentLearningRate * inputValue * targetValue;

                    // Añadir regularizacion suave
                    float regularization = -0.001f * layerWeights[neuronIdx][outputIdx];

                    layerWeights[neuronIdx][outputIdx] += deltaWeight + regularization;
                }
            }
        }
    }

    /// <summary>
    /// Actualiza pesos de una sola capa desde un frame
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    void UpdateSingleLayerWeightsFromFrame(DemonstrationFrame frame)
    {
        // Metodo original mejorado
        var lastLayerWeights = learnedWeights[0];
        float learningRate = 0.01f * frame.frameQuality;

        for (int neuronIdx = 0; neuronIdx < lastLayerWeights.Length; neuronIdx++)
        {
            for (int outputIdx = 0; outputIdx < lastLayerWeights[neuronIdx].Length; outputIdx++)
            {
                float sensorValue = neuronIdx < frame.sensorInputs.Length ? frame.sensorInputs[neuronIdx] : 0f;
                float targetAction = outputIdx < frame.humanActions.Length ? frame.humanActions[outputIdx] : 0f;

                // Regla mejorada con contexto
                float deltaWeight = learningRate * sensorValue * targetAction;

                // Añadir componente contextual
                float contextBonus = CalculateContextualBonus(frame, neuronIdx, outputIdx);
                deltaWeight += contextBonus * learningRate * 0.5f;

                lastLayerWeights[neuronIdx][outputIdx] += deltaWeight;
            }
        }
    }

    /// <summary>
    /// Calcula bonus contextual para mejorar el aprendizaje
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="neuronIdx">Indice de neurona</param>
    /// <param name="outputIdx">Indice de salida</param>
    /// <returns>Bonus contextual</returns>
    float CalculateContextualBonus(DemonstrationFrame frame, int neuronIdx, int outputIdx)
    {
        // Bonus basado en contexto de la situacion
        float bonus = 0f;

        // Bonus para saltos inteligentes
        if (outputIdx == 3 && frame.humanActions[3] > 0.5f) // Jump action
        {
            bool hasLowObstacle = frame.sensorInputs.Length > 5 && frame.sensorInputs[5] > 0.3f;
            bool hasHighObstacle = frame.sensorInputs.Length > 6 && frame.sensorInputs[6] > 0.3f;

            if (hasLowObstacle && !hasHighObstacle)
            {
                bonus += 0.5f; // Boost para saltos inteligentes
            }
        }

        // Bonus para navegacion en obstaculos
        if (outputIdx <= 2) // Movement actions
        {
            float obstacleIntensity = 0f;
            for (int i = 0; i < 5 && i < frame.sensorInputs.Length; i++)
            {
                obstacleIntensity += frame.sensorInputs[i];
            }

            if (obstacleIntensity > 0.5f)
            {
                bonus += 0.3f; // Boost para navegacion compleja
            }
        }

        return bonus;
    }

    /// <summary>
    /// Calcula activaciones simuladas de capas ocultas
    /// </summary>
    /// <param name="sensorInputs">Entradas de sensores</param>
    /// <returns>Activaciones de capa oculta</returns>
    float[] CalculateHiddenActivations(float[] sensorInputs)
    {
        // Simular activaciones de capas ocultas usando heuristicas
        float[] hiddenActivations = new float[8]; // Tamaño de capa oculta

        for (int i = 0; i < hiddenActivations.Length; i++)
        {
            // Combinacion no-lineal de sensores para simular capa oculta
            float activation = 0f;
            for (int j = 0; j < sensorInputs.Length; j++)
            {
                activation += sensorInputs[j] * Mathf.Sin(i + j); // Patron no-lineal
            }
            hiddenActivations[i] = (float)Math.Tanh(activation);
        }

        return hiddenActivations;
    }

    /// <summary>
    /// Obtiene el valor de entrada para una capa especifica
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="layerLevel">Nivel de capa</param>
    /// <param name="neuronIdx">Indice de neurona</param>
    /// <param name="hiddenActivations">Activaciones de capa oculta</param>
    /// <returns>Valor de entrada</returns>
    float GetInputForLayer(DemonstrationFrame frame, int layerLevel, int neuronIdx, float[] hiddenActivations)
    {
        if (layerLevel == multiLayerLearnedWeights.Count - 1) // Ultima capa
        {
            return neuronIdx < hiddenActivations.Length ? hiddenActivations[neuronIdx] : 0f;
        }
        else // Capas intermedias
        {
            return neuronIdx < frame.sensorInputs.Length ? frame.sensorInputs[neuronIdx] : 0f;
        }
    }

    /// <summary>
    /// Obtiene el valor objetivo para una capa especifica
    /// </summary>
    /// <param name="frame">Frame de demostracion</param>
    /// <param name="layerLevel">Nivel de capa</param>
    /// <param name="outputIdx">Indice de salida</param>
    /// <returns>Valor objetivo</returns>
    float GetTargetForLayer(DemonstrationFrame frame, int layerLevel, int outputIdx)
    {
        if (layerLevel == multiLayerLearnedWeights.Count - 1) // Ultima capa
        {
            return outputIdx < frame.humanActions.Length ? frame.humanActions[outputIdx] : 0f;
        }
        else // Capas intermedias - target derivado
        {
            // Para capas intermedias, derivar targets de las acciones finales
            float derivedTarget = 0f;
            for (int actionIdx = 0; actionIdx < frame.humanActions.Length; actionIdx++)
            {
                derivedTarget += frame.humanActions[actionIdx] * Mathf.Cos(outputIdx + actionIdx);
            }
            return (float)Math.Tanh(derivedTarget);
        }
    }

    /// <summary>
    /// Normaliza los pesos aprendidos basado en el numero de frames procesados
    /// </summary>
    /// <param name="totalFrames">Total de frames procesados</param>
    void NormalizeLearnedWeights(int totalFrames)
    {
        if (learnedWeights.Count == 0 || totalFrames == 0) return;

        var lastLayerWeights = learnedWeights[0];
        float normalizationFactor = 1.0f / totalFrames;

        for (int i = 0; i < lastLayerWeights.Length; i++)
        {
            for (int j = 0; j < lastLayerWeights[i].Length; j++)
            {
                lastLayerWeights[i][j] *= normalizationFactor;
                // Clamp a rango razonable
                lastLayerWeights[i][j] = Mathf.Clamp(lastLayerWeights[i][j], -2f, 2f);
            }
        }
    }
    #endregion

    #region Metodos de Seleccion y Aplicacion

    /// <summary>
    /// Selecciona NPCs objetivo para aplicar aprendizaje por imitacion
    /// </summary>
    /// <returns>Lista de NPCs seleccionados</returns>
    List<NPCController> SelectTargetNPCs()
    {
        // Ordenar poblacion por fitness
        var sortedPopulation = geneticAlgorithm.population
            .Where(npc => npc != null && npc.brain != null)
            .OrderByDescending(npc => npc.fitness)
            .ToList();

        if (sortedPopulation.Count == 0) return new List<NPCController>();

        // Calcular rango de fitness para seleccion objetivo
        int minIndex = Mathf.FloorToInt(sortedPopulation.Count * minFitnessPercentile);
        int maxIndex = Mathf.FloorToInt(sortedPopulation.Count * maxFitnessPercentile);

        // Asegurar que no seleccionamos NPCs elite (mejores performers)
        minIndex = Mathf.Max(minIndex, geneticAlgorithm.eliteCount);
        maxIndex = Mathf.Min(maxIndex, sortedPopulation.Count - 1);

        if (minIndex >= maxIndex) return new List<NPCController>();

        // Seleccionar NPCs del rango objetivo
        var candidateNPCs = sortedPopulation.Skip(minIndex).Take(maxIndex - minIndex + 1).ToList();

        // Seleccionar aleatoriamente NPCs objetivo de los candidatos
        var targetNPCs = candidateNPCs.OrderBy(x => UnityEngine.Random.value).Take(targetNPCCount).ToList();

        Debug.Log($"Seleccionados {targetNPCs.Count} NPCs objetivo del rango de fitness [{sortedPopulation[maxIndex].fitness:F1}, {sortedPopulation[minIndex].fitness:F1}]");

        return targetNPCs;
    }

    /// <summary>
    /// Aplica los pesos aprendidos a un NPC especifico
    /// </summary>
    /// <param name="npc">NPC objetivo</param>
    void ApplyLearnedWeightsToNPC(NPCController npc)
    {
        if (!hasValidLearnedWeights || learnedWeights.Count == 0) return;

        var currentWeights = npc.brain.GetWeights();
        if (currentWeights == null) return;

        // Aplicar pesos aprendidos solo a la ultima capa (capa de salida)
        int lastLayerIndex = currentWeights.Length - 1;
        var lastLayerWeights = currentWeights[lastLayerIndex];
        var learnedLastLayer = learnedWeights[0];

        // Mezclar pesos geneticos con pesos aprendidos
        for (int i = 0; i < lastLayerWeights.Length && i < learnedLastLayer.Length; i++)
        {
            for (int j = 0; j < lastLayerWeights[i].Length && j < learnedLastLayer[i].Length; j++)
            {
                float geneticWeight = lastLayerWeights[i][j];
                float learnedWeight = learnedLastLayer[i][j];

                // Mezcla: (1-strength) * genetico + strength * aprendido
                float blendedWeight = (1f - imitationStrength) * geneticWeight + imitationStrength * learnedWeight;
                lastLayerWeights[i][j] = blendedWeight;
            }
        }

        // Establecer los pesos modificados de vuelta a la red neuronal
        npc.brain.SetWeights(currentWeights);
    }

    /// <summary>
    /// Estima la mejora potencial para un NPC
    /// </summary>
    /// <param name="npc">NPC a evaluar</param>
    /// <returns>Mejora estimada</returns>
    float EstimateImprovement(NPCController npc)
    {
        // Heuristica simple: estimar mejora basada en fuerza de imitacion y calidad de demo
        float avgDemoFitness = demonstrationManager.loadedDemonstrations.Average(d => d.totalFitness);
        float currentFitness = npc.fitness;

        // Estimar mejora potencial
        float maxPotentialImprovement = (avgDemoFitness - currentFitness) * imitationStrength;
        return Mathf.Max(0f, maxPotentialImprovement * 0.5f); // Estimacion conservadora
    }
    #endregion

    #region Interfaz de Usuario

    /// <summary>
    /// Dibuja la interfaz de usuario con estadisticas del sistema
    /// </summary>
    /// 
    void Awake()
    {
        // Cargar configuración guardada
        imitationStatusWindow.LoadConfig();
    }

    [Header("Configuración de Ventanas GUI")]
    public WindowConfig imitationStatusWindow = new WindowConfig("Estado Imitación", new Rect(Screen.width - 300, 70, 290, 70));

    void OnGUI()
    {
        if (imitationStatusWindow.enabled)
        {
            imitationStatusWindow.windowRect = GUI.Window(4, imitationStatusWindow.windowRect, DrawImitationStatusWindow, imitationStatusWindow.windowName);
        }
    }

    void DrawImitationStatusWindow(int windowID)
    {
        GUI.color = Color.magenta;
        GUI.Label(new Rect(10, 25, 270, 20), $"Aplicaciones de Imitación: {totalApplications}");

        if (hasValidLearnedWeights)
        {
            GUI.Label(new Rect(10, 45, 270, 20), $"Pesos Aprendidos: LISTOS");
        }

        if (lastApplicationImprovement > 0)
        {
            GUI.Label(new Rect(10, 65, 270, 20), $"Última Mejora: +{lastApplicationImprovement:F1}");
        }

        GUI.color = Color.white;

        // Resizable y draggable
        if (imitationStatusWindow.isResizable)
        {
            // Área de redimensionamiento en la esquina inferior derecha
            GUI.Box(new Rect(imitationStatusWindow.windowRect.width - 15, imitationStatusWindow.windowRect.height - 15, 10, 10), "");

            // Lógica de redimensionamiento
            Rect resizeRect = new Rect(imitationStatusWindow.windowRect.width - 15, imitationStatusWindow.windowRect.height - 15, 15, 15);
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.Box(resizeRect, "");
            GUI.color = Color.white;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeRect.Contains(currentEvent.mousePosition))
            {
                imitationStatusWindow.isResizing = true;
            }

            if (imitationStatusWindow.isResizing && currentEvent.type == EventType.MouseDrag)
            {
                imitationStatusWindow.windowRect.width = Mathf.Clamp(currentEvent.mousePosition.x, imitationStatusWindow.minSize.x, imitationStatusWindow.maxSize.x);
                imitationStatusWindow.windowRect.height = Mathf.Clamp(currentEvent.mousePosition.y, imitationStatusWindow.minSize.y, imitationStatusWindow.maxSize.y);
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                imitationStatusWindow.isResizing = false;
            }
        }

        // Área para arrastrar
        if (imitationStatusWindow.isDraggable)
        {
            GUI.DragWindow();
        }
    }

    // Agregar en OnDestroy o crear si no existe:
    void OnDestroy()
    {
        // Guardar configuración de ventanas
        imitationStatusWindow.SaveConfig();
    }
    #endregion
}