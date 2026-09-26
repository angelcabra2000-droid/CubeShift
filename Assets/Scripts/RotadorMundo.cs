using System.Collections;
using UnityEngine;

/// <summary>
/// FASE 1 — Rotación básica del "Mundo".
/// Colocar este script sobre el GameObject "Mundo" (el padre del que cuelga
/// todo el laberinto/cubo). Rota 90° sobre X o Y con una transición suave
/// (Slerp), bloqueando nuevas rotaciones mientras una está en curso.
///
/// Controles (X y Z; el eje Y no se usa porque rotar sobre el eje vertical
/// nunca cambia qué pared queda abajo, ver SensorPisoRotacion):
///   W / S -> rotar en X (+90 / -90)  -> pared de adelante/atrás pasa a piso
///   A / D -> rotar en Z (+90 / -90)  -> pared de izquierda/derecha pasa a piso
///
/// VALIDACIÓN POR SENSOR: si se asigna 'sensor' (SensorPisoRotacion, en
/// el Jugador), cada tecla solo dispara la rotación si
/// sensor.PuedeRotar(eje, grados) devuelve true, es decir, si el Jugador
/// quedaría con una pared bajo sus pies tras girar. Sin sensor asignado,
/// se comporta igual que antes (cualquier rotación es válida).
///
/// Al terminar cada rotación se dispara OnRotacionCompletada(eje, grados)
/// para que MovimientoJugador pueda reubicar al Jugador.
/// </summary>
public class RotadorMundo : MonoBehaviour
{
    [Header("Configuración de rotación")]
    [Tooltip("Duración en segundos que tarda el giro de 90°.")]
    [SerializeField] private float duracionGiro = 0.35f;

    [Tooltip("Curva de aceleración del giro (deja 'EaseInOut' si no sabes qué usar).")]
    [SerializeField] private AnimationCurve curvaGiro = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Validación externa (sensor de piso)")]
    [Tooltip("Opcional. El SensorPisoRotacion del Jugador. Si se asigna, cada rotación solo se ejecuta si sensor.PuedeRotar(eje, grados) es true.")]
    [SerializeField] private SensorPisoRotacion sensor;

    private bool rotando = false;

    /// <summary>True mientras el Mundo está a mitad de un giro.</summary>
    public bool EstaRotando => rotando;

    /// <summary>Se dispara cuando una rotación termina, con el mismo (eje, grados) que se usó para girar.</summary>
    public event System.Action<Vector3, float> OnRotacionCompletada;

    void Update()
    {
        if (rotando) return; // ignora input mientras el cubo está girando

        if (Input.GetKeyDown(KeyCode.S) && PuedeRotar(Vector3.right, -90f)) IniciarRotacion(Vector3.right, -90f);
        if (Input.GetKeyDown(KeyCode.W) && PuedeRotar(Vector3.right, 90f)) IniciarRotacion(Vector3.right, 90f);

        if (Input.GetKeyDown(KeyCode.D) && PuedeRotar(Vector3.forward, -90f)) IniciarRotacion(Vector3.forward, -90f);
        if (Input.GetKeyDown(KeyCode.A) && PuedeRotar(Vector3.forward, 90f)) IniciarRotacion(Vector3.forward, 90f);
    }

    private bool PuedeRotar(Vector3 eje, float grados)
    {
        return sensor == null || sensor.PuedeRotar(eje, grados);
    }

    private void IniciarRotacion(Vector3 eje, float grados)
    {
        StartCoroutine(RotarSuave(eje, grados));
    }

    private IEnumerator RotarSuave(Vector3 eje, float grados)
    {
        rotando = true;

        Quaternion rotacionInicial = transform.rotation;
        // Rotación en espacio MUNDIAL (no local): "eje" siempre significa lo
        // mismo para el jugador, sin importar la orientación previa del cubo.
        Quaternion rotacionFinal = Quaternion.AngleAxis(grados, eje) * rotacionInicial;

        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionGiro)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.Clamp01(tiempoTranscurrido / duracionGiro);
            float tCurva = curvaGiro.Evaluate(t);

            transform.rotation = Quaternion.Slerp(rotacionInicial, rotacionFinal, tCurva);
            yield return null;
        }

        // Snap final exacto para evitar arrastre de error de punto flotante
        transform.rotation = rotacionFinal;
        rotando = false;

        OnRotacionCompletada?.Invoke(eje, grados);
    }
}