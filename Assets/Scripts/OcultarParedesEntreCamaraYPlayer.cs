
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(1001)]
public class OcultarParedesEntreCamaraYPlayer : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CamaraVertices camaraVertices;
    [SerializeField] private Transform jugador;
    [SerializeField] private MovimientoJugador movimientoJugador;
    [SerializeField] private LayerMask capaParedes;

    [Header("Deteccion")]
    [SerializeField] private float radioDeteccion = 0.15f;
    [SerializeField] private float margenAntesDelJugador = 0.3f;

    [Header("Pared al retroceder")]
    [SerializeField] private Material materialTransparente;
    [SerializeField] private float distanciaDeteccionAtras = 4f;
    [SerializeField] private float radioDeteccionAtras = 0.3f;

    private readonly HashSet<Renderer> paredesOcultas =
        new HashSet<Renderer>();

    private readonly HashSet<Renderer> paredesEsteFrame =
        new HashSet<Renderer>();

    private readonly HashSet<Renderer> paredesTransparentes =
        new HashSet<Renderer>();

    private readonly HashSet<Renderer> transparentesEsteFrame =
        new HashSet<Renderer>();

    private readonly List<Renderer> restaurar =
        new List<Renderer>();

    private void Awake()
    {
        if (camaraVertices == null)
            camaraVertices = GetComponent<CamaraVertices>();

        if (movimientoJugador == null && jugador != null)
            movimientoJugador =
                jugador.GetComponent<MovimientoJugador>();
    }

    private void LateUpdate()
    {
        transparentesEsteFrame.Clear();

        DetectarParedDetras();

        restaurar.Clear();

        foreach (Renderer rend in paredesTransparentes)
        {
            if (!transparentesEsteFrame.Contains(rend))
                restaurar.Add(rend);
        }

        foreach (Renderer rend in restaurar)
        {
            if (movimientoJugador != null)
                movimientoJugador.DesactivarTransparencia(rend);

            paredesTransparentes.Remove(rend);
        }

        ActualizarOcultamiento();
    }

    private void DetectarParedDetras()
    {
        if (jugador == null ||
            camaraVertices == null ||
            movimientoJugador == null ||
            materialTransparente == null)
            return;

        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
            return;

        if (!Input.GetKey(KeyCode.S))
            return;

        Vector3 direccionAtras = -camaraVertices.Adelante;

        RaycastHit[] impactos = Physics.SphereCastAll(
            jugador.position,
            radioDeteccionAtras,
            direccionAtras,
            distanciaDeteccionAtras,
            capaParedes,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit impacto in impactos)
        {
            Renderer rend =
                impacto.collider.GetComponentInParent<Renderer>();

            if (rend == null)
                continue;

            transparentesEsteFrame.Add(rend);

            if (!paredesTransparentes.Contains(rend))
            {
                movimientoJugador.ActivarTransparencia(
                    rend,
                    materialTransparente
                );

                paredesTransparentes.Add(rend);
            }

            rend.enabled = true;
            paredesOcultas.Remove(rend);
        }
    }

    private void ActualizarOcultamiento()
    {
        Vector3 foco;

        if (camaraVertices != null)
            foco = camaraVertices.PuntoDeFoco;
        else if (jugador != null)
            foco = jugador.position;
        else
            return;

        paredesEsteFrame.Clear();

        Vector3 origen = transform.position;
        Vector3 hacia = foco - origen;

        float distancia = Mathf.Max(
            0f,
            hacia.magnitude - margenAntesDelJugador
        );

        if (distancia > 0f)
        {
            RaycastHit[] impactos = Physics.SphereCastAll(
                origen,
                radioDeteccion,
                hacia.normalized,
                distancia,
                capaParedes,
                QueryTriggerInteraction.Ignore
            );

            foreach (RaycastHit impacto in impactos)
            {
                Renderer rend =
                    impacto.collider.GetComponentInParent<Renderer>();

                if (rend == null)
                    continue;

                if (paredesTransparentes.Contains(rend))
                    continue;

                paredesEsteFrame.Add(rend);

                if (paredesOcultas.Add(rend))
                    rend.enabled = false;
            }
        }

        paredesOcultas.RemoveWhere(rend =>
        {
            if (rend == null)
                return true;

            if (!paredesEsteFrame.Contains(rend))
            {
                rend.enabled = true;
                return true;
            }

            return false;
        });
    }

    private void OnDisable()
    {
        restaurar.Clear();
        restaurar.AddRange(paredesTransparentes);

        foreach (Renderer rend in restaurar)
        {
            if (movimientoJugador != null)
                movimientoJugador.DesactivarTransparencia(rend);
        }

        foreach (Renderer rend in paredesOcultas)
        {
            if (rend != null)
                rend.enabled = true;
        }

        paredesTransparentes.Clear();
        transparentesEsteFrame.Clear();
        paredesOcultas.Clear();
        paredesEsteFrame.Clear();
    }
}
