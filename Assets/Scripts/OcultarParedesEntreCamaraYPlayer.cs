using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oculta (o hace transparentes) las paredes que quedan entre la cámara y
/// lo que la cámara está enfocando: el Player normalmente, o la pelota mientras
/// se mantiene ESPACIO (y el punto intermedio durante la transición).
///
/// Cada frame lanza un SphereCast desde la cámara hacia el punto de foco. Todo
/// lo que golpee en el camino (que esté en el Layer "Paredes") se oculta;
/// en cuanto deja de estar en el camino, se vuelve a mostrar.
///
/// Colocar este script sobre la Main Camera (el mismo objeto que tiene CamaraVertices).
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Layer "Paredes" asignado a todas las paredes del laberinto (NO al Player ni a la pelota).
/// 2. Asigna ese Layer en "Capa Paredes".
/// 3. "Camara Vertices" se busca solo en este mismo objeto; si no está, se usa "Jugador".
/// </summary>
[DefaultExecutionOrder(1001)] // después de CamaraVertices (1000) y de BallSimulator
public class OcultarParedesEntreCamaraYPlayer : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Si se deja vacío, se busca en este mismo GameObject.")]
    [SerializeField] private CamaraVertices camaraVertices;

    [Tooltip("Solo se usa como respaldo si no hay CamaraVertices.")]
    [SerializeField] private Transform jugador;

    [Tooltip("Layer que usan las paredes/geometría que se pueden ocultar. El Player y la pelota NO deben estar en este layer.")]
    [SerializeField] private LayerMask capaParedes;

    [Header("Detección")]
    [Tooltip("Radio del SphereCast. Un poco más grueso que un rayo fino evita que se 'cuelen' bordes de pared sin ocultar.")]
    [SerializeField] private float radioDeteccion = 0.15f;

    [Tooltip("Margen que se resta a la distancia al foco, para no ocultar accidentalmente algo justo detrás/alrededor de él.")]
    [SerializeField] private float margenAntesDelJugador = 0.3f;

    private readonly HashSet<Renderer> paredesOcultas = new HashSet<Renderer>();
    private readonly HashSet<Renderer> paredesEsteFrame = new HashSet<Renderer>();

    void Awake()
    {
        if (camaraVertices == null) camaraVertices = GetComponent<CamaraVertices>();
    }

    void LateUpdate()
    {
        Vector3 foco;
        if (camaraVertices != null) foco = camaraVertices.PuntoDeFoco;
        else if (jugador != null) foco = jugador.position;
        else return;

        paredesEsteFrame.Clear();

        Vector3 origen = transform.position;
        Vector3 hacia = foco - origen;
        float distancia = Mathf.Max(0f, hacia.magnitude - margenAntesDelJugador);

        if (distancia > 0f)
        {
            RaycastHit[] impactos = Physics.SphereCastAll(
                origen, radioDeteccion, hacia.normalized, distancia, capaParedes);

            foreach (var impacto in impactos)
            {
                Renderer rend = impacto.collider.GetComponentInParent<Renderer>();
                if (rend == null) continue;

                paredesEsteFrame.Add(rend);

                if (paredesOcultas.Add(rend))
                {
                    rend.enabled = false;
                }
            }
        }

        // Vuelve a mostrar las que ya no están en el camino este frame.
        paredesOcultas.RemoveWhere(rend =>
        {
            if (rend == null) return true; // limpia referencias destruidas
            if (!paredesEsteFrame.Contains(rend))
            {
                rend.enabled = true;
                return true;
            }
            return false;
        });
    }
}