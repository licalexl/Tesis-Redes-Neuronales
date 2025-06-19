using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Sistema de visualizacion de entrenamientos adaptado al proyecto de NPCs con algoritmos geneticos.
/// Permite analizar el progreso de generaciones y crear graficas de metricas del entrenamiento.
/// </summary>
public class TrainingVisualizer : MonoBehaviour
{
    #region Variables de Referencias UI
    [Header("Referencias UI")]
    [Tooltip("Panel principal que contendra las graficas de metricas.")]
    public RectTransform graphContainer;

    [Tooltip("Prefab para crear puntos de datos en las graficas.")]
    public GameObject dataPointPrefab;

    [Tooltip("Prefab para crear lineas que conectan los puntos de datos.")]
    public GameObject lineRendererPrefab;

    [Tooltip("Componente de texto para mostrar estadisticas detalladas del entrenamiento.")]
    public TextMeshProUGUI statsText;

    [Tooltip("Menu desplegable para seleccionar que metrica mostrar en la grafica.")]
    public TMP_Dropdown metricDropdown;

    [Tooltip("Menu desplegable para seleccionar archivos de entrenamiento guardados.")]
    public TMP_Dropdown filesDropdown;

    [Tooltip("Boton para refrescar la lista de archivos.")]
    public Button refreshButton;
    #endregion

    #region Variables de Configuracion de Carga
    [Header("Configuracion de Carga")]
    [Tooltip("Si esta activo, carga archivos desde la carpeta del proyecto (Assets).")]
    public bool loadFromProjectFolder = true;

    [Tooltip("Nombre de la carpeta donde buscar archivos guardados (en persistentDataPath).")]
    public string loadFolder = "TrainingData";

    [Tooltip("Nombre de la carpeta dentro de Assets para cargar archivos.")]
    public string projectLoadFolder = "SavedTrainings";
    #endregion

    #region Variables de Configuracion de Graficas
    [Header("Configuracion de Graficas")]
    [Tooltip("Altura maxima de la grafica en pixeles.")]
    public float graphHeight = 400f;

    [Tooltip("Ancho maximo de la grafica en pixeles.")]
    public float graphWidth = 800f;

    [Tooltip("Color de la linea que representa el mejor fitness.")]
    public Color bestFitnessColor = Color.green;

    [Tooltip("Color de la linea que representa el fitness promedio.")]
    public Color avgFitnessColor = Color.yellow;

    [Tooltip("Color de la linea que representa el peor fitness.")]
    public Color worstFitnessColor = Color.red;

    [Tooltip("Color para metricas derivadas adicionales.")]
    public Color derivedMetricColor = Color.cyan;

    [Tooltip("Si esta activo, muestra valores numericos en cada punto de la grafica.")]
    public bool showValueLabels = true;
    #endregion

    #region Variables Internas de Estado
    /// <summary>
    /// Lista publica que contiene los datos de todas las generaciones cargadas para la visualizacion.
    /// </summary>
    public List<TrainingData> loadedTrainingDataList = new List<TrainingData>();

    // Lista de rutas de archivo completas para los entrenamientos encontrados.
    private List<string> trainingFiles = new List<string>();
    // Diccionario para almacenar los GameObjects (puntos, lineas) de cada metrica dibujada.
    private Dictionary<string, List<GameObject>> graphElements = new Dictionary<string, List<GameObject>>();
    // La metrica que se esta visualizando actualmente en la grafica.
    private string currentMetric = "bestFitness";
    // Indice del archivo de entrenamiento seleccionado en el menu desplegable.
    private int selectedFileIndex = -1;

    // Diccionario para almacenar metricas calculadas que no estan directamente en el JSON.
    private Dictionary<string, List<float>> derivedMetrics = new Dictionary<string, List<float>>();

    // Referencia al cargador de entrenamiento de IA, si existe en la escena.
    private AITrainingLoader trainingLoader;
    #endregion

    #region Metodos de Inicializacion
    /// <summary>
    /// Inicializa el componente, la UI y carga los archivos de entrenamiento iniciales.
    /// </summary>
    void Start()
    {
        // Buscar referencia al loader existente
        trainingLoader = FindObjectOfType<AITrainingLoader>();

        InitializeUI();
        LoadTrainingFiles();
    }

    /// <summary>
    /// Configura los componentes de la interfaz de usuario, como los menus desplegables y el boton de refresco.
    /// </summary>
    private void InitializeUI()
    {
        if (metricDropdown != null)
        {
            metricDropdown.ClearOptions();
            List<string> options = new List<string>
            {
                "Mejor Fitness",
                "Fitness Promedio",
                "Peor Fitness",
                "Rango de Fitness",
                "Complejidad de Red",
                "Diversidad Poblacional",
                "Progreso de Aprendizaje",
                "Todas las Metricas"
            };
            metricDropdown.AddOptions(options);
            metricDropdown.onValueChanged.AddListener(OnMetricSelected);
        }

        if (filesDropdown != null)
        {
            filesDropdown.onValueChanged.AddListener(OnFileSelected);
        }

        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(LoadTrainingFiles);
        }
    }
    #endregion

    #region Metodos de Carga de Archivos
    /// <summary>
    /// Escanea la carpeta de datos de entrenamiento, carga los nombres de archivo .json, los ordena por fecha y actualiza el menu desplegable.
    /// </summary>
    private void LoadTrainingFiles()
    {
        trainingFiles.Clear();
        if (filesDropdown != null)
        {
            filesDropdown.ClearOptions();
        }

        string fullPath = GetTrainingDataPath();

        if (!Directory.Exists(fullPath))
        {
            Debug.LogWarning($"La carpeta {fullPath} no existe. Creandola.");
            Directory.CreateDirectory(fullPath);
            return;
        }

        string[] files = Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            trainingFiles.Add(file);

            string displayName = fileName;
            try
            {
                string json = File.ReadAllText(file);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    displayName = $"Gen {data.generation} - {data.timestamp} (Mejor: {data.bestFitness:F1})";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error al leer archivo {fileName}: {e.Message}");
            }

            options.Add(new TMP_Dropdown.OptionData(displayName));
        }

        if (filesDropdown != null)
        {
            filesDropdown.AddOptions(options);
            filesDropdown.RefreshShownValue();
        }

        if (trainingFiles.Count > 0)
        {
            if (filesDropdown != null)
            {
                filesDropdown.value = 0;
            }
            OnFileSelected(0);
        }
    }

    /// <summary>
    /// Devuelve la ruta de la carpeta de datos de entrenamiento basandose en la configuracion 'loadFromProjectFolder'.
    /// </summary>
    /// <returns>La ruta completa a la carpeta de datos.</returns>
    private string GetTrainingDataPath()
    {
        if (loadFromProjectFolder)
        {
            return Path.Combine(Application.dataPath, projectLoadFolder);
        }
        else
        {
            return Path.Combine(Application.persistentDataPath, loadFolder);
        }
    }

    /// <summary>
    /// Carga todas las generaciones relacionadas (anteriores y posteriores) a un archivo de generacion especifico.
    /// Busca archivos con el patron "Generation_X.json".
    /// </summary>
    /// <param name="primaryFilePath">La ruta del archivo de generacion principal que se selecciono.</param>
    private void LoadRelatedGenerations(string primaryFilePath)
    {
        string fileName = Path.GetFileNameWithoutExtension(primaryFilePath);

        // Buscar por patron "Generation_X" o similar
        if (!fileName.StartsWith("Generation_")) return;

        string genNumberStr = fileName.Substring("Generation_".Length);
        if (!int.TryParse(genNumberStr, out int genNumber)) return;

        string directoryPath = Path.GetDirectoryName(primaryFilePath);

        // Buscar generaciones anteriores
        for (int i = genNumber - 1; i >= 1; i--)
        {
            string previousGenFile = Path.Combine(directoryPath, $"Generation_{i}.json");
            if (File.Exists(previousGenFile))
            {
                try
                {
                    string json = File.ReadAllText(previousGenFile);
                    TrainingData prevData = JsonUtility.FromJson<TrainingData>(json);
                    if (prevData != null)
                    {
                        loadedTrainingDataList.Insert(0, prevData);
                    }
                }
                catch (Exception)
                {
                    // Ignorar archivos con errores
                }
            }
        }

        // Buscar generaciones posteriores
        int nextGen = genNumber + 1;
        bool continueSearching = true;

        while (continueSearching)
        {
            string nextGenFile = Path.Combine(directoryPath, $"Generation_{nextGen}.json");
            if (File.Exists(nextGenFile))
            {
                try
                {
                    string json = File.ReadAllText(nextGenFile);
                    TrainingData nextData = JsonUtility.FromJson<TrainingData>(json);
                    if (nextData != null)
                    {
                        loadedTrainingDataList.Add(nextData);
                    }
                }
                catch (Exception)
                {
                    // Ignorar archivos con errores
                }
                nextGen++;
            }
            else
            {
                continueSearching = false;
            }
        }

        loadedTrainingDataList = loadedTrainingDataList.OrderBy(data => data.generation).ToList();
    }
    #endregion

    #region Metodos de Manejo de Eventos
    /// <summary>
    /// Metodo invocado cuando se selecciona un archivo del menu desplegable. Carga los datos, los valida y actualiza la visualizacion.
    /// </summary>
    /// <param name="index">El indice del archivo seleccionado en la lista.</param>
    private void OnFileSelected(int index)
    {
        if (index < 0 || index >= trainingFiles.Count) return;

        selectedFileIndex = index;
        string filePath = trainingFiles[index];

        try
        {
            string json = File.ReadAllText(filePath);
            TrainingData data = JsonUtility.FromJson<TrainingData>(json);

            if (data != null)
            {
                // Validar y corregir datos si es necesario
                ValidateTrainingData(data);

                ClearGraph();

                loadedTrainingDataList.Clear();
                loadedTrainingDataList.Add(data);

                // Cargar generaciones relacionadas si existe el patron
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                if (fileName.StartsWith("Generation_"))
                {
                    LoadRelatedGenerations(filePath);
                }

                CalculateDerivedMetrics();
                UpdateGraph();
                UpdateStatsText();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error cargando archivo {filePath}: {e.Message}");
        }
    }

    /// <summary>
    /// Metodo invocado cuando se selecciona una metrica del menu desplegable. Actualiza la grafica para mostrar la nueva metrica.
    /// </summary>
    /// <param name="index">El indice de la metrica seleccionada.</param>
    private void OnMetricSelected(int index)
    {
        switch (index)
        {
            case 0: currentMetric = "bestFitness"; break;
            case 1: currentMetric = "averageFitness"; break;
            case 2: currentMetric = "worstFitness"; break;
            case 3: currentMetric = "fitnessRange"; break;
            case 4: currentMetric = "networkComplexity"; break;
            case 5: currentMetric = "diversityIndex"; break;
            case 6: currentMetric = "learningRate"; break;
            case 7: currentMetric = "all"; break;
            default: currentMetric = "bestFitness"; break;
        }

        UpdateGraph();
    }
    #endregion

    #region Metodos de Validacion de Datos
    /// <summary>
    /// Valida y corrige los datos de entrenamiento cargados. Por ejemplo, recalcula el fitness promedio o corrige un 'worstFitness' de cero.
    /// </summary>
    /// <param name="data">El objeto de datos de entrenamiento a validar.</param>
    private void ValidateTrainingData(TrainingData data)
    {
        // Corregir worstFitness si es 0 (problema comun en el sistema anterior)
        if (data.worstFitness == 0 && data.networks != null && data.networks.Count > 0)
        {
            var validFitnesses = data.networks
                .Where(n => n.fitness > 0)
                .Select(n => n.fitness)
                .ToList();

            if (validFitnesses.Count > 0)
            {
                data.worstFitness = validFitnesses.Min();
                Debug.Log($"Worst fitness corregido: {data.worstFitness} (era 0)");
            }
        }

        // Recalcular average fitness si es necesario para asegurar consistencia
        if (data.networks != null && data.networks.Count > 0)
        {
            float calculatedAverage = data.networks.Average(n => n.fitness);
            if (Mathf.Abs(data.averageFitness - calculatedAverage) > 0.1f)
            {
                data.averageFitness = calculatedAverage;
                Debug.Log($"Average fitness recalculado: {data.averageFitness}");
            }
        }
    }
    #endregion

    #region Metodos de Calculo de Metricas
    /// <summary>
    /// Calcula metricas adicionales (derivadas) a partir de los datos de entrenamiento cargados, como el rango de fitness o la complejidad de la red.
    /// </summary>
    private void CalculateDerivedMetrics()
    {
        derivedMetrics.Clear();

        derivedMetrics["fitnessRange"] = new List<float>();
        derivedMetrics["networkComplexity"] = new List<float>();
        derivedMetrics["diversityIndex"] = new List<float>();
        derivedMetrics["learningRate"] = new List<float>();

        // ===== NUEVAS METRICAS USANDO JSON =====
        derivedMetrics["survivalRate"] = new List<float>();        // % de NPCs que sobreviven
        derivedMetrics["jumpAccuracy"] = new List<float>();        // Precision de saltos
        derivedMetrics["explorationIndex"] = new List<float>();    // Indice de exploracion

        float previousBestFitness = 0;

        foreach (var data in loadedTrainingDataList)
        {
            // Metricas existentes (usar datos JSON cuando esten disponibles)
            if (data.fitnessRange > 0)
                derivedMetrics["fitnessRange"].Add(data.fitnessRange);
            else
                derivedMetrics["fitnessRange"].Add(data.bestFitness - data.worstFitness);

            if (data.diversityIndex > 0)
                derivedMetrics["diversityIndex"].Add(data.diversityIndex);
            else
                derivedMetrics["diversityIndex"].Add(CalculateDiversityIndex(data));

            // ===== NUEVAS METRICAS =====
            // Tasa de supervivencia
            if (data.populationSize > 0)
            {
                float survivalRate = (float)data.aliveNPCs / data.populationSize * 100f;
                derivedMetrics["survivalRate"].Add(survivalRate);
            }
            else
            {
                derivedMetrics["survivalRate"].Add(0);
            }

            // Precision de saltos
            if (data.networks != null && data.networks.Count > 0)
            {
                int totalCorrect = data.networks.Sum(n => n.correctJumps);
                int totalIncorrect = data.networks.Sum(n => n.incorrectJumps);

                if (totalCorrect + totalIncorrect > 0)
                {
                    float accuracy = (float)totalCorrect / (totalCorrect + totalIncorrect) * 100f;
                    derivedMetrics["jumpAccuracy"].Add(accuracy);
                }
                else
                {
                    derivedMetrics["jumpAccuracy"].Add(0);
                }

                // Indice de exploracion
                float avgExploration = (float)data.networks.Average(n => n.uniqueAreasVisited);
                derivedMetrics["explorationIndex"].Add(avgExploration);
            }
            else
            {
                derivedMetrics["jumpAccuracy"].Add(0);
                derivedMetrics["explorationIndex"].Add(0);
            }

            // Tasa de aprendizaje (usando JSON cuando este disponible)
            float learningRate = 0;
            if (previousBestFitness > 0)
            {
                learningRate = data.bestFitness - previousBestFitness;
            }
            derivedMetrics["learningRate"].Add(learningRate);
            previousBestFitness = data.bestFitness;

            // Complejidad de red (usar datos JSON)
            if (data.networks != null && data.networks.Count > 0)
            {
                float avgComplexity = (float)data.networks.Average(n => n.activeWeightsCount);
                derivedMetrics["networkComplexity"].Add(avgComplexity);
            }
            else
            {
                derivedMetrics["networkComplexity"].Add(0);
            }
        }
    }

    /// <summary>
    /// Calcula la complejidad promedio de la red para una generacion, contando el numero de pesos "activos".
    /// </summary>
    /// <param name="data">Los datos de la generacion.</param>
    /// <returns>La complejidad promedio.</returns>
    private float CalculateNetworkComplexity(TrainingData data)
    {
        if (data.networks == null || data.networks.Count == 0) return 0;

        float totalComplexity = 0;
        int validNetworks = 0;

        foreach (var network in data.networks)
        {
            if (network.flattenedWeights != null && network.flattenedWeights.Count > 0)
            {
                // Contar pesos significativos (valor absoluto > 0.01)
                int activeWeights = network.flattenedWeights.Count(w => Mathf.Abs(w) > 0.01f);
                totalComplexity += activeWeights;
                validNetworks++;
            }
        }

        return validNetworks > 0 ? totalComplexity / validNetworks : 0;
    }

    /// <summary>
    /// Calcula un indice de diversidad poblacional basado en la desviacion estandar del fitness de la generacion.
    /// </summary>
    /// <param name="data">Los datos de la generacion.</param>
    /// <returns>El indice de diversidad.</returns>
    private float CalculateDiversityIndex(TrainingData data)
    {
        if (data.networks == null || data.networks.Count < 2) return 0;

        // Calcular desviacion estandar de fitness
        float sumSquaredDiff = 0;
        foreach (var network in data.networks)
        {
            float diff = network.fitness - data.averageFitness;
            sumSquaredDiff += diff * diff;
        }

        return Mathf.Sqrt(sumSquaredDiff / data.networks.Count);
    }
    #endregion

    #region Metodos de Creacion de Graficas
    /// <summary>
    /// Limpia la grafica actual y dibuja las lineas y puntos para la metrica seleccionada.
    /// </summary>
    public void UpdateGraph()
    {
        ClearGraph();

        if (loadedTrainingDataList.Count == 0) return;

        if (currentMetric == "all")
        {
            CreateGraphLine("bestFitness", bestFitnessColor);
            CreateGraphLine("averageFitness", avgFitnessColor);
            CreateGraphLine("worstFitness", worstFitnessColor);
        }
        else if (derivedMetrics.ContainsKey(currentMetric))
        {
            CreateDerivedMetricLine(currentMetric, derivedMetricColor);
        }
        else
        {
            Color color = GetColorForMetric(currentMetric);
            CreateGraphLine(currentMetric, color);
        }
    }

    /// <summary>
    /// Devuelve el color apropiado para una metrica especifica.
    /// </summary>
    /// <param name="metric">El nombre de la metrica.</param>
    /// <returns>Un objeto Color para la metrica.</returns>
    private Color GetColorForMetric(string metric)
    {
        switch (metric)
        {
            case "bestFitness": return bestFitnessColor;
            case "averageFitness": return avgFitnessColor;
            case "worstFitness": return worstFitnessColor;
            default: return derivedMetricColor;
        }
    }

    /// <summary>
    /// Dibuja una linea en la grafica para una metrica basica (best, avg, worst fitness).
    /// </summary>
    /// <param name="metric">Nombre de la metrica a dibujar.</param>
    /// <param name="color">Color para la linea y los puntos.</param>
    private void CreateGraphLine(string metric, Color color)
    {
        if (loadedTrainingDataList.Count <= 1) return;

        // Calcular rango de valores
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;

        foreach (var data in loadedTrainingDataList)
        {
            float value = GetMetricValue(data, metric);
            if (value < minVal) minVal = value;
            if (value > maxVal) maxVal = value;
        }

        // Anadir margen para que la grafica no toque los bordes
        float range = maxVal - minVal;
        if (range == 0) range = 1; // Evitar division por cero
        minVal -= range * 0.1f;
        maxVal += range * 0.1f;

        CreateGraphPoints(metric, color, minVal, maxVal);
    }

    /// <summary>
    /// Obtiene el valor de una metrica basica desde un objeto TrainingData.
    /// </summary>
    /// <param name="data">El objeto de datos de la generacion.</param>
    /// <param name="metric">El nombre de la metrica a obtener.</param>
    /// <returns>El valor de la metrica.</returns>
    private float GetMetricValue(TrainingData data, string metric)
    {
        switch (metric)
        {
            case "bestFitness": return data.bestFitness;
            case "averageFitness": return data.averageFitness;
            case "worstFitness": return data.worstFitness;
            default: return 0;
        }
    }

    /// <summary>
    /// Crea los GameObjects (puntos y etiquetas) para una linea de la grafica.
    /// </summary>
    /// <param name="metric">La metrica que se esta dibujando.</param>
    /// <param name="color">El color a usar para los puntos y etiquetas.</param>
    /// <param name="minVal">El valor minimo en el eje Y para la normalizacion.</param>
    /// <param name="maxVal">El valor maximo en el eje Y para la normalizacion.</param>
    private void CreateGraphPoints(string metric, Color color, float minVal, float maxVal)
    {
        List<GameObject> linePoints = new List<GameObject>();
        List<Vector2> pointPositions = new List<Vector2>();

        for (int i = 0; i < loadedTrainingDataList.Count; i++)
        {
            var data = loadedTrainingDataList[i];
            float value = GetMetricValue(data, metric);

            float normalizedY = Mathf.InverseLerp(minVal, maxVal, value);
            float xPos = (loadedTrainingDataList.Count > 1) ? (float)i / (loadedTrainingDataList.Count - 1) * graphWidth : 0;
            float yPos = normalizedY * graphHeight;

            // Crear punto
            GameObject point = Instantiate(dataPointPrefab, graphContainer);
            point.transform.localPosition = new Vector3(xPos, yPos, 0);

            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = color;
            }

            // Crear etiqueta si esta habilitado
            if (showValueLabels)
            {
                CreateValueLabel(point, value, color);
            }

            linePoints.Add(point);
            pointPositions.Add(new Vector2(xPos, yPos));
        }

        // Crear lineas conectoras
        CreateConnectingLines(pointPositions, color, linePoints);

        graphElements[metric] = linePoints;
    }

    /// <summary>
    /// Crea una etiqueta de texto (TextMeshProUGUI) para un punto de datos en la grafica.
    /// </summary>
    /// <param name="point">El GameObject del punto al que se adjuntara la etiqueta.</param>
    /// <param name="value">El valor numerico a mostrar.</param>
    /// <param name="color">El color del texto de la etiqueta.</param>
    private void CreateValueLabel(GameObject point, float value, Color color)
    {
        GameObject label = new GameObject($"Label_{value:F1}");
        label.transform.SetParent(point.transform, false);

        TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text = value.ToString("F1");
        labelText.fontSize = 10;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = color;
        label.transform.localPosition = new Vector3(0, 15, 0);
    }

    /// <summary>
    /// Crea los GameObjects de linea (usando Images) para conectar los puntos de datos en la grafica.
    /// </summary>
    /// <param name="pointPositions">Lista de posiciones de los puntos a conectar.</param>
    /// <param name="color">El color de las lineas.</param>
    /// <param name="linePoints">La lista de GameObjects de la grafica donde se anadiran las lineas creadas.</param>
    private void CreateConnectingLines(List<Vector2> pointPositions, Color color, List<GameObject> linePoints)
    {
        for (int i = 0; i < pointPositions.Count - 1; i++)
        {
            GameObject line = Instantiate(lineRendererPrefab, graphContainer);
            RectTransform lineRect = line.GetComponent<RectTransform>();

            Vector2 point1 = pointPositions[i];
            Vector2 point2 = pointPositions[i + 1];
            float distance = Vector2.Distance(point1, point2);
            Vector2 midPoint = (point1 + point2) / 2;

            lineRect.localPosition = new Vector3(midPoint.x, midPoint.y, 0);
            lineRect.sizeDelta = new Vector2(distance, 2);

            float angle = Mathf.Atan2(point2.y - point1.y, point2.x - point1.x) * Mathf.Rad2Deg;
            lineRect.localRotation = Quaternion.Euler(0, 0, angle);

            Image lineImage = line.GetComponent<Image>();
            if (lineImage != null)
            {
                lineImage.color = color;
            }

            linePoints.Add(line);
        }
    }

    /// <summary>
    /// Dibuja una linea en la grafica para una metrica derivada.
    /// </summary>
    /// <param name="metric">Nombre de la metrica derivada a dibujar.</param>
    /// <param name="color">Color para la linea y los puntos.</param>
    private void CreateDerivedMetricLine(string metric, Color color)
    {
        if (!derivedMetrics.ContainsKey(metric) || derivedMetrics[metric].Count <= 1) return;

        List<float> values = derivedMetrics[metric];

        float minVal = values.Min();
        float maxVal = values.Max();

        float range = maxVal - minVal;
        if (range == 0) range = 1;
        minVal -= range * 0.1f;
        maxVal += range * 0.1f;

        List<GameObject> linePoints = new List<GameObject>();
        List<Vector2> pointPositions = new List<Vector2>();

        for (int i = 0; i < values.Count; i++)
        {
            float value = values[i];

            float normalizedY = Mathf.InverseLerp(minVal, maxVal, value);
            float xPos = (values.Count > 1) ? (float)i / (values.Count - 1) * graphWidth : 0;
            float yPos = normalizedY * graphHeight;

            GameObject point = Instantiate(dataPointPrefab, graphContainer);
            point.transform.localPosition = new Vector3(xPos, yPos, 0);

            Image pointImage = point.GetComponent<Image>();
            if (pointImage != null)
            {
                pointImage.color = color;
            }

            if (showValueLabels)
            {
                CreateValueLabel(point, value, color);
            }

            linePoints.Add(point);
            pointPositions.Add(new Vector2(xPos, yPos));
        }

        CreateConnectingLines(pointPositions, color, linePoints);
        graphElements[metric] = linePoints;
    }

    /// <summary>
    /// Elimina todos los GameObjects que componen la grafica actual (puntos, lineas, etiquetas).
    /// </summary>
    private void ClearGraph()
    {
        foreach (var elements in graphElements.Values)
        {
            foreach (var element in elements)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
        }
        graphElements.Clear();
    }
    #endregion

    #region Metodos de Actualizacion de Estadisticas
    /// <summary>
    /// Actualiza el panel de texto de estadisticas con la informacion de la ultima generacion cargada.
    /// </summary>
    private void UpdateStatsText()
    {
        if (loadedTrainingDataList.Count == 0 || statsText == null) return;

        TrainingData latestData = loadedTrainingDataList.Last();

        string statsInfo = BuildStatsText(latestData);
        statsText.text = statsInfo;
    }

    /// <summary>
    /// Construye la cadena de texto formateada con todas las estadisticas de una generacion.
    /// </summary>
    /// <param name="latestData">Los datos de la generacion mas reciente.</param>
    /// <returns>La cadena de texto con la informacion de estadisticas.</returns>
    private string BuildStatsText(TrainingData latestData)
    {
        string statsInfo = $"<b>Estadisticas de Entrenamiento</b>\n" +
                          $"Generacion: {latestData.generation}\n" +
                          $"Guardado: {latestData.timestamp}\n\n" +
                          $"<color=green>Mejor Fitness: {latestData.bestFitness:F2}</color>\n" +
                          $"<color=yellow>Fitness Promedio: {latestData.averageFitness:F2}</color>\n" +
                          $"<color=red>Peor Fitness: {latestData.worstFitness:F2}</color>\n" +
                          $"Rango: {latestData.fitnessRange:F2}\n\n";

        // ===== USAR NUEVOS CAMPOS DEL JSON =====
        statsInfo += $"<b>Poblacion:</b>\n";
        statsInfo += $"Tamano: {latestData.populationSize}\n";
        statsInfo += $"NPCs vivos al final: {latestData.aliveNPCs}\n";
        statsInfo += $"Diversidad: {latestData.diversityIndex:F2}\n\n";

        statsInfo += $"<b>Configuracion:</b>\n";
        statsInfo += $"Tasa de mutacion: {latestData.mutationRate:F3}\n";
        statsInfo += $"Elites preservados: {latestData.eliteCount}\n";

        // Mostrar bloqueos activos
        if (latestData.globalLockStatus != null)
        {
            string[] actions = { "Movimiento", "Giro Izq.", "Giro Der.", "Salto" };
            List<string> lockedActions = new List<string>();

            for (int i = 0; i < latestData.globalLockStatus.Length && i < actions.Length; i++)
            {
                if (latestData.globalLockStatus[i])
                    lockedActions.Add(actions[i]);
            }

            if (lockedActions.Count > 0)
                statsInfo += $"Acciones bloqueadas: {string.Join(", ", lockedActions)}\n";
        }

        statsInfo += "\n";

        // ===== ESTADISTICAS DE COMPORTAMIENTO =====
        if (latestData.networks != null && latestData.networks.Count > 0)
        {
            statsInfo += BuildBehaviorStats(latestData);
            statsInfo += BuildNetworkStatsFromJSON(latestData);
        }

        return statsInfo;
    }

    /// <summary>
    /// Construye la seccion de la cadena de estadisticas relacionada con el comportamiento de la poblacion.
    /// </summary>
    /// <param name="data">Los datos de la generacion.</param>
    /// <returns>Una cadena de texto con las estadisticas de comportamiento.</returns>
    private string BuildBehaviorStats(TrainingData data)
    {
        string stats = $"<b>Estadisticas de Comportamiento:</b>\n";

        stats += $"Tiempo promedio vivo: {data.averageTimeAlive:F1}s\n";
        stats += $"Distancia total recorrida: {data.totalDistanceSum:F1}\n";
        stats += $"Saltos exitosos totales: {data.totalSuccessfulJumps}\n";

        if (data.networks.Count > 0)
        {
            float avgCheckpoints = (float)data.networks.Average(n => n.checkpointsReached);
            float avgExploration = (float)data.networks.Average(n => n.uniqueAreasVisited);
            int totalCorrectJumps = data.networks.Sum(n => n.correctJumps);
            int totalIncorrectJumps = data.networks.Sum(n => n.incorrectJumps);

            stats += $"Checkpoints promedio: {avgCheckpoints:F1}\n";
            stats += $"Areas exploradas promedio: {avgExploration:F1}\n";

            if (totalCorrectJumps + totalIncorrectJumps > 0)
            {
                float jumpAccuracy = (float)totalCorrectJumps / (totalCorrectJumps + totalIncorrectJumps) * 100f;
                stats += $"Precision de saltos: {jumpAccuracy:F1}%\n";
            }
        }

        return stats + "\n";
    }

    /// <summary>
    /// Construye la seccion de la cadena de estadisticas detallando la mejor red neuronal de la generacion, usando todos los campos del JSON.
    /// </summary>
    /// <param name="data">Los datos de la generacion.</param>
    /// <returns>Una cadena de texto con las estadisticas de la mejor red.</returns>
    private string BuildNetworkStatsFromJSON(TrainingData data)
    {
        string stats = $"<b>Mejor Red de la Generacion:</b>\n";

        SerializedNetwork bestNetwork = data.networks.OrderByDescending(n => n.fitness).First();

        stats += $"Fitness: {bestNetwork.fitness:F2}\n";

        if (bestNetwork.layers != null)
        {
            stats += $"Estructura: {string.Join("-", bestNetwork.layers)}\n";
        }

        // ===== USAR NUEVOS CAMPOS =====
        stats += $"Tiempo vivo: {bestNetwork.timeAlive:F1}s\n";
        stats += $"Distancia: {bestNetwork.totalDistance:F1}\n";
        stats += $"Saltos exitosos: {bestNetwork.successfulJumps}\n";
        stats += $"Checkpoints: {bestNetwork.checkpointsReached}\n";
        stats += $"Areas exploradas: {bestNetwork.uniqueAreasVisited}\n";

        // Analisis de red neuronal
        if (bestNetwork.flattenedWeights != null)
        {
            stats += $"Pesos totales: {bestNetwork.flattenedWeights.Count}\n";
            stats += $"Pesos activos: {bestNetwork.activeWeightsCount}\n";
            stats += $"Complejidad: {bestNetwork.weightComplexity * 100:F1}%\n";
        }

        // Eficiencia de saltos
        if (bestNetwork.correctJumps + bestNetwork.incorrectJumps > 0)
        {
            float jumpEff = (float)bestNetwork.correctJumps / (bestNetwork.correctJumps + bestNetwork.incorrectJumps) * 100f;
            stats += $"Eficiencia saltos: {jumpEff:F1}%\n";
        }

        return stats;
    }

    /// <summary>
    /// (Metodo alternativo/anterior) Construye una seccion basica de estadisticas de la mejor red.
    /// </summary>
    /// <param name="data">Los datos de la generacion.</param>
    /// <returns>Una cadena de texto con estadisticas basicas de la red.</returns>
    private string BuildNetworkStats(TrainingData data)
    {
        string stats = $"Redes guardadas: {data.networks.Count}\n";

        SerializedNetwork bestNetwork = data.networks.OrderByDescending(n => n.fitness).First();

        stats += "\n<b>Mejor Red:</b>\n" +
                $"Fitness: {bestNetwork.fitness:F2}\n";

        if (bestNetwork.layers != null)
        {
            stats += $"Estructura: {string.Join("-", bestNetwork.layers)}\n";
        }

        if (bestNetwork.flattenedWeights != null)
        {
            int activeWeights = bestNetwork.flattenedWeights.Count(w => Mathf.Abs(w) > 0.01f);
            stats += $"Pesos activos: {activeWeights}/{bestNetwork.flattenedWeights.Count}\n";
        }

        return stats;
    }

    /// <summary>
    /// Construye la seccion de la cadena de estadisticas que muestra tendencias a lo largo de las generaciones cargadas.
    /// </summary>
    /// <returns>Una cadena de texto con las estadisticas de tendencias.</returns>
    private string BuildTrendStats()
    {
        string stats = "\n<b>Tendencias:</b>\n";

        if (derivedMetrics.ContainsKey("learningRate") && derivedMetrics["learningRate"].Count > 0)
        {
            float avgLearningRate = derivedMetrics["learningRate"].Skip(1).Average(); // Skip primer valor (0)
            stats += $"Mejora promedio: {avgLearningRate:F2} por generacion\n";
        }

        if (derivedMetrics.ContainsKey("diversityIndex") && derivedMetrics["diversityIndex"].Count > 0)
        {
            float latestDiversity = derivedMetrics["diversityIndex"].Last();
            stats += $"Diversidad actual: {latestDiversity:F2}\n";
        }

        if (derivedMetrics.ContainsKey("networkComplexity") && derivedMetrics["networkComplexity"].Count > 0)
        {
            float latestComplexity = derivedMetrics["networkComplexity"].Last();
            stats += $"Complejidad promedio: {latestComplexity:F0} pesos activos\n";
        }

        return stats;
    }
    #endregion

    #region Metodos Publicos de API
    /// <summary>
    /// Metodo publico para forzar una actualizacion completa de la visualizacion, recargando los archivos.
    /// </summary>
    public void RefreshVisualization()
    {
        LoadTrainingFiles();
    }

    /// <summary>
    /// Metodo publico para habilitar o deshabilitar las etiquetas de valor en los puntos de la grafica.
    /// </summary>
    /// <param name="show">True para mostrar las etiquetas, false para ocultarlas.</param>
    public void SetShowValueLabels(bool show)
    {
        showValueLabels = show;
        UpdateGraph();
    }
    #endregion
}

