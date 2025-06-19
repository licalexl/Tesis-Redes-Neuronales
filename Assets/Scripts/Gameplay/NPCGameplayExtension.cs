using UnityEngine;

/// <summary>
/// Extensión para el NPCController que modifica su comportamiento para gameplay final.
/// Permite usar NPCs entrenados sin la lógica de entrenamiento/muerte.
/// Se añade como componente adicional al GameObject del NPC.
/// </summary>
public class NPCGameplayExtension : MonoBehaviour
{
    #region Variables de Configuración
    [Header("Multiplicadores de Movimiento")]
    [Tooltip("Multiplicador de velocidad de movimiento")]
    [Range(0.1f, 5f)]
    public float speedMultiplier = 1f;

    [Tooltip("Multiplicador de velocidad de rotación")]
    [Range(0.1f, 5f)]
    public float rotationMultiplier = 1f;

    [Tooltip("Multiplicador de fuerza de salto")]
    [Range(0.1f, 5f)]
    public float jumpMultiplier = 1f;

    [Space]
    [Header("Modificadores de Sensores")]
    [Tooltip("Multiplicador de alcance de sensores")]
    [Range(0.5f, 3f)]
    public float sensorRangeMultiplier = 1f;

    [Space]
    [Header("Modos Especiales")]
    [Tooltip("Modo sigiloso (movimiento más lento y preciso)")]
    public bool stealthMode = false;

    [Tooltip("Modo agresivo (movimiento más rápido y directo)")]
    public bool aggressiveMode = false;

    [Tooltip("Modo explorador (sensores mejorados)")]
    public bool explorerMode = false;
    #endregion

    #region Variables de Estado
    [Header("Estado")]
    [SerializeField] private bool modifierActive = true;
    [SerializeField] private string currentMode = "Normal";
    #endregion

    #region Referencias
    private GameplayNPC npcController;
    private float originalMoveSpeed;
    private float originalRotationSpeed;
    private float originalJumpForce;
    private float originalSensorLength;
    private bool valuesStored = false;
    #endregion

    #region Inicialización
    void Start()
    {
        npcController = GetComponent<GameplayNPC>();
        if (npcController == null)
        {
            Debug.LogError($"[{gameObject.name}] StandaloneGameplayModifier requiere StandaloneGameplayNPC");
            enabled = false;
            return;
        }

        StoreOriginalValues();
        ApplyModifiers();
    }

    void StoreOriginalValues()
    {
        if (npcController == null) return;

        originalMoveSpeed = npcController.moveSpeed;
        originalRotationSpeed = npcController.rotationSpeed;
        originalJumpForce = npcController.jumpForce;
        originalSensorLength = npcController.sensorLength;

        valuesStored = true;
        Debug.Log($"[{gameObject.name}] Valores originales guardados");
    }
    #endregion

    #region Update
    void Update()
    {
        if (!modifierActive || !valuesStored) return;

        // Aplicar modificadores continuamente
        ApplyModifiers();

        // Detectar cambio de modo
        UpdateCurrentMode();
    }

    void UpdateCurrentMode()
    {
        string newMode = "Normal";

        if (stealthMode) newMode = "Sigiloso";
        else if (aggressiveMode) newMode = "Agresivo";
        else if (explorerMode) newMode = "Explorador";

        if (newMode != currentMode)
        {
            currentMode = newMode;
            OnModeChanged(newMode);
        }
    }
    #endregion

    #region Aplicación de Modificadores
    void ApplyModifiers()
    {
        if (npcController == null || !valuesStored) return;

        // Calcular multiplicadores finales basados en modos especiales
        float finalSpeedMult = speedMultiplier;
        float finalRotationMult = rotationMultiplier;
        float finalJumpMult = jumpMultiplier;
        float finalSensorMult = sensorRangeMultiplier;

        // Aplicar efectos de modos especiales
        if (stealthMode)
        {
            finalSpeedMult *= 0.6f;      // Más lento
            finalRotationMult *= 0.8f;   // Rotación más precisa
            finalSensorMult *= 1.3f;     // Mejor detección
        }
        else if (aggressiveMode)
        {
            finalSpeedMult *= 1.5f;      // Más rápido
            finalRotationMult *= 1.3f;   // Rotación más rápida
            finalJumpMult *= 1.2f;       // Saltos más potentes
        }
        else if (explorerMode)
        {
            finalSensorMult *= 2f;       // Sensores muy mejorados
            finalSpeedMult *= 1.1f;      // Ligeramente más rápido
        }

        // Aplicar valores modificados
        npcController.moveSpeed = originalMoveSpeed * finalSpeedMult;
        npcController.rotationSpeed = originalRotationSpeed * finalRotationMult;
        npcController.jumpForce = originalJumpForce * finalJumpMult;
        npcController.sensorLength = originalSensorLength * finalSensorMult;
    }

    void OnModeChanged(string newMode)
    {
        Debug.Log($"[{gameObject.name}] Modo cambiado a: {newMode}");

        // Efectos visuales o sonoros del cambio de modo podrían ir aquí
        switch (newMode)
        {
            case "Sigiloso":
                // Cambiar material, reducir ruido, etc.
                break;
            case "Agresivo":
                // Efectos de agresividad, cambio de color, etc.
                break;
            case "Explorador":
                // Efectos de exploración, brillos en sensores, etc.
                break;
        }
    }
    #endregion

    #region Métodos Públicos
    /// <summary>
    /// Activa o desactiva el modificador
    /// </summary>
    public void SetModifierActive(bool active)
    {
        modifierActive = active;

        if (!active && valuesStored)
        {
            // Restaurar valores originales
            RestoreOriginalValues();
        }
        else if (active)
        {
            ApplyModifiers();
        }
    }

    /// <summary>
    /// Establece multiplicador de velocidad
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        speedMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    /// <summary>
    /// Establece multiplicador de rotación
    /// </summary>
    public void SetRotationMultiplier(float multiplier)
    {
        rotationMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    /// <summary>
    /// Establece multiplicador de salto
    /// </summary>
    public void SetJumpMultiplier(float multiplier)
    {
        jumpMultiplier = Mathf.Clamp(multiplier, 0.1f, 5f);
    }

    /// <summary>
    /// Activa modo sigiloso
    /// </summary>
    [ContextMenu("Activar Modo Sigiloso")]
    public void ActivateStealthMode()
    {
        stealthMode = true;
        aggressiveMode = false;
        explorerMode = false;
    }

    /// <summary>
    /// Activa modo agresivo
    /// </summary>
    [ContextMenu("Activar Modo Agresivo")]
    public void ActivateAggressiveMode()
    {
        stealthMode = false;
        aggressiveMode = true;
        explorerMode = false;
    }

    /// <summary>
    /// Activa modo explorador
    /// </summary>
    [ContextMenu("Activar Modo Explorador")]
    public void ActivateExplorerMode()
    {
        stealthMode = false;
        aggressiveMode = false;
        explorerMode = true;
    }

    /// <summary>
    /// Desactiva todos los modos especiales
    /// </summary>
    [ContextMenu("Modo Normal")]
    public void ActivateNormalMode()
    {
        stealthMode = false;
        aggressiveMode = false;
        explorerMode = false;
    }

    /// <summary>
    /// Restaura valores originales
    /// </summary>
    void RestoreOriginalValues()
    {
        if (npcController == null || !valuesStored) return;

        npcController.moveSpeed = originalMoveSpeed;
        npcController.rotationSpeed = originalRotationSpeed;
        npcController.jumpForce = originalJumpForce;
        npcController.sensorLength = originalSensorLength;
    }

    /// <summary>
    /// Aplica preset de modificadores
    /// </summary>
    public void ApplyPreset(string presetName)
    {
        switch (presetName.ToLower())
        {
            case "rapido":
                SetSpeedMultiplier(2f);
                SetRotationMultiplier(1.5f);
                break;
            case "lento":
                SetSpeedMultiplier(0.5f);
                SetRotationMultiplier(0.7f);
                break;
            case "saltarin":
                SetJumpMultiplier(2f);
                break;
            case "precision":
                SetSpeedMultiplier(0.8f);
                SetRotationMultiplier(0.6f);
                sensorRangeMultiplier = 1.5f;
                break;
            default:
                // Reset a valores normales
                speedMultiplier = 1f;
                rotationMultiplier = 1f;
                jumpMultiplier = 1f;
                sensorRangeMultiplier = 1f;
                break;
        }
    }
    #endregion

    #region Getters
    public bool IsModifierActive() => modifierActive;
    public string GetCurrentMode() => currentMode;
    public float GetSpeedMultiplier() => speedMultiplier;
    public float GetRotationMultiplier() => rotationMultiplier;
    public float GetJumpMultiplier() => jumpMultiplier;
    #endregion

    #region Cleanup
    void OnDestroy()
    {
        // Restaurar valores al destruir el componente
        if (modifierActive && valuesStored)
        {
            RestoreOriginalValues();
        }
    }
    #endregion
}