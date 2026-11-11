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
    [SerializeField] private float segundosQuietoParaBailar = 8f;
    [Range(0f, 1f)][SerializeField] private float probabilidadBaile = 0.5f;

    private static readonly int VelocidadHash = Animator.StringToHash("Velocidad");
    private static readonly int RetrocesoHash = Animator.StringToHash("Retroceso");
    private static readonly int EnSueloHash = Animator.StringToHash("EnSuelo");
    private static readonly int VictoriaHash = Animator.StringToHash("Victoria");
    private static readonly int BaileHash = Animator.StringToHash("Baile");

    private CapsuleCollider col;
    private bool quieto = true;
    private float tiempoSinSuelo;
    private float tiempoQuieto;

    private void Awake()
    {
        col = GetComponent<CapsuleCollider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (rotadorMundo == null) rotadorMundo = FindFirstObjectByType<RotadorMundo>();
        if (metaPelota == null) metaPelota = FindFirstObjectByType<MetaPelota>();

        if (animator == null)
            Debug.LogError("[AnimacionJugador] No encuentro el Animator. Asigna el objeto Modelo.", this);
    }

    private void OnEnable()
    {
        if (metaPelota != null) metaPelota.OnPelotaEncontrada += Victoria;
    }

    private void OnDisable()
    {
        if (metaPelota != null) metaPelota.OnPelotaEncontrada -= Victoria;
    }

    // La llama MovimientoJugador en cada frame
    public void SetMovimiento(float velocidad, bool retroceso)
    {
        if (animator == null) return;
        quieto = velocidad < 0.1f;
        animator.SetFloat(VelocidadHash, velocidad);
        animator.SetBool(RetrocesoHash, retroceso);
    }

    public void Victoria()
    {
        if (animator != null) animator.SetTrigger(VictoriaHash);
    }

    private void Update()
    {
        if (animator == null) return;

        // Mientras el Mundo gira, el suelo desaparece un instante: se mantiene el estado anterior.
        bool rotando = rotadorMundo != null && rotadorMundo.EstaRotando;
        if (!rotando)
        {
            if (DetectarSuelo()) tiempoSinSuelo = 0f;
            else tiempoSinSuelo += Time.deltaTime;
        }

        bool enSuelo = tiempoSinSuelo < retrasoCaida;
        animator.SetBool(EnSueloHash, enSuelo);

        // Baile aleatorio solo cuando está quieto y en el suelo
        if (quieto && enSuelo)
        {
            tiempoQuieto += Time.deltaTime;
            if (tiempoQuieto >= segundosQuietoParaBailar)
            {
                tiempoQuieto = 0f;
                if (Random.value < probabilidadBaile)
                    animator.SetTrigger(BaileHash);
            }
        }
        else
        {
            tiempoQuieto = 0f;
        }
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