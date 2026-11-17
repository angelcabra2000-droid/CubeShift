
using UnityEngine;

public class ZumbidoBooster : MonoBehaviour
{
    [Header("Referencias")]
    public Transform jugador;
    public AudioSource audioEnergia;
    public AudioClip sonidoEnergia;

    [Header("Configuracion")]
    public float distanciaActivacion = 4f;
    [Range(0f, 1f)] public float volumenMaximo = 0.5f;

    private void Start()
    {
        if (jugador == null)
        {
            GameObject objetoJugador =
                GameObject.FindGameObjectWithTag("Player");

            if (objetoJugador != null)
                jugador = objetoJugador.transform;
        }

        if (audioEnergia != null)
        {
            audioEnergia.playOnAwake = false;
            audioEnergia.loop = true;
            audioEnergia.spatialBlend = 0f;
            audioEnergia.volume = 0f;
            audioEnergia.clip = sonidoEnergia;
            audioEnergia.Stop();
        }
    }

    private void Update()
    {
        if (jugador == null ||
            audioEnergia == null ||
            sonidoEnergia == null)
            return;

        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
        {
            if (audioEnergia.isPlaying)
                audioEnergia.Stop();

            return;
        }

        float distancia = Vector3.Distance(
            transform.position,
            jugador.position
        );

        if (distancia <= distanciaActivacion)
        {
            if (!audioEnergia.isPlaying)
                audioEnergia.Play();

            float intensidad =
                1f - distancia / distanciaActivacion;

            audioEnergia.volume =
                volumenMaximo * intensidad;
        }
        else
        {
            if (audioEnergia.isPlaying)
                audioEnergia.Stop();
        }
    }
}
