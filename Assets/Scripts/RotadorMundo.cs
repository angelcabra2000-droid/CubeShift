
using System.Collections;
using UnityEngine;

public class RotadorMundo : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform jugador;

    [Header("Configuración de rotación")]
    [SerializeField] private float duracionGiro = 0.35f;

    [SerializeField]
    private AnimationCurve curvaGiro =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Validación externa")]
    [SerializeField] private SensorPisoRotacion sensor;

    [Header("Sonido de rotacion")]
    [SerializeField] private AudioSource audioRotacion;
    [SerializeField] private AudioClip sonidoRotacion;

    [Header("Sonido de giro horizontal")]
    [SerializeField] private AudioClip sonidoGiroHorizontal;

    private bool rotando = false;

    public bool EstaRotando => rotando;

    private bool bloqueaGravedad;

    public bool PausaGravedad =>
        rotando && bloqueaGravedad;

    public event System.Action<Vector3, float> OnRotacionCompletada;

    public void SolicitarRotacionHacia(Vector3 direccionMundial)
    {
        if (rotando)
            return;

        if (sensor == null)
        {
            Debug.LogWarning(
                "[RotadorMundo] No hay sensor asignado."
            );
            return;
        }

        if (sensor.ObtenerRotacionParaDireccion(
            direccionMundial,
            out Vector3 eje,
            out float grados))
        {
            sensor.ObtenerCorreccionVertical(
                direccionMundial,
                out float correccion
            );

            IniciarRotacion(eje, grados, correccion);
        }
        else
        {
            Debug.LogWarning(
                "[RotadorMundo] No se encontro pared."
            );
        }
    }

    public void Rotar(
        Vector3 eje,
        float grados,
        float correccionVertical = 0f)
    {
        if (rotando)
            return;

        if (jugador == null)
        {
            Debug.LogWarning(
                "[RotadorMundo] No hay jugador asignado."
            );
        }

        IniciarRotacion(eje, grados, correccionVertical);
    }

    private void IniciarRotacion(
        Vector3 eje,
        float grados,
        float correccionVertical)
    {
        if (audioRotacion != null &&
            sonidoGiroHorizontal != null &&
            Mathf.Abs(Vector3.Dot(
                eje.normalized,
                Vector3.up)) >= 0.99f)
        {
            audioRotacion.PlayOneShot(sonidoGiroHorizontal);
        }

        StartCoroutine(
            RotarSuave(eje, grados, correccionVertical)
        );
    }

    public void ReproducirSonidoRotacion()
    {
        if (audioRotacion != null && sonidoRotacion != null)
        {
            audioRotacion.PlayOneShot(sonidoRotacion);
        }
    }

    private IEnumerator RotarSuave(
        Vector3 eje,
        float grados,
        float correccionVertical)
    {
        rotando = true;

        bloqueaGravedad =
            Mathf.Abs(Vector3.Dot(
                eje.normalized,
                Vector3.up)) < 0.99f;

        Quaternion deltaFinal =
            Quaternion.AngleAxis(grados, eje);

        Quaternion rotacionFinal =
            deltaFinal * transform.rotation;

        float tiempo = 0f;
        float tPrevio = 0f;

        while (tiempo < duracionGiro)
        {
            tiempo += Time.deltaTime;

            float t = curvaGiro.Evaluate(
                Mathf.Clamp01(tiempo / duracionGiro)
            );

            Quaternion qPrevio = Quaternion.Slerp(
                Quaternion.identity,
                deltaFinal,
                tPrevio
            );

            Quaternion qActual = Quaternion.Slerp(
                Quaternion.identity,
                deltaFinal,
                t
            );

            Quaternion paso =
                qActual * Quaternion.Inverse(qPrevio);

            Vector3 pivote = jugador != null
                ? jugador.position
                : transform.position;

            transform.position =
                pivote +
                paso * (transform.position - pivote) +
                Vector3.up * (correccionVertical * (t - tPrevio));

            transform.rotation =
                paso * transform.rotation;

            tPrevio = t;

            Physics.SyncTransforms();

            yield return null;
        }

        transform.rotation = rotacionFinal;

        Physics.SyncTransforms();

        rotando = false;
        bloqueaGravedad = false;

        OnRotacionCompletada?.Invoke(eje, grados);
    }
}
