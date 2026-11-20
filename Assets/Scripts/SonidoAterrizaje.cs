
using UnityEngine;

[RequireComponent(typeof(GravedadJugador))]
public class SonidoAterrizaje : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GravedadJugador gravedadJugador;
    [SerializeField] private AudioSource audioAterrizaje;
    [SerializeField] private AudioClip sonidoAterrizaje;

    [Header("Configuracion")]
    [SerializeField] private float tiempoMinimoCaida = 0.2f;
    [SerializeField] private float volumen = 0.8f;

    private bool estabaEnSuelo;
    private float tiempoEnAire;

    private void Awake()
    {
        if (gravedadJugador == null)
            gravedadJugador = GetComponent<GravedadJugador>();

        if (audioAterrizaje == null)
            audioAterrizaje = GetComponent<AudioSource>();
    }

    private void Start()
    {
        estabaEnSuelo = gravedadJugador.EstaEnSuelo;
    }

    private void LateUpdate()
    {
        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
        {
            ReiniciarDeteccion();
            return;
        }

        bool enSuelo = gravedadJugador.EstaEnSuelo;

        if (!enSuelo)
        {
            tiempoEnAire += Time.deltaTime;
        }
        else if (!estabaEnSuelo)
        {
            if (tiempoEnAire >= tiempoMinimoCaida)
                ReproducirAterrizaje();

            tiempoEnAire = 0f;
        }

        estabaEnSuelo = enSuelo;
    }

    private void ReproducirAterrizaje()
    {
        if (audioAterrizaje != null &&
            sonidoAterrizaje != null)
        {
            audioAterrizaje.PlayOneShot(
                sonidoAterrizaje,
                volumen
            );
        }
    }

    private void ReiniciarDeteccion()
    {
        estabaEnSuelo = gravedadJugador.EstaEnSuelo;
        tiempoEnAire = 0f;
    }
}
