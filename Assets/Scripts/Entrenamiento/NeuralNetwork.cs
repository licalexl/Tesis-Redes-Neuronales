using UnityEngine;
using System;

/// <summary>
/// Implementa una red neuronal multicapa (perceptron multicapa).
/// Esta red neuronal toma decisiones para los NPCs basandose en sus sensores.
/// </summary>
public class NeuralNetwork
{
    #region Variables Privadas de Estructura
    /// <summary>
    /// La estructura de la red: cantidad de neuronas por capa
    /// </summary>
    private int[] layers;

    /// <summary>
    /// Matriz para almacenar los valores de las neuronas
    /// Primera dimension: capa, Segunda dimension: neurona en esa capa
    /// </summary>
    private float[][] neurons;

    /// <summary>
    /// Matriz tridimensional para almacenar los pesos de las conexiones
    /// Primera dimension: capa de origen
    /// Segunda dimension: neurona de origen en esa capa
    /// Tercera dimension: neurona destino en la siguiente capa
    /// </summary>
    private float[][][] weights;
    #endregion

    #region Constructor
    /// <summary>
    /// Constructor que crea una nueva red neuronal con la estructura especificada.
    /// </summary>
    /// <param name="layers">Numero de neuronas en cada capa (entrada, ocultas, salida)</param>
    public NeuralNetwork(params int[] layers)
    {
        this.layers = layers;
        InitializeNeurons();
        InitializeWeights();
    }
    #endregion

    #region Metodos de Inicializacion
    /// <summary>
    /// Inicializa las matrices para almacenar los valores de las neuronas.
    /// </summary>
    private void InitializeNeurons()
    {
        // Creamos el array de arrays para las neuronas
        neurons = new float[layers.Length][];

        // Para cada capa, creamos un array del tamaño adecuado
        for (int i = 0; i < layers.Length; i++)
        {
            neurons[i] = new float[layers[i]];
        }
    }

    /// <summary>
    /// Inicializa los pesos de las conexiones con valores aleatorios.
    /// </summary>
    private void InitializeWeights()
    {
        try
        {
            // Creamos el array tridimensional para los pesos
            // El numero de capas de pesos es uno menos que el numero de capas de neuronas
            weights = new float[layers.Length - 1][][];

            // Para cada capa de pesos (entre capas de neuronas)
            for (int i = 0; i < layers.Length - 1; i++)
            {
                // Creamos un array de arrays para las neuronas de origen
                weights[i] = new float[layers[i]][];

                // Para cada neurona en la capa actual (origen)
                for (int j = 0; j < layers[i]; j++)
                {
                    // Creamos un array para los pesos de las conexiones a la siguiente capa
                    weights[i][j] = new float[layers[i + 1]];

                    // Para cada neurona en la capa siguiente (destino)
                    for (int k = 0; k < layers[i + 1]; k++)
                    {
                        // Inicializamos con un peso aleatorio entre -1 y 1
                        weights[i][j][k] = UnityEngine.Random.Range(-1f, 1f);
                    }
                }
            }

            // Añadimos un sesgo a los pesos que conectan con la primera neurona de salida
            // La ultima capa de pesos es weights.Length - 1
            int lastWeightLayerIndex = weights.Length - 1;

            // Para cada neurona en la penultima capa
            for (int j = 0; j < weights[lastWeightLayerIndex].Length; j++)
            {
                // Añadir sesgo a la conexion con la primera neurona de salida (neurona de avance)
                weights[lastWeightLayerIndex][j][0] += 0.5f;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al inicializar pesos: {e.Message}");
            Debug.LogException(e);
        }
    }
    #endregion

    #region Metodos de Procesamiento
    /// <summary>
    /// Ejecuta el algoritmo de propagacion hacia adelante (feedforward) de la red neuronal.
    /// </summary>
    /// <param name="inputs">Array de valores de entrada para la red neuronal</param>
    /// <returns>Array de valores de salida procesados por la red</returns>
    public float[] FeedForward(float[] inputs)
    {
        // Colocamos los valores de entrada en la primera capa de neuronas
        for (int i = 0; i < inputs.Length; i++)
        {
            neurons[0][i] = inputs[i];
        }

        // Procesamos capa por capa, desde la primera capa oculta hasta la de salida
        for (int i = 1; i < layers.Length; i++)
        {
            // Para cada neurona en la capa actual
            for (int j = 0; j < layers[i]; j++)
            {
                float sum = 0;

                // Sumamos todas las entradas ponderadas desde la capa anterior
                for (int k = 0; k < layers[i - 1]; k++)
                {
                    // Valor de la neurona anterior * peso de la conexion
                    sum += neurons[i - 1][k] * weights[i - 1][k][j];
                }

                // Aplicamos la funcion de activacion (tanh) y guardamos el resultado
                neurons[i][j] = (float)Math.Tanh(sum);
            }
        }

        // Devolvemos la ultima capa (valores de salida)
        return neurons[neurons.Length - 1];
    }
    #endregion

    #region Metodos de Mutacion y Evolucion
    /// <summary>
    /// Aplica mutacion aleatoria a los pesos de la red neuronal.
    /// </summary>
    /// <param name="mutationRate">Probabilidad de que cada peso sea mutado (0.0 a 1.0)</param>
    /// <param name="lockedOutputs">Array opcional que especifica que salidas no deben ser mutadas</param>
    public void Mutate(float mutationRate, bool[] lockedOutputs = null)
    {
        // Recorremos todos los pesos de la red
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    bool isBlocked = false;

                    // Solo bloquear mutacion en la ultima capa para salidas especificas
                    if (lockedOutputs != null && i == weights.Length - 1 && k < lockedOutputs.Length)
                    {
                        isBlocked = lockedOutputs[k];
                    }

                    // Con una probabilidad igual a mutationRate, modificamos el peso
                    if (!isBlocked && UnityEngine.Random.value < mutationRate)
                    {
                        // Añadimos un valor aleatorio pequeño al peso
                        weights[i][j][k] += UnityEngine.Random.Range(-0.1f, 0.1f);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Realiza cruce genetico (crossover) entre esta red y otra red neuronal.
    /// </summary>
    /// <param name="other">La otra red neuronal con la que realizar el cruce</param>
    /// <param name="lockedOutputs">Array opcional que especifica que salidas no deben participar en el cruce</param>
    public void Crossover(NeuralNetwork other, bool[] lockedOutputs = null)
    {
        // Recorremos todos los pesos
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    bool isBlocked = false;

                    if (lockedOutputs != null && i == weights.Length - 1 && k < lockedOutputs.Length)
                    {
                        isBlocked = lockedOutputs[k];
                    }

                    if (!isBlocked && UnityEngine.Random.value < 0.5f)
                    {
                        weights[i][j][k] = other.weights[i][j][k];
                    }
                    // Si no, mantenemos nuestro peso actual
                }
            }
        }
    }

    /// <summary>
    /// Aplica pesos bloqueados de individuos elite a salidas especificas.
    /// </summary>
    /// <param name="lockedOutputs">Array que especifica que salidas estan bloqueadas</param>
    /// <param name="lockedWeights">Matriz de pesos de elite para aplicar a las salidas bloqueadas</param>
    public void ApplyLockedWeights(bool[] lockedOutputs, float[][] lockedWeights)
    {
        if (lockedOutputs == null || lockedWeights == null || weights == null) return;
        if (weights.Length == 0) return;

        // Ultima capa de pesos
        var lastLayerWeights = weights[weights.Length - 1];

        // Aplicar pesos bloqueados a las salidas correspondientes
        for (int output = 0; output < lockedOutputs.Length && output < 4; output++)
        {
            if (lockedOutputs[output] && lockedWeights[output] != null)
            {
                // Aplicar pesos de la elite para esta salida especifica
                for (int i = 0; i < lastLayerWeights.Length && i < lockedWeights[output].Length; i++)
                {
                    if (lastLayerWeights[i].Length > output)
                    {
                        lastLayerWeights[i][output] = lockedWeights[output][i];
                    }
                }
            }
        }
    }
    #endregion

    #region Metodos de Copia
    /// <summary>
    /// Crea una copia exacta de esta red neuronal con la misma estructura y pesos.
    /// </summary>
    /// <returns>Nueva instancia de NeuralNetwork con los mismos pesos y estructura</returns>
    public NeuralNetwork Copy()
    {
        // Creamos una nueva red con la misma estructura
        NeuralNetwork copy = new NeuralNetwork(layers);

        // Copiamos todos los pesos
        for (int i = 0; i < weights.Length; i++)
        {
            for (int j = 0; j < weights[i].Length; j++)
            {
                for (int k = 0; k < weights[i][j].Length; k++)
                {
                    copy.weights[i][j][k] = weights[i][j][k];
                }
            }
        }

        return copy;
    }
    #endregion

    #region Metodos de Acceso y Serializacion
    /// <summary>
    /// Obtiene la estructura de capas de la red.
    /// Usado para serializacion.
    /// </summary>
    /// <returns>Array con el numero de neuronas por capa</returns>
    public int[] GetLayers()
    {
        return layers;
    }

    /// <summary>
    /// Obtiene todos los pesos de la red.
    /// Usado para serializacion.
    /// </summary>
    /// <returns>Matriz tridimensional con todos los pesos de la red</returns>
    public float[][][] GetWeights()
    {
        return weights;
    }

    /// <summary>
    /// Establece todos los pesos de la red.
    /// Usado al cargar una red previamente guardada.
    /// </summary>
    /// <param name="newWeights">Matriz tridimensional con los nuevos pesos a aplicar</param>
    public void SetWeights(float[][][] newWeights)
    {
        // Verificar que newWeights no sea nulo
        if (newWeights == null)
        {
            Debug.LogError("Error al establecer pesos: El array de pesos es nulo");
            return;
        }

        // Verificar que la estructura coincida con nuestra red
        if (newWeights.Length != weights.Length)
        {
            Debug.LogError($"Error al establecer pesos: Dimensiones incompatibles. Se esperaba {weights.Length} capas, pero se recibieron {newWeights.Length}");
            return;
        }

        try
        {
            // Copiamos los pesos desde la matriz proporcionada
            for (int i = 0; i < weights.Length; i++)
            {
                if (newWeights[i] == null)
                {
                    Debug.LogError($"Error: La capa de pesos {i} es nula");
                    continue;
                }

                if (newWeights[i].Length != weights[i].Length)
                {
                    Debug.LogError($"Error: Dimensiones incorrectas en capa {i}. Se esperaba {weights[i].Length}, se recibio {newWeights[i].Length}");
                    continue;
                }

                for (int j = 0; j < weights[i].Length; j++)
                {
                    if (newWeights[i][j] == null)
                    {
                        Debug.LogError($"Error: Pesos nulos en capa {i}, neurona {j}");
                        continue;
                    }

                    if (newWeights[i][j].Length != weights[i][j].Length)
                    {
                        Debug.LogError($"Error: Dimensiones incorrectas en capa {i}, neurona {j}. Se esperaba {weights[i][j].Length}, se recibio {newWeights[i][j].Length}");
                        continue;
                    }

                    for (int k = 0; k < weights[i][j].Length; k++)
                    {
                        weights[i][j][k] = newWeights[i][j][k];
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error al establecer pesos: {e.Message}");
            Debug.LogException(e);
        }
    }
    #endregion
}