
using UnityEngine;

[DefaultExecutionOrder(1000)]
public class CamaraVertices : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform objetivo;
    [SerializeField] private Transform pelota;

    [Header("Posicion de la camara (jugador)")]
    [SerializeField] private float distancia = 14f;
    [SerializeField] private float altura = 12f;

    [Header("Posicion de la camara (pelota)")]
    [SerializeField] private float distanciaPelota = 14f;
    [SerializeField] private float alturaPelota = 12f;

    [Header("Seguimiento de la pelota")]
    [SerializeField] private KeyCode teclaPelota = KeyCode.Space;
    [SerializeField] private float duracionCambioFoco = 0.35f;
    [SerializeField] private float suavizadoPelota = 10f;

    [Header("Transicion entre vistas")]
    [SerializeField] private float duracionTransicion = 0.5f;
    [SerializeField]
    private AnimationCurve curvaTransicion =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Vista inicial")]
    [SerializeField] private int vistaInicial = 1;

    [Header("Camara al retroceder")]
    [SerializeField] private LayerMask capaParedes;
    [SerializeField] private float distanciaDeteccionAtras = 4f;
    [SerializeField] private float alturaExtraRetroceso = 5f;
    [SerializeField] private float distanciaExtraRetroceso = 3f;
    [SerializeField] private float suavizadoRetroceso = 5f;

    private readonly Vector3[] direcciones = new Vector3[4]
    {
        new Vector3(0, 0, -1),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(-1, 0, 0),
    };

    private int vistaActual;

    private bool moviendo = false;
    private float tiempoTransicion;
    private Vector3 dirInicial;
    private Vector3 dirFinal;

    private float mezclaCruda = 0f;
    private Vector3 focoPelota;

    private float ajusteRetroceso = 0f;

    private int IndiceVista =>
        Mathf.Clamp(
            Application.isPlaying ? vistaActual : vistaInicial - 1,
            0,
            direcciones.Length - 1
        );

    private float Mezcla =>
        Mathf.SmoothStep(0f, 1f, mezclaCruda);

    public Transform ObjetivoActual =>
        (pelota != null && mezclaCruda > 0.5f)
            ? pelota
            : objetivo;

    public Vector3 PuntoDeFoco => ObjetivoPos();

    void Awake()
    {
        vistaActual =
            Mathf.Clamp(vistaInicial, 1, direcciones.Length) - 1;
    }

    void Start()
    {
        if (pelota != null)
            focoPelota = pelota.position;

        ColocarEnVista(direcciones[vistaActual]);
    }

    void LateUpdate()
    {
        ActualizarMezcla();
        ActualizarFocoPelota();
        ActualizarRetroceso();

        if (moviendo)
        {
            tiempoTransicion += Time.deltaTime;

            float t = curvaTransicion.Evaluate(
                Mathf.Clamp01(
                    tiempoTransicion /
                    Mathf.Max(0.0001f, duracionTransicion)
                )
            );

            Vector3 dirActual =
                Vector3.Slerp(dirInicial, dirFinal, t);

            ColocarEnVista(dirActual);

            if (tiempoTransicion >= duracionTransicion)
            {
                moviendo = false;
                ColocarEnVista(dirFinal);
            }
        }
        else
        {
            ColocarEnVista(direcciones[IndiceVista]);
        }
    }

    private void ActualizarRetroceso()
    {
        bool retrocediendo = false;

        if (ControlPartida.Instancia != null &&
            ControlPartida.Instancia.EstaJugando &&
            objetivo != null &&
            mezclaCruda <= 0f &&
            Input.GetKey(KeyCode.S))
        {
            Vector3 direccionAtras = -Adelante;

            retrocediendo = Physics.SphereCast(
                objetivo.position,
                0.3f,
                direccionAtras,
                out RaycastHit hit,
                distanciaDeteccionAtras,
                capaParedes,
                QueryTriggerInteraction.Ignore
            );
        }

        float valorObjetivo = retrocediendo ? 1f : 0f;

        float suavizado =
            1f - Mathf.Exp(
                -Mathf.Max(0f, suavizadoRetroceso) *
                Time.deltaTime
            );

        ajusteRetroceso = Mathf.Lerp(
            ajusteRetroceso,
            valorObjetivo,
            suavizado
        );
    }

    private void ActualizarMezcla()
    {
        bool quierePelota =
            pelota != null && Input.GetKey(teclaPelota);

        float objetivoMezcla = quierePelota ? 1f : 0f;

        float paso = duracionCambioFoco > 0.0001f
            ? Time.deltaTime / duracionCambioFoco
            : 1f;

        mezclaCruda = Mathf.MoveTowards(
            mezclaCruda,
            objetivoMezcla,
            paso
        );
    }

    private void ActualizarFocoPelota()
    {
        if (pelota == null) return;

        if (mezclaCruda <= 0f || suavizadoPelota <= 0f)
        {
            focoPelota = pelota.position;
            return;
        }

        float k =
            1f - Mathf.Exp(-suavizadoPelota * Time.deltaTime);

        focoPelota = Vector3.Lerp(
            focoPelota,
            pelota.position,
            k
        );
    }

    public Vector3 Adelante => -direcciones[IndiceVista];

    public Vector3 Derecha =>
        Vector3.Cross(Vector3.up, Adelante);

    private Vector3 PosicionJugador() =>
        objetivo != null ? objetivo.position : Vector3.zero;

    private Vector3 ObjetivoPos()
    {
        Vector3 jugador = PosicionJugador();

        if (pelota == null) return jugador;

        return Vector3.Lerp(
            jugador,
            focoPelota,
            Mezcla
        );
    }

    private float DistanciaActual()
    {
        float baseDistancia =
            Mathf.Lerp(distancia, distanciaPelota, Mezcla);

        return baseDistancia +
            distanciaExtraRetroceso *
            ajusteRetroceso *
            (1f - Mezcla);
    }

    private float AlturaActual()
    {
        float baseAltura =
            Mathf.Lerp(altura, alturaPelota, Mezcla);

        return baseAltura +
            alturaExtraRetroceso *
            ajusteRetroceso *
            (1f - Mezcla);
    }

    private Vector3 PosicionPara(Vector3 dirHorizontal) =>
        ObjetivoPos() +
        dirHorizontal * DistanciaActual() +
        Vector3.up * AlturaActual();

    private void ColocarEnVista(Vector3 dirHorizontal)
    {
        Vector3 pos = PosicionPara(dirHorizontal);

        transform.position = pos;

        Vector3 direccion = ObjetivoPos() - pos;

        if (direccion.sqrMagnitude > 0.0001f)
        {
            transform.rotation =
                Quaternion.LookRotation(direccion.normalized);
        }
    }

    private void IrAVista(int indice)
    {
        if (indice == vistaActual) return;

        dirInicial = transform.position - ObjetivoPos();
        dirInicial.y = 0f;
        dirInicial.Normalize();

        dirFinal = direcciones[indice];

        vistaActual = indice;
        tiempoTransicion = 0f;
        moviendo = true;
    }
}
