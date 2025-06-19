using UnityEngine;
using System.Collections.Generic;

public class CheckpointSystem : MonoBehaviour
{
    public static CheckpointSystem Instance { get; private set; }

    [Header("Configuración de Checkpoints")]
    public List<Transform> checkpoints = new List<Transform>();
    public float checkpointRadius = 3f;
    public float baseCheckpointReward = 20f;
    public float orderBonusMultiplier = 1.5f; // Multiplicador para orden correcto
    public bool showGizmos = true;

    [Header("Visualización")]
    public Color startColor = Color.green;
    public Color endColor = Color.red;
    public Color lineColor = Color.yellow;
    public float lineWidth = 0.1f;

    // Diccionarios para trackear progreso de NPCs
    private Dictionary<NPCController, HashSet<int>> npcCheckpoints = new Dictionary<NPCController, HashSet<int>>();
    private Dictionary<NPCController, int> npcLastOrderlyCheckpoint = new Dictionary<NPCController, int>();

    void Awake()
    {
        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject); // Añadir esta línea
            }
            else if (Instance != this)
            {
                // Solo destruir si realmente es una instancia diferente
                Debug.Log($"Destruyendo CheckpointSystem duplicado");
                Destroy(gameObject);
            }
        }
    }
    public void ClearAllNPCs()
    {
        npcCheckpoints.Clear();
        npcLastOrderlyCheckpoint.Clear();
      
    }

    public static void ReplaceInstance(CheckpointSystem newInstance)
    {
        if (Instance != null && Instance != newInstance)
        {
            Instance.ClearAllNPCs();
        }
        Instance = newInstance;
        Debug.Log($"CheckpointSystem instance actualizada");
    }


    public void RegisterNPC(NPCController npc)
    {
        if (!npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints[npc] = new HashSet<int>();
            npcLastOrderlyCheckpoint[npc] = -1; // Empezar antes del primer checkpoint
        }
    }

    public void UnregisterNPC(NPCController npc)
    {
        if (npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints.Remove(npc);
            npcLastOrderlyCheckpoint.Remove(npc);
        }
    }

    public float CheckCheckpoints(NPCController npc)
    {
        if (!npcCheckpoints.ContainsKey(npc))
        {
            RegisterNPC(npc);
        }

        float newReward = 0f;
        Vector3 npcPosition = npc.transform.position;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == null) continue;

            float distance = Vector3.Distance(npcPosition, checkpoints[i].position);

            // Solo procesar si está en rango Y no ha tocado este checkpoint antes
            if (distance < checkpointRadius && !npcCheckpoints[npc].Contains(i))
            {
                // Marcar checkpoint como visitado
                npcCheckpoints[npc].Add(i);

                // Determinar tipo de recompensa
                bool inOrder = IsInCorrectOrder(npc, i);
                float reward = CalculateReward(i, inOrder);

                newReward += reward;

                // Actualizar progreso ordenado
                if (inOrder)
                {
                    npcLastOrderlyCheckpoint[npc] = i;
                }

                Debug.Log($"NPC {npc.name} alcanzó checkpoint {i}! " +
                         $"Orden: {(inOrder ? "CORRECTO" : "Incorrecto")} | " +
                         $"Recompensa: {reward:F1}");
            }
        }

        return newReward;
    }

    private bool IsInCorrectOrder(NPCController npc, int checkpointIndex)
    {
        if (!npcLastOrderlyCheckpoint.ContainsKey(npc))
        {
            npcLastOrderlyCheckpoint[npc] = -1;
        }

        // El checkpoint está en orden si es el siguiente al último ordenado
        return checkpointIndex == npcLastOrderlyCheckpoint[npc] + 1;
    }

    private float CalculateReward(int checkpointIndex, bool inOrder)
    {
        float baseReward = baseCheckpointReward;

        if (inOrder)
        {
            // Recompensa mayor por orden correcto
            float orderedReward = baseReward * orderBonusMultiplier;

            // Bonus progresivo: checkpoints más avanzados valen más
            float progressBonus = (checkpointIndex + 1) * 2f;

            return orderedReward + progressBonus;
        }
        else
        {
            // Recompensa estándar por checkpoint fuera de orden
            return baseReward;
        }
    }

    public void ResetNPCCheckpoints(NPCController npc)
    {
        if (npcCheckpoints.ContainsKey(npc))
        {
            npcCheckpoints[npc].Clear();
        }

        if (npcLastOrderlyCheckpoint.ContainsKey(npc))
        {
            npcLastOrderlyCheckpoint[npc] = -1;
        }
    }

    // Métodos para obtener estadísticas (útil para debugging)
    public int GetCheckpointsReached(NPCController npc)
    {
        return npcCheckpoints.ContainsKey(npc) ? npcCheckpoints[npc].Count : 0;
    }

    public int GetOrderlyProgress(NPCController npc)
    {
        return npcLastOrderlyCheckpoint.ContainsKey(npc) ? npcLastOrderlyCheckpoint[npc] + 1 : 0;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || checkpoints.Count == 0) return;

        for (int i = 0; i < checkpoints.Count; i++)
        {
            if (checkpoints[i] == null) continue;

            // Calcular color del checkpoint (gradiente de verde a rojo)
            Color checkpointColor = GetCheckpointColor(i);

            // Dibujar esfera del checkpoint
            Gizmos.color = checkpointColor;
            Gizmos.DrawWireSphere(checkpoints[i].position, checkpointRadius);

            // Dibujar núcleo sólido para mejor visibilidad
            Gizmos.color = new Color(checkpointColor.r, checkpointColor.g, checkpointColor.b, 0.3f);
            Gizmos.DrawSphere(checkpoints[i].position, checkpointRadius * 0.5f);

            // Dibujar líneas conectoras
            if (i < checkpoints.Count - 1 && checkpoints[i + 1] != null)
            {
                Gizmos.color = lineColor;
                Gizmos.DrawLine(checkpoints[i].position, checkpoints[i + 1].position);

                // Dibujar flecha direccional
                Vector3 direction = (checkpoints[i + 1].position - checkpoints[i].position).normalized;
                Vector3 midPoint = Vector3.Lerp(checkpoints[i].position, checkpoints[i + 1].position, 0.7f);
                DrawArrow(midPoint, direction);
            }

#if UNITY_EDITOR
            // Etiquetas con información
            string label = $"CP {i}";
            if (i == 0) label += " (START)";
            else if (i == checkpoints.Count - 1) label += " (END)";

            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(checkpoints[i].position + Vector3.up * 2f, label);
#endif
        }
    }

    private Color GetCheckpointColor(int index)
    {
        if (checkpoints.Count <= 1) return startColor;

        float t = (float)index / (checkpoints.Count - 1);
        return Color.Lerp(startColor, endColor, t);
    }

    private void DrawArrow(Vector3 position, Vector3 direction)
    {
        float arrowSize = 0.5f;
        Vector3 right = Vector3.Cross(direction, Vector3.up).normalized * arrowSize;
        Vector3 backward = -direction * arrowSize;

        Gizmos.DrawLine(position, position + backward + right);
        Gizmos.DrawLine(position, position + backward - right);
    }
}