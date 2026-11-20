
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class GeneradorBoosters : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabBooster;
    public Transform mundo;
    public Transform jugador;
    public LayerMask capaParedes;

    [Header("Generacion")]
    public float tiempoGeneracion = 15f;
    public int maximoBoosters = 3;
    public float alturaSobreSuelo = 0.6f;
    public float radioLibre = 0.3f;
    public float margenBorde = 0.5f;
    public int intentosMaximos = 100;

    [Header("Dificultad Facil")]
    public float tiempoFacil = 10f;
    public int maximoFacil = 5;

    [Header("Dificultad Dificil")]
    public float tiempoDificil = 18f;
    public int maximoDificil = 3;

    [Header("Sonido de aparicion")]
    public AudioClip sonidoAparicion;
    [Range(0f, 1f)] public float volumenAparicion = 0.8f;
    public float distanciaMinima = 1f;
    public float distanciaMaxima = 100f;

    [Header("Audio Mixer")]
    public AudioMixerGroup grupoEfectos;

    private float temporizador;

    private readonly List<GameObject> boosters =
        new List<GameObject>();

    private readonly List<Collider> superficies =
        new List<Collider>();

    void Start()
    {
        ConfigurarDificultad();

        if (mundo == null || prefabBooster == null || jugador == null)
        {
            Debug.LogError("Faltan referencias en GeneradorBoosters.");
            enabled = false;
            return;
        }

        Collider[] colliders = mundo.GetComponentsInChildren<Collider>();

        foreach (Collider col in colliders)
        {
            if (!col.enabled || col.isTrigger)
                continue;

            if (((1 << col.gameObject.layer) & capaParedes.value) == 0)
                continue;

            superficies.Add(col);
        }

        if (superficies.Count == 0)
        {
            Debug.LogError("No se encontraron superficies.");
            enabled = false;
        }
    }

    public void ConfigurarDificultad()
    {
        if (DificultadJuego.dificultadActual ==
            DificultadJuego.Nivel.Facil)
        {
            tiempoGeneracion = tiempoFacil;
            maximoBoosters = maximoFacil;
        }
        else
        {
            tiempoGeneracion = tiempoDificil;
            maximoBoosters = maximoDificil;
        }

        temporizador = 0f;
    }

    void Update()
    {
        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
            return;

        boosters.RemoveAll(b => b == null);

        temporizador += Time.deltaTime;

        if (temporizador >= tiempoGeneracion)
        {
            temporizador = 0f;

            if (boosters.Count < maximoBoosters)
                GenerarBooster();
        }
    }

    void GenerarBooster()
    {
        Bounds limites = new Bounds();
        bool primero = true;

        foreach (Collider col in superficies)
        {
            if (col == null || !col.enabled)
                continue;

            if (primero)
            {
                limites = col.bounds;
                primero = false;
            }
            else
            {
                limites.Encapsulate(col.bounds);
            }
        }

        if (primero)
            return;

        for (int i = 0; i < intentosMaximos; i++)
        {
            float x = Random.Range(
                limites.min.x + margenBorde,
                limites.max.x - margenBorde
            );

            float z = Random.Range(
                limites.min.z + margenBorde,
                limites.max.z - margenBorde
            );

            Vector3 origen = new Vector3(
                x,
                jugador.position.y + 0.5f,
                z
            );

            if (!Physics.Raycast(
                origen,
                Vector3.down,
                out RaycastHit hit,
                limites.size.y,
                capaParedes,
                QueryTriggerInteraction.Ignore))
                continue;

            if (!hit.collider.transform.IsChildOf(mundo))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.9f)
                continue;

            Vector3 posicion =
                hit.point + Vector3.up * alturaSobreSuelo;

            if (Physics.CheckSphere(
                posicion,
                radioLibre,
                capaParedes,
                QueryTriggerInteraction.Ignore))
                continue;

            bool ocupado = false;

            foreach (GameObject booster in boosters)
            {
                if (booster != null &&
                    Vector3.Distance(
                        booster.transform.position,
                        posicion) < 2f)
                {
                    ocupado = true;
                    break;
                }
            }

            if (ocupado)
                continue;

            GameObject nuevo = Instantiate(
                prefabBooster,
                posicion,
                Quaternion.identity,
                mundo
            );

            nuevo.SetActive(true);
            boosters.Add(nuevo);

            ReproducirAparicion(posicion);

            Debug.Log("Booster generado: " + posicion);
            return;
        }

        Debug.LogWarning("No se encontro posicion valida.");
    }

    void ReproducirAparicion(Vector3 posicion)
    {
        if (sonidoAparicion == null)
            return;

        GameObject objetoAudio =
            new GameObject("SonidoAparicionBooster");

        objetoAudio.transform.position = posicion;

        AudioSource audio =
            objetoAudio.AddComponent<AudioSource>();

        audio.playOnAwake = false;
        audio.clip = sonidoAparicion;
        audio.outputAudioMixerGroup = grupoEfectos;
        audio.volume = volumenAparicion;
        audio.spatialBlend = 1f;
        audio.rolloffMode = AudioRolloffMode.Linear;
        audio.minDistance = distanciaMinima;
        audio.maxDistance = distanciaMaxima;
        audio.dopplerLevel = 0f;

        audio.Play();

        Destroy(
            objetoAudio,
            sonidoAparicion.length + 0.1f
        );
    }
}
