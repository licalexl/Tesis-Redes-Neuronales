using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GUIWindowManager : MonoBehaviour
{
    // Lista de todas las configuraciones de ventanas
    private List<WindowConfig> allWindows = new List<WindowConfig>();

    // Variables para el menú de configuración
    private bool showConfigMenu = false;
    private Rect configMenuRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 150, 400, 300);
    private Vector2 scrollPosition;

    // Registrar una ventana en el gestor
    public void RegisterWindow(WindowConfig window)
    {
        if (!allWindows.Contains(window))
        {
            allWindows.Add(window);
        }
    }

    void Update()
    {
        // Tecla para mostrar/ocultar el menú de configuración 
        if (Input.GetKeyDown(KeyCode.F4))
        {
            showConfigMenu = !showConfigMenu;
        }
    }

    void OnGUI()
    {
        // Si está activado, mostrar menú de configuración
        if (showConfigMenu)
        {
            configMenuRect = GUI.Window(9999, configMenuRect, DrawConfigMenu, "Configuración de Ventanas GUI");
        }
    }

    void DrawConfigMenu(int windowID)
    {
        GUILayout.BeginVertical(GUILayout.ExpandHeight(true));

        GUILayout.Label("Configuración de Ventanas", GUI.skin.box);
        GUILayout.Space(10);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        foreach (var window in allWindows)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            GUILayout.BeginHorizontal();
            GUILayout.Label(window.windowName, GUILayout.Width(150));
            window.enabled = GUILayout.Toggle(window.enabled, "Visible");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Posición X:", GUILayout.Width(70));
            float x = window.windowRect.x;
            if (float.TryParse(GUILayout.TextField(x.ToString(), GUILayout.Width(50)), out float newX))
            {
                window.windowRect.x = newX;
            }

            GUILayout.Label("Y:", GUILayout.Width(20));
            float y = window.windowRect.y;
            if (float.TryParse(GUILayout.TextField(y.ToString(), GUILayout.Width(50)), out float newY))
            {
                window.windowRect.y = newY;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Tamaño W:", GUILayout.Width(70));
            float w = window.windowRect.width;
            if (float.TryParse(GUILayout.TextField(w.ToString(), GUILayout.Width(50)), out float newW))
            {
                window.windowRect.width = Mathf.Clamp(newW, window.minSize.x, window.maxSize.x);
            }

            GUILayout.Label("H:", GUILayout.Width(20));
            float h = window.windowRect.height;
            if (float.TryParse(GUILayout.TextField(h.ToString(), GUILayout.Width(50)), out float newH))
            {
                window.windowRect.height = Mathf.Clamp(newH, window.minSize.y, window.maxSize.y);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            window.isDraggable = GUILayout.Toggle(window.isDraggable, "Arrastrable");
            window.isResizable = GUILayout.Toggle(window.isResizable, "Redimensionable");
            window.savePositionOnExit = GUILayout.Toggle(window.savePositionOnExit, "Guardar posición");
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            if (GUILayout.Button("Restablecer valores predeterminados"))
            {
                ResetWindowToDefault(window);
            }

            GUILayout.EndVertical();
            GUILayout.Space(10);
        }

        GUILayout.EndScrollView();

        GUILayout.Space(10);
        if (GUILayout.Button("Guardar todas las configuraciones"))
        {
            SaveAllConfigurations();
        }

        if (GUILayout.Button("Cerrar"))
        {
            showConfigMenu = false;
        }

        GUILayout.EndVertical();

        // Permitir arrastrar la ventana de configuración
        GUI.DragWindow();
    }

    void ResetWindowToDefault(WindowConfig window)
    {
        
        window.windowRect.width = 300;
        window.windowRect.height = 200;
        window.isDraggable = true;
        window.isResizable = true;
    }

    void SaveAllConfigurations()
    {
        foreach (var window in allWindows)
        {
            window.SaveConfig();
        }
    }

    void OnDestroy()
    {
        SaveAllConfigurations();
    }
}
