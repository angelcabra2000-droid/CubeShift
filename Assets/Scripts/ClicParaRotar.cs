using UnityEngine;

/// <summary>
/// Al hacer click con el mouse sobre una de las paredes resaltadas
/// (candidatas a convertirse en el nuevo piso, ver MovimientoJugador), dispara
/// esa rotación en vez de usar teclas. Reemplaza el control por W/A/S/D
/// para rotar el Mundo.
///
/// Colocar este script en cualquier GameObject de la escena (por ejemplo,
/// el mismo Player o la Main Camera).
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Asignar 'camara' (si se deja vacío, usa Camera.main).
/// 2. Asignar 'rotadorMundo' (el del GameObject "Mundo") y 'sensor' (el
///    SensorPisoRotacion del Player).
/// 3. Asignar 'capaParedes' (Layer "Paredes", la misma de siempre).
/// </summary>
public class ClicParaRotar : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío, se usa Camera.main.")]
    [SerializeField] private Camera camara;

    [SerializeField] private RotadorMundo rotadorMundo;
    [SerializeField] private SensorPisoRotacion sensor;

    [Header("Detección de click")]
    [Tooltip("Layer de las paredes/geometría del laberinto.")]
    [SerializeField] private LayerMask capaParedes;

    [Tooltip("Distancia máxima del raycast del mouse.")]
    [SerializeField] private float distanciaMaxima = 100f;

    private void Awake()
    {
        if (camara == null) camara = Camera.main;
    }

    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (camara == null || rotadorMundo == null || sensor == null) return;
        if (rotadorMundo.EstaRotando) return;

        Ray rayo = camara.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(rayo, out RaycastHit hit, distanciaMaxima, capaParedes)) return;

        if (sensor.DireccionParaPared(hit.collider, out Vector3 direccion))
            rotadorMundo.SolicitarRotacionHacia(direccion);
        // Si el click no cayó sobre ninguna de las 4 paredes resaltadas
        // actuales, no pasa nada (click "en el aire" o sobre una pared no válida).
    }
}