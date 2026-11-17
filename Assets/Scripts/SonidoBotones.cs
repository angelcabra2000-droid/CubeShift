
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class SonidoBotones : MonoBehaviour,
    IPointerEnterHandler, IPointerClickHandler
{
    public static AudioSource audioGlobal;
    public static AudioClip sonidoHover;
    public static AudioClip sonidoClick;

    [Header("Configuracion")]
    public bool activarHover = true;
    public bool activarClick = true;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!activarHover)
            return;

        if (audioGlobal != null && sonidoHover != null)
            audioGlobal.PlayOneShot(sonidoHover);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!activarClick)
            return;

        if (audioGlobal != null && sonidoClick != null)
            audioGlobal.PlayOneShot(sonidoClick);
    }
}
