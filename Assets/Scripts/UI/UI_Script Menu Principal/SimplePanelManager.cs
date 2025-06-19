using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SimplePanelManager : MonoBehaviour
{
    // Singleton
    private static SimplePanelManager _instance;
    public static SimplePanelManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("SimplePanelManager");
                _instance = go.AddComponent<SimplePanelManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    [Header("Configuración Global")]
    public bool closeAllOnEscape = true;

    // Lista de paneles registrados
    private List<SimplePanelController> registeredPanels = new List<SimplePanelController>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        // Cerrar todos los paneles con Escape
        if (closeAllOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseAllPanels(false); // No forzar, respetar pins
        }
    }

    public void RegisterPanel(SimplePanelController panel)
    {
        if (panel == null || string.IsNullOrEmpty(panel.panelID))
        {
            Debug.LogWarning("No se puede registrar un panel sin ID válido.");
            return;
        }

        if (!registeredPanels.Contains(panel))
        {
            registeredPanels.Add(panel);
            Debug.Log($"Panel '{panel.panelID}' registrado correctamente.");
        }
    }

    public void UnregisterPanel(SimplePanelController panel)
    {
        if (panel != null && registeredPanels.Contains(panel))
        {
            registeredPanels.Remove(panel);
            Debug.Log($"Panel '{panel.panelID}' desregistrado.");
        }
    }

    public SimplePanelController GetPanel(string panelID)
    {
        return registeredPanels.FirstOrDefault(p => p != null && p.panelID == panelID);
    }

    public void OpenPanel(string panelID)
    {
        SimplePanelController panel = GetPanel(panelID);
        if (panel != null)
        {
            panel.OpenPanel();
        }
        else
        {
            Debug.LogWarning($"Panel '{panelID}' no encontrado.");
        }
    }

    public void ClosePanel(string panelID, bool force = false)
    {
        SimplePanelController panel = GetPanel(panelID);
        if (panel != null)
        {
            if (force)
                panel.ForceClose();
            else
                panel.ClosePanel();
        }
        else
        {
            Debug.LogWarning($"Panel '{panelID}' no encontrado.");
        }
    }

    public void OnPanelOpening(SimplePanelController openingPanel)
    {
        // Cerrar todos los paneles que no estén pinned, excepto el que se está abriendo
        foreach (var panel in registeredPanels)
        {
            if (panel != null &&
                panel != openingPanel &&
                panel.IsOpen &&
                !panel.IsPinned &&
                !panel.IsAnimating)
            {
                panel.ClosePanel();
            }
        }
    }

    public void CloseAllPanels(bool force = false)
    {
        foreach (var panel in registeredPanels)
        {
            if (panel != null && panel.IsOpen)
            {
                if (force)
                    panel.ForceClose();
                else if (!panel.IsPinned)
                    panel.ClosePanel();
            }
        }
    }

    public void CloseAllUnpinnedPanels()
    {
        foreach (var panel in registeredPanels)
        {
            if (panel != null && panel.IsOpen && !panel.IsPinned)
            {
                panel.ClosePanel();
            }
        }
    }

    // Métodos de utilidad
    public bool IsPanelOpen(string panelID)
    {
        SimplePanelController panel = GetPanel(panelID);
        return panel != null && panel.IsOpen;
    }

    public bool IsPanelPinned(string panelID)
    {
        SimplePanelController panel = GetPanel(panelID);
        return panel != null && panel.IsPinned;
    }

    public void SetPanelPinned(string panelID, bool pinned)
    {
        SimplePanelController panel = GetPanel(panelID);
        if (panel != null)
        {
            panel.SetPinned(pinned);
        }
    }

    public List<string> GetOpenPanelIDs()
    {
        return registeredPanels
            .Where(p => p != null && p.IsOpen)
            .Select(p => p.panelID)
            .ToList();
    }

    public List<string> GetPinnedPanelIDs()
    {
        return registeredPanels
            .Where(p => p != null && p.IsPinned)
            .Select(p => p.panelID)
            .ToList();
    }

    public int GetOpenPanelCount()
    {
        return registeredPanels.Count(p => p != null && p.IsOpen);
    }

    // Métodos para debugging
    [ContextMenu("Debug All Panels")]
    public void DebugAllPanels()
    {
        Debug.Log("=== SIMPLE PANEL MANAGER DEBUG ===");
        Debug.Log($"Paneles registrados: {registeredPanels.Count}");

        foreach (var panel in registeredPanels)
        {
            if (panel != null)
            {
                Debug.Log($"- {panel.panelID}: Open={panel.IsOpen}, Pinned={panel.IsPinned}");
            }
        }
    }

    [ContextMenu("Close All Panels")]
    public void DebugCloseAllPanels()
    {
        CloseAllPanels(false);
    }

    [ContextMenu("Force Close All Panels")]
    public void DebugForceCloseAllPanels()
    {
        CloseAllPanels(true);
    }
}