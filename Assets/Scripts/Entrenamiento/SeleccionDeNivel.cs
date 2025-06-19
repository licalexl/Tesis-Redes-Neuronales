using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeleccionDeNivel : MonoBehaviour
{
    public GameObject[] escenarios;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            ActivarEscenario(0);
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            ActivarEscenario(1);
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            ActivarEscenario(2);
        else if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            ActivarEscenario(3);
    }

    void ActivarEscenario(int index)
    {
        // Desactivar todos los escenarios
        for (int i = 0; i < escenarios.Length; i++)
        {
            escenarios[i].SetActive(i == index);
        }

        // Notificar al CheckpointSystem del cambio de nivel
        StartCoroutine(NotificarCambioDeNivel(index));
    }

    IEnumerator NotificarCambioDeNivel(int nivelIndex)
    {
        // Esperar un frame para que se active completamente el nuevo nivel
        yield return null;

        // Buscar el CheckpointSystem activo en el nuevo nivel
        CheckpointSystem nuevoCheckpointSystem = null;
        
        if (nivelIndex < escenarios.Length && escenarios[nivelIndex] != null)
        {
            nuevoCheckpointSystem = escenarios[nivelIndex].GetComponentInChildren<CheckpointSystem>();
        }

        if (nuevoCheckpointSystem != null)
        {
            // Reemplazar la instancia del CheckpointSystem
            CheckpointSystem.ReplaceInstance(nuevoCheckpointSystem);
            Debug.Log($"CheckpointSystem actualizado para el nivel {nivelIndex}");
        }
        else
        {
            Debug.LogWarning($"No se encontró CheckpointSystem en el nivel {nivelIndex}");
        }
    }
}