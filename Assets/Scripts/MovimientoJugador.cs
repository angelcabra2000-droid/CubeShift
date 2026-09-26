using UnityEngine;

/// <summary>
/// Movimiento del Jugador: SOLO adelante/atrás (Flecha Arriba / Flecha
/// Abajo) a lo largo de su propio transform.forward. W/A/S/D siguen
/// siendo, como siempre, las teclas que rotan el Mundo (RotadorMundo).
///
/// Cuando el jugador choca con una pared en la dirección de avance, deja
/// de moverse y le pregunta a SensorPisoRotacion cuáles de las 4
/// rotaciones (W/A/S/D) dejarían piso bajo sus pies. Para cada una que
/// sea válida, activa un "fantasma" semitransparente en la posición
/// donde quedaría el jugador si esa rotación se ejecuta.
///
/// Este script NO rota el Mundo: eso lo sigue haciendo RotadorMundo (ya
/// editado para consultar 'sensor.PuedeRotar' antes de girar). Este
/// script solo se encarga de:
///   1) mover al jugador adelante/atrás y detectar choques,
///   2) mostrar/ocultar los fantasmas de proyección,
///   3) cuando una rotación termina (evento OnRotacionCompletada de
///      RotadorMundo), teletransportar al Jugador a su nueva posición y
///      rotación relativas al Mundo.
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Colocar este script y SensorPisoRotacion sobre el mismo GameObject
///    (el Jugador).
/// 2. Asignar 'rotadorMundo' (el componente RotadorMundo del GameObject
///    "Mundo") y 'capaParedes' (el mismo Layer "Paredes" de siempre).
/// 3. Opcional: crear 4 objetos "fantasma" (una copia del modelo del
///    jugador con un material semitransparente, desactivados por
///    defecto) y asignarlos en fantasmaW/A/S/D. Si se deja alguno vacío,
///    simplemente no se muestra proyección para esa dirección.
/// </summary>
[RequireComponent(typeof(SensorPisoRotacion))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RotadorMundo rotadorMundo;
    private SensorPisoRotacion sensor;

    [Header("Movimiento")]
    [Tooltip("Velocidad de avance/retroceso en unidades por segundo.")]
    [SerializeField] private float velocidad = 3f;

    [Tooltip("Distancia a la que se detecta una pared por delante/detrás antes de bloquear el avance.")]
    [SerializeField] private float distanciaChoque = 0.5f;

    [Tooltip("Radio del SphereCast usado para detectar paredes por delante/detrás.")]
    [SerializeField] private float radioChoque = 0.3f;

    [Tooltip("Layer de las paredes/geometría del laberinto.")]
    [SerializeField] private LayerMask capaParedes;

    [Header("Fantasmas de proyección")]
    [Tooltip("Se activa/posiciona cuando la rotación W (eje X, +90°) es válida ahora mismo.")]
    [SerializeField] private Transform fantasmaW;
    [Tooltip("Se activa/posiciona cuando la rotación A (eje Y, +90°) es válida ahora mismo.")]
    [SerializeField] private Transform fantasmaA;
    [Tooltip("Se activa/posiciona cuando la rotación S (eje X, -90°) es válida ahora mismo.")]
    [SerializeField] private Transform fantasmaS;
    [Tooltip("Se activa/posiciona cuando la rotación D (eje Y, -90°) es válida ahora mismo.")]
    [SerializeField] private Transform fantasmaD;

    private void Awake()
    {
        sensor = GetComponent<SensorPisoRotacion>();
        OcultarTodosLosFantasmas();
    }

    private void OnEnable()
    {
        if (rotadorMundo != null)
            rotadorMundo.OnRotacionCompletada += ManejarRotacionCompletada;
    }

    private void OnDisable()
    {
        if (rotadorMundo != null)
            rotadorMundo.OnRotacionCompletada -= ManejarRotacionCompletada;
    }

    private void Update()
    {
        // Mientras el Mundo está en medio de un giro, no muevas al jugador
        // ni muestres proyecciones (la geometría real está a mitad de camino).
        if (rotadorMundo != null && rotadorMundo.EstaRotando)
        {
            OcultarTodosLosFantasmas();
            return;
        }

        bool bloqueado = MoverJugador();

        if (bloqueado) ActualizarFantasmas();
        else OcultarTodosLosFantasmas();
    }

    /// <summary>Mueve al jugador adelante/atrás según input. Devuelve true si el avance está bloqueado por una pared.</summary>
    private bool MoverJugador()
    {
        float entrada = 0f;
        if (Input.GetKey(KeyCode.UpArrow)) entrada = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) entrada = -1f;

        if (Mathf.Approximately(entrada, 0f))
        {
            // Sin input de movimiento: igual comprobamos si hay pared
            // inmediatamente delante o detrás, para poder mostrar los
            // fantasmas apenas el jugador se queda quieto contra una pared.
            return HayParedEnDireccion(transform.forward) || HayParedEnDireccion(-transform.forward);
        }

        Vector3 direccion = transform.forward * entrada;

        if (HayParedEnDireccion(direccion))
            return true; // bloqueado: no lo dejamos meterse en la pared

        transform.position += direccion * velocidad * Time.deltaTime;
        return false;
    }

    private bool HayParedEnDireccion(Vector3 direccion)
    {
        return Physics.SphereCast(transform.position, radioChoque, direccion.normalized,
            out _, distanciaChoque, capaParedes);
    }

    private void ActualizarFantasmas()
    {
        ActualizarFantasma(fantasmaW, Vector3.right, 90f);
        ActualizarFantasma(fantasmaA, Vector3.forward, 90f);
        ActualizarFantasma(fantasmaS, Vector3.right, -90f);
        ActualizarFantasma(fantasmaD, Vector3.forward, -90f);
    }

    private void ActualizarFantasma(Transform fantasma, Vector3 eje, float grados)
    {
        if (fantasma == null) return;

        if (sensor != null && sensor.PuedeRotar(eje, grados))
        {
            fantasma.gameObject.SetActive(true);
            fantasma.position = sensor.PosicionProyectada(eje, grados);
            fantasma.rotation = sensor.RotacionProyectada(eje, grados);
        }
        else
        {
            fantasma.gameObject.SetActive(false);
        }
    }

    private void OcultarTodosLosFantasmas()
    {
        if (fantasmaW != null) fantasmaW.gameObject.SetActive(false);
        if (fantasmaA != null) fantasmaA.gameObject.SetActive(false);
        if (fantasmaS != null) fantasmaS.gameObject.SetActive(false);
        if (fantasmaD != null) fantasmaD.gameObject.SetActive(false);
    }

    /// <summary>Se llama cuando RotadorMundo termina de girar: reubica al jugador como si hubiera girado pegado al Mundo.</summary>
    private void ManejarRotacionCompletada(Vector3 eje, float grados)
    {
        if (sensor == null) return;

        transform.position = sensor.PosicionProyectada(eje, grados);
        transform.rotation = sensor.RotacionProyectada(eje, grados);
    }
}