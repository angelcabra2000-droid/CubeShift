using UnityEngine;

/// <summary>
/// Gravedad para el Jugador (que se mueve por transform, sin Rigidbody).
///
/// Cada frame mira hacia abajo con un SphereCast contra las paredes. Si no hay
/// piso bajo los pies, el Jugador cae con aceleración constante; al llegar al
/// piso se detiene justo encima de él.
///
/// No toca ningún otro script: MovimientoJugador sigue moviendo en horizontal y
/// RotadorMundo sigue girando el Mundo. Mientras el Mundo está rotando la
/// gravedad se pausa (RotadorMundo toma la posición del Jugador como pivote al
/// empezar el giro, y esta no debe cambiar a mitad del giro).
///
/// Colocar sobre el Jugador (el mismo objeto que tiene el CapsuleCollider).
/// </summary>
[RequireComponent(typeof(Collider))]
public class GravedadJugador : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El RotadorMundo del GameObject 'Mundo'. Pausa la caída mientras el mundo gira.")]
    [SerializeField] private RotadorMundo rotadorMundo;

    [Tooltip("Layer 'Paredes' (la misma que usan los demás scripts).")]
    [SerializeField] private LayerMask capaParedes;

    [Header("Caída")]
    [Tooltip("Aceleración de la gravedad (m/s²). Usa el mismo valor que la pelota para que se sienta igual.")]
    [SerializeField] private float gravedad = 20f;

    [Tooltip("Velocidad máxima de caída (m/s).")]
    [SerializeField] private float velocidadMaximaCaida = 30f;

    [Tooltip("Si el piso está a menos de esta distancia bajo los pies, se considera apoyado.")]
    [SerializeField] private float margenSuelo = 0.03f;

    private Collider col;
    private float velocidadCaida;          // positiva = hacia abajo
    public bool EstaEnSuelo { get; private set; }

    private void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void Update()
    {
        if (rotadorMundo != null && rotadorMundo.EstaRotando)
        {
            velocidadCaida = 0f;
            return;
        }

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        Bounds b = col.bounds;
        float r = Mathf.Min(b.extents.x, b.extents.z) * 0.9f;   // un poco más angosto que el cuerpo
        float centroAPies = b.extents.y;                        // distancia del centro a los pies
        float libreInicial = Mathf.Max(0f, centroAPies - r);    // lo que baja la esfera antes de que sus pies toquen

        // ¿Cuánto hay libre bajo los pies?
        float distanciaMirar = libreInicial + velocidadMaximaCaida * dt + margenSuelo + 1f;
        float libreBajoPies = float.PositiveInfinity;
        if (Physics.SphereCast(b.center, r, Vector3.down, out RaycastHit hit,
                distanciaMirar, capaParedes, QueryTriggerInteraction.Collide))
        {
            libreBajoPies = Mathf.Max(0f, hit.distance - libreInicial);
        }

        if (libreBajoPies <= margenSuelo)
        {
            // Apoyado: sin caída
            EstaEnSuelo = true;
            velocidadCaida = 0f;
            return;
        }

        // En el aire: cae (Euler semi-implícito)
        EstaEnSuelo = false;
        velocidadCaida = Mathf.Min(velocidadCaida + gravedad * dt, velocidadMaximaCaida);
        float paso = velocidadCaida * dt;

        // Nunca bajar más de lo que hay libre (para no hundirse en el piso)
        float bajar = Mathf.Min(paso, libreBajoPies);
        transform.position += Vector3.down * bajar;

        if (paso >= libreBajoPies)
        {
            // Aterrizó en este frame
            EstaEnSuelo = true;
            velocidadCaida = 0f;
        }
    }
}
