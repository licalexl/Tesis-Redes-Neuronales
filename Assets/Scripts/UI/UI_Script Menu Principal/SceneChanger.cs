using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    [Header("Configuraci�n de Escena")]
    [SerializeField] private string nombreEscena;

    /// <summary>
    /// M�todo para cambiar a la escena especificada
    /// Conecta este m�todo al evento OnClick del bot�n
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
    /// M�todo alternativo que permite especificar la escena por par�metro
    /// </summary>
    public void CambiarEscena(string escena)
    {
        if (string.IsNullOrEmpty(escena))
        {
            Debug.LogWarning("El nombre de escena proporcionado est� vac�o!");
            return;
        }

        Debug.Log($"Cambiando a la escena: {escena}");
        SceneManager.LoadScene(escena);
    }
}