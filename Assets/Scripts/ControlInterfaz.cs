
using UnityEngine;

public class ControlInterfaz : MonoBehaviour
{
    [Header("Interfaz del juego")]
    public GameObject marcoEnergia;
    public GameObject marcoReloj;
    public GameObject botonPausa;

    private ControlPartida.Estado ultimoEstado;

    void Start()
    {
        ultimoEstado = ControlPartida.Estado.Menu;
        ActualizarInterfaz();
    }

    void Update()
    {
        if (ControlPartida.Instancia == null)
            return;

        if (ultimoEstado != ControlPartida.Instancia.estadoActual)
        {
            ActualizarInterfaz();
        }
    }

    void ActualizarInterfaz()
    {
        if (ControlPartida.Instancia == null)
            return;

        var estado = ControlPartida.Instancia.estadoActual;
        ultimoEstado = estado;

        bool mostrarJuego =
            estado == ControlPartida.Estado.Esperando ||
            estado == ControlPartida.Estado.Jugando;

        bool mostrarPausa =
            estado == ControlPartida.Estado.Esperando ||
            estado == ControlPartida.Estado.Jugando;

        if (marcoEnergia != null)
            marcoEnergia.SetActive(mostrarJuego);

        if (marcoReloj != null)
            marcoReloj.SetActive(mostrarJuego);

        if (botonPausa != null)
            botonPausa.SetActive(mostrarPausa);
    }
}
