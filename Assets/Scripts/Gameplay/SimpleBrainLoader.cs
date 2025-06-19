using UnityEngine;
using System.IO;

/// <summary>
/// Cargador de cerebros específico para StandaloneGameplayNPC.
/// Versión ultra-simplificada sin dependencias externas.
/// </summary>
public class SimpleBrainLoader : MonoBehaviour
{
    #region Variables de Configuración
    [Header("Configuración de Carga")]
    [Tooltip("Nombre del archivo JSON con el entrenamiento")]
    public string trainingFileName = "";

    [Tooltip("Índice de la red a cargar (0 = mejor)")]
    [Range(0, 9)]
    public int networkIndex = 0;

    [Tooltip("Cargar desde carpeta del proyecto")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta de entrenamientos")]
    public string saveFolder = "SavedTrainings";

    [Tooltip("Cargar automáticamente al inicio")]
    public bool loadOnStart = true;
    #endregion

    #region Variables de Estado
    [Header("Estado")]
    [SerializeField] private bool brainLoaded = false;
    [SerializeField] private string brainInfo = "Sin cargar";
    #endregion

    #region Referencias
    private GameplayNPC npcController;
    #endregion

    #region Inicialización
    void Start()
    {
        npcController = GetComponent<GameplayNPC>();
        if (npcController == null)
        {
            Debug.LogError($"[{gameObject.name}] Se requiere StandaloneGameplayNPC");
            enabled = false;
            return;
        }

        if (loadOnStart && !string.IsNullOrEmpty(trainingFileName))
        {
            LoadBrain();
        }
    }
    #endregion

    #region Métodos de Carga
    [ContextMenu("Cargar Cerebro")]
    public void LoadBrain()
    {
        if (string.IsNullOrEmpty(trainingFileName))
        {
            Debug.LogError($"[{gameObject.name}] Nombre de archivo vacío");
            return;
        }

        LoadBrainFromFile(trainingFileName, networkIndex);
    }

    public void LoadBrainFromFile(string fileName, int netIndex)
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
            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            if (data?.networks == null || data.networks.Count == 0)
            {
                Debug.LogError($"[{gameObject.name}] Sin redes válidas en archivo");
                return;
            }

            if (netIndex >= data.networks.Count)
            {
                Debug.LogWarning($"[{gameObject.name}] Índice {netIndex} fuera de rango, usando 0");
                netIndex = 0;
            }

            SerializedNetwork network = data.networks[netIndex];

            if (CreateBrain(network))
            {
                brainLoaded = true;
                brainInfo = $"Gen:{data.generation} Fitness:{network.fitness:F1} Saltos:{network.successfulJumps}";
                Debug.Log($"[{gameObject.name}] Cerebro cargado: {brainInfo}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{gameObject.name}] Error cargando: {e.Message}");
        }
    }

    bool CreateBrain(SerializedNetwork network)
    {
        if (network.layers == null || network.layers.Length < 2)
        {
            Debug.LogError("Estructura de red inválida");
            return false;
        }

        try
        {
            // Crear nueva red
            var newBrain = new NeuralNetwork(network.layers);

            // Aplicar pesos si existen
            if (network.flattenedWeights != null && network.flattenedWeights.Count > 0)
            {
                var weights = RebuildWeights(network.flattenedWeights, network.layers);
                if (weights != null)
                {
                    newBrain.SetWeights(weights);
                }
            }

            // Asignar al NPC
            npcController.SetBrain(newBrain);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creando cerebro: {e.Message}");
            return false;
        }
    }

    float[][][] RebuildWeights(System.Collections.Generic.List<float> flatWeights, int[] layers)
    {
        if (flatWeights == null || layers == null) return null;

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

    #region Utilidades
    string GetFilePath(string fileName)
    {
        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        return Path.Combine(folderPath, fileName);
    }

    [ContextMenu("Reset Posición")]
    public void ResetPosition()
    {
        if (npcController != null)
        {
            npcController.ResetPosition();
        }
    }

    [ContextMenu("Buscar Archivos")]
    public void FindTrainingFiles()
    {
        string folderPath = loadFromProjectFolder ?
            Path.Combine(Application.dataPath, saveFolder) :
            Path.Combine(Application.persistentDataPath, saveFolder);

        if (Directory.Exists(folderPath))
        {
            string[] files = Directory.GetFiles(folderPath, "*.json");
            Debug.Log($"Archivos encontrados en {folderPath}:");
            foreach (string file in files)
            {
                Debug.Log($"  - {Path.GetFileName(file)}");
            }
        }
        else
        {
            Debug.LogWarning($"Carpeta no encontrada: {folderPath}");
        }
    }

    public bool IsBrainLoaded() => brainLoaded;
    public string GetBrainInfo() => brainInfo;
    #endregion
}