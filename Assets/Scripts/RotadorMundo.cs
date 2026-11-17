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

    [Header("Sonido de rotacion")]
    [SerializeField] private AudioSource audioRotacion;
    [SerializeField] private AudioClip sonidoRotacion;

    private bool rotando = false;
    public bool EstaRotando => rotando;

    private bool bloqueaGravedad;
    /// <summary>True solo mientras gira un cambio de piso (eje horizontal). Los giros A/D no pausan la gravedad.</summary>
    public bool PausaGravedad => rotando && bloqueaGravedad;

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
        if (rotando) return;

        if (sensor == null)
        {
            Debug.LogWarning("[RotadorMundo] El campo 'Sensor' está vacío en el Inspector de este objeto: no puedo calcular la rotación. (ClicParaRotar usa Rotar(eje, grados) y no depende de este campo.)", this);
            return;
        }

        if (sensor.ObtenerRotacionParaDireccion(direccionMundial, out Vector3 eje, out float grados))
        {
            sensor.ObtenerCorreccionVertical(direccionMundial, out float correccion);
            IniciarRotacion(eje, grados, correccion);
        }
        else
            Debug.LogWarning("[RotadorMundo] El sensor no encontró pared en esa dirección, no se rota.", this);
    }

    /// <summary>
    /// Rota el Mundo alrededor del Jugador con un (eje, grados) ya calculado
    /// por quien llama (ej. ClicParaRotar con su propio sensor). No depende
    /// del campo 'sensor' de este componente.
    ///
    /// 'correccionVertical' (opcional): cuánto subir (+) o bajar (-) el Mundo
    /// durante el mismo giro para que la pared que pasa a ser piso quede
    /// EXACTAMENTE bajo los pies del Jugador, en vez de hundirlo o dejarlo
    /// flotando. Se calcula con SensorPisoRotacion.ObtenerCorreccionVertical.
    /// Para giros que no cambian el piso (A/D) se deja en 0.
    /// </summary>
    public void Rotar(Vector3 eje, float grados, float correccionVertical = 0f)
    {
        if (rotando) return;

        if (jugador == null)
            Debug.LogWarning("[RotadorMundo] El campo 'Jugador' está vacío: el Mundo va a rotar alrededor de su propio centro, no del Player.", this);

        IniciarRotacion(eje, grados, correccionVertical);
    }

    private void IniciarRotacion(Vector3 eje, float grados, float correccionVertical)
    {
        StartCoroutine(RotarSuave(eje, grados, correccionVertical));
    }

    public void ReproducirSonidoRotacion()
    {
        if (audioRotacion != null && sonidoRotacion != null)
        {
            audioRotacion.PlayOneShot(sonidoRotacion);
        }
    }

    private IEnumerator RotarSuave(Vector3 eje, float grados, float correccionVertical)
    {
        rotando = true;
        // Solo los giros que cambian el piso (eje horizontal) necesitan pausar la gravedad.
        bloqueaGravedad = Mathf.Abs(Vector3.Dot(eje.normalized, Vector3.up)) < 0.99f;

        Quaternion deltaFinal = Quaternion.AngleAxis(grados, eje);
        Quaternion rotacionFinal = deltaFinal * transform.rotation;
        float tiempo = 0f;
        float tPrevio = 0f;

        while (tiempo < duracionGiro)
        {
            tiempo += Time.deltaTime;
            float t = curvaGiro.Evaluate(Mathf.Clamp01(tiempo / duracionGiro));

            // Rotación incremental de este frame, alrededor de donde está el jugador AHORA
            Quaternion qPrevio = Quaternion.Slerp(Quaternion.identity, deltaFinal, tPrevio);
            Quaternion qActual = Quaternion.Slerp(Quaternion.identity, deltaFinal, t);
            Quaternion paso = qActual * Quaternion.Inverse(qPrevio);

            Vector3 pivote = jugador != null ? jugador.position : transform.position;
            transform.position = pivote
                + paso * (transform.position - pivote)
                + Vector3.up * (correccionVertical * (t - tPrevio));
            transform.rotation = paso * transform.rotation;
            tPrevio = t;

            // Actualiza las físicas en tiempo real para que la gravedad no use posiciones viejas
            Physics.SyncTransforms();
            yield return null;
        }

        // Snap final exacto contra error de punto flotante
        transform.rotation = rotacionFinal;
        Physics.SyncTransforms();
        rotando = false;
        bloqueaGravedad = false;

        OnRotacionCompletada?.Invoke(eje, grados);
    }
}