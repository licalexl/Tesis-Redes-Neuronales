using UnityEngine;

/// <summary>
/// Controlador de NPC simplificado para gameplay final.
/// Versión independiente sin dependencias del sistema de entrenamiento.
/// </summary>
public class GameplayNPC : MonoBehaviour
{
    #region Variables de Red Neuronal
    [Header("Red Neuronal")]
    [Tooltip("Red neuronal que controla el comportamiento del NPC")]
    public NeuralNetwork brain;

    [Tooltip("Valores de los sensores (entrada para la red neuronal)")]
    public float[] inputs = new float[8]; // 7 sensores + 1 entrada constante

    [Tooltip("Valores de salida de la red neuronal: avanzar, girar izquierda, girar derecha, saltar")]
    public float[] outputs = new float[4];
    #endregion

    #region Variables de Movimiento
    [Header("Configuración de Movimiento")]
    [Tooltip("Velocidad máxima de movimiento")]
    public float moveSpeed = 5f;

    [Tooltip("Velocidad mínima para no considerarse inactivo")]
    public float minSpeed = 0.5f;

    [Tooltip("Velocidad de rotación en grados por segundo")]
    public float rotationSpeed = 120f;
    #endregion

    #region Variables de Salto
    [Header("Configuración de Salto")]
    [Tooltip("Fuerza de salto")]
    public float jumpForce = 5f;

    [Tooltip("Cooldown entre saltos")]
    public float jumpCooldown = 1f;

    [Tooltip("LayerMask para detectar el suelo")]
    public LayerMask groundLayer = 1;
    #endregion

    #region Variables de Sensores
    [Header("Configuración de Sensores")]
    [Tooltip("Distancia máxima de detección de los sensores")]
    public float sensorLength = 10f;

    [Tooltip("Desplazamiento hacia adelante de los sensores desde el centro del NPC")]
    public float sensorForwardOffset = 1.0f;

    [Tooltip("Altura de los sensores desde la base del NPC")]
    public float sensorHeight = 1.0f;

    [Tooltip("Altura del sensor inferior para detectar obstáculos saltables")]
    public float lowerSensorHeight = 0.2f;
    #endregion

    #region Variables Privadas
    private Rigidbody rb;
    private Animator animator;
    private bool isGrounded = false;
    private float lastJumpTime = -1f;
    private Vector3 lastPosition;
    private Vector3 startPosition;
    private Quaternion startRotation;

    // Estadísticas simples
    public float totalDistance = 0f;
    public float timeAlive = 0f;
    public int successfulJumps = 0;
    #endregion

    #region Métodos de Inicialización
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
    }

    void Start()
    {
        // Crear cerebro básico si no existe
        if (brain == null)
        {
            brain = new NeuralNetwork(8, 8, 6, 4);
            Debug.Log($"[{gameObject.name}] Cerebro básico creado");
        }

        // Inicializar arrays
        if (inputs == null || inputs.Length != 8)
            inputs = new float[8];

        if (outputs == null || outputs.Length != 4)
            outputs = new float[4];

        Debug.Log($"[{gameObject.name}] StandaloneGameplayNPC inicializado");
    }
    #endregion

    #region Métodos de Update
    void Update()
    {
        UpdateSensors();

        if (brain != null)
        {
            outputs = brain.FeedForward(inputs);
        }

        timeAlive += Time.deltaTime;
    }

    void FixedUpdate()
    {
        CheckGrounded();
        ApplyOutputs();
        UpdateStats();
    }
    #endregion

    #region Métodos de Sensores
    void UpdateSensors()
    {
        // Usar múltiples puntos de inicio para mayor precisión
        Vector3 basePos = transform.position;
        Vector3 sensorStartPos = basePos + Vector3.up * sensorHeight;
        Vector3 lowerSensorStartPos = basePos + Vector3.up * lowerSensorHeight;

        // También usar sensores con offset para detectar objetos más lejanos
        Vector3 offsetSensorStartPos = basePos + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;

        // 5 sensores direccionales - usando el punto base SIN offset
        for (int i = 0; i < 5; i++)
        {
            RaycastHit hit;
            Vector3 sensorDirection = Quaternion.Euler(0, -90 + 45 * i, 0) * transform.forward;

            // Intentar raycast desde la posición base primero
            if (Physics.Raycast(sensorStartPos, sensorDirection, out hit, sensorLength))
            {
                inputs[i] = 1 - (hit.distance / sensorLength);
                Debug.DrawRay(sensorStartPos, sensorDirection * hit.distance, Color.red);
            }
            // Si no detecta nada desde la base, intentar desde la posición con offset
            else if (Physics.Raycast(offsetSensorStartPos, sensorDirection, out hit, sensorLength))
            {
                // Ajustar la distancia considerando el offset
                float adjustedDistance = hit.distance + sensorForwardOffset;
                inputs[i] = 1 - (adjustedDistance / sensorLength);
                Debug.DrawRay(offsetSensorStartPos, sensorDirection * hit.distance, Color.yellow);
            }
            else
            {
                inputs[i] = 0f;
                Debug.DrawRay(sensorStartPos, sensorDirection * sensorLength, Color.white);
            }
        }

        // Sensor frontal bajo - doble verificación
        RaycastHit lowerHit;
        bool detectedLow = false;

        // Primero desde la posición base
        if (Physics.Raycast(lowerSensorStartPos, transform.forward, out lowerHit, sensorLength))
        {
            inputs[5] = 1 - (lowerHit.distance / sensorLength);
            Debug.DrawRay(lowerSensorStartPos, transform.forward * lowerHit.distance, Color.yellow);
            detectedLow = true;
        }
        // Si no detecta desde la base, verificar desde posición con offset
        else if (Physics.Raycast(lowerSensorStartPos + transform.forward * sensorForwardOffset, transform.forward, out lowerHit, sensorLength))
        {
            float adjustedDistance = lowerHit.distance + sensorForwardOffset;
            inputs[5] = 1 - (adjustedDistance / sensorLength);
            Debug.DrawRay(lowerSensorStartPos + transform.forward * sensorForwardOffset, transform.forward * lowerHit.distance, Color.gray);
            detectedLow = true;
        }

        if (!detectedLow)
        {
            inputs[5] = 0f;
            Debug.DrawRay(lowerSensorStartPos, transform.forward * sensorLength, Color.blue);
        }

        // Sensor frontal alto - doble verificación
        Vector3 upperSensorStartPos = basePos + Vector3.up * (sensorHeight * 1.5f);
        RaycastHit upperHit;
        bool detectedHigh = false;

        // Primero desde la posición base
        if (Physics.Raycast(upperSensorStartPos, transform.forward, out upperHit, sensorLength))
        {
            inputs[6] = 1 - (upperHit.distance / sensorLength);
            Debug.DrawRay(upperSensorStartPos, transform.forward * upperHit.distance, Color.magenta);
            detectedHigh = true;
        }
        // Si no detecta desde la base, verificar desde posición con offset
        else if (Physics.Raycast(upperSensorStartPos + transform.forward * sensorForwardOffset, transform.forward, out upperHit, sensorLength))
        {
            float adjustedDistance = upperHit.distance + sensorForwardOffset;
            inputs[6] = 1 - (adjustedDistance / sensorLength);
            Debug.DrawRay(upperSensorStartPos + transform.forward * sensorForwardOffset, transform.forward * upperHit.distance, Color.red);
            detectedHigh = true;
        }

        if (!detectedHigh)
        {
            inputs[6] = 0f;
            Debug.DrawRay(upperSensorStartPos, transform.forward * sensorLength, Color.cyan);
        }

        // Entrada constante (bias)
        inputs[7] = 1f;
    }
    #endregion

    #region Métodos de Movimiento
    void ApplyOutputs()
    {
        if (outputs.Length < 4) return;

        float forwardSpeed = Mathf.Clamp(outputs[0] + 0.3f, 0, 1) * moveSpeed;
        float turnAmount = (outputs[1] - outputs[2]) * rotationSpeed;

        // Asegurar velocidad mínima
        if (forwardSpeed > 0)
        {
            forwardSpeed = Mathf.Max(forwardSpeed, minSpeed);
        }

        // Aplicar movimiento
        rb.velocity = new Vector3(0, rb.velocity.y, 0) + transform.forward * forwardSpeed;
        transform.Rotate(Vector3.up * turnAmount * Time.fixedDeltaTime);

        // Salto
        if (outputs[3] > 0.5f && CanJump())
        {
            Jump();
        }

        // Actualizar animador si existe
        if (animator != null)
        {
            //animator.SetFloat("Speed", forwardSpeed / moveSpeed);
            //animator.SetFloat("Turn", turnAmount / rotationSpeed);
           // animator.SetBool("IsGrounded", isGrounded);
        }
    }

    bool CanJump()
    {
        return isGrounded && (Time.time - lastJumpTime > jumpCooldown);
    }

    void Jump()
    {
        bool hasLowObstacle = inputs[5] > 0.3f;
        bool hasHighObstacle = inputs[6] > 0.3f;

        if (hasLowObstacle && !hasHighObstacle)
        {
            successfulJumps++;
        }

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        lastJumpTime = Time.time;
        isGrounded = false;
    }

    void CheckGrounded()
    {
        float rayDistance = 0.1f;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.05f;

        RaycastHit hit;
        isGrounded = Physics.Raycast(rayOrigin, Vector3.down, out hit, rayDistance, groundLayer);

        Debug.DrawRay(rayOrigin, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
    }
    #endregion

    #region Métodos de Estadísticas
    void UpdateStats()
    {
        float distanceMoved = Vector3.Distance(transform.position, lastPosition);
        totalDistance += distanceMoved;
        lastPosition = transform.position;
    }
    #endregion

    #region Métodos Públicos
    /// <summary>
    /// Establece el cerebro del NPC
    /// </summary>
    public void SetBrain(NeuralNetwork newBrain)
    {
        brain = newBrain;
        Debug.Log($"[{gameObject.name}] Nuevo cerebro asignado");
    }

    /// <summary>
    /// Resetea la posición del NPC manteniendo el cerebro
    /// </summary>
    public void ResetPosition()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        lastPosition = startPosition;

        totalDistance = 0f;
        timeAlive = 0f;
        successfulJumps = 0;

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Debug.Log($"[{gameObject.name}] Posición reseteada");
    }

    /// <summary>
    /// Obtiene estadísticas básicas del NPC
    /// </summary>
    public string GetStats()
    {
        return $"Tiempo: {timeAlive:F1}s, Distancia: {totalDistance:F1}, Saltos: {successfulJumps}";
    }

    /// <summary>
    /// Verifica si el NPC tiene un cerebro válido
    /// </summary>
    public bool HasValidBrain()
    {
        return brain != null;
    }
    #endregion

    #region Debug
    void OnDrawGizmos()
    {
        // Dibujar sensores en el editor
        if (Application.isPlaying)
        {
            Gizmos.color = Color.blue;
            Vector3 sensorPos = transform.position + transform.forward * sensorForwardOffset + Vector3.up * sensorHeight;
            Gizmos.DrawWireSphere(sensorPos, 0.1f);
        }
    }
    #endregion
}