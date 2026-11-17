
using UnityEngine;

public class EnergiaJugador : MonoBehaviour
{
    public float energiaMaxima = 100f;
    public float energiaActual = 100f;
    public float consumoPorSegundo = 5f;

    [Header("Sonido de derrota")]
    public AudioSource audioDerrota;
    public AudioClip sonidoDerrota;

    private bool derrotaReproducida = false;

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

            if (energiaActual <= 0f && !derrotaReproducida)
            {
                ReproducirDerrota();
            }
        }
    }

    private void ReproducirDerrota()
    {
        derrotaReproducida = true;

        if (audioDerrota != null && sonidoDerrota != null)
        {
            audioDerrota.PlayOneShot(sonidoDerrota);
        }
    }

    public void RecuperarEnergia()
    {
        energiaActual = energiaMaxima;
        derrotaReproducida = false;

        Debug.Log("Energia recuperada al 100%");
    }
}
