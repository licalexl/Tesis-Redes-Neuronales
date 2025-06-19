using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Gestiona la carga de entrenamientos previamente guardados.
/// Permite recuperar redes neuronales y continuar el entrenamiento desde un punto anterior.
/// Actualizado para soportar NPCs con saltos y sistema de aliados/enemigos.
/// </summary>
public class AITrainingLoader : MonoBehaviour
{
    #region Variables de Referencias
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genetico")]
    public NPCGeneticAlgorithm geneticAlgorithm;
    #endregion

    #region Variables de Configuracion
    [Header("Configuracion")]
    [Tooltip("Si es true, carga desde una carpeta del proyecto en lugar de persistentDataPath")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Carpeta donde se buscaran los archivos guardados (si loadFromProjectFolder es false)")]
    public string saveFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para cargar archivos (si loadFromProjectFolder es true)")]
    public string projectSaveFolder = "SavedTrainings";
    #endregion

    #region Variables de UI
    [Header("UI (Opcional)")]
    [Tooltip("Dropdown para seleccionar archivos guardados")]
    public TMP_Dropdown saveFilesDropdown;

    [Tooltip("Boton para cargar el archivo seleccionado")]
    public Button loadButton;
    #endregion

    #region Variables de Estado
    /// <summary>
    /// Bandera para indicar que estamos en proceso de carga.
    /// Evita que el sistema de guardado automatico interfiera.
    /// </summary>
    [HideInInspector]
    public bool isLoading = false;

    /// <summary>
    /// Lista con los nombres de archivos guardados
    /// </summary>
    private List<string> saveFiles = new List<string>();
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa el componente, busca referencias y configura la UI
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
                enabled = false;
                return;
            }
        }

        // Inicializamos la UI si existe
        if (saveFilesDropdown != null)
        {
            // Llenamos el dropdown con los archivos existentes
            RefreshSaveFilesList();

            // Configuramos el listener para eventos de cambio
            saveFilesDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }

        // Configuramos el boton de carga
        if (loadButton != null)
        {
            loadButton.onClick.AddListener(LoadSelectedTraining);
        }

        // Mostrar la ruta en la consola para saber donde estan los archivos
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
            Debug.Log("Cargando desde carpeta del proyecto: " + fullPath);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
            Debug.Log("Cargando desde carpeta persistente: " + fullPath);
        }
    }
    #endregion

    #region Metodos de Reconstruccion de Pesos

    /// <summary>
    /// Reconstruye la estructura de pesos 3D a partir de la lista plana
    /// </summary>
    /// <param name="flatWeights">Lista plana de pesos</param>
    /// <param name="layers">Estructura de capas de la red neuronal</param>
    /// <returns>Estructura 3D de pesos reconstruida o null en caso de error</returns>
    private float[][][] RebuildWeights(List<float> flatWeights, int[] layers)
    {
        // Verificamos que los datos sean validos
        if (flatWeights == null || flatWeights.Count == 0)
        {
            Debug.LogError("Error: La lista de pesos planos es nula o vacia");
            return null;
        }

        if (layers == null || layers.Length < 2)
        {
            Debug.LogError("Error: La estructura de capas es invalida");
            return null;
        }

        float[][][] weights = new float[layers.Length - 1][][];

        int weightIndex = 0;

        try
        {
            // Recreamos la estructura original
            for (int i = 0; i < layers.Length - 1; i++)
            {
                weights[i] = new float[layers[i]][];

                for (int j = 0; j < layers[i]; j++)
                {
                    weights[i][j] = new float[layers[i + 1]];

                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        // Verificamos que no nos quedemos sin indices
                        if (weightIndex < flatWeights.Count)
                        {
                            weights[i][j][k] = flatWeights[weightIndex];
                            weightIndex++;
                        }
                        else
                        {
                            // Si faltan pesos, usamos valores aleatorios
                            weights[i][j][k] = UnityEngine.Random.Range(-1f, 1f);
                            Debug.LogWarning("Faltaron pesos al reconstruir. Usando valores aleatorios.");
                        }
                    }
                }
            }

            Debug.Log($"Pesos reconstruidos correctamente. Utilizados: {weightIndex}/{flatWeights.Count}");
            return weights;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al reconstruir pesos: {e.Message}");
            return null;
        }
    }
    #endregion

    #region Metodos de Gestion de Archivos

    /// <summary>
    /// Actualiza la lista de archivos guardados disponibles y refresca el dropdown
    /// </summary>
    public void RefreshSaveFilesList()
    {
        if (saveFilesDropdown == null) return;

        // Limpiamos listas existentes
        saveFiles.Clear();
        saveFilesDropdown.ClearOptions();

        // Determina la ruta segun la configuracion
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder);
        }

        // Verificamos que exista la carpeta de guardado
        if (!Directory.Exists(fullPath))
        {
            Directory.CreateDirectory(fullPath);
            return;
        }

        // Obtenemos todos los archivos JSON de la carpeta
        string[] files = Directory.GetFiles(fullPath, "*.json");
        foreach (string file in files)
        {
            saveFiles.Add(Path.GetFileName(file));
        }

        // Añadimos las opciones al dropdown
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        foreach (string file in saveFiles)
        {
            options.Add(new TMP_Dropdown.OptionData(file));
        }
        saveFilesDropdown.AddOptions(options);

        Debug.Log($"Se encontraron {saveFiles.Count} archivos de guardado");
    }
    #endregion

    #region Metodos de UI

    /// <summary>
    /// Maneja el evento de cambio de seleccion en el dropdown
    /// </summary>
    /// <param name="index">Indice del archivo seleccionado</param>
    private void OnDropdownValueChanged(int index)
    {
        // Aqui se pueden añadir acciones al cambiar la seleccion
        if (index >= 0 && index < saveFiles.Count)
        {
            Debug.Log($"Archivo seleccionado: {saveFiles[index]}");
        }
    }

    /// <summary>
    /// Carga el archivo de entrenamiento seleccionado en el dropdown
    /// </summary>
    public void LoadSelectedTraining()
    {
        // Verificamos que haya un archivo seleccionado valido
        if (saveFilesDropdown == null ||
            saveFilesDropdown.value < 0 ||
            saveFilesDropdown.value >= saveFiles.Count)
        {
            Debug.LogWarning("No hay archivo de guardado seleccionado");
            return;
        }

        // Obtenemos el nombre del archivo seleccionado
        string fileName = saveFiles[saveFilesDropdown.value];

        // Cargamos el archivo
        LoadTraining(fileName);
    }
    #endregion

    #region Metodos de Carga Principal

    /// <summary>
    /// Carga un archivo de entrenamiento especifico y restaura el estado del algoritmo genetico
    /// </summary>
    /// <param name="fileName">Nombre del archivo a cargar</param>
    public void LoadTraining(string fileName)
    {
        // Determina la ruta segun la configuracion
        string fullPath;
        if (loadFromProjectFolder)
        {
            fullPath = Path.Combine(Application.dataPath, projectSaveFolder, fileName);
        }
        else
        {
            fullPath = Path.Combine(Application.persistentDataPath, saveFolder, fileName);
        }

        // Verificamos que el archivo exista
        if (!File.Exists(fullPath))
        {
            Debug.LogError($"Archivo no encontrado: {fullPath}");
            return;
        }

        // Establece isPaused en true para evitar que Update() procese la poblacion mientras cargas
        if (geneticAlgorithm != null)
        {
            geneticAlgorithm.isPaused = true;
        }

        try
        {
            // Activamos la bandera de carga
            isLoading = true;

            // Leemos y deserializamos el archivo JSON
            string json = File.ReadAllText(fullPath);
            Debug.Log($"Contenido JSON leido: {json.Substring(0, Math.Min(json.Length, 100))}..."); // Mostrar parte del JSON

            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            // Verificar que la deserializacion fue exitosa
            if (data == null)
            {
                Debug.LogError("Error al deserializar los datos: El resultado es nulo");
                return;
            }

            // Verificar que la lista de redes exista
            if (data.networks == null)
            {
                Debug.LogError("La lista de redes es nula en los datos cargados");
                return;
            }

            Debug.Log($"Se encontraron {data.networks.Count} redes en el archivo");

            Debug.Log($"Cargando entrenamiento - Generacion: {data.generation}, " +
                     $"Mejor Fitness: {data.bestFitness}, " +
                     $"Guardado: {data.timestamp}");

            // Procesamos la carga de la poblacion
            ProcessPopulationLoad(data);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al cargar entrenamiento: {e.Message}");
            Debug.LogException(e);
        }
        finally
        {
            // Desactivamos la bandera de carga
            isLoading = false;
        }
    }
    #endregion

    #region Metodos de Procesamiento de Carga

    /// <summary>
    /// Procesa la carga de la poblacion a partir de los datos deserializados
    /// </summary>
    /// <param name="data">Datos de entrenamiento cargados</param>
    private void ProcessPopulationLoad(TrainingData data)
    {
        // Destruimos la poblacion actual
        DestroyCurrentPopulation();

        // Actualizamos el numero de generacion
        geneticAlgorithm.generation = data.generation;

        // Importante: Inicializamos la nueva poblacion antes de añadir NPCs
        geneticAlgorithm.population = new List<NPCController>();

        // Verificar que hay redes guardadas para cargar
        if (data.networks.Count == 0)
        {
            Debug.LogError("El archivo no contiene redes neuronales para cargar.");
            return;
        }

        // Creamos los NPCs con las redes neuronales cargadas
        CreateNPCsFromData(data);

        // Verificamos y reanudamos el algoritmo
        FinalizePopulationLoad();
    }

    /// <summary>
    /// Destruye la poblacion actual de NPCs
    /// </summary>
    private void DestroyCurrentPopulation()
    {
        if (geneticAlgorithm.population != null)
        {
            foreach (var npc in geneticAlgorithm.population)
            {
                if (npc != null)
                {
                    Destroy(npc.gameObject);
                }
            }
            geneticAlgorithm.population.Clear();
        }
    }

    /// <summary>
    /// Crea los NPCs a partir de los datos cargados
    /// </summary>
    /// <param name="data">Datos de entrenamiento que contienen las redes neuronales</param>
    private void CreateNPCsFromData(TrainingData data)
    {
        for (int i = 0; i < geneticAlgorithm.populationSize; i++)
        {
            // Instanciamos un nuevo NPC
            GameObject npcGO = Instantiate(geneticAlgorithm.npcPrefab,
                                         geneticAlgorithm.startPosition.position,
                                         geneticAlgorithm.startPosition.rotation);

            NPCController npc = npcGO.GetComponent<NPCController>();

            if (npc != null)
            {
                // Configuramos la red neuronal del NPC
                ConfigureNPCBrain(npc, data, i);

                // Añadimos el NPC a la poblacion
                geneticAlgorithm.population.Add(npc);

                // Log para verificar la creacion
                Debug.Log($"NPC {i} creado con exito");
            }
            else
            {
                Debug.LogError("NPCController no esta en los componentes del prefab");
            }
        }
    }

    /// <summary>
    /// Configura la red neuronal de un NPC especifico
    /// </summary>
    /// <param name="npc">NPC a configurar</param>
    /// <param name="data">Datos de entrenamiento</param>
    /// <param name="index">Indice del NPC en la poblacion</param>
    private void ConfigureNPCBrain(NPCController npc, TrainingData data, int index)
    {
        // Determinamos que red neuronal usar para este NPC
        if (index < data.networks.Count)
        {
            // Si tenemos suficientes redes guardadas, usamos una directamente
            ConfigureDirectNetwork(npc, data.networks[index], index);
        }
        else
        {
            // Si necesitamos mas NPCs que los guardados, hacemos copias
            // y las mutamos ligeramente para añadir diversidad
            ConfigureMutatedNetwork(npc, data, index);
        }
    }

    /// <summary>
    /// Configura una red neuronal directamente desde los datos guardados
    /// </summary>
    /// <param name="npc">NPC a configurar</param>
    /// <param name="savedNetwork">Red neuronal guardada</param>
    /// <param name="index">Indice del NPC</param>
    private void ConfigureDirectNetwork(NPCController npc, SerializedNetwork savedNetwork, int index)
    {
        // Verificar que la informacion de la red sea valida
        if (savedNetwork == null)
        {
            Debug.LogError($"Red guardada #{index} es nula");
            return;
        }

        if (savedNetwork.layers == null)
        {
            Debug.LogError($"Capas de la red #{index} son nulas");
            return;
        }

        // Inicializar la red neuronal con las dimensiones correctas
        try
        {
            npc.brain = new NeuralNetwork(savedNetwork.layers);
            Debug.Log($"Red #{index} creada con exito. Estructura: {string.Join(",", savedNetwork.layers)}");

            // Verificamos si tenemos los pesos en formato plano
            if (savedNetwork.flattenedWeights != null && savedNetwork.flattenedWeights.Count > 0)
            {
                // Reconstruimos la estructura 3D
                float[][][] rebuiltWeights = RebuildWeights(savedNetwork.flattenedWeights, savedNetwork.layers);
                if (rebuiltWeights != null)
                {
                    npc.brain.SetWeights(rebuiltWeights);
                    Debug.Log($"Pesos reconstruidos correctamente para la red #{index}");
                }
                else
                {
                    Debug.LogError($"Error al reconstruir pesos para la red #{index}");
                }
            }
            else
            {
                Debug.LogWarning($"No se encontraron pesos planos para la red #{index}. Usando pesos aleatorios.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al crear/configurar red #{index}: {e.Message}");
            Debug.LogException(e);

            // Crear una red por defecto en caso de error
            npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
        }
    }

    /// <summary>
    /// Configura una red neuronal mutada basada en una red existente
    /// </summary>
    /// <param name="npc">NPC a configurar</param>
    /// <param name="data">Datos de entrenamiento</param>
    /// <param name="index">Indice del NPC</param>
    private void ConfigureMutatedNetwork(NPCController npc, TrainingData data, int index)
    {
        try
        {
            int sourceIndex = index % data.networks.Count;
            SerializedNetwork sourceNetwork = data.networks[sourceIndex];

            if (sourceNetwork == null || sourceNetwork.layers == null)
            {
                Debug.LogError($"Red fuente #{sourceIndex} es invalida");
                npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
            }
            else
            {
                npc.brain = new NeuralNetwork(sourceNetwork.layers);

                if (sourceNetwork.flattenedWeights != null && sourceNetwork.flattenedWeights.Count > 0)
                {
                    float[][][] rebuiltWeights = RebuildWeights(sourceNetwork.flattenedWeights, sourceNetwork.layers);
                    if (rebuiltWeights != null)
                    {
                        npc.brain.SetWeights(rebuiltWeights);
                        // Aplicamos una mutacion mas alta para diversidad
                        npc.brain.Mutate(geneticAlgorithm.mutationRate * 2);
                    }
                }
                else
                {
                    Debug.LogWarning($"Pesos nulos en red fuente #{sourceIndex}, no se aplico mutacion");
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al copiar/mutar red para NPC #{index}: {e.Message}");
            // Fallback: crear NPC con configuracion por defecto
            npc.brain = new NeuralNetwork(8, 8, 6, 4); // Estructura actualizada
        }
    }

    /// <summary>
    /// Finaliza el proceso de carga y reanuda el algoritmo genetico
    /// </summary>
    private void FinalizePopulationLoad()
    {
        // Verifica el tamaño de la poblacion despues de la creacion
        Debug.Log($"Poblacion despues de cargar: {geneticAlgorithm.population.Count}");

        // Solo reanuda si la poblacion se creo correctamente
        if (geneticAlgorithm.population.Count > 0)
        {
            Debug.Log("Entrenamiento cargado exitosamente. Reanudando simulacion.");
            // Reanuda el algoritmo
            geneticAlgorithm.isPaused = false;
        }
        else
        {
            Debug.LogError("La poblacion esta vacia despues de la carga. No se reanudara el algoritmo.");
        }
    }
    #endregion
}