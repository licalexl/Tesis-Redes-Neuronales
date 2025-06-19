using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

/// <summary>
/// Sistema central de dashboard para visualizacion y analisis de entrenamientos de IA.
/// Adaptado al proyecto de NPCs con algoritmos geneticos.
/// Proporciona interfaces para monitorear generaciones, analizar redes neuronales y comparar resultados.
/// </summary>
public class TrainingDashboard : MonoBehaviour
{
    #region Variables de Referencias
    [Header("Referencias")]
    [Tooltip("Componente que maneja la visualizacion de graficas y metricas de entrenamiento.")]
    public TrainingVisualizer trainingVisualizer;

    [Tooltip("Componente que analiza y visualiza la estructura interna de las redes neuronales.")]
    public NetworkAnalyzer networkAnalyzer;
    #endregion

    #region Variables de UI - Componentes Principales
    [Header("Componentes UI")]
    [Tooltip("Botones de navegacion entre diferentes pestanas del dashboard.")]
    public GameObject[] tabButtons;

    [Tooltip("Paneles de contenido correspondientes a cada pestana del dashboard.")]
    public GameObject[] tabPanels;

    [Tooltip("Panel principal para visualizar graficas y estadisticas de generaciones.")]
    public GameObject generationsPanel;

    [Tooltip("Panel dedicado al analisis detallado de redes neuronales individuales.")]
    public GameObject networksPanel;

    [Tooltip("Panel para comparar resultados entre diferentes entrenamientos.")]
    public GameObject comparisonPanel;

    [Tooltip("Panel de informacion general y documentacion del sistema.")]
    public GameObject infoPanel;
    #endregion

    #region Variables de UI - Elementos de Informacion
    [Header("Informacion General")]
    [Tooltip("Muestra el nombre del archivo de entrenamiento actualmente cargado.")]
    public TextMeshProUGUI currentFileText;

    [Tooltip("Boton para actualizar manualmente la visualizacion con datos mas recientes.")]
    public Button refreshButton;
    #endregion

    #region Variables de UI - Sistema de Comparacion
    [Header("Comparacion de Archivos")]
    [Tooltip("Lista desplegable para seleccionar archivos de entrenamiento a comparar.")]
    public TMP_Dropdown compareFileDropdown;

    [Tooltip("Boton para anadir el archivo seleccionado a la lista de comparacion.")]
    public Button addCompareButton;

    [Tooltip("Boton para limpiar la seleccion de archivos para comparacion.")]
    public Button clearCompareButton;

    [Tooltip("Muestra la lista de archivos seleccionados para comparacion.")]
    public TextMeshProUGUI selectedFilesText;

    [Tooltip("Contenedor donde se generan las graficas de comparacion entre entrenamientos.")]
    public RectTransform comparisonGraphContainer;

    [Tooltip("Selecciona que metrica usar para la comparacion.")]
    public TMP_Dropdown compareMetricDropdown;
    #endregion

    #region Variables de UI - Estadisticas de Redes
    [Header("Estadisticas de Redes")]
    [Tooltip("Muestra estadisticas detalladas sobre el rendimiento de las redes neuronales.")]
    public TextMeshProUGUI networkStatsText;
    #endregion

    #region Variables de UI - Filtros y Ordenamiento
    [Header("Filtros y Ordenacion")]
    [Tooltip("Cambia el criterio de ordenamiento de la lista de redes neuronales.")]
    public TMP_Dropdown sortNetworksDropdown;

    [Tooltip("Control deslizante para filtrar redes por fitness minimo.")]
    public Slider fitnessFilterSlider;

    [Tooltip("Texto que muestra el valor actual del filtro de fitness.")]
    public TextMeshProUGUI fitnessFilterText;
    #endregion

    #region Variables Internas de Estado
    /// <summary>
    /// Indice de la pestana activa actualmente.
    /// </summary>
    private int currentTabIndex = 0;
    /// <summary>
    /// Lista de rutas de archivos seleccionados para la comparacion.
    /// </summary>
    private List<string> filesToCompare = new List<string>();
    /// <summary>
    /// Datos de entrenamiento cargados de los archivos en 'filesToCompare'.
    /// </summary>
    private List<TrainingData> loadedComparisonData = new List<TrainingData>();
    /// <summary>
    /// Diccionario para gestionar los elementos de UI generados en la grafica de comparacion.
    /// </summary>
    private Dictionary<string, List<GameObject>> comparisonGraphElements = new Dictionary<string, List<GameObject>>();
    /// <summary>
    /// Lista de redes neuronales despues de aplicar filtros.
    /// </summary>
    private List<SerializedNetwork> filteredNetworks = new List<SerializedNetwork>();
    /// <summary>
    /// Valor actual del filtro de fitness.
    /// </summary>
    private float currentFitnessFilter = 0f;
    #endregion

    #region Metodos de Inicializacion
    /// <summary>
    /// Metodo de Unity llamado al iniciar el script. Se encarga de llamar a la inicializacion del dashboard.
    /// </summary>
    void Start()
    {
        InitializeDashboard();
    }

    /// <summary>
    /// Configura e inicializa todos los componentes del dashboard, botones y listeners.
    /// </summary>
    private void InitializeDashboard()
    {
        // Verificar dependencias
        if (trainingVisualizer == null)
        {
            trainingVisualizer = FindObjectOfType<TrainingVisualizer>();
        }

        if (networkAnalyzer == null)
        {
            networkAnalyzer = FindObjectOfType<NetworkAnalyzer>();
        }

        // Inicializar pestanas
        SetActiveTab(0);

        for (int i = 0; i < tabButtons.Length; i++)
        {
            int tabIndex = i;
            tabButtons[i].GetComponent<Button>().onClick.AddListener(() => SetActiveTab(tabIndex));
        }

        // Configurar botones
        if (refreshButton != null)
        {
            refreshButton.onClick.AddListener(RefreshDashboard);
        }

        if (addCompareButton != null)
        {
            addCompareButton.onClick.AddListener(AddFileToComparison);
        }

        if (clearCompareButton != null)
        {
            clearCompareButton.onClick.AddListener(ClearComparisonSelection);
        }

        // Configurar dropdowns
        if (compareMetricDropdown != null)
        {
            compareMetricDropdown.ClearOptions();
            List<string> metrics = new List<string> { "Mejor Fitness", "Fitness Promedio", "Peor Fitness" };
            compareMetricDropdown.AddOptions(metrics);
            compareMetricDropdown.onValueChanged.AddListener(_ => UpdateComparisonGraph());
        }

        if (sortNetworksDropdown != null)
        {
            sortNetworksDropdown.ClearOptions();
            List<string> sortOptions = new List<string>
            {
                "Mejor Fitness Primero",
                "Peor Fitness Primero",
                "Mas Pesos Activos",
                "Menos Pesos Activos"
            };
            sortNetworksDropdown.AddOptions(sortOptions);
            sortNetworksDropdown.onValueChanged.AddListener(_ => SortAndFilterNetworks());
        }

        // Configurar slider de filtro
        if (fitnessFilterSlider != null)
        {
            fitnessFilterSlider.onValueChanged.AddListener(OnFitnessFilterChanged);
        }
    }
    #endregion

    #region Metodos de Gestion de Pestanas
    /// <summary>
    /// Activa una pestana especifica del dashboard, actualizando la UI correspondiente.
    /// </summary>
    /// <param name="tabIndex">El indice de la pestana a activar.</param>
    private void SetActiveTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabPanels.Length) return;

        currentTabIndex = tabIndex;

        // Desactivar todos los paneles
        foreach (var panel in tabPanels)
        {
            panel.SetActive(false);
        }

        // Activar el panel seleccionado
        tabPanels[tabIndex].SetActive(true);

        // Cambiar estilo de los botones
        for (int i = 0; i < tabButtons.Length; i++)
        {
            Image btnImage = tabButtons[i].GetComponent<Image>();
            if (btnImage != null)
            {
                btnImage.color = i == tabIndex ? new Color(0.8f, 0.8f, 1f) : new Color(0.6f, 0.6f, 0.6f);
            }

            TextMeshProUGUI btnText = tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.fontStyle = i == tabIndex ? FontStyles.Bold : FontStyles.Normal;
            }
        }

        // Actualizar la pestana seleccionada
        UpdateCurrentTabContent();
    }

    /// <summary>
    /// Llama al metodo de actualizacion correspondiente a la pestana activa.
    /// </summary>
    private void UpdateCurrentTabContent()
    {
        switch (currentTabIndex)
        {
            case 0: // Generaciones
                // Contenido ya actualizado por TrainingVisualizer
                break;

            case 1: // Redes
                UpdateNetworksPanel();
                break;

            case 2: // Comparacion
                RefreshComparisonFiles();
                break;

            case 3: // Informacion
                UpdateInfoPanel();
                break;
        }
    }
    #endregion

    #region Metodos de Actualizacion de Datos
    /// <summary>
    /// Refresca los datos del dashboard, actualizando el visualizador principal y la informacion del archivo.
    /// </summary>
    public void RefreshDashboard()
    {
        // Actualizar visualizador de entrenamiento
        if (trainingVisualizer != null)
        {
            trainingVisualizer.RefreshVisualization();
        }

        // Actualizar informacion de archivo actual
        UpdateCurrentFileInfo();

        // Actualizar panel de comparacion
        RefreshComparisonFiles();
    }

    /// <summary>
    /// Actualiza el texto de la UI que muestra la informacion del archivo de entrenamiento cargado.
    /// </summary>
    private void UpdateCurrentFileInfo()
    {
        if (currentFileText == null) return;

        if (trainingVisualizer != null && trainingVisualizer.loadedTrainingDataList.Count > 0)
        {
            var latestData = trainingVisualizer.loadedTrainingDataList.Last();
            currentFileText.text = $"Generacion {latestData.generation} - {latestData.timestamp}";
        }
        else
        {
            currentFileText.text = "Ningun archivo cargado";
        }
    }

    /// <summary>
    /// Actualiza el panel de redes, filtrando, ordenando y mostrando estadisticas.
    /// </summary>
    private void UpdateNetworksPanel()
    {
        if (trainingVisualizer == null ||
            trainingVisualizer.loadedTrainingDataList.Count == 0 ||
            networkAnalyzer == null) return;

        // Obtener el archivo mas reciente
        TrainingData latestData = trainingVisualizer.loadedTrainingDataList.Last();

        if (latestData.networks == null || latestData.networks.Count == 0)
        {
            if (networkStatsText != null)
            {
                networkStatsText.text = "No hay redes disponibles para analizar.";
            }
            return;
        }

        // Filtrar y ordenar redes
        SortAndFilterNetworks();

        // Mostrar estadisticas
        if (networkStatsText != null)
        {
            UpdateNetworkStatsText(latestData);
        }
    }

    /// <summary>
    /// Genera y muestra el texto con las estadisticas detalladas de las redes neuronales.
    /// </summary>
    /// <param name="data">Los datos de entrenamiento a partir de los cuales generar las estadisticas.</param>
    private void UpdateNetworkStatsText(TrainingData data)
    {
        string stats = $"<b>Estadisticas de Redes Neuronales</b>\n\n";
        stats += $"Total de redes: {data.networks.Count}\n";
        stats += $"Redes filtradas: {filteredNetworks.Count}\n";
        stats += $"Poblacion original: {data.populationSize}\n";
        stats += $"NPCs supervivientes: {data.aliveNPCs}\n\n";

        if (data.networks.Count > 0)
        {
            // ===== USAR CAMPOS CALCULADOS DEL JSON =====
            stats += $"<b>Distribucion de Fitness:</b>\n";
            stats += $"Promedio: {data.averageFitness:F2}\n";
            stats += $"Maximo: {data.bestFitness:F2}\n";
            stats += $"Minimo: {data.worstFitness:F2}\n";
            stats += $"Rango: {data.fitnessRange:F2}\n";
            stats += $"Diversidad: {data.diversityIndex:F2}\n\n";

            // ===== ESTADISTICAS DE COMPORTAMIENTO =====
            stats += $"<b>Comportamiento Promedio:</b>\n";
            stats += $"Tiempo vivo: {data.averageTimeAlive:F1}s\n";

            if (data.networks.Count > 0)
            {
                float avgDistance = data.networks.Average(n => n.totalDistance);
                float avgCheckpoints = (float)data.networks.Average(n => n.checkpointsReached);
                float avgExploration = (float)data.networks.Average(n => n.uniqueAreasVisited);

                stats += $"Distancia promedio: {avgDistance:F1}\n";
                stats += $"Checkpoints promedio: {avgCheckpoints:F1}\n";
                stats += $"Exploracion promedio: {avgExploration:F1}\n";
            }

            // ===== ANALISIS DE COMPLEJIDAD =====
            var networksWithWeights = data.networks.Where(n => n.flattenedWeights != null).ToList();

            if (networksWithWeights.Count > 0)
            {
                float avgWeights = (float)networksWithWeights.Average(n => n.flattenedWeights.Count);
                float avgActiveWeights = (float)networksWithWeights.Average(n => n.activeWeightsCount);
                float avgComplexity = networksWithWeights.Average(n => n.weightComplexity);

                stats += $"\n<b>Complejidad de Redes:</b>\n";
                stats += $"Pesos promedio: {avgWeights:F0}\n";
                stats += $"Pesos activos promedio: {avgActiveWeights:F0}\n";
                stats += $"Complejidad promedio: {avgComplexity * 100:F1}%\n";
            }

            // ===== ANALISIS DE SALTOS =====
            int totalCorrect = data.networks.Sum(n => n.correctJumps);
            int totalIncorrect = data.networks.Sum(n => n.incorrectJumps);

            if (totalCorrect + totalIncorrect > 0)
            {
                float accuracy = (float)totalCorrect / (totalCorrect + totalIncorrect) * 100f;
                stats += $"\n<b>Analisis de Saltos:</b>\n";
                stats += $"Saltos correctos: {totalCorrect}\n";
                stats += $"Saltos incorrectos: {totalIncorrect}\n";
                stats += $"Precision general: {accuracy:F1}%\n";
            }
        }

        networkStatsText.text = stats;
    }

    /// <summary>
    /// Actualiza el panel de informacion. (Implementacion pendiente)
    /// </summary>
    private void UpdateInfoPanel()
    {
        // Aqui se puede anadir informacion sobre el sistema, guias de uso, etc.
    }
    #endregion

    #region Metodos de Filtrado y Ordenamiento
    /// <summary>
    /// Metodo llamado cuando el valor del slider de filtro de fitness cambia.
    /// </summary>
    /// <param name="value">El nuevo valor del slider.</param>
    private void OnFitnessFilterChanged(float value)
    {
        currentFitnessFilter = value;

        if (fitnessFilterText != null)
        {
            fitnessFilterText.text = $"Fitness minimo: {value:F1}";
        }

        SortAndFilterNetworks();
    }

    /// <summary>
    /// Aplica los criterios de ordenamiento y filtrado a la lista de redes neuronales.
    /// </summary>
    private void SortAndFilterNetworks()
    {
        if (trainingVisualizer == null ||
            trainingVisualizer.loadedTrainingDataList.Count == 0) return;

        TrainingData latestData = trainingVisualizer.loadedTrainingDataList.Last();

        if (latestData.networks == null)
        {
            filteredNetworks.Clear();
            return;
        }

        // Filtrar por fitness minimo
        var filtered = latestData.networks.Where(n => n.fitness >= currentFitnessFilter).ToList();

        // Ordenar segun criterio seleccionado
        if (sortNetworksDropdown != null)
        {
            switch (sortNetworksDropdown.value)
            {
                case 0: // Mejor fitness primero
                    filtered = filtered.OrderByDescending(n => n.fitness).ToList();
                    break;

                case 1: // Peor fitness primero
                    filtered = filtered.OrderBy(n => n.fitness).ToList();
                    break;

                case 2: // Mas pesos activos
                    filtered = filtered.OrderByDescending(n =>
                        n.flattenedWeights?.Count(w => Mathf.Abs(w) > 0.01f) ?? 0).ToList();
                    break;

                case 3: // Menos pesos activos
                    filtered = filtered.OrderBy(n =>
                        n.flattenedWeights?.Count(w => Mathf.Abs(w) > 0.01f) ?? 0).ToList();
                    break;
            }
        }

        filteredNetworks = filtered;

        // Actualizar visualizador de redes
        if (networkAnalyzer != null)
        {
            networkAnalyzer.SetNetworks(filteredNetworks);
        }
    }
    #endregion

    #region Metodos de Sistema de Comparacion
    /// <summary>
    /// Refresca la lista de archivos disponibles para comparacion en el menu desplegable.
    /// </summary>
    private void RefreshComparisonFiles()
    {
        if (compareFileDropdown == null) return;

        string fullPath = GetTrainingDataPath();

        if (!Directory.Exists(fullPath)) return;

        string[] files = Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        // Actualizar dropdown
        compareFileDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (string file in files)
        {
            string fileName = Path.GetFileName(file);
            string displayName = fileName;

            try
            {
                string json = File.ReadAllText(file);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    displayName = $"Gen {data.generation} - Mejor: {data.bestFitness:F1}";
                }
            }
            catch (Exception) { }

            options.Add(displayName);
        }

        compareFileDropdown.AddOptions(options);
        UpdateSelectedFilesText();
    }

    /// <summary>
    /// Obtiene la ruta a la carpeta de datos de entrenamiento basandose en la configuracion.
    /// </summary>
    /// <returns>La ruta completa a la carpeta de datos.</returns>
    private string GetTrainingDataPath()
    {
        if (trainingVisualizer.loadFromProjectFolder)
        {
            return Path.Combine(Application.dataPath, trainingVisualizer.projectLoadFolder);
        }
        else
        {
            return Path.Combine(Application.persistentDataPath, trainingVisualizer.loadFolder);
        }
    }

    /// <summary>
    /// Anade el archivo seleccionado en el dropdown a la lista de comparacion.
    /// </summary>
    private void AddFileToComparison()
    {
        if (compareFileDropdown == null) return;

        int index = compareFileDropdown.value;
        string fullPath = GetTrainingDataPath();
        string[] files = Directory.GetFiles(fullPath, "*.json");
        files = files.OrderByDescending(f => new FileInfo(f).LastWriteTime).ToArray();

        if (index < 0 || index >= files.Length) return;

        string selectedFile = files[index];

        // Evitar duplicados
        if (!filesToCompare.Contains(selectedFile))
        {
            filesToCompare.Add(selectedFile);

            // Cargar el archivo
            try
            {
                string json = File.ReadAllText(selectedFile);
                TrainingData data = JsonUtility.FromJson<TrainingData>(json);
                if (data != null)
                {
                    loadedComparisonData.Add(data);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error al cargar archivo para comparacion: {e.Message}");
            }

            UpdateSelectedFilesText();
            UpdateComparisonGraph();
        }
    }

    /// <summary>
    /// Actualiza el texto de la UI que muestra la lista de archivos seleccionados para comparar.
    /// </summary>
    private void UpdateSelectedFilesText()
    {
        if (selectedFilesText == null) return;

        if (filesToCompare.Count == 0)
        {
            selectedFilesText.text = "Ningun archivo seleccionado para comparar.";
            return;
        }

        string text = "<b>Archivos seleccionados:</b>\n";

        for (int i = 0; i < filesToCompare.Count; i++)
        {
            string file = Path.GetFileName(filesToCompare[i]);

            if (i < loadedComparisonData.Count && loadedComparisonData[i] != null)
            {
                text += $"{i + 1}. Gen {loadedComparisonData[i].generation} - Mejor: {loadedComparisonData[i].bestFitness:F1}\n";
            }
            else
            {
                text += $"{i + 1}. {file}\n";
            }
        }

        selectedFilesText.text = text;
    }

    /// <summary>
    /// Genera o actualiza la grafica de barras para comparar los archivos seleccionados.
    /// </summary>
    private void UpdateComparisonGraph()
    {
        ClearComparisonGraph();

        if (loadedComparisonData.Count < 2) return;

        int metricIndex = compareMetricDropdown != null ? compareMetricDropdown.value : 0;

        // Crear barras para cada archivo
        float barWidth = 60f;
        float spacing = 20f;
        float totalWidth = (barWidth + spacing) * loadedComparisonData.Count;
        float startX = -totalWidth / 2 + barWidth / 2;

        // Encontrar valor maximo para escalar
        float maxValue = 0;

        foreach (var data in loadedComparisonData)
        {
            float value = GetComparisonValue(data, metricIndex);
            if (value > maxValue) maxValue = value;
        }

        float maxBarHeight = 200f;

        List<GameObject> elements = new List<GameObject>();
        Color[] barColors = {
            new Color(0.2f, 0.6f, 1f),
            new Color(0.2f, 0.8f, 0.2f),
            new Color(1f, 0.6f, 0.2f),
            new Color(0.8f, 0.2f, 0.8f),
            new Color(1f, 0.8f, 0.2f)
        };

        for (int i = 0; i < loadedComparisonData.Count; i++)
        {
            var data = loadedComparisonData[i];
            float value = GetComparisonValue(data, metricIndex);

            float normalizedHeight = maxValue > 0 ? value / maxValue : 0;
            float barHeight = normalizedHeight * maxBarHeight;

            // Crear barra
            GameObject barObj = new GameObject($"Bar_{i}");
            barObj.transform.SetParent(comparisonGraphContainer, false);

            RectTransform barRect = barObj.AddComponent<RectTransform>();
            barRect.sizeDelta = new Vector2(barWidth, barHeight);
            barRect.anchoredPosition = new Vector2(startX + (barWidth + spacing) * i, barHeight / 2);

            Image barImage = barObj.AddComponent<Image>();
            barImage.color = barColors[i % barColors.Length];

            elements.Add(barObj);

            // Etiqueta de valor
            CreateBarLabel(barObj, value.ToString("F1"), new Vector2(0, barHeight / 2 + 15));

            // Etiqueta de generacion
            CreateBarLabel(barObj, $"Gen {data.generation}", new Vector2(0, -15));
        }

        comparisonGraphElements["bars"] = elements;
    }

    /// <summary>
    /// Obtiene el valor de una metrica especifica (mejor, promedio, peor fitness) desde un objeto TrainingData.
    /// </summary>
    /// <param name="data">El objeto de datos de entrenamiento.</param>
    /// <param name="metricIndex">El indice de la metrica (0: Mejor, 1: Promedio, 2: Peor).</param>
    /// <returns>El valor de la metrica solicitada.</returns>
    private float GetComparisonValue(TrainingData data, int metricIndex)
    {
        switch (metricIndex)
        {
            case 0: return data.bestFitness;
            case 1: return data.averageFitness;
            case 2: return data.worstFitness;
            default: return data.bestFitness;
        }
    }

    /// <summary>
    /// Crea una etiqueta de texto (TextMeshPro) como hija de un objeto en la grafica de comparacion.
    /// </summary>
    /// <param name="parent">El objeto padre para la etiqueta.</param>
    /// <param name="text">El texto a mostrar en la etiqueta.</param>
    /// <param name="position">La posicion anclada de la etiqueta relativa al padre.</param>
    private void CreateBarLabel(GameObject parent, string text, Vector2 position)
    {
        GameObject labelObj = new GameObject($"Label_{text}");
        labelObj.transform.SetParent(parent.transform, false);

        RectTransform labelRect = labelObj.AddComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(80, 20);
        labelRect.anchoredPosition = position;

        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        labelText.text = text;
        labelText.fontSize = 10;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
    }

    /// <summary>
    /// Elimina todos los elementos visuales de la grafica de comparacion.
    /// </summary>
    private void ClearComparisonGraph()
    {
        foreach (var elements in comparisonGraphElements.Values)
        {
            foreach (var element in elements)
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
        }

        comparisonGraphElements.Clear();
    }

    /// <summary>
    /// Limpia la seleccion de archivos para comparar y resetea la grafica.
    /// </summary>
    public void ClearComparisonSelection()
    {
        filesToCompare.Clear();
        loadedComparisonData.Clear();
        UpdateSelectedFilesText();
        ClearComparisonGraph();
    }
    #endregion
}