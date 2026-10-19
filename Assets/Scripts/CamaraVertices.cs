using System.Collections;
using UnityEngine;

/// <summary>
/// Cámara con 4 vistas (teclas 1-4), ahora ELEVADA: mira al objetivo desde
/// arriba en diagonal, en vez de a su misma altura. Así se ve mejor el
/// laberinto. Mismo nombre de clase para no romper tus referencias.
///
/// Los ejes de movimiento (Adelante / Derecha) siguen siendo HORIZONTALES,
/// así que MovimientoJugador y SensorPisoRotacion funcionan igual.
/// </summary>
public class CamaraVertices : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform objetivo;

    [Header("Posición de la cámara")]
    [Tooltip("Distancia horizontal desde el objetivo.")]
    [SerializeField] private float distancia = 14f;

    [Tooltip("Altura de la cámara sobre el objetivo. Mayor = más vista aérea.")]
    [SerializeField] private float altura = 12f;

    [Header("Transición")]
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

    private bool moviendo = false;
    private int vistaActual;

    private int IndiceVista =>
        Mathf.Clamp(Application.isPlaying ? vistaActual : vistaInicial - 1, 0, direcciones.Length - 1);

    void Awake()
    {
        vistaActual = Mathf.Clamp(vistaInicial, 1, direcciones.Length) - 1;
    }

    void Start()
    {
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
        if (moviendo) return;
        ColocarEnVista(direcciones[IndiceVista]);
    }

    /// <summary>"Adelante" HORIZONTAL (hacia adentro de la pantalla). Nunca tiene componente Y.</summary>
    public Vector3 Adelante => -direcciones[IndiceVista];

    public Vector3 Derecha => Vector3.Cross(Vector3.up, Adelante);

    private Vector3 ObjetivoPos() => objetivo != null ? objetivo.position : Vector3.zero;

    private Vector3 PosicionPara(Vector3 dirHorizontal) =>
        ObjetivoPos() + dirHorizontal * distancia + Vector3.up * altura;

    private void ColocarEnVista(Vector3 dirHorizontal)
    {
        Vector3 pos = PosicionPara(dirHorizontal);
        transform.position = pos;
        transform.rotation = Quaternion.LookRotation((ObjetivoPos() - pos).normalized);
    }

    private void IrAVista(int indice)
    {
        if (indice == vistaActual) return;
        vistaActual = indice;
        StartCoroutine(TransicionarA(indice));
    }

    private IEnumerator TransicionarA(int indice)
    {
        moviendo = true;

        // Dirección horizontal actual (sin la altura)
        Vector3 dirInicial = transform.position - ObjetivoPos();
        dirInicial.y = 0f;
        dirInicial.Normalize();

        Quaternion rotInicial = transform.rotation;
        Vector3 dirFinal = direcciones[indice];
        Quaternion rotFinal = Quaternion.LookRotation(-(dirFinal * distancia + Vector3.up * altura));

        float tiempo = 0f;
        while (tiempo < duracionTransicion)
        {
            tiempo += Time.deltaTime;
            float t = curvaTransicion.Evaluate(Mathf.Clamp01(tiempo / duracionTransicion));

            Vector3 dirActual = Vector3.Slerp(dirInicial, dirFinal, t);
            transform.position = PosicionPara(dirActual);
            transform.rotation = Quaternion.Slerp(rotInicial, rotFinal, t);
            yield return null;
        }

        ColocarEnVista(dirFinal);
        moviendo = false;
    }
}
