
using UnityEngine;

public class SonidoPasos : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioPasos;
    [SerializeField] private AudioClip[] sonidosPasos;

    [Header("Configuracion")]
    [SerializeField] private float intervalo = 0.4f;
    [SerializeField] private float movimientoMinimo = 0.001f;

    [Header("Deteccion de suelo")]
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private float distanciaSuelo = 0.15f;

    private Vector3 posicionAnterior;
    private float contador;
    private Collider colliderJugador;

    private void Start()
    {
        posicionAnterior = transform.position;
        colliderJugador = GetComponent<Collider>();
    }

    private void Update()
    {
        Vector3 desplazamiento = transform.position - posicionAnterior;
        posicionAnterior = transform.position;

        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
        {
            contador = 0f;
            return;
        }

        bool presionandoMovimiento =
            Input.GetKey(KeyCode.W) ||
            Input.GetKey(KeyCode.S);

        bool caminando =
            new Vector2(desplazamiento.x, desplazamiento.z).sqrMagnitude >
            movimientoMinimo * movimientoMinimo;

        bool enSuelo = EstaEnSuelo();

        if (!presionandoMovimiento || !caminando || !enSuelo)
        {
            contador = 0f;
            return;
        }

        contador += Time.deltaTime;

        if (contador >= intervalo)
        {
            ReproducirPaso();
            contador = 0f;
        }
    }

    private bool EstaEnSuelo()
    {
        if (colliderJugador == null)
            return false;

        Vector3 origen = colliderJugador.bounds.center;
        float distancia = colliderJugador.bounds.extents.y + distanciaSuelo;

        return Physics.Raycast(
            origen,
            Vector3.down,
            distancia,
            capaSuelo,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ReproducirPaso()
    {
        if (audioPasos == null ||
            sonidosPasos == null ||
            sonidosPasos.Length == 0)
            return;

        AudioClip clip = sonidosPasos[
            Random.Range(0, sonidosPasos.Length)
        ];

        if (clip == null)
            return;

        audioPasos.pitch = Random.Range(0.95f, 1.05f);
        audioPasos.PlayOneShot(clip);
    }
}
