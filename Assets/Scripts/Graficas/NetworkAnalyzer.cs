using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// Analizador y visualizador de redes neuronales entrenadas, adaptado al proyecto de NPCs.
/// Permite visualizar la estructura, pesos y conexiones de las redes neuronales
/// con capacidades de filtrado y analisis detallado.
/// </summary>
public class NetworkAnalyzer : MonoBehaviour
{
    #region Variables Publicas - Referencias UI

    [Header("Referencias UI")]
    [Tooltip("Panel contenedor donde se dibujara la visualizacion de la red neuronal.")]
    public RectTransform networkContainer;

    [Tooltip("Prefab usado para crear las representaciones visuales de cada neurona.")]
    public GameObject neuronPrefab;

    [Tooltip("Prefab usado para crear las lineas que representan conexiones entre neuronas.")]
    public GameObject connectionPrefab;

    [Tooltip("Menu desplegable para seleccionar cual red neuronal visualizar.")]
    public TMP_Dropdown networkDropdown;

    [Tooltip("Texto donde se muestran los detalles tecnicos y estadisticas de la red seleccionada.")]
    public TextMeshProUGUI networkDetailsText;

    #endregion

    #region Variables Publicas - Configuracion Visual

    [Header("Configuracion de Visualizacion")]
    [Tooltip("Distancia horizontal en pixeles entre cada capa de neuronas.")]
    public float layerDistance = 150f;

    [Tooltip("Distancia vertical en pixeles entre neuronas de la misma capa.")]
    public float neuronDistance = 60f;

    [Tooltip("Multiplicador para el grosor de las conexiones basado en su peso.")]
    public float weightScale = 3f;

    [Tooltip("Color usado para mostrar conexiones con pesos positivos.")]
    public Color positiveWeightColor = Color.green;

    [Tooltip("Color usado para mostrar conexiones con pesos negativos.")]
    public Color negativeWeightColor = Color.red;

    #endregion

    #region Variables Publicas - Opciones de Analisis

    [Header("Opciones de Analisis")]
    [Tooltip("Valor minimo (en absoluto) para considerar un peso como significativo.")]
    public float significantWeightThreshold = 0.2f;

    [Tooltip("Si esta activo, solo se muestran conexiones con pesos superiores al umbral.")]
    public bool showOnlySignificantConnections = false;

    [Tooltip("Si esta activo, muestra etiquetas de texto identificando cada neurona.")]
    public bool showNeuronLabels = true;

    #endregion

    #region Variables Privadas

    /// <summary>
    /// Referencia al visualizador principal de entrenamiento.
    /// </summary>
    private TrainingVisualizer visualizer;

    /// <summary>
    /// Lista de todas las redes serializadas disponibles y cargadas para el analisis.
    /// </summary>
    private List<SerializedNetwork> currentNetworks = new List<SerializedNetwork>();

    /// <summary>
    /// La red neuronal actualmente seleccionada que se esta visualizando.
    /// </summary>
    private SerializedNetwork selectedNetwork;

    /// <summary>
    /// Diccionario para mantener un registro de los elementos de UI creados (neuronas, conexiones) para una limpieza facil.
    /// </summary>
    private Dictionary<string, List<GameObject>> networkElements = new Dictionary<string, List<GameObject>>();

    #endregion

    #region Constantes

    /// <summary>
    /// Nombres por defecto para las neuronas de la capa de entrada.
    /// </summary>
    private readonly string[] inputNeuronNames = {
        "Sensor Izq", "Sensor Izq-Centro", "Sensor Centro",
        "Sensor Der-Centro", "Sensor Der", "Sensor Bajo",
        "Sensor Alto", "Constante (Bias)"
    };

    /// <summary>
    /// Nombres por defecto para las neuronas de la capa de salida.
    /// </summary>
    private readonly string[] outputNeuronNames = {
        "Avanzar", "Girar Izq", "Girar Der", "Saltar"
    };

    #endregion

    #region Ciclo de Vida de Unity

    /// <summary>
    /// Metodo llamado cuando la instancia del script se carga.
    /// Inicializa referencias y configura los listeners de eventos de la UI.
    /// </summary>
    void Start()
    {
        visualizer = FindObjectOfType<TrainingVisualizer>();

        if (networkDropdown != null)
        {
            networkDropdown.onValueChanged.AddListener(OnNetworkSelected);
        }
    }

    #endregion

    #region Metodos Publicos Principales

    /// <summary>
    /// Establece la lista de redes neuronales para analizar y actualiza la interfaz de usuario.
    /// </summary>
    /// <param name="networks">La lista de redes serializadas a mostrar.</param>
    public void SetNetworks(List<SerializedNetwork> networks)
    {
        currentNetworks = networks;
        PopulateNetworkDropdown();

        if (currentNetworks.Count > 0)
        {
            networkDropdown.value = 0;
            OnNetworkSelected(0);
        }
        else
        {
            ClearNetworkVisualization();
            if (networkDetailsText != null)
            {
                networkDetailsText.text = "No hay redes disponibles para analizar.";
            }
        }
    }

    /// <summary>
    /// Activa o desactiva el filtro para mostrar solo conexiones significativas.
    /// </summary>
    /// <param name="value">El nuevo estado del filtro.</param>
    public void ToggleSignificantConnectionsOnly(bool value)
    {
        showOnlySignificantConnections = value;
        if (selectedNetwork != null)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }

    /// <summary>
    /// Establece el umbral minimo para considerar un peso como significativo.
    /// </summary>
    /// <param name="value">El nuevo valor del umbral.</param>
    public void SetSignificantWeightThreshold(float value)
    {
        significantWeightThreshold = value;
        if (selectedNetwork != null && showOnlySignificantConnections)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }

    /// <summary>
    /// Activa o desactiva la visualizacion de etiquetas en las neuronas.
    /// </summary>
    /// <param name="value">El nuevo estado de visibilidad de las etiquetas.</param>
    public void ToggleNeuronLabels(bool value)
    {
        showNeuronLabels = value;
        if (selectedNetwork != null)
        {
            VisualizeNetwork(selectedNetwork);
        }
    }

    #endregion

    #region Metodos Privados - Inicializacion

    /// <summary>
    /// Rellena el menu desplegable con las redes disponibles para su analisis.
    /// </summary>
    private void PopulateNetworkDropdown()
    {
        if (networkDropdown == null) return;

        networkDropdown.ClearOptions();
        List<string> options = new List<string>();

        for (int i = 0; i < currentNetworks.Count; i++)
        {
            SerializedNetwork network = currentNetworks[i];
            options.Add($"Red #{i + 1} - Fitness: {network.fitness:F2}");
        }

        networkDropdown.AddOptions(options);
        networkDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Gestiona el evento que se dispara cuando se selecciona una nueva red del menu desplegable.
    /// </summary>
    /// <param name="index">El indice de la red seleccionada.</param>
    private void OnNetworkSelected(int index)
    {
        if (index < 0 || index >= currentNetworks.Count) return;

        selectedNetwork = currentNetworks[index];
        VisualizeNetwork(selectedNetwork);
        UpdateNetworkDetails();
    }

    #endregion

    #region Metodos Privados - Visualizacion Principal

    /// <summary>
    /// Metodo principal que orquesta la visualizacion de una red neuronal especifica.
    /// </summary>
    /// <param name="network">Los datos de la red serializada a visualizar.</param>
    private void VisualizeNetwork(SerializedNetwork network)
    {
        ClearNetworkVisualization();

        if (network == null || network.layers == null || network.flattenedWeights == null)
        {
            Debug.LogWarning("Red neuronal invalida o incompleta.");
            return;
        }

        List<GameObject> elements = new List<GameObject>();
        Dictionary<string, GameObject> neurons = new Dictionary<string, GameObject>();

        float[][][] weights = RebuildWeights(network.flattenedWeights, network.layers);

        if (weights == null)
        {
            Debug.LogError("Error al reconstruir los pesos de la red.");
            return;
        }

        CreateNeuronVisuals(network, elements, neurons);
        CreateConnectionVisuals(weights, neurons, elements);

        networkElements["current"] = elements;
    }

    /// <summary>
    /// Crea y posiciona los objetos de UI para todas las neuronas de la red.
    /// </summary>
    /// <param name="network">La red a visualizar.</param>
    /// <param name="elements">Lista para almacenar los GameObjects creados.</param>
    /// <param name="neurons">Diccionario para mapear IDs de neuronas a sus GameObjects.</param>
    private void CreateNeuronVisuals(SerializedNetwork network, List<GameObject> elements, Dictionary<string, GameObject> neurons)
    {
        for (int layer = 0; layer < network.layers.Length; layer++)
        {
            int neuronsInLayer = network.layers[layer];
            float layerWidth = layerDistance * layer;
            float layerHeight = (neuronsInLayer - 1) * neuronDistance;
            float startY = -layerHeight / 2;

            for (int neuronIdx = 0; neuronIdx < neuronsInLayer; neuronIdx++)
            {
                float xPos = layerWidth;
                float yPos = startY + (neuronIdx * neuronDistance);

                GameObject neuronObj = Instantiate(neuronPrefab, networkContainer);
                neuronObj.transform.localPosition = new Vector3(xPos, yPos, 0);

                string neuronId = $"{layer}_{neuronIdx}";
                neurons[neuronId] = neuronObj;

                ConfigureNeuronAppearance(neuronObj, layer);
                CreateNeuronLabel(neuronObj, layer, neuronIdx, elements);

                elements.Add(neuronObj);
            }
        }
    }

    /// <summary>
    /// Configura la apariencia visual (color) de un objeto de neurona segun su capa.
    /// </summary>
    /// <param name="neuronObj">El GameObject de la neurona.</param>
    /// <param name="layer">El indice de la capa a la que pertenece la neurona.</param>
    private void ConfigureNeuronAppearance(GameObject neuronObj, int layer)
    {
        Image neuronImage = neuronObj.GetComponent<Image>();
        if (neuronImage != null)
        {
            // Asigna un color diferente para la capa de entrada, las ocultas y la de salida
            if (layer == 0)
            {
                neuronImage.color = Color.cyan; // Entradas
            }
            else if (layer == selectedNetwork.layers.Length - 1)
            {
                neuronImage.color = Color.yellow; // Salidas
            }
            else
            {
                neuronImage.color = Color.white; // Capas ocultas
            }
        }
    }

    /// <summary>
    /// Crea y configura la etiqueta de texto para un objeto de neurona.
    /// </summary>
    /// <param name="neuronObj">El GameObject de la neurona padre.</param>
    /// <param name="layer">El indice de la capa.</param>
    /// <param name="neuronIdx">El indice de la neurona dentro de la capa.</param>
    /// <param name="elements">Lista para almacenar el GameObject de la etiqueta creada.</param>
    private void CreateNeuronLabel(GameObject neuronObj, int layer, int neuronIdx, List<GameObject> elements)
    {
        if (!showNeuronLabels) return;

        GameObject labelObj = new GameObject($"Label_{layer}_{neuronIdx}");
        labelObj.transform.SetParent(neuronObj.transform, false);

        // 2. Asegurar que se renderiza por encima del Image de la neurona
        labelObj.transform.SetAsLastSibling();  

        // 3. Añadir TextMeshProUGUI y configurar el texto
        TextMeshProUGUI labelText = labelObj.AddComponent<TextMeshProUGUI>();
        string neuronName = GetNeuronName(layer, neuronIdx);
        ConfigureLabelText(labelText, neuronName);

        // 4. Posicionar la etiqueta (izq., der. o abajo según la capa)
        PositionLabel(labelObj.GetComponent<RectTransform>(), layer+120);

        // 5. Registrar en la lista para fácil limpieza
        elements.Add(labelObj);
    }

    /// <summary>
    /// Obtiene el nombre apropiado para una neurona basado en su capa e indice.
    /// </summary>
    /// <param name="layer">Indice de la capa.</param>
    /// <param name="neuronIdx">Indice de la neurona.</param>
    /// <returns>El nombre descriptivo de la neurona.</returns>
    private string GetNeuronName(int layer, int neuronIdx)
    {
        if (layer == 0 && neuronIdx < inputNeuronNames.Length)
        {
            return inputNeuronNames[neuronIdx];
        }
        else if (layer == selectedNetwork.layers.Length - 1 && neuronIdx < outputNeuronNames.Length)
        {
            return outputNeuronNames[neuronIdx];
        }
        else
        {
            return $"H{layer}_{neuronIdx}"; // Neurona de capa oculta
        }
    }

    /// <summary>
    /// Configura las propiedades del componente TextMeshProUGUI para la etiqueta de una neurona.
    /// </summary>
    /// <param name="labelText">El componente de texto a configurar.</param>
    /// <param name="neuronName">El texto a mostrar.</param>
    private void ConfigureLabelText(TextMeshProUGUI labelText, string neuronName)
    {
        labelText.text = neuronName;
        labelText.fontSize = 10;
        labelText.alignment = TextAlignmentOptions.Center;
        labelText.color = Color.white;
    }

    /// <summary>
    /// Establece la posicion de la etiqueta de una neurona relativa a la propia neurona.
    /// </summary>
    /// <param name="labelRect">El RectTransform de la etiqueta.</param>
    /// <param name="layer">El indice de la capa de la neurona.</param>
    private void PositionLabel(RectTransform labelRect, int layer)
    {
        labelRect.sizeDelta = new Vector2(100, 20); // Ancho para evitar recortes
        if (layer == 0)
        {
            // Etiquetas de entrada a la izquierda
            labelRect.localPosition = new Vector3(-60, 0, 0);
            labelRect.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Right;
        }
        else if (layer == selectedNetwork.layers.Length - 1)
        {
            // Etiquetas de salida a la derecha
            labelRect.localPosition = new Vector3(60, 0, 0);
            labelRect.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Left;
        }
        else
        {
            // Etiquetas de capas ocultas abajo
            labelRect.localPosition = new Vector3(0, -25, 0);
        }
    }

    #endregion

    #region Metodos Privados - Visualizacion de Conexiones

    /// <summary>
    /// Crea las lineas de UI que representan las conexiones entre todas las neuronas.
    /// </summary>
    /// <param name="weights">La matriz 3D de pesos.</param>
    /// <param name="neurons">Diccionario que mapea IDs de neuronas a sus GameObjects.</param>
    /// <param name="elements">Lista para almacenar los GameObjects de las conexiones creadas.</param>
    private void CreateConnectionVisuals(float[][][] weights, Dictionary<string, GameObject> neurons, List<GameObject> elements)
    {
        for (int layer = 0; layer < weights.Length; layer++)
        {
            for (int neuronFrom = 0; neuronFrom < weights[layer].Length; neuronFrom++)
            {
                for (int neuronTo = 0; neuronTo < weights[layer][neuronFrom].Length; neuronTo++)
                {
                    float weight = weights[layer][neuronFrom][neuronTo];

                    if (ShouldSkipConnection(weight)) continue;

                    string fromId = $"{layer}_{neuronFrom}";
                    string toId = $"{layer + 1}_{neuronTo}";

                    if (!neurons.ContainsKey(fromId) || !neurons.ContainsKey(toId)) continue;

                    GameObject fromNeuron = neurons[fromId];
                    GameObject toNeuron = neurons[toId];

                    CreateConnectionLine(fromNeuron, toNeuron, weight, elements);
                }
            }
        }
    }

    /// <summary>
    /// Determina si una conexion debe ser omitida basado en la configuracion actual del filtro.
    /// </summary>
    /// <param name="weight">El peso de la conexion.</param>
    /// <returns>True si la conexion debe omitirse, de lo contrario False.</returns>
    private bool ShouldSkipConnection(float weight)
    {
        return showOnlySignificantConnections && Mathf.Abs(weight) < significantWeightThreshold;
    }

    /// <summary>
    /// Crea una unica linea de UI para una conexion entre dos neuronas.
    /// </summary>
    /// <param name="fromNeuron">GameObject de la neurona de origen.</param>
    /// <param name="toNeuron">GameObject de la neurona de destino.</param>
    /// <param name="weight">El peso de la conexion.</param>
    /// <param name="elements">Lista para almacenar el GameObject de la conexion creada.</param>
    private void CreateConnectionLine(GameObject fromNeuron, GameObject toNeuron, float weight, List<GameObject> elements)
    {
        GameObject connection = Instantiate(connectionPrefab, networkContainer);
        RectTransform connRect = connection.GetComponent<RectTransform>();

        Vector2 fromPos = fromNeuron.transform.localPosition;
        Vector2 toPos = toNeuron.transform.localPosition;

        ConfigureConnectionTransform(connRect, fromPos, toPos, weight);
        ConfigureConnectionAppearance(connection.GetComponent<Image>(), weight);

        elements.Add(connection);
    }

    /// <summary>
    /// Establece la posicion, rotacion y tamaño del RectTransform de una conexion.
    /// </summary>
    /// <param name="connRect">El RectTransform a configurar.</param>
    /// <param name="fromPos">Posicion de la neurona de origen.</param>
    /// <param name="toPos">Posicion de la neurona de destino.</param>
    /// <param name="weight">Peso de la conexion.</param>
    private void ConfigureConnectionTransform(RectTransform connRect, Vector2 fromPos, Vector2 toPos, float weight)
    {
        Vector2 midPoint = (fromPos + toPos) / 2;
        float distance = Vector2.Distance(fromPos, toPos);

        connRect.localPosition = new Vector3(midPoint.x, midPoint.y, 0);

        float thickness = Mathf.Abs(weight) * weightScale;
        thickness = Mathf.Clamp(thickness, 0.5f, 8f);
        connRect.sizeDelta = new Vector2(distance, thickness);

        float angle = Mathf.Atan2(toPos.y - fromPos.y, toPos.x - fromPos.x) * Mathf.Rad2Deg;
        connRect.localRotation = Quaternion.Euler(0, 0, angle);
    }

    /// <summary>
    /// Establece el color y la transparencia de la imagen de una conexion basado en su peso.
    /// </summary>
    /// <param name="connImage">El componente Image de la conexion.</param>
    /// <param name="weight">El peso de la conexion.</param>
    private void ConfigureConnectionAppearance(Image connImage, float weight)
    {
        if (connImage == null) return;

        connImage.color = weight >= 0 ? positiveWeightColor : negativeWeightColor;

        Color color = connImage.color;
        float alpha = Mathf.Clamp01(Mathf.Abs(weight) / 2f);
        alpha = Mathf.Max(0.1f, alpha); // Asegura una minima visibilidad
        connImage.color = new Color(color.r, color.g, color.b, alpha);
    }

    #endregion

    #region Metodos Privados - Utilidades

    /// <summary>
    /// Reconstruye la matriz de pesos 3D a partir de una lista aplanada de pesos.
    /// </summary>
    /// <param name="flatWeights">La lista de pesos aplanada.</param>
    /// <param name="layers">El array con el numero de neuronas por capa.</param>
    /// <returns>Una matriz de pesos 3D (capa, neurona_origen, neurona_destino).</returns>
    private float[][][] RebuildWeights(List<float> flatWeights, int[] layers)
    {
        if (flatWeights == null || flatWeights.Count == 0 || layers == null || layers.Length < 2)
        {
            return null;
        }

        float[][][] weights = new float[layers.Length - 1][][];
        int weightIndex = 0;

        try
        {
            for (int i = 0; i < layers.Length - 1; i++)
            {
                weights[i] = new float[layers[i]][];
                for (int j = 0; j < layers[i]; j++)
                {
                    weights[i][j] = new float[layers[i + 1]];
                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        if (weightIndex < flatWeights.Count)
                        {
                            weights[i][j][k] = flatWeights[weightIndex++];
                        }
                    }
                }
            }
            return weights;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al reconstruir los pesos: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Actualiza el panel de texto con los detalles y estadisticas de la red seleccionada.
    /// </summary>
    private void UpdateNetworkDetails()
    {
        if (networkDetailsText == null || selectedNetwork == null) return;

        string details = "<b>Detalles de la Red Neuronal</b>\n\n";
        details += BuildBasicInfo();
        details += BuildWeightAnalysis();
        details += BuildBehaviorAnalysis();

        networkDetailsText.text = details;
    }

    /// <summary>
    /// Construye el bloque de texto con la informacion basica de la red.
    /// </summary>
    /// <returns>Una cadena con la informacion basica.</returns>
    private string BuildBasicInfo()
    {
        string info = $"Fitness: {selectedNetwork.fitness:F2}\n";
        info += $"Tiempo vivo: {selectedNetwork.timeAlive:F1}s\n";
        info += $"Distancia recorrida: {selectedNetwork.totalDistance:F1}\n";
        info += $"Saltos exitosos: {selectedNetwork.successfulJumps}\n";
        info += $"Checkpoints alcanzados: {selectedNetwork.checkpointsReached}\n";
        info += $"Areas exploradas: {selectedNetwork.uniqueAreasVisited}\n\n";

        if (selectedNetwork.layers != null)
        {
            info += $"Estructura: {string.Join("-", selectedNetwork.layers)}\n";
            info += $"Total de capas: {selectedNetwork.layers.Length}\n";
            info += $"Total de neuronas: {selectedNetwork.layers.Sum()}\n\n";
        }
        return info;
    }

    /// <summary>
    /// Construye el bloque de texto con el analisis de los pesos de la red.
    /// </summary>
    /// <returns>Una cadena con el analisis de pesos.</returns>
    private string BuildWeightAnalysis()
    {
        if (selectedNetwork.flattenedWeights == null || selectedNetwork.flattenedWeights.Count == 0) return "";

        string analysis = $"<b>Analisis de Pesos:</b>\n";
        analysis += $"Total de pesos: {selectedNetwork.flattenedWeights.Count}\n";
        analysis += $"Pesos activos: {selectedNetwork.activeWeightsCount}\n";
        analysis += $"Complejidad de red: {selectedNetwork.weightComplexity * 100:F1}%\n";
        analysis += $"Pesos positivos: {selectedNetwork.flattenedWeights.Count(w => w > 0.01f)}\n";
        analysis += $"Pesos negativos: {selectedNetwork.flattenedWeights.Count(w => w < -0.01f)}\n";
        analysis += $"Peso promedio: {selectedNetwork.flattenedWeights.Average():F3}\n";
        analysis += $"Peso maximo: {selectedNetwork.flattenedWeights.Max():F3}\n";
        analysis += $"Peso minimo: {selectedNetwork.flattenedWeights.Min():F3}\n\n";

        return analysis;
    }

    /// <summary>
    /// Construye el bloque de texto con el analisis del comportamiento inferido de la red.
    /// </summary>
    /// <returns>Una cadena con el analisis de comportamiento.</returns>
    private string BuildBehaviorAnalysis()
    {
        string analysis = "<b>Analisis de Comportamiento:</b>\n";
        if (selectedNetwork.flattenedWeights != null && selectedNetwork.layers != null)
        {
            float[] outputInfluence = CalculateOutputInfluence();
            analysis += BuildInfluenceReport(outputInfluence);
        }
        return analysis;
    }

    /// <summary>
    /// Calcula la influencia total (suma de pesos absolutos) que llega a cada neurona de salida.
    /// </summary>
    /// <returns>Un array de floats donde cada elemento es la suma de pesos para una neurona de salida.</returns>
    private float[] CalculateOutputInfluence()
    {
        float[][][] weights = RebuildWeights(selectedNetwork.flattenedWeights, selectedNetwork.layers);
        if (weights == null || weights.Length == 0) return new float[0];

        int lastLayerIndex = weights.Length - 1;
        int outputNeuronCount = selectedNetwork.layers[selectedNetwork.layers.Length - 1];
        float[] outputWeightSums = new float[outputNeuronCount];
        int hiddenNeuronCount = selectedNetwork.layers[lastLayerIndex];

        for (int j = 0; j < hiddenNeuronCount; j++)
        {
            for (int k = 0; k < outputNeuronCount; k++)
            {
                outputWeightSums[k] += Mathf.Abs(weights[lastLayerIndex][j][k]);
            }
        }
        return outputWeightSums;
    }

    /// <summary>
    /// Construye un informe legible de la influencia de las neuronas de salida.
    /// </summary>
    /// <param name="outputInfluence">El array con la suma de pesos para cada neurona de salida.</param>
    /// <returns>Una cadena formateada con el informe.</returns>
    private string BuildInfluenceReport(float[] outputInfluence)
    {
        if (outputInfluence.Length == 0) return "";
        float totalWeightSum = outputInfluence.Sum();
        if (totalWeightSum == 0) return "Sin influencia medible en la salida.\n";

        string report = "";
        for (int i = 0; i < outputInfluence.Length && i < outputNeuronNames.Length; i++)
        {
            float percentage = (outputInfluence[i] / totalWeightSum) * 100f;
            report += $"{outputNeuronNames[i]}: {percentage:F1}% de influencia\n";
        }
        return report;
    }

    /// <summary>
    /// Destruye todos los GameObjects de la visualizacion actual (neuronas, conexiones, etiquetas) para limpiar la vista.
    /// </summary>
    private void ClearNetworkVisualization()
    {
        if (networkElements.ContainsKey("current"))
        {
            foreach (var element in networkElements["current"])
            {
                if (element != null)
                {
                    Destroy(element);
                }
            }
            networkElements["current"].Clear();
            networkElements.Remove("current");
        }
    }

    #endregion
}