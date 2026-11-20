
using UnityEngine;

public class ControladorMusica : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource audioSource;

    [Header("Musicas")]
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;
    public AudioClip musicaJuegoDificil;
    public AudioClip musicaDerrota;
    public AudioClip musicaVictoria;
    public AudioClip musicaNuevoRecord;

    [Header("Volumenes")]
    [Range(0f, 1f)] public float volumenMenu = 0.7f;
    [Range(0f, 1f)] public float volumenJuego = 0.7f;
    [Range(0f, 1f)] public float volumenJuegoDificil = 0.7f;
    [Range(0f, 1f)] public float volumenDerrota = 0.35f;
    [Range(0f, 1f)] public float volumenVictoria = 0.65f;
    [Range(0f, 1f)] public float volumenNuevoRecord = 0.75f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
        {
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
        }
    }

    private void CambiarMusica(AudioClip nuevaMusica, float volumen)
    {
        if (audioSource == null || nuevaMusica == null)
            return;

        audioSource.Stop();
        audioSource.clip = nuevaMusica;
        audioSource.volume = volumen;
        audioSource.loop = true;
        audioSource.Play();
    }

    public void CambiarAJuego()
    {
        if (DificultadJuego.dificultadActual ==
            DificultadJuego.Nivel.Dificil)
        {
            AudioClip musica = musicaJuegoDificil != null
                ? musicaJuegoDificil
                : musicaJuego;

            float volumen = musicaJuegoDificil != null
                ? volumenJuegoDificil
                : volumenJuego;

            CambiarMusica(musica, volumen);
        }
        else
        {
            CambiarMusica(musicaJuego, volumenJuego);
        }
    }

    public void CambiarAMenuYReiniciar()
    {
        CambiarMusica(musicaMenu, volumenMenu);
    }

    public void CambiarADerrota()
    {
        Debug.Log("CAMBIANDO A MUSICA DE DERROTA");

        CambiarMusica(musicaDerrota, volumenDerrota);
    }

    public void CambiarAVictoria()
    {
        CambiarMusica(musicaVictoria, volumenVictoria);
    }

    public void CambiarANuevoRecord()
    {
        if (musicaNuevoRecord != null)
            CambiarMusica(musicaNuevoRecord, volumenNuevoRecord);
        else
            CambiarAVictoria();
    }

    public void DetenerMusica()
    {
        if (audioSource != null)
            audioSource.Stop();
    }

    public void PausarMusica()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void ReanudarMusica()
    {
        if (audioSource != null)
            audioSource.UnPause();
    }
}
