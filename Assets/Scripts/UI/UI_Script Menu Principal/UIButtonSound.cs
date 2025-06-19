using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerClickHandler
{
    [Header("Sonidos")]
    public AudioClip hoverSound;
    public AudioClip clickSound;

    [Header("Configuración")]
    public AudioSource audioSource;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (hoverSound && audioSource)
            audioSource.PlayOneShot(hoverSound);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound && audioSource)
            audioSource.PlayOneShot(clickSound);
    }
}
