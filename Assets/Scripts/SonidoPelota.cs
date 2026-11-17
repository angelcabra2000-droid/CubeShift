
using UnityEngine;

public class SonidoPelota : MonoBehaviour
{
    [Header("Referencias")]
    public BallSimulator simulador;
    public AudioSource audioEnergia;
    public AudioSource audioRebote;

    [Header("Rebote")]
    public AudioClip sonidoRebote;
    public float velocidadMinimaRebote = 1f;
    public float intervaloMinimo = 0.15f;

    [Header("Movimiento")]
    public float velocidadMinimaMovimiento = 0.15f;

    [Header("Sonido")]
    [SerializeField] private SonidoPelota sonidoPelota;

    private float ultimoRebote = -10f;
    private bool detenida = false;

    private void Start()
    {
        if (simulador == null)
            simulador = GetComponent<BallSimulator>();

        if (audioEnergia != null)
        {
            audioEnergia.playOnAwake = false;
            audioEnergia.loop = true;
            audioEnergia.Stop();
        }
    }

    private void LateUpdate()
    {
        if (detenida || simulador == null || audioEnergia == null)
            return;

        bool seMueve =
            simulador.Speed > velocidadMinimaMovimiento;

        if (seMueve)
        {
            if (!audioEnergia.isPlaying)
                audioEnergia.Play();
        }
        else
        {
            if (audioEnergia.isPlaying)
                audioEnergia.Stop();
        }
    }

    public void ReproducirRebote(float velocidad)
    {
        if (detenida)
            return;

        if (velocidad < velocidadMinimaRebote)
            return;

        if (Time.time - ultimoRebote < intervaloMinimo)
            return;

        if (audioRebote != null && sonidoRebote != null)
        {
            audioRebote.pitch = Random.Range(0.95f, 1.05f);
            audioRebote.PlayOneShot(sonidoRebote);

            ultimoRebote = Time.time;
        }
    }

    public void DetenerSonidos()
    {
        detenida = true;

        if (audioEnergia != null)
            audioEnergia.Stop();

        if (audioRebote != null)
            audioRebote.Stop();
    }
}
