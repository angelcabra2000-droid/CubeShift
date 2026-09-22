using System.Collections;
using UnityEngine;

/// <summary>
/// FASE — Pruebas de cámara: 8 posiciones fijas en los vértices de un cubo
/// imaginario alrededor del "Mundo", todas mirando hacia su centro.
/// Presiona las teclas 1-8 para saltar entre vistas, con una transición
/// suave (igual que la rotación del Mundo).
///
/// Colocar este script sobre tu Main Camera.
///
/// La cámara viaja ORBITANDO por fuera de una esfera alrededor del objetivo
/// (interpolando la dirección con Slerp, no la posición con Lerp en línea
/// recta), así que nunca se acerca al cubo durante la transición, sin
/// importar qué dos vistas elijas.
///
/// Numeración de vértices (en orden de "camino por las aristas": cada
/// vecino solo cambia un eje respecto al anterior):
///   1: (-x,-y,-z)   2: (-x,-y,+z)   3: (-x,+y,+z)   4: (-x,+y,-z)
///   5: (+x,+y,-z)   6: (+x,+y,+z)   7: (+x,-y,+z)   8: (+x,-y,-z)
/// </summary>
public class CamaraVertices : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El punto que la cámara siempre mira: normalmente el centro del Mundo.")]
    [SerializeField] private Transform objetivo;

    [Header("Geometría de las 8 vistas")]
    [Tooltip("Distancia desde el objetivo hasta cada vértice.")]
    [SerializeField] private float distancia = 8f;

    [Header("Transición")]
    [Tooltip("Duración en segundos del movimiento entre vistas.")]
    [SerializeField] private float duracionTransicion = 0.5f;

    [Tooltip("Curva de aceleración de la transición.")]
    [SerializeField] private AnimationCurve curvaTransicion = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Vista inicial")]
    [Tooltip("Vértice (1-8) donde arranca la cámara.")]
    [SerializeField] private int vistaInicial = 8;

    private Vector3[] direcciones;
    private bool moviendo = false;
    private int vistaActual;

    void Start()
    {
        // Las 8 combinaciones de signo (-1,+1) en X, Y, Z = los 8 vértices
        // de un cubo centrado en el objetivo.
        // Orden tipo "Gray code": cada vértice solo cambia UN eje respecto
        // al anterior (y el último respecto al primero), formando un camino
        // continuo por las aristas del cubo.
        direcciones = new Vector3[8]
        {
            new Vector3(-1, -1, -1).normalized, // 1
            new Vector3(-1, -1, +1).normalized, // 2
            new Vector3(-1, +1, +1).normalized, // 3
            new Vector3(-1, +1, -1).normalized, // 4
            new Vector3(+1, +1, -1).normalized, // 5
            new Vector3(+1, +1, +1).normalized, // 6
            new Vector3(+1, -1, +1).normalized, // 7
            new Vector3(+1, -1, -1).normalized, // 8
        };

        vistaActual = Mathf.Clamp(vistaInicial, 1, 8) - 1;
        Vector3 posInicial = ObtenerPosicionVista(vistaActual);
        transform.position = posInicial;
        transform.rotation = Quaternion.LookRotation((ObjetivoPos() - posInicial).normalized);
    }

    void Update()
    {
        if (moviendo) return; // ignora input mientras la cámara está en transición

        if (Input.GetKeyDown(KeyCode.Alpha1)) IrAVista(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) IrAVista(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) IrAVista(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) IrAVista(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) IrAVista(4);
        if (Input.GetKeyDown(KeyCode.Alpha6)) IrAVista(5);
        if (Input.GetKeyDown(KeyCode.Alpha7)) IrAVista(6);
        if (Input.GetKeyDown(KeyCode.Alpha8)) IrAVista(7);
    }

    private Vector3 ObjetivoPos() => objetivo != null ? objetivo.position : Vector3.zero;

    private Vector3 ObtenerPosicionVista(int indice) => ObjetivoPos() + direcciones[indice] * distancia;

    private void IrAVista(int indice)
    {
        if (indice == vistaActual) return;
        vistaActual = indice;
        StartCoroutine(TransicionarA(indice));
    }

    private IEnumerator TransicionarA(int indice)
    {
        moviendo = true;

        Vector3 objetivoPos = ObjetivoPos();
        Vector3 dirInicial = (transform.position - objetivoPos).normalized;
        Quaternion rotInicial = transform.rotation;

        Vector3 dirFinal = direcciones[indice];
        Quaternion rotFinal = Quaternion.LookRotation(-dirFinal);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionTransicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / duracionTransicion);
            float tCurva = curvaTransicion.Evaluate(t);

            // Slerp de la DIRECCIÓN (no de la posición): esto hace que la
            // cámara viaje por un arco sobre la esfera de radio 'distancia'
            // alrededor del objetivo, en vez de una línea recta que podría
            // pasar por dentro del cubo.
            Vector3 dirActual = Vector3.Slerp(dirInicial, dirFinal, tCurva);
            transform.position = objetivoPos + dirActual * distancia;
            transform.rotation = Quaternion.Slerp(rotInicial, rotFinal, tCurva);
            yield return null;
        }

        transform.position = objetivoPos + dirFinal * distancia;
        transform.rotation = rotFinal;
        moviendo = false;
    }
}