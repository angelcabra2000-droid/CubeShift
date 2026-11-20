
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

    [Header("Depuracion")]
    [SerializeField] private bool registrarBloqueos = true;

    [Header("Resaltado de paredes")]
    [SerializeField] private Material materialResaltado;
    [SerializeField] private Material materialResaltadoDificil;

    private Material materialActual;

    private class EstadoPared
    {
        public Material[] originales;
        public bool resaltada;
        public bool transparente;
        public Material materialTransparente;
    }

    private readonly Dictionary<Renderer, EstadoPared> estados =
        new Dictionary<Renderer, EstadoPared>();

    private readonly HashSet<Renderer> resaltadasEsteFrame =
        new HashSet<Renderer>();

    private readonly List<Renderer> buffer =
        new List<Renderer>();

    private string ultimoBloqueo = "";

    private void Awake()
    {
        sensor = GetComponent<SensorPisoRotacion>();
        anim = GetComponent<AnimacionJugador>();
        energia = GetComponent<EnergiaJugador>();

        materialActual = materialResaltado;
    }

    public void ConfigurarResaltado(bool dificil)
    {
        materialActual =
            dificil && materialResaltadoDificil != null
                ? materialResaltadoDificil
                : materialResaltado;

        QuitarTodosLosResaltados();
    }

    private EstadoPared ObtenerEstado(Renderer rend)
    {
        if (!estados.TryGetValue(rend, out EstadoPared estado))
        {
            estado = new EstadoPared();
            estado.originales =
                (Material[])rend.sharedMaterials.Clone();

            estados.Add(rend, estado);
        }

        return estado;
    }

    private void AplicarMaterial(Renderer rend, EstadoPared estado)
    {
        if (rend == null)
            return;

        Material material = null;

        if (estado.transparente)
            material = estado.materialTransparente;
        else if (estado.resaltada)
            material = materialActual;

        if (material == null)
        {
            rend.sharedMaterials = estado.originales;
            return;
        }

        Material[] nuevos =
            new Material[estado.originales.Length];

        for (int i = 0; i < nuevos.Length; i++)
            nuevos[i] = material;

        rend.sharedMaterials = nuevos;
    }

    public void ActivarTransparencia(
        Renderer rend,
        Material materialTransparente)
    {
        if (rend == null || materialTransparente == null)
            return;

        EstadoPared estado = ObtenerEstado(rend);

        estado.transparente = true;
        estado.materialTransparente = materialTransparente;

        AplicarMaterial(rend, estado);
    }

    public void DesactivarTransparencia(Renderer rend)
    {
        if (rend == null)
            return;

        if (!estados.TryGetValue(rend, out EstadoPared estado))
            return;

        estado.transparente = false;
        estado.materialTransparente = null;

        AplicarMaterial(rend, estado);
        LimpiarEstado(rend, estado);
    }

    private void LimpiarEstado(Renderer rend, EstadoPared estado)
    {
        if (!estado.transparente && !estado.resaltada)
            estados.Remove(rend);
    }

    private void Update()
    {
        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
        {
            QuitarTodosLosResaltados();
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

        Vector3 posicionAnterior = transform.position;

        transform.position +=
            direccion * velocidad * Time.deltaTime;

        if (energia != null &&
            (transform.position - posicionAnterior).sqrMagnitude >
            0.0000001f)
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
                Debug.Log(
                    "[MovimientoJugador] " + msg,
                    hit.collider
                );

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

        buffer.Clear();

        foreach (var par in estados)
        {
            if (par.Value.resaltada &&
                !resaltadasEsteFrame.Contains(par.Key))
            {
                buffer.Add(par.Key);
            }
        }

        foreach (Renderer rend in buffer)
            QuitarResaltado(rend);
    }

    private void Resaltar(RaycastHit hit)
    {
        if (materialActual == null || hit.collider == null)
            return;

        Renderer rend =
            hit.collider.GetComponentInParent<Renderer>();

        if (rend == null)
            return;

        resaltadasEsteFrame.Add(rend);

        EstadoPared estado = ObtenerEstado(rend);

        if (estado.resaltada)
            return;

        estado.resaltada = true;
        AplicarMaterial(rend, estado);
    }

    private void QuitarResaltado(Renderer rend)
    {
        if (rend == null)
            return;

        if (!estados.TryGetValue(rend, out EstadoPared estado))
            return;

        estado.resaltada = false;

        AplicarMaterial(rend, estado);
        LimpiarEstado(rend, estado);
    }

    private void QuitarTodosLosResaltados()
    {
        buffer.Clear();

        foreach (var par in estados)
        {
            if (par.Value.resaltada)
                buffer.Add(par.Key);
        }

        foreach (Renderer rend in buffer)
            QuitarResaltado(rend);
    }

    private void OnDisable()
    {
        foreach (var par in estados)
        {
            if (par.Key != null)
                par.Key.sharedMaterials = par.Value.originales;
        }

        estados.Clear();
        resaltadasEsteFrame.Clear();
        buffer.Clear();
    }
}
