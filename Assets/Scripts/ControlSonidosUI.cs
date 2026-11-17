
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
public class ControlSonidosUI : MonoBehaviour
{
    public AudioClip hover;
    public AudioClip click;

    private void Awake()
    {
        AudioSource audio = GetComponent<AudioSource>();

        audio.playOnAwake = false;
        audio.loop = false;
        audio.spatialBlend = 0f;

        SonidoBotones.audioGlobal = audio;
        SonidoBotones.sonidoHover = hover;
        SonidoBotones.sonidoClick = click;

        Button[] botones = FindObjectsByType<Button>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (Button boton in botones)
        {
            if (boton.GetComponent<SonidoBotones>() == null)
                boton.gameObject.AddComponent<SonidoBotones>();
        }
    }
}
