using System.Collections;
using UnityEngine;

/// <summary>
/// FASE 1 — Rotación básica del "Mundo".
/// Colocar este script sobre el GameObject "Mundo" (el padre del que cuelga
/// todo el laberinto/cubo). Rota 90° sobre X o Y con una transición suave
/// (Slerp), bloqueando nuevas rotaciones mientras una está en curso.
///
/// Controles (solo X e Y, Z no se usa):
///   W / S -> rotar en X (-90 / +90)
///   A / D -> rotar en Y (-90 / +90)
/// </summary>
public class RotadorMundo : MonoBehaviour
{
    [Header("Configuración de rotación")]
    [Tooltip("Duración en segundos que tarda el giro de 90°.")]
    [SerializeField] private float duracionGiro = 0.35f;

    [Tooltip("Curva de aceleración del giro (deja 'EaseInOut' si no sabes qué usar).")]
    [SerializeField] private AnimationCurve curvaGiro = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool rotando = false;

    void Update()
    {
        if (rotando) return; // ignora input mientras el cubo está girando

        if (Input.GetKeyDown(KeyCode.S)) IniciarRotacion(Vector3.right, -90f);
        if (Input.GetKeyDown(KeyCode.W)) IniciarRotacion(Vector3.right, 90f);

        if (Input.GetKeyDown(KeyCode.D)) IniciarRotacion(Vector3.up, -90f);
        if (Input.GetKeyDown(KeyCode.A)) IniciarRotacion(Vector3.up, 90f);
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
    }
}