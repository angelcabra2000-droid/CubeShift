
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SensorPisoRotacion))]
public class MovimientoJugador : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RotadorMundo rotadorMundo;
    private SensorPisoRotacion sensor;
    private AnimacionJugador anim;
    private EnergiaJugador energia;

    [Header("Movimiento")]
    [SerializeField] private float velocidad = 3f;
    [SerializeField] private float distanciaChoque = 0.5f;
    [SerializeField] private float radioChoque = 0.3f;
    [SerializeField] private float margenSobreSuelo = 0.05f;
    [SerializeField] private LayerMask capaParedes;

    [Header("Giro con A/D")]
    [SerializeField] private bool invertirGiroMirandoAtras = true;

    [Header("Depuración")]
    [SerializeField] private bool registrarBloqueos = true;

    [Header("Resaltado de paredes candidatas")]
    [SerializeField] private Material materialResaltado;

    private readonly Dictionary<Renderer, Material> materialesOriginales =
        new Dictionary<Renderer, Material>();

    private readonly HashSet<Renderer> resaltadasEsteFrame =
        new HashSet<Renderer>();

    private readonly List<Renderer> bufferRestaurar =
        new List<Renderer>();

    private string ultimoBloqueo = "";

    private void Awake()
    {
        sensor = GetComponent<SensorPisoRotacion>();
        anim = GetComponent<AnimacionJugador>();
        energia = GetComponent<EnergiaJugador>();
    }

    private void Update()
    {
        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
        {
            ActualizarAnimacion(0f, false);
            return;
        }

        if (rotadorMundo != null && rotadorMundo.EstaRotando)
        {
            QuitarTodosLosResaltados();
            ActualizarAnimacion(0f, false);
            return;
        }

        if (rotadorMundo != null)
        {
            float signo =
                (invertirGiroMirandoAtras &&
                 anim != null &&
                 anim.MiraAtras) ? -1f : 1f;

            if (Input.GetKeyDown(KeyCode.A))
            {
                ActualizarAnimacion(0f, false);
                rotadorMundo.Rotar(Vector3.up, 90f * signo);
                return;
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                ActualizarAnimacion(0f, false);
                rotadorMundo.Rotar(Vector3.up, -90f * signo);
                return;
            }
        }

        bool bloqueado = MoverJugador();

        if (bloqueado)
            ActualizarResaltados();
        else
            QuitarTodosLosResaltados();
    }

    private bool MoverJugador()
    {
        float entrada = 0f;

        if (Input.GetKey(KeyCode.W))
            entrada = 1f;
        else if (Input.GetKey(KeyCode.S))
            entrada = -1f;

        Vector3 adelante = sensor.Adelante;

        if (Mathf.Approximately(entrada, 0f))
        {
            ActualizarAnimacion(0f, false);

            return HayParedEnDireccion(adelante) ||
                   HayParedEnDireccion(-adelante);
        }

        Vector3 direccion = adelante * entrada;

        if (HayParedEnDireccion(direccion, true))
        {
            ActualizarAnimacion(0f, false);
            return true;
        }

        ActualizarAnimacion(1f, entrada < 0f);

        if (anim != null && anim.DebeEsperarGiro)
            return false;

        // Movimiento
        Vector3 posicionAnterior = transform.position;

        transform.position +=
            direccion * velocidad * Time.deltaTime;

        // Consumo de energia
        if (energia != null &&
            (transform.position - posicionAnterior).sqrMagnitude > 0.0000001f)
        {
            energia.ConsumirEnergia(1f);
        }

        return false;
    }

    private void ActualizarAnimacion(float velocidadAnim, bool retroceso)
    {
        if (anim != null)
            anim.SetMovimiento(velocidadAnim, retroceso);
    }

    private bool HayParedEnDireccion(
        Vector3 direccion,
        bool registrar = false)
    {
        Vector3 origen = transform.position;

        float sueloY = float.NaN;
        float centroSobreSuelo = float.NaN;

        if (Physics.Raycast(
            origen,
            Vector3.down,
            out RaycastHit suelo,
            50f,
            capaParedes,
            QueryTriggerInteraction.Ignore))
        {
            sueloY = suelo.point.y;
            centroSobreSuelo = origen.y - sueloY;

            float minimo =
                sueloY + radioChoque + margenSobreSuelo;

            if (origen.y < minimo)
                origen.y = minimo;
        }

        bool hay = Physics.SphereCast(
            origen,
            radioChoque,
            direccion.normalized,
            out RaycastHit hit,
            distanciaChoque,
            capaParedes,
            QueryTriggerInteraction.Ignore);

        if (hay && registrar && registrarBloqueos)
        {
            Renderer rend =
                hit.collider.GetComponentInParent<Renderer>();

            string estado = rend == null
                ? "sin renderer"
                : (rend.enabled ? "visible" : "OCULTA");

            string msg =
                "Bloquea: " + hit.collider.name +
                " | normal " + hit.normal.ToString("F2") +
                " | contacto sobre el suelo " +
                (hit.point.y - sueloY).ToString("F2") +
                " | centro sobre el suelo " +
                centroSobreSuelo.ToString("F2") +
                " | " + estado;

            if (msg != ultimoBloqueo)
            {
                Debug.Log("[MovimientoJugador] " + msg, hit.collider);
                ultimoBloqueo = msg;
            }
        }

        return hay;
    }

    private void ActualizarResaltados()
    {
        resaltadasEsteFrame.Clear();

        if (sensor.IntentarAdelante(out RaycastHit hAdelante))
            Resaltar(hAdelante);

        if (sensor.IntentarAtras(out RaycastHit hAtras))
            Resaltar(hAtras);

        if (sensor.IntentarIzquierda(out RaycastHit hIzq))
            Resaltar(hIzq);

        if (sensor.IntentarDerecha(out RaycastHit hDer))
            Resaltar(hDer);

        bufferRestaurar.Clear();

        foreach (var rend in materialesOriginales.Keys)
        {
            if (!resaltadasEsteFrame.Contains(rend))
                bufferRestaurar.Add(rend);
        }

        foreach (var rend in bufferRestaurar)
            Restaurar(rend);
    }

    private void Resaltar(RaycastHit hit)
    {
        if (materialResaltado == null)
            return;

        Renderer rend = hit.collider != null
            ? hit.collider.GetComponentInParent<Renderer>()
            : null;

        if (rend == null)
            return;

        resaltadasEsteFrame.Add(rend);

        if (!materialesOriginales.ContainsKey(rend))
        {
            materialesOriginales[rend] = rend.sharedMaterial;
            rend.material = materialResaltado;
        }
    }

    private void Restaurar(Renderer rend)
    {
        if (rend != null &&
            materialesOriginales.TryGetValue(
                rend, out Material original))
        {
            rend.material = original;
        }

        materialesOriginales.Remove(rend);
    }

    private void QuitarTodosLosResaltados()
    {
        bufferRestaurar.Clear();
        bufferRestaurar.AddRange(materialesOriginales.Keys);

        foreach (var rend in bufferRestaurar)
            Restaurar(rend);
    }
}
