
using UnityEngine;

public class BoosterEnergia : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            EnergiaJugador energia =
                other.GetComponentInParent<EnergiaJugador>();

            if (energia != null)
            {
                energia.RecuperarEnergia();
                Destroy(gameObject);
            }
        }
    }
}
