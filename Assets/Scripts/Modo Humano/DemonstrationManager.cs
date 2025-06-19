using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// Gestiona la carga, organizacion y limpieza de demostraciones humanas grabadas.
/// Proporciona interfaz de usuario para seleccionar y aplicar demostraciones al entrenamiento de IA.
/// </summary>
public class DemonstrationManager : MonoBehaviour
{
    #region Referencias y Componentes
    [Header("Referencias")]
    [Tooltip("Referencia al grabador de demostraciones")]
    public HumanDemonstrationRecorder recorder;
    #endregion

    #region Datos de Demostraciones
    [Header("Demostraciones Cargadas")]
    [Tooltip("Demostraciones actualmente cargadas en memoria")]
    public List<DemonstrationData> loadedDemonstrations = new List<DemonstrationData>();
    #endregion

    #region Componentes de UI
    [Header("Interfaz de Usuario (Opcional)")]
    [Tooltip("Dropdown para seleccionar demostraciones")]
    public TMP_Dropdown demonstrationDropdown;

    [Tooltip("Boton para cargar la demostracion seleccionada")]
    public Button loadDemoButton;

    [Tooltip("Boton para limpiar todas las demostraciones")]
    public Button clearDemoButton;

    [Tooltip("Texto para mostrar informacion de la demostracion")]
    public TextMeshProUGUI demoInfoText;
    #endregion

    #region Configuracion de Carga Automatica
    [Header("Configuracion de Carga Automatica")]
    [Tooltip("Cargar demostraciones automaticamente al iniciar")]
    public bool autoLoadOnStart = true;

    [Tooltip("Maximo de demostraciones a mantener en memoria")]
    public int maxDemonstrationsInMemory = 10;
    #endregion

    #region Configuracion de Limpieza Automatica
    [Header("Configuracion de Limpieza Automatica")]
    [Tooltip("Limpiar demostraciones antiguas automaticamente al iniciar")]
    public bool autoCleanupOnStart = true;

    [Tooltip("Maximo de demostraciones a conservar (debe coincidir con configuracion del grabador)")]
    public int maxDemosToKeep = 3;
    #endregion

    #region Metodos de Inicializacion

    /// <summary>
    /// Inicializa el gestor de demostraciones, configura la UI y ejecuta limpieza si esta habilitada
    /// </summary>
    void Start()
    {
        // Buscar el grabador si no esta asignado
        if (recorder == null)
        {
            recorder = FindObjectOfType<HumanDemonstrationRecorder>();
        }

        SetupUI();

        // Ejecutar limpieza automatica si esta habilitada
        if (autoCleanupOnStart)
        {
            CleanupExcessDemonstrations();
        }

        // Cargar demostraciones automaticamente si esta habilitado
        if (autoLoadOnStart)
        {
            LoadAllDemonstrations();
        }

        Debug.Log("Demonstration Manager inicializado con limpieza automatica");
    }
    #endregion

    #region Metodos de Limpieza

    /// <summary>
    /// Limpia las demostraciones excedentes manteniendo solo las mejores basadas en fitness
    /// </summary>
    private void CleanupExcessDemonstrations()
    {
        string demonstrationPath = GetDemonstrationPath();

        if (!Directory.Exists(demonstrationPath))
        {
            Debug.LogWarning($"Carpeta de demostraciones no encontrada: {demonstrationPath}");
            return;
        }

        string[] files = Directory.GetFiles(demonstrationPath, "*.json");

        if (files.Length <= maxDemosToKeep) return; // No necesita limpieza

        Debug.Log($"Limpieza: Encontradas {files.Length} demos, conservando las mejores {maxDemosToKeep}");

        // Cargar todas las demos y ordenar por fitness
        List<(string filePath, float fitness, DemonstrationData demo)> allDemos = new List<(string, float, DemonstrationData)>();

        foreach (string file in files)
        {
            DemonstrationData demo = LoadDemonstrationFromFile(file);
            if (demo != null)
            {
                allDemos.Add((file, demo.totalFitness, demo));
            }
        }

        // Ordenar por fitness (mejores primero) y tomar solo las mejores
        var demosToKeep = allDemos.OrderByDescending(x => x.fitness).Take(maxDemosToKeep).ToList();
        var demosToDelete = allDemos.OrderByDescending(x => x.fitness).Skip(maxDemosToKeep).ToList();

        // Eliminar demos excedentes
        foreach (var (filePath, fitness, demo) in demosToDelete)
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Eliminada demo antigua: {Path.GetFileName(filePath)} (fitness: {fitness:F1})");
            }
            catch (Exception e)
            {
                Debug.LogError($"Fallo al eliminar archivo de demo {filePath}: {e.Message}");
            }
        }

        Debug.Log($"Limpieza completada - Conservadas {demosToKeep.Count} mejores demostraciones");
    }

    /// <summary>
    /// Fuerza la limpieza de demostraciones y recarga la lista
    /// </summary>
    public void ForceCleanupDemonstrations()
    {
        CleanupExcessDemonstrations();
        LoadAllDemonstrations(); // Recargar despues de la limpieza
        RefreshUI();
    }
    #endregion

    #region Metodos de UI

    /// <summary>
    /// Configura los eventos de la interfaz de usuario
    /// </summary>
    void SetupUI()
    {
        if (loadDemoButton != null)
        {
            loadDemoButton.onClick.AddListener(LoadSelectedDemonstration);
        }

        if (clearDemoButton != null)
        {
            clearDemoButton.onClick.AddListener(ClearAllDemonstrations);
        }

        if (demonstrationDropdown != null)
        {
            demonstrationDropdown.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    /// <summary>
    /// Maneja el cambio de valor en el dropdown de demostraciones
    /// </summary>
    /// <param name="index">Indice seleccionado en el dropdown</param>
    void OnDropdownValueChanged(int index)
    {
        if (index >= 0 && index < loadedDemonstrations.Count)
        {
            UpdateDemoInfo(loadedDemonstrations[index]);
        }
    }

    /// <summary>
    /// Actualiza la informacion mostrada de la demostracion seleccionada
    /// </summary>
    /// <param name="demo">Datos de la demostracion a mostrar</param>
    void UpdateDemoInfo(DemonstrationData demo)
    {
        if (demoInfoText == null || demo == null) return;

        float avgQuality = demo.frames.Count > 0 ? demo.frames.Average(f => f.frameQuality) : 0f;

        demoInfoText.text = $"Demo: {demo.sessionName}\n" +
                           $"Frames: {demo.frames.Count}\n" +
                           $"Duracion: {demo.sessionDuration:F1}s\n" +
                           $"Fitness: {demo.totalFitness:F1}\n" +
                           $"Calidad Promedio: {avgQuality:F2}";
    }

    /// <summary>
    /// Actualiza todos los elementos de la interfaz de usuario
    /// </summary>
    void RefreshUI()
    {
        if (demonstrationDropdown == null) return;

        demonstrationDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < loadedDemonstrations.Count; i++)
        {
            var demo = loadedDemonstrations[i];
            string optionText = $"{demo.sessionName} (F:{demo.totalFitness:F0}, {demo.frames.Count} frames)";
            options.Add(new TMP_Dropdown.OptionData(optionText));
        }

        demonstrationDropdown.AddOptions(options);

        // Actualizar info para la primera demo si esta disponible
        if (loadedDemonstrations.Count > 0)
        {
            UpdateDemoInfo(loadedDemonstrations[0]);
        }
    }
    #endregion

    #region Metodos de Carga de Demostraciones

    /// <summary>
    /// Carga todas las demostraciones disponibles desde el directorio configurado
    /// </summary>
    public void LoadAllDemonstrations()
    {
        loadedDemonstrations.Clear();

        string demonstrationPath = GetDemonstrationPath();

        if (!Directory.Exists(demonstrationPath))
        {
            Debug.LogWarning($"Carpeta de demostraciones no encontrada: {demonstrationPath}");
            return;
        }

        string[] files = Directory.GetFiles(demonstrationPath, "*.json");

        foreach (string file in files)
        {
            DemonstrationData demo = LoadDemonstrationFromFile(file);
            if (demo != null)
            {
                loadedDemonstrations.Add(demo);

                // Limitar uso de memoria
                if (loadedDemonstrations.Count >= maxDemonstrationsInMemory)
                {
                    break;
                }
            }
        }

        // Ordenar por fitness (mejores primero)
        loadedDemonstrations = loadedDemonstrations.OrderByDescending(d => d.totalFitness).ToList();

        RefreshUI();

        Debug.Log($"Cargadas {loadedDemonstrations.Count} demostraciones");
    }

    /// <summary>
    /// Carga una demostracion desde un archivo especifico
    /// </summary>
    /// <param name="filePath">Ruta del archivo de demostracion</param>
    /// <returns>Datos de la demostracion o null si falla</returns>
    DemonstrationData LoadDemonstrationFromFile(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            DemonstrationData demo = JsonUtility.FromJson<DemonstrationData>(json);

            // Validar demostracion
            if (demo != null && demo.frames != null && demo.frames.Count > 0)
            {
                return demo;
            }
            else
            {
                Debug.LogWarning($"Archivo de demostracion invalido: {filePath}");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Fallo al cargar demostracion desde {filePath}: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Carga la demostracion seleccionada en el dropdown
    /// </summary>
    public void LoadSelectedDemonstration()
    {
        if (demonstrationDropdown == null || loadedDemonstrations.Count == 0) return;

        int selectedIndex = demonstrationDropdown.value;
        if (selectedIndex >= 0 && selectedIndex < loadedDemonstrations.Count)
        {
            DemonstrationData selectedDemo = loadedDemonstrations[selectedIndex];
            Debug.Log($"Demostracion seleccionada: {selectedDemo.sessionName} con {selectedDemo.frames.Count} frames");

            // Aqui se agregara la logica de aplicacion en Fase 3
            // Por ahora, solo registrar la seleccion
        }
    }

    /// <summary>
    /// Limpia todas las demostraciones de la memoria
    /// </summary>
    public void ClearAllDemonstrations()
    {
        loadedDemonstrations.Clear();
        RefreshUI();

        if (demoInfoText != null)
        {
            demoInfoText.text = "No hay demostraciones cargadas";
        }

        Debug.Log("Todas las demostraciones eliminadas de la memoria");
    }
    #endregion

    #region Metodos de Utilidad

    /// <summary>
    /// Obtiene la ruta del directorio de demostraciones basada en la configuracion
    /// </summary>
    /// <returns>Ruta completa al directorio de demostraciones</returns>
    string GetDemonstrationPath()
    {
        if (recorder != null && recorder.saveInProjectFolder)
        {
            return Path.Combine(Application.dataPath, recorder.demonstrationFolder);
        }
        else
        {
            return Path.Combine(Application.persistentDataPath, "Demonstrations");
        }
    }

    /// <summary>
    /// Obtiene la demostracion con mayor fitness
    /// </summary>
    /// <returns>La mejor demostracion o null si no hay demostraciones cargadas</returns>
    public DemonstrationData GetBestDemonstration()
    {
        if (loadedDemonstrations.Count == 0) return null;

        return loadedDemonstrations.OrderByDescending(d => d.totalFitness).First();
    }

    /// <summary>
    /// Obtiene las mejores demostraciones hasta un numero especificado
    /// </summary>
    /// <param name="count">Numero de demostraciones a obtener</param>
    /// <returns>Lista de las mejores demostraciones ordenadas por fitness</returns>
    public List<DemonstrationData> GetTopDemonstrations(int count)
    {
        return loadedDemonstrations.OrderByDescending(d => d.totalFitness).Take(count).ToList();
    }

    /// <summary>
    /// Calcula el numero total de frames disponibles en todas las demostraciones
    /// </summary>
    /// <returns>Numero total de frames</returns>
    public int GetTotalFramesAvailable()
    {
        return loadedDemonstrations.Sum(d => d.frames.Count);
    }
    #endregion

    #region Metodos de Debugging

    /// <summary>
    /// Muestra informacion de debug en pantalla usando OnGUI
    /// </summary>
    void OnGUI()
    {
        if (loadedDemonstrations.Count > 0)
        {
            GUI.color = Color.cyan;
            GUI.Label(new Rect(Screen.width - 300, 10, 290, 30), $"Demos Cargadas: {loadedDemonstrations.Count}");
            GUI.Label(new Rect(Screen.width - 300, 40, 290, 30), $"Total Frames: {GetTotalFramesAvailable()}");
            GUI.color = Color.white;
        }
    }
    #endregion
}