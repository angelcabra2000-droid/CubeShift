using UnityEngine;

public class AnimacionJugador : MonoBehaviour
{
    [Header("Referencias (si se dejan vacías, se buscan solas)")]
    [SerializeField] private Animator animator;          // el del objeto Modelo
    [SerializeField] private RotadorMundo rotadorMundo;
    [SerializeField] private MetaPelota metaPelota;

    [Header("Detección de suelo")]
    [Tooltip("La misma capa que usas para las paredes (Paredes).")]
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private float distanciaSuelo = 0.15f;
    [Tooltip("Segundos sin tocar suelo antes de iniciar la caída.")]
    [SerializeField] private float retrasoCaida = 0.15f;

    [Header("Baile")]
    [SerializeField] private KeyCode teclaBaile = KeyCode.T;
    [Tooltip("Segundos que se recuerda la tecla de baile si se pulsa antes de que el personaje esté en reposo (por ejemplo, mientras termina el giro). Baila en cuanto pueda.")]
    [SerializeField] private float memoriaBaile = 1.5f;

    [Header("Giro del modelo")]
    [Tooltip("Marcado: al empezar, el modelo ya mira hacia donde irá con W (hacia adentro de la pantalla). Desmárcalo si empieza mirando hacia la cámara.")]
    [SerializeField] private bool modeloMiraHaciaAdelante = true;
    [Tooltip("Segundos que tarda el giro de 180°. Ponlo igual a la duración del clip Walking Turn 180 × 0.9.")]
    [SerializeField] private float duracionGiro = 0.9f;
    [Tooltip("Marcado: el jugador gira primero y empieza a caminar cuando termina el giro.")]
    [SerializeField] private bool esperarGiroParaMover = true;

    private static readonly int VelocidadHash = Animator.StringToHash("Velocidad");
    private static readonly int EnSueloHash = Animator.StringToHash("EnSuelo");
    private static readonly int VictoriaHash = Animator.StringToHash("Victoria");
    private static readonly int BaileHash = Animator.StringToHash("Baile");
    // Giro de adelante -> atrás (W a S)
    private static readonly int Giro180Hash = Animator.StringToHash("Giro180");
    // Giro de atrás -> adelante (S a W): usa el estado espejado
    private static readonly int Giro180InversoHash = Animator.StringToHash("Giro180Inverso");

    private CapsuleCollider col;
    private Transform modelo;
    private Quaternion rotacionBase;
    private bool inicioMiraAtras;
    private bool miraAtras;
    private bool girando;
    private bool quieto = true;
    private bool juegoTerminado;
    private float tiempoSinSuelo;
    private float tiempoPedidoBaile = -999f;

    /// <summary>True mientras debe esperarse a que termine el giro antes de caminar.</summary>
    public bool DebeEsperarGiro => esperarGiroParaMover && girando;

    /// <summary>True si el personaje está mirando hacia atrás (hacia la cámara, tras pulsar S).</summary>
    public bool MiraAtras => miraAtras;

    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rotadorMundo == null) rotadorMundo = FindFirstObjectByType<RotadorMundo>();
        if (metaPelota == null) metaPelota = FindFirstObjectByType<MetaPelota>();

        if (animator == null)
        {
            Debug.LogError("[AnimacionJugador] No encuentro el Animator. Asigna el objeto Modelo.", this);
            return;
        }

        // MetaPelota congela el juego con Time.timeScale = 0. Con el modo normal el Animator
        // también se congela y la animación de victoria nunca se ve. En "Unscaled Time"
        // el Animator sigue corriendo aunque el tiempo del juego esté en 0.
        animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        VerificarParametros();

        modelo = animator.transform;
        rotacionBase = modelo.localRotation;
        inicioMiraAtras = !modeloMiraHaciaAdelante;
        miraAtras = inicioMiraAtras;
    }

    // Avisa claro en la Console si falta algún parámetro en el Animator Controller
    // (si falta, SetTrigger/SetBool no hacen nada y la animación jamás se reproduce).
    private void VerificarParametros()
    {
        string[] necesarios = { "Velocidad", "EnSuelo", "Victoria", "Baile", "Giro180", "Giro180Inverso" };
        foreach (string nombre in necesarios)
        {
            bool existe = false;
            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.name == nombre) { existe = true; break; }
            }
            if (!existe)
                Debug.LogError("[AnimacionJugador] El Animator no tiene el parámetro '" + nombre + "'. Agrégalo en la pestaña Parameters.", this);
        }
    }

    private void OnEnable()
    {
        if (metaPelota != null) metaPelota.OnPelotaEncontrada += Victoria;
    }

    private void OnDisable()
    {
        if (metaPelota != null) metaPelota.OnPelotaEncontrada -= Victoria;
    }

    // La llama MovimientoJugador en cada frame.
    // 'retroceso' = true si va hacia atrás (S). Si el sentido cambia, inicia el giro de 180°.
    public void SetMovimiento(float velocidad, bool retroceso)
    {
        if (animator == null || juegoTerminado) return;

        quieto = velocidad < 0.1f;
        animator.SetFloat(VelocidadHash, velocidad);

        if (!quieto && retroceso != miraAtras)
        {
            miraAtras = retroceso;
            girando = true;

            // Limpia el otro trigger por si quedó pendiente
            animator.ResetTrigger(Giro180Hash);
            animator.ResetTrigger(Giro180InversoHash);

            // W -> S: giro normal. S -> W: giro espejado (hacia el otro lado)
            animator.SetTrigger(retroceso ? Giro180Hash : Giro180InversoHash);
        }
    }

    public void Victoria()
    {
        if (animator == null) return;
        juegoTerminado = true;

        // Cancela cualquier giro pendiente para que no tape la victoria
        animator.ResetTrigger(Giro180Hash);
        animator.ResetTrigger(Giro180InversoHash);
        animator.SetTrigger(VictoriaHash);
    }

    private void Update()
    {
        if (animator == null || juegoTerminado) return;

        // Suelo (mientras el Mundo gira se mantiene el estado anterior)
        bool rotando = rotadorMundo != null && rotadorMundo.EstaRotando;
        if (!rotando)
        {
            if (DetectarSuelo()) tiempoSinSuelo = 0f;
            else tiempoSinSuelo += Time.deltaTime;
        }

        bool enSuelo = tiempoSinSuelo < retrasoCaida;
        animator.SetBool(EnSueloHash, enSuelo);

        // Baile: la tecla se "recuerda" unos instantes, así no se pierde si se pulsa mientras
        // el personaje aún termina el giro o la transición a Idle (justo después de S).
        // Baila apenas esté quieto, en el suelo, sin girar y en Idle, de frente o de espaldas.
        if (Input.GetKeyDown(teclaBaile)) tiempoPedidoBaile = Time.unscaledTime;

        if (!quieto)
        {
            tiempoPedidoBaile = -999f;   // si empieza a caminar, se cancela el baile pendiente
        }
        else if (Time.unscaledTime - tiempoPedidoBaile <= memoriaBaile
                 && enSuelo && !girando && EstaEnIdle())
        {
            tiempoPedidoBaile = -999f;
            animator.SetTrigger(BaileHash);
        }

        ActualizarOrientacion();
    }

    private void ActualizarOrientacion()
    {
        Quaternion objetivo = (miraAtras == inicioMiraAtras)
            ? rotacionBase
            : rotacionBase * Quaternion.Euler(0f, 180f, 0f);

        float gradosPorSegundo = 180f / Mathf.Max(0.05f, duracionGiro);
        modelo.localRotation = Quaternion.RotateTowards(
            modelo.localRotation, objetivo, gradosPorSegundo * Time.deltaTime);

        girando = Quaternion.Angle(modelo.localRotation, objetivo) > 0.5f;
    }

    private bool EstaEnIdle()
    {
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.IsName("Idle") && !animator.IsInTransition(0);
    }

    private bool DetectarSuelo()
    {
        if (col == null) return true;

        Bounds b = col.bounds;
        float radio = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;
        float distancia = b.extents.y - radio + distanciaSuelo;

        return Physics.SphereCast(b.center, radio, Vector3.down, out _, distancia,
            capaSuelo, QueryTriggerInteraction.Ignore);
    }
}