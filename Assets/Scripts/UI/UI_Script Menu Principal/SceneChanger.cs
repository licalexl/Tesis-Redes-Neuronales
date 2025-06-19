using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nombreEscena;

    /// <summary>
    /// Método para cambiar a la escena especificada
    /// Conecta este método al evento OnClick del botón
    /// </summary>
    public void CambiarEscena()
    {
        if (string.IsNullOrEmpty(nombreEscena))
        {
            Debug.LogWarning("No se ha especificado un nombre de escena!");
            return;
        }

        Debug.Log($"Cambiando a la escena: {nombreEscena}");
        SceneManager.LoadScene(nombreEscena);
    }

    /// <summary>
    /// Método alternativo que permite especificar la escena por parámetro
    /// </summary>
    public void CambiarEscena(string escena)
    {
        if (string.IsNullOrEmpty(escena))
        {
            Debug.LogWarning("El nombre de escena proporcionado está vacío!");
            return;
        }

        Debug.Log($"Cambiando a la escena: {escena}");
        SceneManager.LoadScene(escena);
    }
}