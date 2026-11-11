using UnityEngine;

/// <summary>
/// Cámara con 4 vistas (teclas 1-4), elevada, que mira al objetivo desde arriba en diagonal.
/// Mientras se MANTIENE la tecla ESPACIO, el foco pasa de forma suave del jugador a la pelota
/// (con su propia distancia y altura) y la sigue. Al soltar, vuelve al jugador.
///
/// Todo se hace en LateUpdate y con un orden de ejecución ALTO, para que corra DESPUÉS de
/// BallSimulator (que mueve la pelota en su LateUpdate). Así la cámara nunca va un frame
/// atrasada y no tiembla.
///
/// Los ejes Adelante / Derecha siguen siendo HORIZONTALES y dependen solo de la vista (1-4).
/// </summary>
[DefaultExecutionOrder(1000)]
public class CamaraVertices : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform objetivo;
    [Tooltip("La pelota que se sigue al mantener ESPACIO.")]
    [SerializeField] private Transform pelota;

    [Header("Posición de la cámara (jugador)")]
    [Tooltip("Distancia horizontal desde el objetivo.")]
    [SerializeField] private float distancia = 14f;

    [Tooltip("Altura de la cámara sobre el objetivo. Mayor = más vista aérea.")]
    [SerializeField] private float altura = 12f;

    [Header("Posición de la cámara (pelota)")]
    [SerializeField] private float distanciaPelota = 14f;
    [SerializeField] private float alturaPelota = 12f;

    [Header("Seguimiento de la pelota")]
    [SerializeField] private KeyCode teclaPelota = KeyCode.Space;
    [Tooltip("Segundos que tarda en pasar del jugador a la pelota (y viceversa).")]
    [SerializeField] private float duracionCambioFoco = 0.35f;
    [Tooltip("Suavizado del seguimiento de la pelota. 0 = pegada exacta a la pelota (más brusco). " +
             "Valores 6-15 amortiguan los rebotes y golpes; menor = más suave pero con más retraso.")]
    [SerializeField] private float suavizadoPelota = 10f;

    [Header("Transición entre vistas")]
    [SerializeField] private float duracionTransicion = 0.5f;
    [SerializeField] private AnimationCurve curvaTransicion = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Vista inicial")]
    [SerializeField] private int vistaInicial = 1;

    private readonly Vector3[] direcciones = new Vector3[4]
    {
        new Vector3(0, 0, -1),
        new Vector3(1, 0, 0),
        new Vector3(0, 0, 1),
        new Vector3(-1, 0, 0),
    };

    private int vistaActual;

    // Transición entre vistas (se avanza en LateUpdate)
    private bool moviendo = false;
    private float tiempoTransicion;
    private Vector3 dirInicial;
    private Vector3 dirFinal;

    // 0 = enfocando al jugador, 1 = enfocando a la pelota (valor crudo, sin suavizar)
    private float mezclaCruda = 0f;
    // Posición suavizada de la pelota (el foco usa esta)
    private Vector3 focoPelota;

    private int IndiceVista =>
        Mathf.Clamp(Application.isPlaying ? vistaActual : vistaInicial - 1, 0, direcciones.Length - 1);

    private float Mezcla => Mathf.SmoothStep(0f, 1f, mezclaCruda);

    /// <summary>Transform al que la cámara apunta "principalmente" ahora mismo.</summary>
    public Transform ObjetivoActual => (pelota != null && mezclaCruda > 0.5f) ? pelota : objetivo;

    /// <summary>
    /// Punto exacto al que mira la cámara (mezcla entre jugador y pelota).
    /// Lo usa OcultarParedesEntreCamaraYPlayer para saber hasta dónde ocultar paredes.
    /// </summary>
    public Vector3 PuntoDeFoco => ObjetivoPos();

    void Awake()
    {
        vistaActual = Mathf.Clamp(vistaInicial, 1, direcciones.Length) - 1;
    }

    void Start()
    {
        if (pelota != null) focoPelota = pelota.position;
        ColocarEnVista(direcciones[vistaActual]);
    }

    void Update()
    {
        if (moviendo) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) IrAVista(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) IrAVista(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) IrAVista(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) IrAVista(3);
    }

    void LateUpdate()
    {
        ActualizarMezcla();
        ActualizarFocoPelota();

        if (moviendo)
        {
            tiempoTransicion += Time.deltaTime;
            float t = curvaTransicion.Evaluate(Mathf.Clamp01(tiempoTransicion / duracionTransicion));

            Vector3 dirActual = Vector3.Slerp(dirInicial, dirFinal, t);
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

    private void ActualizarMezcla()
    {
        bool quierePelota = pelota != null && Input.GetKey(teclaPelota);
        float objetivoMezcla = quierePelota ? 1f : 0f;
        float paso = duracionCambioFoco > 0.0001f ? Time.deltaTime / duracionCambioFoco : 1f;
        mezclaCruda = Mathf.MoveTowards(mezclaCruda, objetivoMezcla, paso);
    }

    private void ActualizarFocoPelota()
    {
        if (pelota == null) return;

        // Mientras no se está usando, la posición suavizada queda pegada a la pelota
        // (así al pulsar espacio no hay un "salto" desde una posición vieja).
        if (mezclaCruda <= 0f || suavizadoPelota <= 0f)
        {
            focoPelota = pelota.position;
            return;
        }

        float k = 1f - Mathf.Exp(-suavizadoPelota * Time.deltaTime);
        focoPelota = Vector3.Lerp(focoPelota, pelota.position, k);
    }

    /// <summary>"Adelante" HORIZONTAL (hacia adentro de la pantalla). Nunca tiene componente Y.</summary>
    public Vector3 Adelante => -direcciones[IndiceVista];

    public Vector3 Derecha => Vector3.Cross(Vector3.up, Adelante);

    private Vector3 PosicionJugador() => objetivo != null ? objetivo.position : Vector3.zero;

    private Vector3 ObjetivoPos()
    {
        Vector3 jugador = PosicionJugador();
        if (pelota == null) return jugador;
        return Vector3.Lerp(jugador, focoPelota, Mezcla);
    }

    private float DistanciaActual() => Mathf.Lerp(distancia, distanciaPelota, Mezcla);
    private float AlturaActual() => Mathf.Lerp(altura, alturaPelota, Mezcla);

    private Vector3 PosicionPara(Vector3 dirHorizontal) =>
        ObjetivoPos() + dirHorizontal * DistanciaActual() + Vector3.up * AlturaActual();

    private void ColocarEnVista(Vector3 dirHorizontal)
    {
        Vector3 pos = PosicionPara(dirHorizontal);
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation((ObjetivoPos() - pos).normalized);
    }

    private void IrAVista(int indice)
    {
        if (indice == vistaActual) return;

        // Dirección horizontal actual (sin la altura)
        dirInicial = transform.position - ObjetivoPos();
        dirInicial.y = 0f;
        dirInicial.Normalize();

        dirFinal = direcciones[indice];
        vistaActual = indice;
        tiempoTransicion = 0f;
        moviendo = true;
    }
}