using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Implementa un algoritmo genetico para evolucionar una poblacion de NPCs.
/// Este script gestiona la creacion, evaluacion, seleccion y evolucion de los NPCs.
/// </summary>
public class NPCGeneticAlgorithm : MonoBehaviour
{
    // Variables configurables desde el Inspector de Unity
    [Header("Configuracion del Algoritmo Genetico")]
    [Tooltip("Cantidad de NPCs en cada generacion")]
    public int populationSize = 50;

    [Tooltip("Cantidad de mejores NPCs a preservar automaticamente")]
    [Range(1, 10)]
    public int eliteCount = 1;

    [Header("Configuracion de Poblacion")]
    [Tooltip("Probabilidad de mutacion para cada peso de la red neuronal (0-1)")]
    [Range(0, 1)]
    public float mutationRate = 0.01f;

    [Header("Referencias")]
    [Tooltip("Prefab del NPC que se instanciara")]
    public GameObject npcPrefab;

    [Tooltip("Punto de partida donde apareceran los NPCs")]
    public Transform startPosition;

    // Variables internas
    [HideInInspector]
    public List<NPCController> population; // Lista que contiene todos los NPCs de la generacion actual

    [Header("Estado")]
    [Tooltip("Pausar la simulacion")]
    public bool isPaused = false;

    [Header("Estadisticas")]
    [Tooltip("Generacion actual")]
    public int generation = 1;

    [Header("Sistema de Supervivencia")]
    [Tooltip("Segundos de invencibilidad al aparecer (0 = sin invencibilidad)")]
    public float invincibilityTime = 3f;

    [Tooltip("Choques permitidos antes de morir (1 = muerte instantanea)")]
    public int maxCollisions = 1;

    [Header("Bloqueo de Acciones")]
    [Tooltip("Bloquear entrenamiento del movimiento hacia adelante")]
    public bool lockMovement = false;

    [Tooltip("Bloquear entrenamiento del giro a la izquierda")]
    public bool lockTurnLeft = false;

    [Tooltip("Bloquear entrenamiento del giro a la derecha")]
    public bool lockTurnRight = false;

    [Tooltip("Bloquear entrenamiento del salto")]
    public bool lockJump = false;

    [Header("Human Mode")]
    [Tooltip("Referencia al controlador de modo humano")]
    public HumanModeController humanModeController;

    [Tooltip("Referencia al aplicador de aprendizaje por imitacion")]
    public ImitationLearningApplier imitationLearningApplier;

    // Variables ocultas para almacenar pesos elite
    [HideInInspector] public float[] eliteMovementWeights;
    [HideInInspector] public float[] eliteTurnLeftWeights;
    [HideInInspector] public float[] eliteTurnRightWeights;
    [HideInInspector] public float[] eliteJumpWeights;

    // Variables privadas para estadisticas
    private float bestFitness = 0; // Mejor desempeno en la generacion actual
    private float worstFitness = float.MaxValue; // Peor desempeno en la generacion actual
    private float averageFitness = 0; // Desempeno promedio en la generacion actual

    [Header("Control de Tiempo")]
    [Tooltip("Tiempo limite por generacion en segundos")]
    public float generationTimeLimit = 30f;
    private float generationTimer = 0f;

    /// <summary>
    /// Se llama al iniciar el script. Verifica las referencias y crea la poblacion inicial.
    /// </summary>
    void Start()
    {
        // Verificamos que el prefab del NPC este asignado
        if (npcPrefab == null)
        {
            Debug.LogError("NPC prefab no asignado");
            return;
        }

        // Verificamos que la posicion inicial este asignada
        if (startPosition == null)
        {
            Debug.LogError("Posicion inicial no asignada");
            return;
        }

        // Inicializamos la primera generacion de NPCs
        InitializePopulation();
    }

    /// <summary>
    /// Crea la poblacion inicial de NPCs.
    /// Cada NPC comienza con una red neuronal aleatoria.
    /// </summary>
    public void InitializePopulation()
    {
        // Si ya existe poblacion, no recrear
        if (population != null && population.Count == populationSize)
        {
            Debug.Log("Poblacion ya existe, reutilizando GameObjects existentes");
            return;
        }

        // Limpiar poblacion anterior si existe
        if (population != null)
        {
            foreach (var npc in population)
            {
                if (npc != null) Destroy(npc.gameObject);
            }
        }

        population = new List<NPCController>();

        // Crear nuevos NPCs
        for (int i = 0; i < populationSize; i++)
        {
            GameObject npcGO = Instantiate(npcPrefab, startPosition.position, startPosition.rotation);
            NPCController npc = npcGO.GetComponent<NPCController>();

            if (npc != null)
            {
                population.Add(npc);
            }
            else
            {
                Debug.LogError("NPCController no esta en los componentes del prefab");
            }
        }

        Debug.Log($"Poblacion inicial creada: {populationSize} NPCs");
    }

    /// <summary>
    /// Captura los valores de los pesos del mejor NPC para uso en bloqueo de acciones.
    /// Extrae los pesos de la ultima capa de la red neuronal del NPC elite.
    /// </summary>
    public void CaptureEliteValues()
    {
        if (population == null || population.Count == 0) return;

        // Obtener el mejor NPC
        var bestNPC = population.OrderByDescending(c => c.fitness).First();

        if (bestNPC.brain == null) return;

        // Capturar los pesos de la ultima capa (que van a las 4 salidas)
        var weights = bestNPC.brain.GetWeights();
        if (weights == null || weights.Length == 0) return;

        // Ultima capa de pesos (la que conecta a las salidas)
        var lastLayerWeights = weights[weights.Length - 1];

        // Extraer pesos para cada salida
        int numInputsToOutput = lastLayerWeights.Length;

        eliteMovementWeights = new float[numInputsToOutput];
        eliteTurnLeftWeights = new float[numInputsToOutput];
        eliteTurnRightWeights = new float[numInputsToOutput];
        eliteJumpWeights = new float[numInputsToOutput];

        // Copiar pesos para cada neurona de salida
        for (int i = 0; i < numInputsToOutput; i++)
        {
            if (lastLayerWeights[i].Length >= 4)
            {
                eliteMovementWeights[i] = lastLayerWeights[i][0];  // Pesos hacia salida 0 (movimiento)
                eliteTurnLeftWeights[i] = lastLayerWeights[i][1];  // Pesos hacia salida 1 (giro izq)
                eliteTurnRightWeights[i] = lastLayerWeights[i][2]; // Pesos hacia salida 2 (giro der)
                eliteJumpWeights[i] = lastLayerWeights[i][3];      // Pesos hacia salida 3 (salto)
            }
        }

        Debug.Log($"Pesos elite capturados para {numInputsToOutput} conexiones de salida");
    }

    /// <summary>
    /// Actualiza el algoritmo genetico cada frame.
    /// Maneja el temporizador de generacion y verifica si todos los NPCs han muerto.
    /// </summary>
    void Update()
    {
        if (humanModeController != null && humanModeController.isActive)
        {
            return; // Skip genetic algorithm processing during human mode
        }

        // Si esta pausado o la poblacion no esta lista, no hacer nada
        if (isPaused || population == null || population.Count == 0)
        {
            return;
        }

        generationTimer += Time.deltaTime;

        // Eliminar null referencias si existen
        population = population.Where(p => p != null).ToList();

        // Verificar nuevamente despues de limpiar null referencias
        if (population.Count == 0)
        {
            Debug.LogWarning("La poblacion quedo vacia despues de eliminar referencias nulas.");
            return;
        }

        // Verificar si todos los NPCs estan muertos antes de proceder
        if (generationTimer >= generationTimeLimit)
        {
            ForceNextGeneration();
        }

        if (population.All(c => c.isDead))
        {
            EvaluatePopulation();

            // NEW: Apply imitation learning before selection (if system is available)
            if (imitationLearningApplier != null)
            {
                // Note: The imitation learning applier has its own logic to decide when to apply
                // It will check generation intervals and demonstration availability automatically
            }

            Selection();
            Mutation();
            ResetPopulation();
            generation++;
        }

        // Optimizacion: pausar NPCs muertos por mucho tiempo
        if (Time.frameCount % 300 == 0) // Cada 5 segundos
        {
            foreach (var npc in population.Where(n => n != null && n.isDead))
            {
                // NPCs muertos por mas de 2 segundos: pausar completamente
                if (npc.timeAlive > 0 && Time.time - npc.timeAlive > 2f)
                {
                    npc.enabled = false; // Pausar el componente completamente
                }
            }
        }
    }

    /// <summary>
    /// Aplica el aprendizaje por imitacion de forma inmediata si esta disponible.
    /// </summary>
    public void ApplyImitationLearningNow()
    {
        if (imitationLearningApplier != null)
        {
            imitationLearningApplier.ForceApplyImitationLearning();
        }
        else
        {
            Debug.LogWarning("Imitation Learning Applier not assigned");
        }
    }

    /// <summary>
    /// Evalua el rendimiento de cada NPC en la generacion actual y calcula estadisticas.
    /// Actualiza los valores de mejor, peor y promedio fitness.
    /// </summary>
    void EvaluatePopulation()
    {
        bestFitness = float.MinValue;
        worstFitness = float.MaxValue;
        float totalFitness = 0;

        // Recorremos cada NPC para calcular estadisticas
        foreach (var npc in population)
        {
            // Actualizamos el mejor fitness si encontramos uno mejor
            if (npc.fitness > bestFitness) bestFitness = npc.fitness;

            // Actualizamos el peor fitness si encontramos uno peor
            if (npc.fitness < worstFitness) worstFitness = npc.fitness;

            // Sumamos todos los fitness para calcular el promedio
            totalFitness += npc.fitness;
        }

        // Calculamos el fitness promedio
        averageFitness = totalFitness / population.Count;

        // Mostramos las estadisticas en la consola para seguimiento
        Debug.Log($"Generacion {generation}: Mejor Fitness = {bestFitness}, Peor Fitness = {worstFitness}, Promedio Fitness = {averageFitness}");
    }

    /// <summary>
    /// Selecciona los mejores NPCs para crear la siguiente generacion.
    /// Utiliza elitismo (conservar al mejor) y seleccion por torneo.
    /// Aplica crossover entre padres seleccionados y maneja el bloqueo de pesos.
    /// </summary>
    void Selection()
    {
        if (population == null || population.Count == 0)
        {
            Debug.LogError("La poblacion esta vacia o nula.");
            return;
        }

        var bestNPCs = population.OrderByDescending(c => c.fitness).Take(eliteCount).ToList();

        // Capturar valores elite automaticamente
        CaptureEliteValues();

        List<NeuralNetwork> newBrains = new List<NeuralNetwork>();

        // Preservar elites
        foreach (var elite in bestNPCs)
        {
            newBrains.Add(elite.brain.Copy());
        }

        bool[] locks = { lockMovement, lockTurnLeft, lockTurnRight, lockJump };

        // Generar resto de poblacion mediante crossover
        for (int i = eliteCount; i < populationSize; i++)
        {
            if (population.Count >= 2)
            {
                NPCController parent1 = TournamentSelection();
                NPCController parent2 = TournamentSelection();

                NeuralNetwork childBrain = parent1.brain.Copy();
                childBrain.Crossover(parent2.brain, locks);

                newBrains.Add(childBrain);
            }
            else
            {
                newBrains.Add(population[0].brain.Copy());
            }
        }

        // Asignar nuevos cerebros
        for (int i = 0; i < population.Count && i < newBrains.Count; i++)
        {
            population[i].brain = newBrains[i];
        }

        // Aplicar pesos bloqueados despues de asignar cerebros
        ApplyLockedWeightsToPopulation();

        // Debug con informacion de elites
        if (bestNPCs.Count >= eliteCount)
        {
            string eliteInfo = "";
            for (int i = 0; i < eliteCount; i++)
            {
                eliteInfo += $"{i + 1}: {bestNPCs[i].fitness:F1}";
                if (i < eliteCount - 1) eliteInfo += ", ";
            }

            Debug.Log($"Generacion {generation}: {eliteCount} elites preservados - {eliteInfo}");
        }

        Debug.Log($"Generacion {generation}: Reutilizados {population.Count} NPCs sin crear/destruir GameObjects");
    }

    /// <summary>
    /// Realiza seleccion por torneo para elegir un padre para reproduccion.
    /// Selecciona aleatoriamente un grupo de NPCs y devuelve el mejor de ellos.
    /// </summary>
    /// <returns>El NPC ganador del torneo</returns>
    NPCController TournamentSelection()
    {
        // Verificar que la poblacion no este vacia
        if (population == null || population.Count == 0)
        {
            Debug.LogError("No se puede realizar la seleccion por torneo: poblacion vacia.");
            return null;
        }

        int tournamentSize = Mathf.Min(5, population.Count); // Evitar seleccionar mas participantes que el tamano de la poblacion
        NPCController best = null;
        float bestFitness = float.MinValue;

        for (int i = 0; i < tournamentSize; i++)
        {
            NPCController tournamentContender = population[Random.Range(0, population.Count)];
            if (tournamentContender != null && tournamentContender.fitness > bestFitness)
            {
                best = tournamentContender;
                bestFitness = tournamentContender.fitness;
            }
        }

        if (best == null)
        {
            Debug.LogError("No se pudo seleccionar un ganador en el torneo. Usando el primer NPC disponible.");
            best = population.FirstOrDefault(p => p != null);
        }

        return best;
    }

    /// <summary>
    /// Aplica mutaciones aleatorias a la red neuronal de cada NPC.
    /// Esto introduce variabilidad y ayuda a explorar nuevas soluciones.
    /// Respeta los bloqueos de acciones configurados.
    /// </summary>
    void Mutation()
    {
        bool[] locks = { lockMovement, lockTurnLeft, lockTurnRight, lockJump };

        foreach (var npc in population)
        {
            if (npc != null && npc.brain != null)
            {
                npc.brain.Mutate(mutationRate, locks);
            }
            else
            {
                Debug.LogWarning("Se encontro un NPC nulo o sin cerebro durante la mutacion");
            }
        }

        // Aplicar pesos bloqueados despues de mutacion
        ApplyLockedWeightsToPopulation();
    }

    /// <summary>
    /// Aplica los pesos bloqueados del NPC elite a toda la poblacion.
    /// Esto asegura que las acciones bloqueadas mantengan los pesos del mejor NPC.
    /// </summary>
    void ApplyLockedWeightsToPopulation()
    {
        if (population == null) return;

        bool[] locks = { lockMovement, lockTurnLeft, lockTurnRight, lockJump };

        // Verificar si hay algo bloqueado
        bool hasLocks = lockMovement || lockTurnLeft || lockTurnRight || lockJump;
        if (!hasLocks) return;

        // Preparar array de pesos bloqueados
        float[][] lockedWeights = new float[4][];
        lockedWeights[0] = eliteMovementWeights;
        lockedWeights[1] = eliteTurnLeftWeights;
        lockedWeights[2] = eliteTurnRightWeights;
        lockedWeights[3] = eliteJumpWeights;

        // Aplicar a todos los NPCs
        foreach (var npc in population)
        {
            if (npc != null && npc.brain != null)
            {
                npc.brain.ApplyLockedWeights(locks, lockedWeights);
            }
        }

        // Log informativo de acciones bloqueadas
        string lockInfo = "";
        if (lockMovement) lockInfo += "Mov ";
        if (lockTurnLeft) lockInfo += "TL ";
        if (lockTurnRight) lockInfo += "TR ";
        if (lockJump) lockInfo += "Jump ";

        if (!string.IsNullOrEmpty(lockInfo))
        {
            Debug.Log($"Pesos bloqueados aplicados: {lockInfo}");
        }
    }

    /// <summary>
    /// Reinicia todos los NPCs para la siguiente generacion.
    /// Reestablece posicion, fitness, estado y reactiva componentes pausados.
    /// </summary>
    void ResetPopulation()
    {
        foreach (var npc in population)
        {
            if (npc != null)
            {
                // Reiniciamos cada NPC (posicion, fitness, estado, etc.)
                npc.Reset();
            }
            else
            {
                Debug.LogError("Error al resetear el NPC: referencia nula");
            }
        }

        // Reactivar todos los NPCs
        foreach (var npc in population.Where(n => n != null))
        {
            npc.enabled = true;
        }
    }

    /// <summary>
    /// Fuerza el avance a la siguiente generacion inmediatamente.
    /// Puede ser llamado por boton o cuando se alcanza el limite de tiempo.
    /// </summary>
    public void ForceNextGeneration()
    {
        if (population == null || population.Count == 0) return;

        Debug.Log("Avance forzado de generacion por boton o timeout");

        EvaluatePopulation();
        Selection();
        Mutation();
        ResetPopulation();
        generation++;

        generationTimer = 0f;
    }
}