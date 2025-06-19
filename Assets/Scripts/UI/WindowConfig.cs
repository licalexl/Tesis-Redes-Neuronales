using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WindowConfig
{
    public string windowName = "Ventana";
    public string uniqueID = "";
    public bool enabled = true;
    public Rect windowRect = new Rect(10, 10, 300, 200);
    public bool isDraggable = true;
    public bool isResizable = true;
    [HideInInspector]
    public bool isResizing = false;
    public Vector2 minSize = new Vector2(200, 100);
    public Vector2 maxSize = new Vector2(500, 400);
    public bool savePositionOnExit = true;

    // Constructor con valores por defecto
    public WindowConfig(string name, Rect defaultRect, string id = "")
    {
        windowName = name;
        windowRect = defaultRect;

       
        if (string.IsNullOrEmpty(id))
        {
            uniqueID = name.Replace(" ", "_") + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
        }
        else
        {
            uniqueID = id;
        }
    }

  
    public void SaveConfig()
    {
        if (!savePositionOnExit) return;

       
        string key = "WIN_" + uniqueID;

        PlayerPrefs.SetFloat(key + "_X", windowRect.x);
        PlayerPrefs.SetFloat(key + "_Y", windowRect.y);
        PlayerPrefs.SetFloat(key + "_Width", windowRect.width);
        PlayerPrefs.SetFloat(key + "_Height", windowRect.height);
        PlayerPrefs.SetInt(key + "_Enabled", enabled ? 1 : 0);

       
        Debug.Log($"Guardando ventana '{windowName}' con ID '{uniqueID}' - Pos: {windowRect.x},{windowRect.y} Size: {windowRect.width}x{windowRect.height}");
    }

  
    public void LoadConfig()
    {
        string key = "WIN_" + uniqueID;

        if (PlayerPrefs.HasKey(key + "_X"))
        {
            windowRect.x = PlayerPrefs.GetFloat(key + "_X");
            windowRect.y = PlayerPrefs.GetFloat(key + "_Y");
            windowRect.width = PlayerPrefs.GetFloat(key + "_Width");
            windowRect.height = PlayerPrefs.GetFloat(key + "_Height");
            enabled = PlayerPrefs.GetInt(key + "_Enabled") == 1;

           
            Debug.Log($"Cargando ventana '{windowName}' con ID '{uniqueID}' - Pos: {windowRect.x},{windowRect.y} Size: {windowRect.width}x{windowRect.height}");
        }
        else
        {
            Debug.Log($"No se encontró configuración guardada para ventana '{windowName}' con ID '{uniqueID}'");
        }
    }

    
    public void ClearSavedConfig()
    {
        string key = "WIN_" + uniqueID;
        PlayerPrefs.DeleteKey(key + "_X");
        PlayerPrefs.DeleteKey(key + "_Y");
        PlayerPrefs.DeleteKey(key + "_Width");
        PlayerPrefs.DeleteKey(key + "_Height");
        PlayerPrefs.DeleteKey(key + "_Enabled");
        PlayerPrefs.Save();
        Debug.Log($"Configuración limpiada para ventana '{windowName}' con ID '{uniqueID}'");
    }
}