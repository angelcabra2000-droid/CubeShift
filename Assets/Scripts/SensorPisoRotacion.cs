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
/// El eje Y a propósito NO se usa para esto: rotar sobre el eje vertical
/// nunca cambia qué hay "abajo" (la gravedad no se mueve), solo gira el
/// Mundo sobre sí mismo. Por eso las únicas 2 rotaciones que realmente
/// cambian el piso son sobre X y sobre Z.
///
/// Colocar este script sobre el Jugador (junto con MovimientoJugador).
/// </summary>
public class SensorPisoRotacion : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El GameObject 'Mundo': su transform.position es el pivote alrededor del cual rota todo el laberinto.")]
    [SerializeField] private Transform mundo;

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

    private Vector3 Pivote => mundo != null ? mundo.position : Vector3.zero;

    public bool PuedeIrAdelante() => HayParedEnDireccion(jugador.forward);
    public bool PuedeIrAtras() => HayParedEnDireccion(-jugador.forward);
    public bool PuedeIrIzquierda() => HayParedEnDireccion(-jugador.right);
    public bool PuedeIrDerecha() => HayParedEnDireccion(jugador.right);

    private bool HayParedEnDireccion(Vector3 direccion)
    {
        return Physics.SphereCast(jugador.position, radioDeteccion, direccion.normalized,
            out _, distanciaDeteccion, capaParedes);
    }

    /// <summary>
    /// ¿Habría piso debajo del jugador SI el Mundo rotara 'grados' sobre 'eje'
    /// (mismos parámetros que usa RotadorMundo.IniciarRotacion)? Traduce el
    /// par (eje, grados) a una de las 4 direcciones de arriba.
    /// </summary>
    public bool PuedeRotar(Vector3 eje, float grados)
    {
        if (eje == Vector3.right) return grados > 0 ? PuedeIrAdelante() : PuedeIrAtras();
        if (eje == Vector3.forward) return grados > 0 ? PuedeIrIzquierda() : PuedeIrDerecha();
        return false; // eje no soportado (Vector3.up: rotar en Y nunca cambia el piso, ver comentario de arriba)
    }

    /// <summary>Posición donde quedaría el jugador tras esa rotación (viaja junto con el Mundo, como si estuviera pegado a él).</summary>
    public Vector3 PosicionProyectada(Vector3 eje, float grados)
    {
        Quaternion r = Quaternion.AngleAxis(grados, eje);
        Vector3 pivote = Pivote;
        return pivote + r * (jugador.position - pivote);
    }

    /// <summary>Rotación que tendría el jugador tras esa rotación (para que su "adelante" siga apuntando al mismo pasillo).</summary>
    public Quaternion RotacionProyectada(Vector3 eje, float grados)
    {
        Quaternion r = Quaternion.AngleAxis(grados, eje);
        return r * jugador.rotation;
    }

    // --- DEBUG VISUAL ---
    // Dibuja en la Scene view las 4 direcciones: verde si hay pared (rotación
    // válida), rojo si no. Seleccioná al Jugador en la Hierarchy mientras
    // estás en Play para verlo.
    private void OnDrawGizmosSelected()
    {
        if (jugador == null) jugador = transform;
        DibujarDireccion(jugador.forward, Application.isPlaying && PuedeIrAdelante());
        DibujarDireccion(-jugador.forward, Application.isPlaying && PuedeIrAtras());
        DibujarDireccion(jugador.right, Application.isPlaying && PuedeIrDerecha());
        DibujarDireccion(-jugador.right, Application.isPlaying && PuedeIrIzquierda());
    }

    private void DibujarDireccion(Vector3 direccion, bool valido)
    {
        Gizmos.color = valido ? Color.green : Color.red;
        Vector3 origen = jugador.position;
        Vector3 destino = origen + direccion.normalized * distanciaDeteccion;
        Gizmos.DrawLine(origen, destino);
        Gizmos.DrawWireSphere(destino, radioDeteccion);
    }
}