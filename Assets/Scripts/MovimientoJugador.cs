using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Movimiento del Jugador, todo con W/A/S/D (ya no se usan las flechas):
///   W / S -> avanzar / retroceder hacia adentro / afuera de la pantalla
///            (sensor.Adelante, que sale de la vista de cámara actual)
///   A / D -> "mirar" a la izquierda / derecha, pero SIN girar al Jugador:
///            lo que gira es el Mundo, alrededor del eje vertical del
///            Jugador y en sentido contrario (si mirás a la izquierda, el
///            Mundo gira hacia la derecha). El Jugador nunca rota.
///
/// La rotación del Mundo ya NO se dispara con teclado — eso ahora lo hace
/// ClicParaRotar, al clickear una de las paredes que este script resalta.
///
/// Cuando el jugador choca con una pared, consulta a SensorPisoRotacion
/// cuáles de las 4 direcciones (adelante/atrás/izquierda/derecha) tienen
/// una pared que podría convertirse en el nuevo piso, y RESALTA esa(s)
/// pared(es) con un material distinto mientras la rotación sea válida.
/// No hay "fantasma" del jugador: como el jugador no cambia de lugar, lo
/// único que tiene sentido mostrar es cuál pared real se va a convertir
/// en piso.
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Colocar este script y SensorPisoRotacion sobre el Jugador.
/// 2. Asignar 'rotadorMundo' y 'capaParedes' (Layer "Paredes").
/// 3. Crear un material brillante/de otro color (puede ser el mismo
///    "Mat_Fantasma" que ya tenés, o uno nuevo tipo "Mat_Resaltado") y
///    asignarlo en 'materialResaltado'.
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

    [Header("Resaltado de paredes candidatas")]
    [Tooltip("Material que se asigna temporalmente a una pared mientras rotar hacia ella sea válido.")]
    [SerializeField] private Material materialResaltado;

    // Guarda el material ORIGINAL de cada pared que resaltamos, para devolvérselo después.
    private readonly Dictionary<Renderer, Material> materialesOriginales = new Dictionary<Renderer, Material>();
    private readonly HashSet<Renderer> resaltadasEsteFrame = new HashSet<Renderer>();
    private readonly List<Renderer> bufferRestaurar = new List<Renderer>();

    private void Awake()
    {
        sensor = GetComponent<SensorPisoRotacion>();
    }

    private void Update()
    {
        if (rotadorMundo != null && rotadorMundo.EstaRotando)
        {
            QuitarTodosLosResaltados();
            return;
        }

        // "Mirar" a un lado = girar el MUNDO en sentido contrario, alrededor
        // del eje vertical que pasa por el Jugador. En Unity un ángulo
        // positivo sobre Vector3.up gira en sentido horario visto desde
        // arriba, así que: A (mirar izquierda) -> Mundo +90°, D (mirar
        // derecha) -> Mundo -90°. Si lo sentís al revés, invertí los signos.
        if (rotadorMundo != null)
        {
            if (Input.GetKeyDown(KeyCode.A)) { rotadorMundo.Rotar(Vector3.up, 90f); return; }
            if (Input.GetKeyDown(KeyCode.D)) { rotadorMundo.Rotar(Vector3.up, -90f); return; }
        }

        bool bloqueado = MoverJugador();

        if (bloqueado) ActualizarResaltados();
        else QuitarTodosLosResaltados();
    }

    /// <summary>Mueve al jugador adelante/atrás según input. Devuelve true si el avance está bloqueado por una pared.</summary>
    private bool MoverJugador()
    {
        float entrada = 0f;
        if (Input.GetKey(KeyCode.W)) entrada = 1f;
        else if (Input.GetKey(KeyCode.S)) entrada = -1f;

        // "Adelante" = hacia adentro de la pantalla según la vista de cámara
        // actual (el Jugador nunca rota, así que su propio forward no sirve).
        Vector3 adelante = sensor.Adelante;

        if (Mathf.Approximately(entrada, 0f))
            return HayParedEnDireccion(adelante) || HayParedEnDireccion(-adelante);

        Vector3 direccion = adelante * entrada;

        if (HayParedEnDireccion(direccion))
            return true;

        transform.position += direccion * velocidad * Time.deltaTime;
        return false;
    }

    private bool HayParedEnDireccion(Vector3 direccion)
    {
        return Physics.SphereCast(transform.position, radioChoque, direccion.normalized,
            out _, distanciaChoque, capaParedes);
    }

    private void ActualizarResaltados()
    {
        resaltadasEsteFrame.Clear();

        if (sensor.IntentarAdelante(out RaycastHit hAdelante)) Resaltar(hAdelante);
        if (sensor.IntentarAtras(out RaycastHit hAtras)) Resaltar(hAtras);
        if (sensor.IntentarIzquierda(out RaycastHit hIzq)) Resaltar(hIzq);
        if (sensor.IntentarDerecha(out RaycastHit hDer)) Resaltar(hDer);

        // Restaurar las que estaban resaltadas y ya no corresponden este frame.
        bufferRestaurar.Clear();
        foreach (var rend in materialesOriginales.Keys)
            if (!resaltadasEsteFrame.Contains(rend)) bufferRestaurar.Add(rend);

        foreach (var rend in bufferRestaurar) Restaurar(rend);
    }

    private void Resaltar(RaycastHit hit)
    {
        if (materialResaltado == null) return;

        Renderer rend = hit.collider != null ? hit.collider.GetComponentInParent<Renderer>() : null;
        if (rend == null) return;

        resaltadasEsteFrame.Add(rend);

        if (!materialesOriginales.ContainsKey(rend))
        {
            materialesOriginales[rend] = rend.sharedMaterial;
            rend.material = materialResaltado;
        }
    }

    private void Restaurar(Renderer rend)
    {
        if (rend != null && materialesOriginales.TryGetValue(rend, out Material original))
            rend.material = original;
        materialesOriginales.Remove(rend);
    }

    private void QuitarTodosLosResaltados()
    {
        bufferRestaurar.Clear();
        bufferRestaurar.AddRange(materialesOriginales.Keys);
        foreach (var rend in bufferRestaurar) Restaurar(rend);
    }
}