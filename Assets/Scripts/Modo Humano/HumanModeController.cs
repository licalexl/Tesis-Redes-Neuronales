using UnityEngine;

/// <summary>
/// Controlador que permite al jugador tomar control manual de un NPC durante la simulacion.
/// Incluye movimiento suavizado, control de camara y visualizacion de sensores en tiempo real.
/// </summary>
public class HumanModeController : MonoBehaviour
{
    #region Referencias y Configuracion
    [Header("Referencias")]
    [Tooltip("Referencia al algoritmo genetico")]
    public NPCGeneticAlgorithm geneticAlgorithm;

    [Tooltip("Camara principal que sera posicionada para control humano")]
    public Camera mainCamera;

    [Tooltip("Referencia al grabador de demostraciones")]
    public HumanDemonstrationRecorder demonstrationRecorder;

    /// <summary>
    /// Componente de movimiento del jugador
    /// </summary>
    public PlayerMovement playerMovement;

    /// <summary>
    /// Controlador de camara del jugador
    /// </summary>
    public CameraController cameraController;
    #endregion

    #region Variables Privadas de UI
    /// <summary>
    /// Fuerza repaint de UI cada frame
    /// </summary>
    private bool forceUIUpdate = true;

    /// <summary>
    /// Tiempo de ultima actualizacion de UI
    /// </summary>
    private float lastUIUpdateTime = 0f;

    /// <summary>
    /// Intervalo de actualizacion de UI en segundos
    /// </summary>
    private float uiUpdateInterval = 0.1f; // 10 veces por segundo
    #endregion

    #region Configuracion de Movimiento
    [Header("Configuracion de Movimiento - OPTIMIZADO")]
    [Tooltip("Aceleracion de movimiento - mayor = mas responsivo")]
    [Range(1f, 50f)]
    public float moveAcceleration = 25f;

    [Tooltip("Velocidad maxima de movimiento")]
    [Range(1f, 12f)]
    public float maxMoveSpeed = 6f;

    [Tooltip("Amortiguacion de movimiento - mayor = parada mas rapida")]
    [Range(15f, 40f)]
    public float moveDamping = 25f;

    [Tooltip("Velocidad de rotacion en grados por segundo")]
    [Range(90f, 200f)]
    public float rotationSpeed = 150f;

    [Tooltip("Suavizado de rotacion")]
    [Range(1f, 20f)]
    public float rotationSmoothing = 8f;
    #endregion

    #region Configuracion de Salto
    [Header("Configuracion de Salto - OPTIMIZADO")]
    [Tooltip("Fuerza de salto")]
    [Range(0f, 6f)]
    public float jumpForce = 10f;

    [Tooltip("Tiempo de espera entre saltos")]
    [Range(0.3f, 1.2f)]
    public float jumpCooldown = 0.5f;
    #endregion

    #region Configuracion de Camara
    [Header("Configuracion de Camara - OPTIMIZADO")]
    [Tooltip("Desplazamiento de camara desde el NPC controlado")]
    public Vector3 cameraOffset = new Vector3(0, 3, -5);

    [Tooltip("Objetivo de desplazamiento de vista de camara")]
    public Vector3 cameraLookOffset = new Vector3(0, 1f, 0);

    [Tooltip("Suavizado de seguimiento de camara - menor = mas responsivo")]
    [Range(1f, 12f)]
    public float cameraFollowSmoothing = 6f;

    [Tooltip("Suavizado de rotacion de camara")]
    [Range(1f, 12f)]
    public float cameraRotationSmoothing = 8f;
    #endregion

    #region Configuracion de Visualizacion de Sensores
    [Header("Visualizacion de Sensores")]
    [Tooltip("Mostrar rayos de sensores durante modo humano")]
    public bool showSensorVisualization = true;

    [Header("Visualizacion de Sensores - VISTA DE JUEGO")]
    [Tooltip("Mostrar rayos de sensores en vista de juego")]
    public bool showSensorsInGame = true;

    [Tooltip("Ancho de linea para rayos de sensores")]
    [Range(0.01f, 0.1f)]
    public float sensorLineWidth = 0.03f;

    [Tooltip("Material para lineas de sensores")]
    public Material sensorLineMaterial;
    #endregion

    #region Variables de Estado
    /// <summary>
    /// Indica si el modo humano esta activo
    /// </summary>
    [HideInInspector]
    public bool isActive = false;

    /// <summary>
    /// NPC actualmente controlado por el jugador
    /// </summary>
    [HideInInspector]
    public NPCController controlledNPC;
    #endregion

    #region Variables Privadas de Sensores
    /// <summary>
    /// Array de LineRenderers para visualizar sensores
    /// </summary>
    private LineRenderer[] sensorLines = new LineRenderer[7];

    /// <summary>
    /// Contenedor de objetos de visualizacion de sensores
    /// </summary>
    private GameObject sensorContainer;

    /// <summary>
    /// Valores actuales de sensores para mostrar
    /// </summary>
    private float[] currentSensorValues = new float[8];

    /// <summary>
    /// Estados de impacto de sensores para retroalimentacion visual
    /// </summary>
    private bool[] sensorHits = new bool[7];
    #endregion

    #region Variables Privadas de Estado
    /// <summary>
    /// Posicion original de la camara antes del modo humano
    /// </summary>
    private Vector3 originalCameraPosition;

    /// <summary>
    /// Rotacion original de la camara antes del modo humano
    /// </summary>
    private Quaternion originalCameraRotation;

    /// <summary>
    /// Estado de pausa del algoritmo antes del modo humano
    /// </summary>
    private bool wasAlgorithmPaused;
    #endregion

    #region Variables Privadas de Movimiento Suavizado
    /// <summary>
    /// Velocidad actual para movimiento suavizado
    /// </summary>
    private Vector3 currentVelocity = Vector3.zero;

    /// <summary>
    /// Velocidad de rotacion actual
    /// </summary>
    private float currentRotationVelocity = 0f;

    /// <summary>
    /// Rotacion objetivo en el eje Y
    /// </summary>
    private float targetRotationY = 0f;

    /// <summary>
    /// Suavizado de input
    /// </summary>
    private float inputSmoothing = 8f;

    /// <summary>
    /// Input horizontal suavizado
    /// </summary>
    private float smoothHorizontal = 0f;

    /// <summary>
    /// Input vertical suavizado
    /// </summary>
    private float smoothVertical = 0f;

    /// <summary>
    /// Input horizontal actual
    /// </summary>
    private float currentHorizontalInput = 0f;

    /// <summary>
    /// Input vertical actual
    /// </summary>
    private float currentVerticalInput = 0f;
    #endregion

    #region Variables Privadas de Camara
    /// <summary>
    /// Velocidad de camara para movimiento suavizado
    /// </summary>
    private Vector3 cameraVelocity = Vector3.zero;

    /// <summary>
    /// Posicion objetivo de la camara
    /// </summary>
    private Vector3 targetCameraPosition;

    /// <summary>
    /// Rotacion objetivo de la camara
    /// </summary>
    private Quaternion targetCameraRotation;
    #endregion

    #region Variables Privadas de Input
    /// <summary>
    /// Input de salto detectado
    /// </summary>
    private bool jumpInput = false;

    /// <summary>
    /// Tiempo del ultimo salto realizado
    /// </summary>
    private float lastJumpTime = -1f;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa las referencias y configuracion inicial del controlador
    /// </summary>
    void Start()
    {
        // Buscar algoritmo genetico si no esta asignado
        if (geneticAlgorithm == null)
        {
            geneticAlgorithm = FindObjectOfType<NPCGeneticAlgorithm>();
        }

        // Buscar camara principal si no esta asignada
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // Buscar grabador de demostraciones si no esta asignado
        if (demonstrationRecorder == null)
        {
            demonstrationRecorder = GetComponent<HumanDemonstrationRecorder>();
        }

        // Almacenar transformacion original de camara
        if (mainCamera != null)
        {
            originalCameraPosition = mainCamera.transform.position;
            originalCameraRotation = mainCamera.transform.rotation;
        }

        if (showSensorsInGame)
        {
            InitializeSensorVisualization();
        }

        Debug.Log("Controlador de Modo Humano inicializado. Presiona R para alternar modo humano.");
    }
    #endregion

    #region Metodos de Update

    /// <summary>
    /// Maneja el input del usuario y actualiza la visualizacion cada frame
    /// </summary>
    void Update()
    {
        // Verificar input para alternar modo
        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleHumanMode();
        }

        // Manejar input humano si el modo esta activo
        if (isActive && controlledNPC != null)
        {
            HandleHumanInput();

            // Actualizar visualizacion de sensores
            if (showSensorVisualization)
            {
                UpdateSensorVisualization();
            }

            // NUEVO: Forzar actualización de UI en tiempo real
            if (Time.time - lastUIUpdateTime > uiUpdateInterval)
            {
                lastUIUpdateTime = Time.time;
                forceUIUpdate = true;
            }
        }
    }

    /// <summary>
    /// Actualiza la camara en LateUpdate para evitar jittering
    /// </summary>
    void LateUpdate()
    {
        if (isActive && controlledNPC != null)
        {
            UpdateCameraSmooth();
        }
    }

    /// <summary>
    /// Aplica movimiento en FixedUpdate para fisica consistente
    /// </summary>
    void FixedUpdate()
    {
        if (isActive && controlledNPC != null)
        {
            ApplySmoothedMovement();

            UpdateNPCMetrics();
        }
    }
    #endregion

    #region Metodos de Control de Modo Humano

    /// <summary>
    /// Alterna entre modo humano activo e inactivo
    /// </summary>
    void ToggleHumanMode()
    {
        if (isActive)
        {
            DeactivateHumanMode();
        }
        else
        {
            ActivateHumanMode();
        }
    }

    /// <summary>
    /// Activa el modo humano y configura el control manual
    /// </summary>
    void ActivateHumanMode()
    {
        // Encontrar el mejor NPC vivo para controlar
        NPCController bestNPC = FindBestAliveNPC();

        if (bestNPC == null)
        {
            Debug.LogWarning("No se encontraron NPCs vivos para controlar");
            return;
        }

        if (showSensorsInGame && sensorContainer != null)
        {
            sensorContainer.SetActive(true);
        }

        // Almacenar estado de pausa del algoritmo y pausarlo
        wasAlgorithmPaused = geneticAlgorithm.isPaused;
        geneticAlgorithm.isPaused = true;

        // Establecer NPC controlado
        controlledNPC = bestNPC;

        // Deshabilitar actualizacion de IA del NPC
        controlledNPC.enabled = false;

        // Inicializar movimiento suavizado
        currentVelocity = Vector3.zero;
        currentRotationVelocity = 0f;
        targetRotationY = controlledNPC.transform.eulerAngles.y;

        // Resetear suavizado de input
        smoothHorizontal = 0f;
        smoothVertical = 0f;

        // Inicializar camara
        cameraController.modoHumano = true;
        playerMovement.modoHumano = true;
        playerMovement.controller.enabled = false;
        InitializeCamera();

        // Activar modo humano
        isActive = true;

        Debug.Log($"Modo humano activado. Controlando NPC: {controlledNPC.name}");
        Debug.Log("Controles: WASD para mover, Espacio para saltar, R para salir");
    }

    /// <summary>
    /// Desactiva el modo humano y restaura el control de IA
    /// </summary>
    void DeactivateHumanMode()
    {
        if (controlledNPC != null)
        {
            // Re-habilitar IA del NPC
            controlledNPC.enabled = true;
            controlledNPC = null;
        }

        // Restaurar posicion original de camara suavemente
        if (mainCamera != null)
        {
            StartCoroutine(SmoothCameraReturn());
        }

        // Restaurar estado de pausa del algoritmo
        geneticAlgorithm.isPaused = wasAlgorithmPaused;

        if (showSensorsInGame && sensorContainer != null)
        {
            sensorContainer.SetActive(false);
        }

        // Desactivar modo humano
        cameraController.modoHumano = false;
        playerMovement.modoHumano = false;
        playerMovement.controller.enabled = true;
        isActive = false;

        Debug.Log("Modo humano desactivado. Regresando a simulacion de IA.");
    }

    /// <summary>
    /// Encuentra el mejor NPC vivo basado en fitness para controlar
    /// </summary>
    /// <returns>El NPC con mayor fitness que esta vivo, o null si no hay ninguno</returns>
    NPCController FindBestAliveNPC()
    {
        if (geneticAlgorithm.population == null || geneticAlgorithm.population.Count == 0)
            return null;

        NPCController bestNPC = null;
        float bestFitness = float.MinValue;

        foreach (var npc in geneticAlgorithm.population)
        {
            if (npc != null && !npc.isDead && npc.fitness > bestFitness)
            {
                bestNPC = npc;
                bestFitness = npc.fitness;
            }
        }

        return bestNPC;
    }
    #endregion

    #region Metodos de Visualizacion de Sensores

    /// <summary>
    /// Inicializa la visualizacion de sensores con LineRenderers
    /// </summary>
    void InitializeSensorVisualization()
    {
        // Crear contenedor para los LineRenderers
        sensorContainer = new GameObject("SensorVisualization");
        sensorContainer.transform.SetParent(transform);

        // Crear LineRenderer para cada sensor
        for (int i = 0; i < sensorLines.Length; i++)
        {
            GameObject sensorLineGO = new GameObject($"SensorLine_{i}");
            sensorLineGO.transform.SetParent(sensorContainer.transform);

            LineRenderer lr = sensorLineGO.AddComponent<LineRenderer>();

            // Configurar LineRenderer
            lr.material = sensorLineMaterial != null ? sensorLineMaterial : CreateDefaultSensorMaterial();
            lr.startWidth = sensorLineWidth;
            lr.endWidth = sensorLineWidth;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.enabled = false; // Empezar desactivado

            sensorLines[i] = lr;
        }
    }

    /// <summary>
    /// Crea un material por defecto para los sensores si no se asigno uno
    /// </summary>
    /// <returns>Material basico para visualizacion de sensores</returns>
    Material CreateDefaultSensorMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = Color.yellow;
        return mat;
    }

    /// <summary>
    /// Actualiza la visualizacion de todos los sensores del NPC controlado
    /// </summary>
    void UpdateSensorVisualization()
    {
        if (controlledNPC == null || !showSensorVisualization)
        {
            // Ocultar lineas si no se debe mostrar
            if (showSensorsInGame && sensorLines != null)
            {
                foreach (var line in sensorLines)
                {
                    if (line != null) line.enabled = false;
                }
            }
            return;
        }

        // Copiar valores actuales del NPC
        if (controlledNPC.inputs != null && controlledNPC.inputs.Length >= 8)
        {
            System.Array.Copy(controlledNPC.inputs, currentSensorValues, 8);
        }

        // Obtener valores del NPC
        float npcSensorLength = controlledNPC.sensorLength;
        float npcSensorForwardOffset = controlledNPC.sensorForwardOffset;
        float npcSensorHeight = controlledNPC.sensorHeight;
        float npcLowerSensorHeight = controlledNPC.lowerSensorHeight;

        // Posiciones de sensores
        Vector3 sensorStartPos = controlledNPC.transform.position +
                                controlledNPC.transform.forward * npcSensorForwardOffset +
                                Vector3.up * npcSensorHeight;

        Vector3 lowerSensorStartPos = controlledNPC.transform.position +
                                     controlledNPC.transform.forward * npcSensorForwardOffset +
                                     Vector3.up * npcLowerSensorHeight;

        Vector3 upperSensorStartPos = controlledNPC.transform.position +
                                     controlledNPC.transform.forward * npcSensorForwardOffset +
                                     Vector3.up * (npcSensorHeight * 1.5f);

        // Actualizar los 5 sensores direccionales
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            Vector3 sensorDirection = Quaternion.Euler(0, -90 + 45 * i, 0) * controlledNPC.transform.forward;

            Color rayColor;
            float drawDistance;
            Vector3 endPosition;

            if (Physics.Raycast(sensorStartPos, sensorDirection, out hit, npcSensorLength))
            {
                sensorHits[i] = true;
                drawDistance = hit.distance;
                endPosition = hit.point;

                // Color basado en intensidad del sensor
                float intensity = currentSensorValues[i];
                rayColor = Color.Lerp(Color.green, Color.red, intensity);
            }
            else
            {
                sensorHits[i] = false;
                drawDistance = npcSensorLength;
                endPosition = sensorStartPos + sensorDirection * drawDistance;
                rayColor = Color.white;
            }

            // Actualizar Debug rays (para Scene view)
            Debug.DrawRay(sensorStartPos, sensorDirection * drawDistance, rayColor, 0.1f);

            // Actualizar LineRenderer para Game view
            if (showSensorsInGame && sensorLines[i] != null)
            {
                sensorLines[i].enabled = true;
                sensorLines[i].SetPosition(0, sensorStartPos);
                sensorLines[i].SetPosition(1, endPosition);
                sensorLines[i].material.color = rayColor;
            }
        }

        // Sensor bajo (indice 5)
        UpdateSpecialSensor(5, lowerSensorStartPos, controlledNPC.transform.forward,
                           npcSensorLength, Color.blue, Color.yellow);

        // Sensor alto (indice 6)  
        UpdateSpecialSensor(6, upperSensorStartPos, controlledNPC.transform.forward,
                           npcSensorLength, Color.cyan, Color.magenta);
    }

    /// <summary>
    /// Actualiza un sensor especial (bajo o alto) con colores personalizados
    /// </summary>
    /// <param name="index">Indice del sensor en el array</param>
    /// <param name="startPos">Posicion inicial del raycast</param>
    /// <param name="direction">Direccion del raycast</param>
    /// <param name="maxDistance">Distancia maxima del raycast</param>
    /// <param name="noHitColor">Color cuando no hay impacto</param>
    /// <param name="hitColor">Color cuando hay impacto</param>
    void UpdateSpecialSensor(int index, Vector3 startPos, Vector3 direction, float maxDistance,
                            Color noHitColor, Color hitColor)
    {
        RaycastHit hit;
        Color rayColor;
        float drawDistance;
        Vector3 endPosition;

        if (Physics.Raycast(startPos, direction, out hit, maxDistance))
        {
            sensorHits[index] = true;
            drawDistance = hit.distance;
            endPosition = hit.point;

            float intensity = currentSensorValues[index];
            rayColor = intensity > 0.3f ? hitColor : Color.green;
        }
        else
        {
            sensorHits[index] = false;
            drawDistance = maxDistance;
            endPosition = startPos + direction * drawDistance;
            rayColor = noHitColor;
        }

        // Debug ray para Scene view
        Debug.DrawRay(startPos, direction * drawDistance, rayColor, 0.1f);

        // LineRenderer para Game view
        if (showSensorsInGame && sensorLines[index] != null)
        {
            sensorLines[index].enabled = true;
            sensorLines[index].SetPosition(0, startPos);
            sensorLines[index].SetPosition(1, endPosition);
            sensorLines[index].material.color = rayColor;
        }
    }
    #endregion

    #region Metodos de Input y Movimiento

    /// <summary>
    /// Captura y procesa el input del jugador
    /// </summary>
    void HandleHumanInput()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        jumpInput = Input.GetKeyDown(KeyCode.Space);

        // Almacenar para aplicar en FixedUpdate
        currentHorizontalInput = horizontal;
        currentVerticalInput = vertical;

        // Aplicar salto inmediatamente para mejor responsividad
        if (jumpInput && CanJump())
        {
            ApplyJump();
        }
    }

    /// <summary>
    /// Aplica movimiento suavizado al NPC controlado
    /// </summary>
    void ApplySmoothedMovement()
    {
        if (controlledNPC == null) return;

        Rigidbody rb = controlledNPC.GetComponent<Rigidbody>();
        if (rb == null) return;

        // Movimiento mas directo y responsivo
        Vector3 moveDirection = controlledNPC.transform.forward * currentVerticalInput;
        Vector3 targetVelocity = moveDirection * maxMoveSpeed;

        // Aplicar movimiento con mejor control
        Vector3 currentHorizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
        Vector3 velocityDifference = targetVelocity - currentHorizontalVelocity;

        // Control mas directo de la aceleracion
        float accelerationForce = moveAcceleration;
        if (currentVerticalInput == 0) accelerationForce = moveDamping; // Frenado mas rapido

        Vector3 forceToApply = velocityDifference * accelerationForce;
        rb.AddForce(forceToApply, ForceMode.Acceleration);

        // Rotacion simplificada y mas natural
        if (Mathf.Abs(currentHorizontalInput) > 0.1f)
        {
            float rotationAmount = currentHorizontalInput * rotationSpeed * Time.fixedDeltaTime;
            controlledNPC.transform.Rotate(0, rotationAmount, 0);
        }
    }
    #endregion

    #region Metodos de Salto

    /// <summary>
    /// Verifica si el NPC puede realizar un salto
    /// </summary>
    /// <returns>True si puede saltar, False en caso contrario</returns>
    bool CanJump()
    {
        return Time.time - lastJumpTime > jumpCooldown && IsNPCGrounded();
    }

    /// <summary>
    /// Verifica si el NPC esta tocando el suelo usando multiples raycasts
    /// </summary>
    /// <returns>True si esta en el suelo, False en caso contrario</returns>
    bool IsNPCGrounded()
    {
        Vector3 rayOrigin = controlledNPC.transform.position + Vector3.up * 0.1f;

        // Multiples raycast para mejor deteccion
        bool centerGrounded = Physics.Raycast(rayOrigin, Vector3.down, 0.2f);
        bool frontGrounded = Physics.Raycast(rayOrigin + controlledNPC.transform.forward * 0.3f, Vector3.down, 0.2f);
        bool backGrounded = Physics.Raycast(rayOrigin - controlledNPC.transform.forward * 0.3f, Vector3.down, 0.2f);

        return centerGrounded || frontGrounded || backGrounded;
    }

    /// <summary>
    /// Ejecuta un salto aplicando fuerza hacia arriba
    /// </summary>
    void ApplyJump()
    {
        Rigidbody rb = controlledNPC.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Limpiar velocidad vertical existente para salto consistente
            Vector3 currentVelocity = rb.velocity;
            rb.velocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // Aplicar impulso de salto
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            lastJumpTime = Time.time;

            Debug.Log("Salto aplicado"); // Para debugging
        }
    }
    #endregion

    #region Metodos de Camara

    /// <summary>
    /// Inicializa la posicion y configuracion de la camara
    /// </summary>
    void InitializeCamera()
    {
        if (mainCamera == null || controlledNPC == null) return;

        UpdateCameraPosition();
    }

    /// <summary>
    /// Actualiza la camara con movimiento suavizado
    /// </summary>
    void UpdateCameraSmooth()
    {
        if (mainCamera == null || controlledNPC == null) return;

        UpdateCameraPosition();
    }

    /// <summary>
    /// Actualiza la posicion y rotacion de la camara siguiendo al NPC
    /// </summary>
    void UpdateCameraPosition()
    {
        Vector3 targetPosition = controlledNPC.transform.position +
                               controlledNPC.transform.TransformDirection(cameraOffset);

        Vector3 lookTarget = controlledNPC.transform.position + cameraLookOffset;

        float smoothSpeed = cameraFollowSmoothing * Time.deltaTime;

        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position,
            targetPosition,
            smoothSpeed
        );

        Quaternion targetRotation = Quaternion.LookRotation(lookTarget - mainCamera.transform.position);
        mainCamera.transform.rotation = Quaternion.Slerp(
            mainCamera.transform.rotation,
            targetRotation,
            cameraRotationSmoothing * Time.deltaTime
        );
    }

    /// <summary>
    /// Corrutina para retornar la camara suavemente a su posicion original
    /// </summary>
    /// <returns>Enumerador para la corrutina</returns>
    System.Collections.IEnumerator SmoothCameraReturn()
    {
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;

        float duration = 0.8f; // Mas rapido
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            t = 1f - Mathf.Pow(1f - t, 3f); // Ease-out cubic

            mainCamera.transform.position = Vector3.Lerp(startPos, originalCameraPosition, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, originalCameraRotation, t);

            yield return null;
        }

        mainCamera.transform.position = originalCameraPosition;
        mainCamera.transform.rotation = originalCameraRotation;
    }
    #endregion

    #region Metodos de Limpieza

    /// <summary>
    /// Limpia recursos al destruir el objeto
    /// </summary>
    void OnDestroy()
    {
        if (sensorContainer != null)
        {
            Destroy(sensorContainer);
        }

        // Guardar configuración de ventanas
        mainInfoWindow.SaveConfig();
        sensorWindow.SaveConfig();
    }
    #endregion

    #region Metodos de UI

    void Awake()
    {
        // Cargar configuración guardada
        mainInfoWindow.LoadConfig();
        sensorWindow.LoadConfig();
    }

    /// <summary>
    /// Dibuja la interfaz de usuario con informacion del modo humano y sensores
    /// </summary>

    [Header("Configuración de Ventanas GUI")]
    public WindowConfig mainInfoWindow = new WindowConfig("Modo Humano", new Rect(10, 10, 300, 150));
    public WindowConfig sensorWindow = new WindowConfig("Sensores", new Rect(Screen.width - 320, 10, 310, 240));
    void OnGUI()
    {
        if (isActive)
        {
            if (forceUIUpdate)
            {
                forceUIUpdate = false;
                GUI.changed = true;
            }

            if (mainInfoWindow.enabled)
            {
                mainInfoWindow.windowRect = GUI.Window(0, mainInfoWindow.windowRect, DrawMainInfoWindow, mainInfoWindow.windowName);
            }

            if (showSensorVisualization && controlledNPC != null && sensorWindow.enabled)
            {
                sensorWindow.windowRect = GUI.Window(1, sensorWindow.windowRect, DrawSensorWindow, sensorWindow.windowName);
            }

            if (Event.current.type == EventType.Repaint)
            {
                forceUIUpdate = true;
            }
        }
    }

    /// <summary>
    /// Actualiza las métricas del NPC controlado en tiempo real
    /// </summary>
    void UpdateNPCMetrics()
    {
        if (controlledNPC == null) return;

        // Actualizar sensores manualmente para asegurar datos frescos
        if (controlledNPC.inputs != null && controlledNPC.inputs.Length >= 8)
        {
            // Forzar actualización de sensores del NPC
            controlledNPC.UpdateSensors();

            // Copiar valores actualizados
            System.Array.Copy(controlledNPC.inputs, currentSensorValues,
                Mathf.Min(controlledNPC.inputs.Length, currentSensorValues.Length));
        }
    }

    void DrawMainInfoWindow(int windowID)
    {
        // Mostrar indicador de modo humano
        GUI.color = Color.green;
        GUI.Label(new Rect(10, 25, 290, 25), "MODO HUMANO ACTIVO - Presiona R para salir");
        GUI.Label(new Rect(10, 50, 280, 25), "Controles: WASD = Mover, Espacio = Saltar");

        if (controlledNPC != null)
        {
            GUI.Label(new Rect(10, 75, 280, 25), $"Controlando: {controlledNPC.name}");
            GUI.Label(new Rect(10, 100, 280, 25), $"Fitness Actual: {controlledNPC.fitness:F1}");
        }

        // Mostrar estado de grabación
        if (demonstrationRecorder != null)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, 125, 280, 25), "Grabación de Demo: ACTIVA");
        }

        // Resizable y draggable
        if (mainInfoWindow.isResizable)
        {
            // Área de redimensionamiento en la esquina inferior derecha
            GUI.Box(new Rect(mainInfoWindow.windowRect.width - 15, mainInfoWindow.windowRect.height - 15, 10, 10), "");

            // Lógica de redimensionamiento
            Rect resizeRect = new Rect(mainInfoWindow.windowRect.width - 15, mainInfoWindow.windowRect.height - 15, 15, 15);
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.Box(resizeRect, "");
            GUI.color = Color.white;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeRect.Contains(currentEvent.mousePosition))
            {
                mainInfoWindow.isResizing = true;
            }

            if (mainInfoWindow.isResizing && currentEvent.type == EventType.MouseDrag)
            {
                mainInfoWindow.windowRect.width = Mathf.Clamp(currentEvent.mousePosition.x, mainInfoWindow.minSize.x, mainInfoWindow.maxSize.x);
                mainInfoWindow.windowRect.height = Mathf.Clamp(currentEvent.mousePosition.y, mainInfoWindow.minSize.y, mainInfoWindow.maxSize.y);
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                mainInfoWindow.isResizing = false;
            }
        }

        // Área para arrastrar
        if (mainInfoWindow.isDraggable)
        {
            GUI.DragWindow();
        }
    }

    void DrawSensorWindow(int windowID)
    {
        if (controlledNPC != null && controlledNPC.inputs != null)
        {
            System.Array.Copy(controlledNPC.inputs, currentSensorValues,
                Mathf.Min(controlledNPC.inputs.Length, currentSensorValues.Length));
        }

        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, 25, 280, 20), "SENSORES DIRECCIONALES:");

        for (int i = 0; i < 5; i++)
        {
            string direction = "";
            switch (i)
            {
                case 0: direction = "Extrema Izquierda"; break;
                case 1: direction = "Izquierda"; break;
                case 2: direction = "Frente"; break;
                case 3: direction = "Derecha"; break;
                case 4: direction = "Extrema Derecha"; break;
            }

            Color valueColor = currentSensorValues[i] > 0.3f ? Color.red : Color.green;
            GUI.color = valueColor;
            GUI.Label(new Rect(10, 45 + i * 20, 280, 20), $"{direction}: {currentSensorValues[i]:F2}");
        }
        // Sensores especiales
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, 150, 280, 20), $"Obstáculo Bajo: {currentSensorValues[5]:F2}");

        GUI.color = Color.magenta;
        GUI.Label(new Rect(10, 170, 280, 20), $"Obstáculo Alto: {currentSensorValues[6]:F2}");

        // Recomendación de salto
        bool shouldJump = currentSensorValues[5] > 0.3f && currentSensorValues[6] < 0.3f;
        GUI.color = shouldJump ? Color.green : Color.gray;
        GUI.Label(new Rect(10, 195, 280, 20), shouldJump ? "RECOMENDADO: SALTAR AHORA" : "RECOMENDADO: NO SALTAR");

        // Mostrar timestamp para verificar actualización
        GUI.color = Color.white;
        GUI.Label(new Rect(10, 215, 280, 20), $"Actualizado: {Time.time:F1}s");

        // Resizable y draggable
        if (sensorWindow.isResizable)
        {
            // Área de redimensionamiento en la esquina inferior derecha
            GUI.Box(new Rect(sensorWindow.windowRect.width - 15, sensorWindow.windowRect.height - 15, 10, 10), "");

            // Lógica de redimensionamiento
            Rect resizeRect = new Rect(sensorWindow.windowRect.width - 15, sensorWindow.windowRect.height - 15, 15, 15);
            GUI.color = new Color(1, 1, 1, 0.1f);
            GUI.Box(resizeRect, "");
            GUI.color = Color.white;

            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.MouseDown && resizeRect.Contains(currentEvent.mousePosition))
            {
                sensorWindow.isResizing = true;
            }

            if (sensorWindow.isResizing && currentEvent.type == EventType.MouseDrag)
            {
                sensorWindow.windowRect.width = Mathf.Clamp(currentEvent.mousePosition.x, sensorWindow.minSize.x, sensorWindow.maxSize.x);
                sensorWindow.windowRect.height = Mathf.Clamp(currentEvent.mousePosition.y, sensorWindow.minSize.y, sensorWindow.maxSize.y);
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                sensorWindow.isResizing = false;
            }
        }

        // Área para arrastrar
        if (sensorWindow.isDraggable)
        {
            GUI.DragWindow();
        }
    }

    // Agregar en OnDestroy:
  
    #endregion
}