
using UnityEngine;

public class ControlPausa : MonoBehaviour
{
    public GameObject menuPausa;
    public GameObject menuPrincipal;
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

        if (ControlPartida.Instancia != null)
            ControlPartida.Instancia.VolverAlMenu();

        if (menuPausa != null)
            menuPausa.SetActive(false);

        if (menuPrincipal != null)
            menuPrincipal.SetActive(true);

        if (controladorMusica != null)
            controladorMusica.CambiarAMenuYReiniciar();

        Debug.Log("Volviendo al menu principal");
    }
}
