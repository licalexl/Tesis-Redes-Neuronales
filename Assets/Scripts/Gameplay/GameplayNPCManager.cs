using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Manager para controlar múltiples NPCs en modo gameplay.
/// Permite cargar cerebros masivamente y gestionar NPCs de gameplay desde un solo lugar.
/// </summary>
public class GameplayNPCManager : MonoBehaviour
{
    #region Variables de Configuración
    [Header("Configuración Global")]
    [Tooltip("Lista de NPCs que serán controlados por este manager")]
    public List<GameplayNPC> gameplayNPCs = new List<GameplayNPC>();

    [Tooltip("Archivo de entrenamiento por defecto")]
    public string defaultTrainingFile = "";

    [Tooltip("Cargar cerebros automáticamente en Start")]
    public bool autoLoadBrains = true;
    #endregion

    #region Variables de Carga
    [Header("Configuración de Carga")]
    [Tooltip("Cargar desde carpeta del proyecto")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta de entrenamientos")]
    public string saveFolder = "SavedTrainings";

    [Tooltip("Estrategia para asignar cerebros")]
    public BrainAssignmentStrategy assignmentStrategy = BrainAssignmentStrategy.Sequential;

    [Tooltip("Índice inicial para asignación")]
    public int startingNetworkIndex = 0;
    #endregion

    #region Variables de Control
    [Header("Control Global")]
    [Tooltip("Multiplicador de velocidad global")]
    [Range(0.1f, 3f)]
    public float globalSpeedMultiplier = 1f;

    [Tooltip("Multiplicador de rotación global")]
    [Range(0.1f, 3f)]
    public float globalRotationMultiplier = 1f;

    [Tooltip("Multiplicador de salto global")]
    [Range(0.1f, 3f)]
    public float globalJumpMultiplier = 1f;
    #endregion

    #region Variables de Estado
    [Header("Estado (Solo Lectura)")]
    [SerializeField] private int npcsWithLoadedBrains = 0;
    [SerializeField] private int totalNPCs = 0;
    [SerializeField] private string loadedTrainingFile = "";
    #endregion

    #region Enums
    public enum BrainAssignmentStrategy
    {
        Sequential,     // 0,1,2,3,0,1,2...
        Random,         // Aleatorio
        BestOnly,       // Todos usan el mejor (índice 0)
        Distributed,    // Distribuir uniformemente
        SameForAll      // Todos usan el mismo índice específico
    }
    #endregion

    #region Inicialización
    void Start()
    {
        // Buscar NPCs automáticamente si la lista está vacía
        if (gameplayNPCs.Count == 0)
        {
            FindStandaloneNPCs();
        }

        totalNPCs = gameplayNPCs.Count;

        // Cargar cerebros automáticamente
        if (autoLoadBrains && !string.IsNullOrEmpty(defaultTrainingFile))
        {
            LoadBrainsForAllNPCs(defaultTrainingFile);
        }

        // Aplicar multiplicadores globales
        ApplyGlobalMultipliers();

        UpdateStatus();
    }

    [ContextMenu("Buscar NPCs Automáticamente")]
    public void FindStandaloneNPCs()
    {
        gameplayNPCs.Clear();

        GameplayNPC[] allNPCs = FindObjectsOfType<GameplayNPC>();
        gameplayNPCs.AddRange(allNPCs);

        Debug.Log($"[StandaloneNPCManager] Encontrados {gameplayNPCs.Count} NPCs independientes");
    }
    #endregion

    #region Carga de Cerebros
    [ContextMenu("Cargar Cerebros para Todos")]
    public void LoadBrainsForAllNPCs()
    {
        if (string.IsNullOrEmpty(defaultTrainingFile))
        {
            Debug.LogError("[StandaloneNPCManager] No hay archivo por defecto especificado");
            return;
        }

        LoadBrainsForAllNPCs(defaultTrainingFile);
    }

    public void LoadBrainsForAllNPCs(string trainingFileName)
    {
        if (gameplayNPCs.Count == 0)
        {
            Debug.LogWarning("[StandaloneNPCManager] No hay NPCs en la lista");
            return;
        }

        TrainingData trainingData = LoadTrainingData(trainingFileName);
        if (trainingData == null) return;

        npcsWithLoadedBrains = 0;

        for (int i = 0; i < gameplayNPCs.Count; i++)
        {
            if (gameplayNPCs[i] == null) continue;

            int networkIndex = GetNetworkIndexForNPC(i, trainingData.networks.Count);

            if (LoadBrainForNPC(gameplayNPCs[i], trainingData, networkIndex))
            {
                npcsWithLoadedBrains++;
            }
        }

        loadedTrainingFile = trainingFileName;
        Debug.Log($"[StandaloneNPCManager] Cerebros cargados para {npcsWithLoadedBrains}/{gameplayNPCs.Count} NPCs");
        UpdateStatus();
    }

    int GetNetworkIndexForNPC(int npcIndex, int totalNetworks)
    {
        switch (assignmentStrategy)
        {
            case BrainAssignmentStrategy.Sequential:
                return (startingNetworkIndex + npcIndex) % totalNetworks;

            case BrainAssignmentStrategy.Random:
                return Random.Range(0, totalNetworks);

            case BrainAssignmentStrategy.BestOnly:
                return 0;

            case BrainAssignmentStrategy.Distributed:
                return Mathf.FloorToInt((float)npcIndex / gameplayNPCs.Count * totalNetworks) % totalNetworks;

            case BrainAssignmentStrategy.SameForAll:
                return Mathf.Clamp(startingNetworkIndex, 0, totalNetworks - 1);

            default:
                return 0;
        }
    }

    bool LoadBrainForNPC(GameplayNPC npc, TrainingData data, int networkIndex)
    {
        if (networkIndex >= data.networks.Count) networkIndex = 0;

        try
        {
            // Buscar StandaloneBrainLoader en el NPC
            var brainLoader = npc.GetComponent<SimpleBrainLoader>();
            if (brainLoader != null)
            {
                brainLoader.LoadBrainFromFile(defaultTrainingFile, networkIndex);
                return true;
            }

            // Si no tiene loader, carga manual
            return LoadBrainManually(npc, data.networks[networkIndex]);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StandaloneNPCManager] Error cargando cerebro para {npc.name}: {e.Message}");
            return false;
        }
    }

    bool LoadBrainManually(GameplayNPC npc, SerializedNetwork network)
    {
        if (network.layers == null || network.layers.Length < 2) return false;

        var newBrain = new NeuralNetwork(network.layers);

        if (network.flattenedWeights != null && network.flattenedWeights.Count > 0)
        {
            var weights = RebuildWeights(network.flattenedWeights, network.layers);
            if (weights != null)
            {
                newBrain.SetWeights(weights);
            }
        }

        npc.SetBrain(newBrain);
        return true;
    }

    float[][][] RebuildWeights(System.Collections.Generic.List<float> flatWeights, int[] layers)
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

    TrainingData LoadTrainingData(string fileName)
    {
        string fullPath = GetTrainingFilePath(fileName);

        if (!File.Exists(fullPath))
        {
            Debug.LogError($"[StandaloneNPCManager] Archivo no encontrado: {fullPath}");
            return null;
        }

        try
        {
            string json = File.ReadAllText(fullPath);
            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            if (data?.networks == null || data.networks.Count == 0)
            {
                Debug.LogError($"[StandaloneNPCManager] Sin redes válidas: {fileName}");
                return null;
            }

            return data;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[StandaloneNPCManager] Error leyendo {fileName}: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Control Global
    [ContextMenu("Resetear Posiciones de Todos")]
    public void ResetAllNPCPositions()
    {
        foreach (var npc in gameplayNPCs)
        {
            if (npc != null)
            {
                npc.ResetPosition();
            }
        }
        Debug.Log("[StandaloneNPCManager] Posiciones reseteadas");
    }

    [ContextMenu("Aplicar Multiplicadores Globales")]
    public void ApplyGlobalMultipliers()
    {
        foreach (var npc in gameplayNPCs)
        {
            if (npc == null) continue;

            // Buscar modificador si existe
            var modifier = npc.GetComponent<NPCGameplayExtension>();
            if (modifier != null)
            {
                modifier.SetSpeedMultiplier(globalSpeedMultiplier);
                modifier.SetRotationMultiplier(globalRotationMultiplier);
                modifier.SetJumpMultiplier(globalJumpMultiplier);
            }
            else
            {
                // Aplicar directamente al NPC
                npc.moveSpeed *= globalSpeedMultiplier;
                npc.rotationSpeed *= globalRotationMultiplier;
                npc.jumpForce *= globalJumpMultiplier;
            }
        }

        Debug.Log($"[StandaloneNPCManager] Multiplicadores aplicados: Speed={globalSpeedMultiplier}, Rotation={globalRotationMultiplier}, Jump={globalJumpMultiplier}");
    }

    public void SetGlobalSpeedMultiplier(float multiplier)
    {
        globalSpeedMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
        ApplyGlobalMultipliers();
    }

    public void SetGlobalRotationMultiplier(float multiplier)
    {
        globalRotationMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
        ApplyGlobalMultipliers();
    }

    public void SetGlobalJumpMultiplier(float multiplier)
    {
        globalJumpMultiplier = Mathf.Clamp(multiplier, 0.1f, 3f);
        ApplyGlobalMultipliers();
    }
    #endregion

    #region Utilidades
    string GetTrainingFilePath(string fileName)
    {
        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        return Path.Combine(folderPath, fileName);
    }

    void UpdateStatus()
    {
        npcsWithLoadedBrains = 0;

        foreach (var npc in gameplayNPCs)
        {
            if (npc != null && npc.HasValidBrain())
            {
                npcsWithLoadedBrains++;
            }
        }
    }

    [ContextMenu("Mostrar Estadísticas")]
    public void ShowStats()
    {
        Debug.Log($"[StandaloneNPCManager] Estadísticas:");
        Debug.Log($"  Total NPCs: {totalNPCs}");
        Debug.Log($"  NPCs con cerebro: {npcsWithLoadedBrains}");
        Debug.Log($"  Archivo cargado: {loadedTrainingFile}");
        Debug.Log($"  Estrategia: {assignmentStrategy}");

        foreach (var npc in gameplayNPCs)
        {
            if (npc != null)
            {
                Debug.Log($"  {npc.name}: {npc.GetStats()}");
            }
        }
    }

    [ContextMenu("Listar Archivos Disponibles")]
    public void ListAvailableFiles()
    {
        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.json");
            Debug.Log($"[StandaloneNPCManager] Archivos disponibles:");
            foreach (string file in files)
            {
                Debug.Log($"  - {Path.GetFileName(file)}");
            }
        }
    }
    #endregion

    #region Getters
    public int GetTotalNPCs() => totalNPCs;
    public int GetNPCsWithBrains() => npcsWithLoadedBrains;
    public string GetLoadedFile() => loadedTrainingFile;
    public List<GameplayNPC> GetNPCs() => new List<GameplayNPC>(gameplayNPCs);
    #endregion
}