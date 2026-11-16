
using UnityEngine;

public class EnergiaJugador : MonoBehaviour
{
    public float energiaMaxima = 100f;
    public float energiaActual = 100f;
    public float consumoPorSegundo = 5f;

    public float PorcentajeEnergia
    {
        get
        {
            return energiaMaxima > 0f
                ? Mathf.Clamp01(energiaActual / energiaMaxima)
                : 0f;
        }
    }

    public void ConsumirEnergia(float movimiento)
    {
        if (movimiento > 0.01f)
        {
            energiaActual -= consumoPorSegundo * Time.deltaTime;
            energiaActual = Mathf.Clamp(energiaActual, 0, energiaMaxima);
        }
    }

    public void RecuperarEnergia()
    {
        energiaActual = energiaMaxima;
        Debug.Log("Energia recuperada al 100%");
    }
}
