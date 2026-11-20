
using UnityEngine;

public class ControlPartida : MonoBehaviour
{
    public static ControlPartida Instancia { get; private set; }

    public enum Estado
    {
        Menu,
        Esperando,
        Jugando,
        Pausa,
        AnimacionVictoria,
        Victoria,
        Derrota
    }

    public Estado estadoActual = Estado.Menu;

    public bool EstaJugando => estadoActual == Estado.Jugando;

    public float tiempoPartida { get; private set; }

    [Header("Musica")]
    public ControladorMusica controladorMusica;

    void Awake()
    {
        Instancia = this;
        estadoActual = Estado.Menu;
        tiempoPartida = 0f;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (estadoActual == Estado.Esperando)
        {
            if (Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.S) ||
                Input.GetKeyDown(KeyCode.A) ||
                Input.GetKeyDown(KeyCode.D))
            {
                IniciarPartida();
            }
        }

        if (EstaJugando)
        {
            tiempoPartida += Time.deltaTime;
        }
    }

    public void PrepararPartida()
    {
        estadoActual = Estado.Esperando;
        tiempoPartida = 0f;
        Time.timeScale = 1f;

        if (controladorMusica != null)
            controladorMusica.CambiarAJuego();
    }

    public void IniciarPartida()
    {
        if (estadoActual != Estado.Esperando)
            return;

        estadoActual = Estado.Jugando;

        Debug.Log("Partida iniciada");
    }

    public void PausarPartida()
    {
        if (!EstaJugando)
            return;

        estadoActual = Estado.Pausa;
        Time.timeScale = 0f;
    }

    public void ReanudarPartida()
    {
        if (estadoActual != Estado.Pausa)
            return;

        estadoActual = Estado.Jugando;
        Time.timeScale = 1f;
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;

        estadoActual = Estado.Menu;
        tiempoPartida = 0f;

        EnergiaJugador energia =
            FindFirstObjectByType<EnergiaJugador>();

        if (energia != null)
        {
            energia.RecuperarEnergia();
        }

        Debug.Log("Regresando al menu principal");
    }

    public void IniciarAnimacionVictoria()
    {
        if (!EstaJugando)
            return;

        estadoActual = Estado.AnimacionVictoria;
        Time.timeScale = 1f;
    }

    public void Ganar()
    {
        if (estadoActual == Estado.Victoria)
            return;

        estadoActual = Estado.Victoria;
        Time.timeScale = 0f;

        if (controladorMusica != null)
            controladorMusica.CambiarAVictoria();
    }

    public void Perder()
    {
        Debug.Log("CONTROL PARTIDA: PERDER EJECUTADO");

        if (estadoActual == Estado.Derrota)
            return;

        estadoActual = Estado.Derrota;
        Time.timeScale = 0f;

        if (controladorMusica != null)
        {
            Debug.Log("CONTROL PARTIDA: CAMBIANDO MUSICA");
            controladorMusica.CambiarADerrota();
        }
        else
        {
            Debug.LogError(
                "CONTROL PARTIDA: NO HAY CONTROLADOR MUSICA"
            );
        }
    }
}
