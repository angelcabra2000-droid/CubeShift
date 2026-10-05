using System.Collections;
using UnityEngine;

/// <summary>
/// Cámara con 4 posiciones fijas, una de frente a cada lado horizontal del
/// "Mundo" (adelante/atrás/izquierda/derecha del objetivo), todas mirando
/// derecho hacia su centro, a la misma altura (sin vistas de arriba/abajo).
/// Presiona las teclas 1-4 para saltar entre vistas, con una transición
/// suave.
///
/// (El nombre de la clase quedó como "CamaraVertices" por compatibilidad
/// con la referencia que ya tenés armada en la escena — aunque ahora ya
/// no mira desde los vértices del cubo, sino de frente a cada cara.)
///
/// Colocar este script sobre tu Main Camera.
///
/// La cámara viaja ORBITANDO por fuera de un círculo horizontal alrededor
/// del objetivo (interpolando la dirección con Slerp, no la posición con
/// Lerp en línea recta), así que nunca se acerca al cubo durante la
/// transición, sin importar qué dos vistas elijas.
///
/// SEGUIMIENTO CONTINUO: el 'objetivo' (normalmente el Jugador) puede
/// moverse en cualquier momento, no solo durante una transición de vista.
/// Por eso, fuera de una transición, la cámara se reubica todos los frames
/// relativa al objetivo actual.
///
/// Numeración de vistas (las 4 direcciones horizontales, en orden):
///   1: -Z (de frente)   2: +X (derecha)   3: +Z (atrás)   4: -X (izquierda)
/// </summary>
public class CamaraVertices : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El punto que la cámara siempre mira. Recomendado: el Jugador.")]
    [SerializeField] private Transform objetivo;

    [Header("Geometría de las 4 vistas")]
    [Tooltip("Distancia horizontal desde el objetivo hasta la cámara.")]
    [SerializeField] private float distancia = 8f;

    [Header("Transición")]
    [Tooltip("Duración en segundos del movimiento entre vistas.")]
    [SerializeField] private float duracionTransicion = 0.5f;

    [Tooltip("Curva de aceleración de la transición.")]
    [SerializeField] private AnimationCurve curvaTransicion = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Vista inicial")]
    [Tooltip("Vista (1-4) donde arranca la cámara.")]
    [SerializeField] private int vistaInicial = 1;

    private Vector3[] direcciones;
    private bool moviendo = false;
    private int vistaActual;

    void Start()
    {
        // Las 4 direcciones horizontales (sin componente Y): frente, derecha,
        // atrás, izquierda. Todas a la misma altura que el objetivo.
        direcciones = new Vector3[4]
        {
            new Vector3(0, 0, -1), // 1: de frente
            new Vector3(1, 0, 0),  // 2: derecha
            new Vector3(0, 0, 1),  // 3: atrás
            new Vector3(-1, 0, 0), // 4: izquierda
        };

        vistaActual = Mathf.Clamp(vistaInicial, 1, 4) - 1;
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
    }

    void LateUpdate()
    {
        // Mientras NO estamos en medio de una transición de vista, seguimos
        // al objetivo todos los frames (el objetivo puede moverse por sí
        // solo, ej. el Jugador caminando).
        if (moviendo) return;

        Vector3 objetivoPos = ObjetivoPos();
        Vector3 posActual = objetivoPos + direcciones[vistaActual] * distancia;
        transform.position = posActual;
        transform.rotation = Quaternion.LookRotation((objetivoPos - posActual).normalized);
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

        Vector3 dirInicial = (transform.position - ObjetivoPos()).normalized;
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
            // cámara viaje por un arco alrededor del objetivo, en vez de una
            // línea recta que podría pasar por dentro del cubo. Seguimos
            // leyendo ObjetivoPos() cada frame para no perder de vista un
            // objetivo que también se mueve.
            Vector3 objetivoPos = ObjetivoPos();
            Vector3 dirActual = Vector3.Slerp(dirInicial, dirFinal, tCurva);
            transform.position = objetivoPos + dirActual * distancia;
            transform.rotation = Quaternion.Slerp(rotInicial, rotFinal, tCurva);
            yield return null;
        }

        transform.position = ObjetivoPos() + dirFinal * distancia;
        transform.rotation = rotFinal;
        moviendo = false;
    }
}