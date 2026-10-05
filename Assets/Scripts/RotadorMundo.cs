using System.Collections;
using UnityEngine;

/// <summary>
/// Rotación del "Mundo" ALREDEDOR DEL JUGADOR (no alrededor de su propio
/// centro). El Jugador nunca se mueve ni rota: lo que pasa es que el
/// laberinto entero gira a su alrededor, y la pared elegida termina, al
/// final del giro, exactamente debajo de sus pies — se convierte en el
/// nuevo piso.
///
/// Colocar este script sobre el GameObject "Mundo".
///
/// YA NO LEE TECLADO: la rotación ahora se dispara desde afuera (ver
/// ClicParaRotar, que llama a SolicitarRotacionHacia al clickear una
/// pared resaltada). El eje y los grados de cada rotación se calculan
/// dinámicamente a partir de la dirección pedida (sensor.ObtenerRotacionParaDireccion),
/// no están fijos a X/Y/Z, porque el Jugador puede girar libremente en el
/// lugar y "adelante" va cambiando de eje del mundo según hacia dónde mire.
/// </summary>
public class RotadorMundo : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El Jugador. El Mundo rota alrededor de ÉL (no de su propio centro) para que el personaje nunca se mueva ni rote.")]
    [SerializeField] private Transform jugador;

    [Header("Configuración de rotación")]
    [Tooltip("Duración en segundos que tarda el giro de 90°.")]
    [SerializeField] private float duracionGiro = 0.35f;

    [Tooltip("Curva de aceleración del giro (deja 'EaseInOut' si no sabes qué usar).")]
    [SerializeField] private AnimationCurve curvaGiro = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Validación externa (sensor de piso)")]
    [Tooltip("El SensorPisoRotacion del Jugador. Sin esto no hay forma de saber el eje/grados correctos para una dirección.")]
    [SerializeField] private SensorPisoRotacion sensor;

    private bool rotando = false;
    public bool EstaRotando => rotando;

    /// <summary>Se dispara cuando una rotación termina, con el mismo (eje, grados) que se usó.</summary>
    public event System.Action<Vector3, float> OnRotacionCompletada;

    /// <summary>
    /// Pedido externo (ej. desde ClicParaRotar) de rotar de forma que la
    /// pared que está en 'direccionMundial' (relativa al Jugador, normalmente
    /// jugador.forward/-forward/right/-right) se convierta en el piso.
    /// No hace nada si ya está rotando, o si en esa dirección no hay pared.
    /// </summary>
    public void SolicitarRotacionHacia(Vector3 direccionMundial)
    {
        if (rotando || sensor == null) return;

        if (sensor.ObtenerRotacionParaDireccion(direccionMundial, out Vector3 eje, out float grados))
            IniciarRotacion(eje, grados);
    }

    private void IniciarRotacion(Vector3 eje, float grados)
    {
        StartCoroutine(RotarSuave(eje, grados));
    }

    private IEnumerator RotarSuave(Vector3 eje, float grados)
    {
        rotando = true;

        // Pivote = posición del Jugador (fija durante todo el giro). Girar
        // el Mundo alrededor de un punto externo requiere animar tanto su
        // posición como su rotación juntas.
        Vector3 pivote = jugador != null ? jugador.position : transform.position;

        Vector3 posicionInicial = transform.position;
        Quaternion rotacionInicial = transform.rotation;
        Quaternion deltaFinal = Quaternion.AngleAxis(grados, eje);

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionGiro)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / duracionGiro);
            float tCurva = curvaGiro.Evaluate(t);

            Quaternion deltaActual = Quaternion.Slerp(Quaternion.identity, deltaFinal, tCurva);

            transform.position = pivote + deltaActual * (posicionInicial - pivote);
            transform.rotation = deltaActual * rotacionInicial;

            yield return null;
        }

        // Snap final exacto para evitar arrastre de error de punto flotante
        transform.position = pivote + deltaFinal * (posicionInicial - pivote);
        transform.rotation = deltaFinal * rotacionInicial;
        rotando = false;

        OnRotacionCompletada?.Invoke(eje, grados);
    }
}