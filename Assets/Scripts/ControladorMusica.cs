using UnityEngine;

public class ControladorMusica : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip musicaMenu;
    public AudioClip musicaJuego;

    // Cambiar a música del juego al iniciar
    public void CambiarAJuego()
    {
        audioSource.Stop();
        audioSource.clip = musicaJuego;
        audioSource.Play();
    }

    // Volver al menú y reiniciar la canción desde cero
    public void CambiarAMenuYReiniciar()
    {
        audioSource.Stop();
        audioSource.clip = musicaMenu;
        audioSource.Play();
    }

    // Pausar la música manteniendo el punto exacto
    public void PausarMusica()
    {
        if (audioSource.isPlaying)
        {
            audioSource.Pause();
        }
    }

    // Reanudar la música desde donde se quedó
    public void ReanudarMusica()
    {
        audioSource.UnPause();
    }
}