
using UnityEngine;
using UnityEngine.SceneManagement;

public class ControlPausa : MonoBehaviour
{
    [Header("Menus")]
    public GameObject menuPausa;
    public GameObject menuPrincipal;

    [Header("Musica")]
    public ControladorMusica controladorMusica;

    public void Pausar()
    {
        if (ControlPartida.Instancia == null)
            return;

        if (!ControlPartida.Instancia.EstaJugando)
            return;

        ControlPartida.Instancia.PausarPartida();

        if (menuPausa != null)
            menuPausa.SetActive(true);

        if (controladorMusica != null)
            controladorMusica.PausarMusica();
    }

    public void Reanudar()
    {
        if (ControlPartida.Instancia == null)
            return;

        ControlPartida.Instancia.ReanudarPartida();

        if (menuPausa != null)
            menuPausa.SetActive(false);

        if (controladorMusica != null)
            controladorMusica.ReanudarMusica();
    }

    public void VolverMenuPrincipal()
    {
        Time.timeScale = 1f;

        PlayerPrefs.SetInt("ReinicioDirecto", 0);
        PlayerPrefs.Save();

        Scene escena = SceneManager.GetActiveScene();

        SceneManager.LoadScene(escena.buildIndex);
    }
}
