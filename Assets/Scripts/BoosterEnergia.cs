
using UnityEngine;

public class BoosterEnergia : MonoBehaviour
{
    [Header("Sonido")]
    public AudioClip sonidoRecoger;
    [Range(0f, 1f)] public float volumen = 0.8f;

    private bool recogido = false;

    private void OnTriggerEnter(Collider other)
    {
        if (recogido)
            return;

        if (other.CompareTag("Player"))
        {
            EnergiaJugador energia =
                other.GetComponentInParent<EnergiaJugador>();

            if (energia != null)
            {
                recogido = true;

                energia.RecuperarEnergia();

                if (sonidoRecoger != null)
                {
                    Debug.Log("SONIDO BOOSTER EJECUTADO");

                    GameObject audioTemporal = new GameObject("SonidoRecogerBooster");

                    AudioSource audio = audioTemporal.AddComponent<AudioSource>();
                    audio.clip = sonidoRecoger;
                    audio.volume = volumen;
                    audio.spatialBlend = 0f;
                    audio.Play();

                    Destroy(audioTemporal, sonidoRecoger.length + 0.1f);
                }

                Destroy(gameObject);
            }
        }
    }
}
