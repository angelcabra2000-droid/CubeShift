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

    [Header("Depuración")]
    [Tooltip("Imprime en la Console qué atraviesa el click. Útil para ver por qué no rota.")]
    [SerializeField] private bool depurar = true;

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

        // RaycastAll en vez de Raycast: el rayo atraviesa TODAS las paredes
        // que haya en el camino, no solo la primera. Esto importa porque
        // OcultarParedesEntreCamaraYPlayer apaga el Renderer de las paredes
        // que tapan al jugador pero su Collider sigue ahí (invisible), y un
        // Raycast normal se detendría en esa pared fantasma en vez de llegar
        // a la pared resaltada de verdad.
        RaycastHit[] impactos = Physics.RaycastAll(
            rayo, distanciaMaxima, capaParedes, QueryTriggerInteraction.Collide);

        if (depurar)
            Debug.Log($"[ClicParaRotar] El rayo atravesó {impactos.Length} colliders.");

        foreach (RaycastHit impacto in impactos)
        {
            if (sensor.DireccionParaPared(impacto.collider, out Vector3 direccion)
                && sensor.ObtenerRotacionParaDireccion(direccion, out Vector3 eje, out float grados))
            {
                if (depurar) Debug.Log($"[ClicParaRotar] Pared válida: {impacto.collider.name}. Rotando eje={eje}, grados={grados}.");
                rotadorMundo.Rotar(eje, grados);
                return;
            }
        }

        if (depurar)
            Debug.Log("[ClicParaRotar] Ninguno de los colliders del rayo es una de las 4 paredes candidatas ahora mismo.");
    }
}