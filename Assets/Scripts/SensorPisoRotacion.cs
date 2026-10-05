using UnityEngine;

/// <summary>
/// Detecta, en las 4 direcciones horizontales alrededor del Jugador
/// (adelante, atrás, izquierda, derecha), si hay una pared que podría
/// convertirse en el nuevo piso al rotar el Mundo hacia ese lado.
///
/// Mapeo de rotaciones (tiene que coincidir EXACTO con RotadorMundo):
///   W: eje X, +90°  -> la pared de ADELANTE   pasa a ser el piso
///   S: eje X, -90°  -> la pared de ATRÁS      pasa a ser el piso
///   A: eje Z, +90°  -> la pared de IZQUIERDA  pasa a ser el piso
///   D: eje Z, -90°  -> la pared de DERECHA    pasa a ser el piso
///
/// El eje Y a propósito no se usa: rotar sobre el eje vertical nunca
/// cambia qué hay "abajo".
///
/// IMPORTANTE: el Jugador NUNCA se mueve ni rota — eso ahora lo hace
/// RotadorMundo, girando el Mundo alrededor del Jugador. Por eso este
/// sensor ya no necesita saber nada del Mundo ni calcular posiciones
/// proyectadas: solo mira hacia afuera desde la posición actual (fija)
/// del Jugador.
///
/// Colocar este script sobre el Jugador (junto con MovimientoJugador).
/// </summary>
public class SensorPisoRotacion : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío, se usa este mismo transform (el del Jugador).")]
    [SerializeField] private Transform jugador;

    [Header("Detección")]
    [Tooltip("Layer que usan las paredes/geometría del laberinto.")]
    [SerializeField] private LayerMask capaParedes;

    [Tooltip("Radio del SphereCast usado para detectar pared en cada dirección.")]
    [SerializeField] private float radioDeteccion = 0.3f;

    [Tooltip("Distancia a la que se busca pared, en cada una de las 4 direcciones horizontales.")]
    [SerializeField] private float distanciaDeteccion = 0.6f;

    private void Awake()
    {
        if (jugador == null) jugador = transform;
    }

    public bool PuedeIrAdelante() => IntentarAdelante(out _);
    public bool PuedeIrAtras() => IntentarAtras(out _);
    public bool PuedeIrIzquierda() => IntentarIzquierda(out _);
    public bool PuedeIrDerecha() => IntentarDerecha(out _);

    /// <summary>Igual que PuedeIrX, pero además devuelve el impacto (para poder resaltar la pared encontrada).</summary>
    public bool IntentarAdelante(out RaycastHit hit) => Intentar(jugador.forward, out hit);
    public bool IntentarAtras(out RaycastHit hit) => Intentar(-jugador.forward, out hit);
    public bool IntentarIzquierda(out RaycastHit hit) => Intentar(-jugador.right, out hit);
    public bool IntentarDerecha(out RaycastHit hit) => Intentar(jugador.right, out hit);

    private bool Intentar(Vector3 direccion, out RaycastHit hit)
    {
        return Physics.SphereCast(jugador.position, radioDeteccion, direccion.normalized,
            out hit, distanciaDeteccion, capaParedes);
    }

    /// <summary>
    /// Para una dirección horizontal del mundo (normalmente jugador.forward,
    /// -jugador.forward, jugador.right o -jugador.right): ¿hay pared ahí
    /// (rotación válida), y si la hay, qué (eje, grados) hace falta pasarle
    /// a RotadorMundo para que esa pared termine siendo el piso?
    ///
    /// El eje se calcula con un producto cruz en vez de venir fijo (X o Z)
    /// porque el Jugador ahora puede girar libremente en el lugar (Flecha
    /// Izquierda/Derecha) — "adelante" ya no es siempre el eje Z del mundo.
    /// </summary>
    public bool ObtenerRotacionParaDireccion(Vector3 direccionMundial, out Vector3 eje, out float grados)
    {
        eje = Vector3.zero;
        grados = 0f;

        if (!Intentar(direccionMundial, out _)) return false;

        // Rotación de 90° que manda 'direccionMundial' exactamente a Vector3.down.
        eje = Vector3.Cross(direccionMundial, Vector3.down).normalized;
        grados = 90f;
        return true;
    }

    /// <summary>
    /// Si 'pared' es (el collider de) una de las 4 paredes candidatas AHORA
    /// MISMO, devuelve la dirección mundial (jugador.forward, etc.) que le
    /// corresponde — para poder disparar esa rotación desde un click.
    /// </summary>
    public bool DireccionParaPared(Collider pared, out Vector3 direccionMundial)
    {
        if (pared != null)
        {
            if (Intentar(jugador.forward, out RaycastHit h) && h.collider == pared) { direccionMundial = jugador.forward; return true; }
            if (Intentar(-jugador.forward, out h) && h.collider == pared) { direccionMundial = -jugador.forward; return true; }
            if (Intentar(-jugador.right, out h) && h.collider == pared) { direccionMundial = -jugador.right; return true; }
            if (Intentar(jugador.right, out h) && h.collider == pared) { direccionMundial = jugador.right; return true; }
        }
        direccionMundial = Vector3.zero;
        return false;
    }

    // --- DEBUG VISUAL ---
    private void OnDrawGizmosSelected()
    {
        if (jugador == null) jugador = transform;
        Dibujar(jugador.forward, Application.isPlaying && PuedeIrAdelante());
        Dibujar(-jugador.forward, Application.isPlaying && PuedeIrAtras());
        Dibujar(jugador.right, Application.isPlaying && PuedeIrDerecha());
        Dibujar(-jugador.right, Application.isPlaying && PuedeIrIzquierda());
    }

    private void Dibujar(Vector3 direccion, bool valido)
    {
        Gizmos.color = valido ? Color.green : Color.red;
        Vector3 origen = jugador.position;
        Vector3 destino = origen + direccion.normalized * distanciaDeteccion;
        Gizmos.DrawLine(origen, destino);
        Gizmos.DrawWireSphere(destino, radioDeteccion);
    }
}