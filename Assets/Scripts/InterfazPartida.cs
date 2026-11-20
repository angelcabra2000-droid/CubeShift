
using UnityEngine;

public class InterfazPartida : MonoBehaviour
{
    [Header("Interfaz del juego")]
    public GameObject marcoEnergia;
    public GameObject marcoReloj;
    public GameObject botonPausa;

    [Header("Otros paneles")]
    public GameObject menuPausa;
    public GameObject alertaEnergia;
    public GameObject gameOver;
    public GameObject menuVictoria;

    void Start()
    {
        OcultarInterfaz();
    }

    public void MostrarInterfaz()
    {
        if (marcoEnergia != null)
            marcoEnergia.SetActive(true);

        if (marcoReloj != null)
            marcoReloj.SetActive(true);

        if (botonPausa != null)
            botonPausa.SetActive(true);

        if (menuPausa != null)
            menuPausa.SetActive(false);

        if (alertaEnergia != null)
            alertaEnergia.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(false);

        if (menuVictoria != null)
            menuVictoria.SetActive(false);
    }

    public void OcultarInterfaz()
    {
        if (marcoEnergia != null)
            marcoEnergia.SetActive(false);

        if (marcoReloj != null)
            marcoReloj.SetActive(false);

        if (botonPausa != null)
            botonPausa.SetActive(false);

        if (menuPausa != null)
            menuPausa.SetActive(false);

        if (alertaEnergia != null)
            alertaEnergia.SetActive(false);

        if (gameOver != null)
            gameOver.SetActive(false);

        if (menuVictoria != null)
            menuVictoria.SetActive(false);
    }
}
