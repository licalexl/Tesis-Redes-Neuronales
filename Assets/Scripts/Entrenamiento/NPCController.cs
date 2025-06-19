using UnityEngine;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;

/// <summary>
/// Controla el comportamiento individual de cada NPC.
/// Maneja los sensores, la red neuronal, el movimiento y el calculo de fitness.
/// Incluye soporte para saltos, deteccion de aliados/enemigos y prevencion de loops.
/// </summary>
public class NPCController : MonoBehaviour
{
    #region Variables de Red Neuronal
    [Header("Entradas/Salidas de la Red Neuronal")]
    [Tooltip("Valores de los 7 sensores, entrada para la red neuronal")]
    public float[] inputs = new float[8]; // 7 sensores + 1 entrada constante

    [Tooltip("Valores de salida de la red neuronal: avanzar, girar izquierda, girar derecha, saltar")]
    public float[] outputs = new float[4]; // Avanzar, girar izquierda, girar derecha, saltar
    #endregion

    #region Variables de IA y Fitness
    [Header("IA y Fitness")]
    [Tooltip("Red neuronal que toma decisiones para este NPC")]
    public NeuralNetwork brain;

    [Tooltip("Puntuacion de rendimiento para la seleccion genetica")]
    public float fitness = 0;

    [Tooltip("Distancia total recorrida por el NPC")]
    public float totalDistance = 0;

    [Tooltip("Tiempo que ha sobrevivido el NPC")]
    public float timeAlive = 0;

    [Tooltip("Saltos exitosos realizados")]
    public int successfulJumps = 0;
    #endregion

    #region Variables de Movimiento
    [Header("Configuracion de Movimiento")]
    [Tooltip("Velocidad maxima de movimiento")]
    public float moveSpeed = 5f;

    [Tooltip("Velocidad minima para no considerarse inactivo")]
    public float minSpeed = 0.5f;

    [Tooltip("Velocidad de rotacion en grados por segundo")]
    public float rotationSpeed = 120f;
    #endregion

    #region Variables de Salto
    [Header("Configuracion de Salto")]
    [Tooltip("Fuerza de salto")]
    public float jumpForce = 5f;

    [Tooltip("Cooldown entre saltos")]
    public float jumpCooldown = 1f;

    [Tooltip("LayerMask para detectar el suelo")]
    public LayerMask groundLayer;
    #endregion

    #region Variables de Sensores
    [Header("Configuracion de Sensores")]
    [Tooltip("Distancia maxima de deteccion de los sensores")]
    public float sensorLength = 10f;

    [Tooltip("Desplazamiento hacia adelante de los sensores desde el centro del NPC")]
    public float sensorForwardOffset = 1.0f;

    [Tooltip("Altura de los sensores desde la base del NPC")]
    public float sensorHeight = 1.0f;

    [Tooltip("Altura del sensor inferior para detectar obstaculos saltables")]
    public float lowerSensorHeight = 0.2f;
    #endregion

    #region Variables de Estado
    [Header("Estado")]
    [Tooltip("Tiempo maximo que puede permanecer inactivo antes de morir")]
    public float maxIdleTime = 5f;

    [Tooltip("Indica si el NPC ha muerto (colision o inactividad)")]
    public bool isDead = false;
    #endregion

    #region Sistema Anti-Loop
    [Header("Sistema Anti-Loop")]
    [Tooltip("Radio para detectar comportamiento circular")]
    public float loopDetectionRadius = 5f;

    [Tooltip("Tiempo minimo entre checkpoints para evitar loops")]
    public float checkpointInterval = 3f;

    [Tooltip("Distancia minima para considerar un nuevo checkpoint")]
    public float minCheckpointDistance = 5f;

    [Tooltip("Penalizacion por comportamiento circular")]
    public float loopPenalty = 10f;

    [Tooltip("Cantidad maxima de loops antes de eliminar al NPC")]
    public int maxLoop = 3;
    #endregion

    #region Recompensas de Exploracion
    [Header("Recompensas de Exploracion")]
    [Tooltip("Recompensa por explorar nuevas areas")]
    public float explorationBonus = 2f;

    [Tooltip("Tamaño de la cuadricula para dividir el mapa")]
    public float gridSize = 5f;
    #endregion

    #region Deteccion de Pared
    [Header("Deteccion de Pared")]
    [Tooltip("Tiempo maximo pegado a pared antes de contar como choque adicional")]
    public float maxWallContactTime = 2f;
    #endregion

    #region Variables Privadas de Sistema
    /// <summary>
    /// Escala de tiempo actual del juego
    /// </summary>
    private float currentTimeScale = 1f;

    /// <summary>
    /// Contador de saltos correctos realizados
    /// </summary>
    public int correctJumps = 0;

    /// <summary>
    /// Contador de saltos incorrectos realizados
    /// </summary>
    public int incorrectJumps = 0;

    /// <summary>
    /// Indica si el NPC esta colisionando con un obstaculo
    /// </summary>
    private bool isCollidingWithObstacle = false;

    /// <summary>
    /// Tiempo de contacto con obstaculos
    /// </summary>
    private float obstacleContactTime = 0f;

    /// <summary>
    /// Tiempo de la ultima penalizacion por colision
    /// </summary>
    private float lastCollisionPenaltyTime = 0f;
    #endregion

    #region Variables Privadas de Control
    /// <summary>
    /// Contador de loops detectados
    /// </summary>
    private int contadorLoop = 0;

    /// <summary>
    /// Tiempo que el NPC ha estado inactivo
    /// </summary>
    private float idleTime = 0f;

    /// <summary>
    /// Ultima posicion registrada del NPC
    /// </summary>
    private Vector3 lastPosition;

    /// <summary>
    /// Posicion inicial del NPC al nacer
    /// </summary>
    private Vector3 startPosition;

    /// <summary>
    /// Rotacion inicial del NPC al nacer
    /// </summary>
    private Quaternion startRotation;

    /// <summary>
    /// Referencia al algoritmo genetico principal
    /// </summary>
    public NPCGeneticAlgorithm geneticAlgorithm;

    /// <summary>
    /// Tiempo cuando nacio este NPC
    /// </summary>
    private float spawnTime = 0f;

    /// <summary>
    /// Numero de colisiones actuales
    /// </summary>
    private int currentCollisions = 0;
    #endregion

    #region Componentes
    /// <summary>
    /// Componente Rigidbody del NPC
    /// </summary>
    private Rigidbody rb;

    /// <summary>
    /// Componente Animator del NPC
    /// </summary>
    private Animator animator;
    #endregion

    #region Variables de Salto
    /// <summary>
    /// Indica si el NPC esta tocando el suelo
    /// </summary>
    private bool isGrounded = false;

    /// <summary>
    /// Tiempo del ultimo salto realizado
    /// </summary>
    private float lastJumpTime = -1f;
    #endregion

    #region Variables Anti-Loop y Exploracion
    /// <summary>
    /// Total de recompensas por checkpoints acumuladas
    /// </summary>
    private float totalCheckpointRewards = 0f;

    /// <summary>
    /// Historial de posiciones para detectar loops
    /// </summary>
    private List<Vector3> positionHistory = new List<Vector3>();

    /// <summary>
    /// Tiempo del ultimo checkpoint registrado
    /// </summary>
    private float lastCheckpointTime = 0f;

    /// <summary>
    /// Posicion del ultimo checkpoint
    /// </summary>
    private Vector3 lastCheckpointPosition;

    /// <summary>
    /// Celdas de la cuadricula ya visitadas
    /// </summary>
    private HashSet<Vector2Int> visitedCells = new HashSet<Vector2Int>();

    /// <summary>
    /// Numero de areas unicas visitadas
    /// </summary>
    public int uniqueAreasVisited = 0;

    /// <summary>
    /// Distancia actual desde el punto de inicio
    /// </summary>
    private float distanceFromStart = 0f;

    /// <summary>
    /// Circulos consecutivos detectados
    /// </summary>
    private int consecutiveCircles = 0;

    /// <summary>
    /// Ultimo angulo registrado para detectar rotacion excesiva
    /// </summary>
    private float lastAngle = 0f;

    /// <summary>
    /// Rotacion total acumulada
    /// </summary>
    private float totalRotation = 0f;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa los componentes y variables basicas del NPC
    /// </summary>
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        }

        startPosition = transform.position;
        startRotation = transform.rotation;
        lastPosition = transform.position;
        lastCheckpointPosition = transform.position;
    }

    /// <summary>
    /// Configura la red neuronal y registra el NPC en los sistemas necesarios
    /// </summary>
    void Start()
    {
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }
        if (brain == null)
        {
            // Estructura actualizada de la red neuronal:
            // - 8 neuronas de entrada (7 sensores + 1 constante)
            // - 8 neuronas en primera capa oculta
            // - 6 neuronas en segunda capa oculta
            // - 4 neuronas de salida (acciones)
            brain = new NeuralNetwork(8, 8, 6, 4);
        }

        // Registrar con el sistema de checkpoints
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.RegisterNPC(this);
        }
        spawnTime = Time.time;
        currentCollisions = 0;

        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }
    }
    #endregion

    #region Metodos de Update

    /// <summary>
    /// Actualiza los sensores y procesa la red neuronal cada frame
    /// </summary>
    void Update()
    {
        if (isDead) return;

        UpdateSensors();
        outputs = brain.FeedForward(inputs);
      

        timeAlive += Time.deltaTime;
    }

    /// <summary>
    /// Aplica las acciones determinadas por la IA y actualiza el estado del NPC
    /// </summary>
    void FixedUpdate()
    {

        if (isDead) return;
        // Verificar si esta en el suelo
      
        CheckGrounded();

        // Aplicamos las acciones determinadas por la red neuronal
        ApplyOutputs();

        // Nuevas verificaciones anti-loop
        CheckForLoopingBehavior();
        UpdateExplorationTracking();

        // Actualizamos la puntuacion de fitness
        UpdateFitness();

        // Verificamos si el NPC esta inactivo por demasiado tiempo
        CheckIdleStatus();
        CheckWallContact();
    }
    #endregion

    #region Metodos de Sensores

    /// <summary>
    /// Actualiza todos los sensores del NPC para detectar obstaculos y otros NPCs
    /// </summary>
    public void UpdateSensors()
    {
        if (inputs.Length < 8)
        {
            inputs = new float[8];
            Debug.LogWarning("Array inputs recreado con tamaño 8");
        }

        // Posicion desde donde parten los rayos de los sensores normales
        Vector3 sensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        // Posicion para el sensor bajo (detecta obstaculos saltables)
        Vector3 lowerSensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * lowerSensorHeight;

        // Actualizamos los 5 sensores originales con deteccion de aliados/enemigos
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            Vector3 sensorDirection = Quaternion.Euler(0, -90 + 45 * i, 0) * transform.forward;

            if (Physics.Raycast(sensorStartPos, sensorDirection, out hit, sensorLength))
            {
                // Verificar si el objeto detectado es otro NPC
                NPCController otherNPC = hit.collider.GetComponent<NPCController>();
                if (otherNPC != null)
                {
                    // Tratar todos los NPCs como obstaculos normales
                    inputs[i] = 1 - (hit.distance / sensorLength);
                    Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.yellow);
                }

                else
                {
                    // Es un obstaculo normal
                    inputs[i] = 1 - (hit.distance / sensorLength);
                    Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.yellow);
                }
            }
            else
            {
                inputs[i] = 0f;
                Debug.DrawRay(sensorStartPos, sensorDirection * sensorLength, Color.white);
            }
        }

        // Sensor 6: Frontal bajo para detectar obstaculos saltables
        RaycastHit lowerHit;
        if (Physics.Raycast(lowerSensorStartPos, transform.forward, out lowerHit, sensorLength))
        {
            inputs[5] = 1 - (lowerHit.distance / sensorLength);
            Debug.DrawRay(lowerSensorStartPos, transform.forward * lowerHit.distance, Color.yellow);
        }
        else
        {
            inputs[5] = 0f;
            Debug.DrawRay(lowerSensorStartPos, transform.forward * sensorLength, Color.blue);
        }

        // Sensor 7: Frontal alto para detectar obstaculos que no se pueden saltar
        RaycastHit upperHit;
        Vector3 upperSensorStartPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * (sensorHeight * 1.5f);
        if (Physics.Raycast(upperSensorStartPos, transform.forward, out upperHit, sensorLength))
        {
            inputs[6] = 1 - (upperHit.distance / sensorLength);
            Debug.DrawRay(upperSensorStartPos, transform.forward * upperHit.distance, Color.magenta);
        }
        else
        {
            inputs[6] = 0f;
            Debug.DrawRay(upperSensorStartPos, transform.forward * sensorLength, Color.cyan);
        }

        // Entrada constante (bias)
        inputs[7] = 1f;
    }
    #endregion

    #region Metodos de Movimiento

    /// <summary>
    /// Aplica las acciones de movimiento basadas en las salidas de la red neuronal
    /// </summary>
    void ApplyOutputs()
    {
        if (outputs.Length < 4)
        {
            // Si el array no es del tamaño correcto, redimensionarlo
            Array.Resize(ref outputs, 4);
            Debug.LogWarning("Array outputs redimensionado a tamaño 4");
        }

        // Añade ruido aleatorio al principio del entrenamiento
        float explorationFactor = Mathf.Max(0, 50 - geneticAlgorithm.generation) / 50f;
        float randomNoise = Random.Range(0f, 0.5f) * explorationFactor;

        float forwardSpeed = Mathf.Clamp(outputs[0] + 0.3f + randomNoise, 0, 1) * moveSpeed;
        float turnAmount = (outputs[1] - outputs[2]) * rotationSpeed;

        // Aseguramos una velocidad minima
        if (forwardSpeed > 0)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, minSpeed);
        }

        // Aplicamos el movimiento al Rigidbody
        rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * forwardSpeed;

        // Aplicamos la rotacion al transform
        transform.Rotate(Vector3.up * turnAmount * Time.fixedDeltaTime);

        // Manejo del salto
        if (outputs.Length >= 4 && outputs[3] > 0.5f && CanJump())
        {
            Jump();
        }

        // Actualizamos las animaciones si hay un componente Animator
        if (animator != null)
        {
            animator.SetFloat("Speed", forwardSpeed / moveSpeed);
            animator.SetFloat("Turn", turnAmount / rotationSpeed);
            animator.SetBool("IsGrounded", isGrounded);
            animator.SetTrigger("Jump"); // Si existe un trigger de salto en el animator
        }

        // Calculamos la distancia recorrida desde el ultimo frame
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        totalDistance += distanceMoved;
        lastPosition = transform.position;
    }
    #endregion

    #region Metodos de Salto

    /// <summary>
    /// Verifica si el NPC puede realizar un salto
    /// </summary>
    /// <returns>True si puede saltar, False en caso contrario</returns>
    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime > jumpCooldown);
    }

    /// <summary>
    /// Ejecuta un salto y calcula si es correcto o incorrecto basado en los sensores
    /// </summary>
    void Jump()
    {
        bool hasLowObstacle = inputs[5] > 0.3f;    // Hay obstaculo bajo (saltable)
        bool hasHighObstacle = inputs[6] > 0.3f;   // Hay obstaculo alto (no saltable)               

        if (hasLowObstacle && !hasHighObstacle)
        {
            // Salto CORRECTO: hay algo que saltar y es saltable
            correctJumps++;
            fitness += 10f; // Recompensa inmediata por salto inteligente
        }
        else
        {
            // Salto INCORRECTO: saltar contra pared alta o salto innecesario
            incorrectJumps++;
            fitness -= 15f; // Castigo inmediato por salto tonto
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;

        successfulJumps = correctJumps;
    }

    /// <summary>
    /// Verifica si el NPC esta tocando el suelo usando un raycast
    /// </summary>
    void CheckGrounded()
    {
        float rayDistance = 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayer))
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }

        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
    }
    #endregion

    #region Metodos Anti-Loop

    /// <summary>
    /// Detecta y penaliza comportamientos circulares o repetitivos
    /// </summary>
    void CheckForLoopingBehavior()
    {
        // Registrar la posicion cada cierto intervalo
        if (Time.time - lastCheckpointTime > checkpointInterval)
        {
            Vector3 currentPos = transform.position;
            float distanceFromCheckpoint = Vector3.Distance(currentPos, lastCheckpointPosition);

            // Si no se ha movido lo suficiente desde el ultimo checkpoint
            if (distanceFromCheckpoint < minCheckpointDistance)
            {
                consecutiveCircles++;
                fitness -= loopPenalty * consecutiveCircles; // Penalizacion progresiva
            }
            else
            {
                consecutiveCircles = 0; // Resetear si se mueve significativamente
            }

            if (consecutiveCircles >= 3)
            {
                fitness = fitness - 50;
                isDead = true;
            }

            lastCheckpointPosition = currentPos;
            lastCheckpointTime = Time.time;
            positionHistory.Add(currentPos);

            // Mantener el historial limitado
            if (positionHistory.Count > 10)
            {
                positionHistory.RemoveAt(0);
            }
        }

        // Detectar rotacion excesiva (indicador de circulos)
        float currentAngle = transform.eulerAngles.y;
        float angleDelta = Mathf.DeltaAngle(lastAngle, currentAngle);
        totalRotation += Mathf.Abs(angleDelta);
        lastAngle = currentAngle;

        // Penalizar si rota demasiado en relacion a la distancia recorrida
        if (totalDistance > 0 && totalRotation / totalDistance > 10f)
        {
            fitness -= loopPenalty * 0.1f;
        }
    }

    /// <summary>
    /// Actualiza el seguimiento de exploracion y areas visitadas
    /// </summary>
    void UpdateExplorationTracking()
    {
        // Convertir la posicion actual a coordenadas de la cuadricula
        Vector2Int gridCell = new Vector2Int(
            Mathf.FloorToInt(transform.position.x / gridSize),
            Mathf.FloorToInt(transform.position.z / gridSize)
        );

        // Si es una nueva celda, premiar la exploracion
        if (!visitedCells.Contains(gridCell))
        {
            visitedCells.Add(gridCell);
            uniqueAreasVisited++;
            fitness += explorationBonus;
        }

        // Actualizar distancia desde el punto de inicio
        distanceFromStart = Vector3.Distance(transform.position, startPosition);
    }
    #endregion

    #region Metodos de Fitness

    /// <summary>
    /// Calcula y actualiza la puntuacion de fitness basada en multiples factores
    /// </summary>
    void UpdateFitness()
    {
        // Nueva formula de fitness
        float baseReward = 0f;

        // 1. Recompensa por distancia real recorrida
        baseReward += totalDistance * 0.5f;

        // 2. Recompensa por exploracion (areas unicas visitadas)
        baseReward += uniqueAreasVisited * explorationBonus;

        // 3. Recompensa por distancia desde el punto de inicio
        baseReward += distanceFromStart * 0.3f;

        // 4. Recompensa por saltos exitosos
        float jumpBonus = (correctJumps * 15f) - (incorrectJumps * 8f);
        baseReward += jumpBonus;

        // Bonus adicional por eficiencia de saltos
        if (correctJumps + incorrectJumps > 0)
        {
            float jumpEfficiency = (float)correctJumps / (correctJumps + incorrectJumps);
            baseReward += jumpEfficiency * 20f;
        }

        // 5. Penalizacion por tiempo (para evitar que el tiempo sea lo unico que importa)
        float timePenalty = Mathf.Min(timeAlive * 0.1f, 10f); // Limitar la penalizacion

        // 6. Penalizacion por comportamiento repetitivo
        float repetitivePenalty = consecutiveCircles * loopPenalty;

        // 7. Recompensa por checkpoints
        if (CheckpointSystem.Instance != null)
        {
            float newCheckpointReward = CheckpointSystem.Instance.CheckCheckpoints(this);

            // Solo acumular si hay nueva recompensa
            if (newCheckpointReward > 0)
            {
                totalCheckpointRewards += newCheckpointReward;
            }

            // Siempre usar el total acumulado
            baseReward += totalCheckpointRewards;
        }
        // Calcular fitness final
        fitness = baseReward - timePenalty - repetitivePenalty;

        // Asegurar que el fitness no sea negativo
        fitness = Mathf.Max(0, fitness);

        if (Time.frameCount % 380 == 0)
        {
            Debug.Log($"NPC {name}: Saltos Correctos: {correctJumps}, Incorrectos: {incorrectJumps}, Eficiencia: {(correctJumps + incorrectJumps > 0 ? (float)correctJumps / (correctJumps + incorrectJumps) * 100f : 0f):F1}%");
        }
    }
    #endregion

    #region Metodos de Estado

    /// <summary>
    /// Verifica si el NPC ha estado inactivo demasiado tiempo
    /// </summary>
    void CheckIdleStatus()
    {
        float currentSpeed = rb.velocity.magnitude;

        if (currentSpeed < minSpeed)
        {
            idleTime += Time.fixedDeltaTime;

            if (idleTime > maxIdleTime)
            {
                isDead = true;
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            idleTime = 0f;
        }
    }

    /// <summary>
    /// Verifica el tiempo de contacto con paredes para aplicar penalizaciones adicionales
    /// </summary>
    void CheckWallContact()
    {
        if (isCollidingWithObstacle && !isDead)
        {
            obstacleContactTime += Time.fixedDeltaTime;

            if (obstacleContactTime >= maxWallContactTime)
            {
                if (Time.time - lastCollisionPenaltyTime >= maxWallContactTime)
                {
                    ProcessCollision();
                    obstacleContactTime = 0f;
                }
            }
        }
    }
    #endregion

    #region Metodos de Colision

    /// <summary>
    /// Maneja el evento de colision con obstaculos
    /// </summary>
    /// <param name="collision">Informacion de la colision</param>
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            float timeSinceSpawn = Time.time - spawnTime;
            if (timeSinceSpawn < geneticAlgorithm.invincibilityTime)
                return;

            isCollidingWithObstacle = true;
            obstacleContactTime = 0f;
            ProcessCollision(); // Nuevo metodo centralizado
        }
    }

    /// <summary>
    /// Maneja el evento de salida de colision con obstaculos
    /// </summary>
    /// <param name="collision">Informacion de la colision</param>
    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle"))
        {
            isCollidingWithObstacle = false;
            obstacleContactTime = 0f;
        }
    }

    /// <summary>
    /// Procesa las colisiones y aplica penalizaciones o muerte segun corresponda
    /// </summary>
    void ProcessCollision()
    {
        currentCollisions++;

        if (currentCollisions >= geneticAlgorithm.maxCollisions)
        {
            isDead = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            fitness -= 5f;
            lastCollisionPenaltyTime = Time.time;
        }
    }
    #endregion

    #region Metodos de Reset

    /// <summary>
    /// Reinicia todas las variables del NPC a su estado inicial
    /// </summary>
    public void Reset()
    {
        if (rb != null)
        {
            transform.position = startPosition;
            transform.rotation = startRotation;
            lastPosition = startPosition;

            isDead = false;
            fitness = 0;
            totalDistance = 0;
            timeAlive = 0;
            idleTime = 0f;
            successfulJumps = 0;
            lastJumpTime = -1f;
            correctJumps = 0;
            incorrectJumps = 0;
            totalCheckpointRewards = 0f;

            // Resetear variables anti-loop y exploracion
            positionHistory.Clear();
            lastCheckpointTime = 0f;
            lastCheckpointPosition = startPosition;
            visitedCells.Clear();
            uniqueAreasVisited = 0;
            distanceFromStart = 0f;
            consecutiveCircles = 0;
            totalRotation = 0f;
            lastAngle = transform.rotation.eulerAngles.y;
            spawnTime = Time.time;
            currentCollisions = 0;
            isCollidingWithObstacle = false;
            obstacleContactTime = 0f;
            lastCollisionPenaltyTime = 0f;

            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Resetear checkpoints
            if (CheckpointSystem.Instance != null)
            {
                CheckpointSystem.Instance.ResetNPCCheckpoints(this);
            }
        }
        else
        {
            Debug.LogError("No se encontro el Rigidbody");
        }
    }
    #endregion

    #region Metodos de Limpieza

    /// <summary>
    /// Limpia recursos y desregistra el NPC de los sistemas al destruirse
    /// </summary>
    void OnDestroy()
    {
        // Desregistrar del sistema de checkpoints
        if (CheckpointSystem.Instance != null)
        {
            CheckpointSystem.Instance.UnregisterNPC(this);
        }
    }
    #endregion

    #region Metodos de Debugging

    /// <summary>
    /// Dibuja gizmos para visualizar el comportamiento del NPC en el editor
    /// </summary>
    void OnDrawGizmos()
    {
        if (Application.isPlaying && positionHistory.Count > 1)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < positionHistory.Count - 1; i++)
            {
                Gizmos.DrawLine(positionHistory[i], positionHistory[i + 1]);
            }

            // Dibujar area de deteccion de loops
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(lastCheckpointPosition, loopDetectionRadius);
        }
    }
    #endregion
}