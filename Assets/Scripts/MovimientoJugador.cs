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
///
/// DETECCIÓN DE CHOQUE: la esfera de choque se mantiene siempre por encima
/// de la superficie del suelo (medida con un rayo hacia abajo). Así no roza
/// las costuras entre piezas de suelo, que antes se confundían con paredes.
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Colocar este script y SensorPisoRotacion sobre el Jugador.
/// 2. Asignar 'rotadorMundo' y 'capaParedes' (Layer "Paredes").
/// 3. Asignar un material brillante/de otro color en 'materialResaltado'.
/// </summary>
[RequireComponent(typeof(SensorPisoRotacion))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RotadorMundo rotadorMundo;
    private SensorPisoRotacion sensor;
    private AnimacionJugador anim;

    [Header("Movimiento")]
    [Tooltip("Velocidad de avance/retroceso en unidades por segundo.")]
    [SerializeField] private float velocidad = 3f;

    [Tooltip("Distancia a la que se detecta una pared por delante/detrás antes de bloquear el avance.")]
    [SerializeField] private float distanciaChoque = 0.5f;

    [Tooltip("Radio del SphereCast usado para detectar paredes por delante/detrás.")]
    [SerializeField] private float radioChoque = 0.3f;

    [Tooltip("Separación mínima entre el fondo de la esfera de choque y la superficie del suelo. Evita que roce las costuras entre piezas de suelo. Si todavía te frena en las uniones, súbelo a 0.1 o 0.15.")]
    [SerializeField] private float margenSobreSuelo = 0.05f;

    [Tooltip("Layer de las paredes/geometría del laberinto.")]
    [SerializeField] private LayerMask capaParedes;

    [Header("Giro con A/D")]
    [Tooltip("Marcado: cuando el personaje mira hacia atrás (después de S), A y D rotan el Mundo en sentido contrario al de cuando mira al frente, para que 'izquierda/derecha' sigan siendo las del personaje. Desmárcalo si lo ves al revés.")]
    [SerializeField] private bool invertirGiroMirandoAtras = true;

    [Header("Depuración")]
    [Tooltip("Escribe en la Console qué objeto te bloquea al caminar (solo cuando cambia).")]
    [SerializeField] private bool registrarBloqueos = true;

    [Header("Resaltado de paredes candidatas")]
    [Tooltip("Material que se asigna temporalmente a una pared mientras rotar hacia ella sea válido.")]
    [SerializeField] private Material materialResaltado;

    // Guarda el material ORIGINAL de cada pared que resaltamos, para devolvérselo después.
    private readonly Dictionary<Renderer, Material> materialesOriginales = new Dictionary<Renderer, Material>();
    private readonly HashSet<Renderer> resaltadasEsteFrame = new HashSet<Renderer>();
    private readonly List<Renderer> bufferRestaurar = new List<Renderer>();

    private string ultimoBloqueo = "";

    private void Awake()
    {
        sensor = GetComponent<SensorPisoRotacion>();
        anim = GetComponent<AnimacionJugador>();
    }

    private void Update()
    {
        if (rotadorMundo != null && rotadorMundo.EstaRotando)
        {
            QuitarTodosLosResaltados();
            ActualizarAnimacion(0f, false);
            return;
        }

        if (rotadorMundo != null)
        {
            // A = el personaje mira a SU izquierda, D = a SU derecha. Si está de espaldas a la
            // cámara, su izquierda es la derecha de la pantalla, así que el Mundo gira al revés.
            float signo = (invertirGiroMirandoAtras && anim != null && anim.MiraAtras) ? -1f : 1f;

            if (Input.GetKeyDown(KeyCode.A)) { ActualizarAnimacion(0f, false); rotadorMundo.Rotar(Vector3.up, 90f * signo); return; }
            if (Input.GetKeyDown(KeyCode.D)) { ActualizarAnimacion(0f, false); rotadorMundo.Rotar(Vector3.up, -90f * signo); return; }
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

        Vector3 adelante = sensor.Adelante;

        if (Mathf.Approximately(entrada, 0f))
        {
            ActualizarAnimacion(0f, false);
            return HayParedEnDireccion(adelante) || HayParedEnDireccion(-adelante);
        }

        Vector3 direccion = adelante * entrada;

        if (HayParedEnDireccion(direccion, true))
        {
            ActualizarAnimacion(0f, false);   // empujando una pared: no camina en el lugar
            return true;
        }

        // Avisa a la animación; si cambia de sentido, inicia el giro de 180°
        ActualizarAnimacion(1f, entrada < 0f);

        // Si hay que girar primero, espera a que termine antes de caminar
        if (anim != null && anim.DebeEsperarGiro) return false;

        transform.position += direccion * velocidad * Time.deltaTime;
        return false;
    }

    private void ActualizarAnimacion(float velocidadAnim, bool retroceso)
    {
        if (anim != null) anim.SetMovimiento(velocidadAnim, retroceso);
    }

    /// <summary>
    /// ¿Hay una pared en 'direccion'? La esfera de choque se eleva si hace falta
    /// para que su fondo quede siempre por encima del suelo real (medido con un
    /// rayo hacia abajo), así las costuras entre piezas de suelo no cuentan como pared.
    /// Los huecos no afectan: si debajo no hay piso cercano, la esfera no se mueve.
    /// </summary>
    private bool HayParedEnDireccion(Vector3 direccion, bool registrar = false)
    {
        Vector3 origen = transform.position;
        float sueloY = float.NaN;
        float centroSobreSuelo = float.NaN;

        if (Physics.Raycast(origen, Vector3.down, out RaycastHit suelo, 50f,
                capaParedes, QueryTriggerInteraction.Ignore))
        {
            sueloY = suelo.point.y;
            centroSobreSuelo = origen.y - sueloY;

            float minimo = sueloY + radioChoque + margenSobreSuelo;
            if (origen.y < minimo) origen.y = minimo;
        }

        bool hay = Physics.SphereCast(origen, radioChoque, direccion.normalized,
            out RaycastHit hit, distanciaChoque, capaParedes, QueryTriggerInteraction.Ignore);

        if (hay && registrar && registrarBloqueos)
        {
            Renderer rend = hit.collider.GetComponentInParent<Renderer>();
            string estado = rend == null ? "sin renderer" : (rend.enabled ? "visible" : "OCULTA");

            string msg = "Bloquea: " + hit.collider.name
                + " | normal " + hit.normal.ToString("F2")
                + " | contacto sobre el suelo " + (hit.point.y - sueloY).ToString("F2")
                + " | centro sobre el suelo " + centroSobreSuelo.ToString("F2")
                + " | " + estado;

            if (msg != ultimoBloqueo)
            {
                Debug.Log("[MovimientoJugador] " + msg, hit.collider);
                ultimoBloqueo = msg;
            }
        }

        return hay;
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