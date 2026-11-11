using UnityEngine;

/// <summary>
/// Coloca al Jugador y a la Pelota en los puntos que tú decidas, usando
/// GameObjects vacíos como marcadores.
///
/// --- CONFIGURACIÓN ---
/// 1. Crea dos GameObjects vacíos (por ejemplo "PuntoInicio" y "PuntoPelota").
///    Se recomienda hacerlos hijos de "Mundo". Muévelos donde quieras DENTRO
///    del cubo: la posición del vacío es la posición del CENTRO del objeto.
///    (Si lo dejas en el aire, el Jugador y la Pelota caen por su gravedad.)
/// 2. En el objeto "Setup" asigna: Punto Inicio, Punto Pelota, Jugador y Pelota.
///
/// Se ejecuta en Awake (antes que cualquier Start) para que BallSimulator,
/// que lee la posición de la pelota en su Start, ya la encuentre colocada.
/// </summary>
public class ColocarInicioYMeta : MonoBehaviour
{
    [Header("Marcadores (GameObjects vacíos)")]
    [SerializeField] private Transform puntoInicio;
    [SerializeField] private Transform puntoPelota;

    [Header("Objetos a colocar")]
    [SerializeField] private Transform jugador;
    [SerializeField] private Transform pelota;

    [Header("Gizmos (solo para verlos en la Scene)")]
    [SerializeField] private Vector3 tamanoGizmoJugador = new Vector3(1f, 2f, 1f);
    [SerializeField] private float radioGizmoPelota = 0.5f;

    private void Awake()
    {
        Colocar();
    }

    private void Colocar()
    {
        if (jugador == null || puntoInicio == null)
            Debug.LogError("[ColocarInicioYMeta] Falta asignar 'Jugador' y/o 'Punto Inicio'.", this);
        else
            jugador.position = puntoInicio.position;

        if (pelota == null || puntoPelota == null)
            Debug.LogError("[ColocarInicioYMeta] Falta asignar 'Pelota' y/o 'Punto Pelota'.", this);
        else
        {
            pelota.position = puntoPelota.position;

            // Por si la pelota ya traía velocidad de una simulación anterior.
            if (pelota.TryGetComponent(out BallSimulator bola))
            {
                bola.Velocity = Vector3.zero;
                bola.AngularVelocity = Vector3.zero;
            }
        }

        // Que los colliders se enteren del cambio de posición ya mismo.
        Physics.SyncTransforms();
    }

    private void OnDrawGizmos()
    {
        if (puntoInicio != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(puntoInicio.position, tamanoGizmoJugador);
        }

        if (puntoPelota != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(puntoPelota.position, radioGizmoPelota);
        }
    }
}