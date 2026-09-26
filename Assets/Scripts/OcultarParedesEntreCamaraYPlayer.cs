using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Oculta (o hace transparentes) las paredes que quedan entre la cámara y
/// el Player, para que siempre puedas verlo aunque el cubo esté cerrado.
///
/// Cada frame lanza un SphereCast desde la cámara hacia el Player. Todo lo
/// que golpee en el camino (que esté en el Layer "Paredes") se oculta;
/// en cuanto deja de estar en el camino, se vuelve a mostrar.
///
/// Colocar este script sobre la Main Camera (o sobre el objeto que tenga
/// el script CamaraVertices).
///
/// --- CONFIGURACIÓN NECESARIA ---
/// 1. Crea un Layer llamado "Paredes" (Tags and Layers) y asígnalo a todas
///    las paredes/geometría del laberinto (NO al Player).
/// 2. Asigna ese Layer en el campo "Capa Paredes" de este script.
/// 3. Asigna el Transform del Player en el campo "Jugador".
/// </summary>
public class OcultarParedesEntreCamaraYPlayer : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Player que siempre quieres poder ver.")]
    [SerializeField] private Transform jugador;

    [Tooltip("Layer que usan las paredes/geometría que se pueden ocultar. El Player NO debe estar en este layer.")]
    [SerializeField] private LayerMask capaParedes;

    [Header("Detección")]
    [Tooltip("Radio del SphereCast. Un poco más grueso que un rayo fino evita que se 'cuelen' bordes de pared sin ocultar.")]
    [SerializeField] private float radioDeteccion = 0.15f;

    [Tooltip("Margen que se resta a la distancia al jugador, para no ocultar accidentalmente algo justo detrás/alrededor de él.")]
    [SerializeField] private float margenAntesDelJugador = 0.3f;

    // Paredes ocultas actualmente, para poder volver a mostrarlas cuando ya no bloqueen la vista.
    private readonly HashSet<Renderer> paredesOcultas = new HashSet<Renderer>();
    private readonly HashSet<Renderer> paredesEsteFrame = new HashSet<Renderer>();

    void LateUpdate()
    {
        if (jugador == null) return;

        paredesEsteFrame.Clear();

        Vector3 origen = transform.position;
        Vector3 hacia = jugador.position - origen;
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
