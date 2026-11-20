
using UnityEngine;
using UnityEngine.UI;

public class InterfazEnergia : MonoBehaviour
{
    [Header("Referencias")]
    public EnergiaJugador energiaJugador;
    public Slider barraEnergia;
    public Image rellenoBarra;

    [Header("Alertas")]
    public GameObject alertaEnergia;
    public GameObject panelFinJuego;

    [Header("Configuracion de alerta")]
    public float duracionAlerta = 3f;
    [Range(0f, 1f)] public float energiaCritica = 0.20f;

    [Header("Sonido energia baja")]
    public AudioSource audioAlerta;
    public AudioClip sonidoAlerta;
    public float intervaloPitido = 1.2f;

    [Header("Parpadeo")]
    public float velocidadParpadeo = 4f;
    [Range(0f, 1f)] public float brilloMinimo = 0.4f;

    [Header("Colores faciles")]
    public Color colorVerde = Color.green;
    public Color colorNaranja = new Color(1f, 0.5f, 0f);
    public Color colorRojo = Color.red;

    [Header("Colores dificiles")]
    public Color colorRosaDificil =
        new Color(1f, 0.176f, 0.584f);

    public Color colorNaranjaDificil =
        new Color(1f, 0.549f, 0f);

    public Color colorRojoDificil =
        new Color(1f, 0.09f, 0.267f);

    private bool dificultadDificil = false;
    private bool juegoTerminado = false;
    private bool alertaMostrada = false;

    private float tiempoAlerta = 0f;
    private float tiempoPitido = 0f;

    void Start()
    {
        if (alertaEnergia != null)
            alertaEnergia.SetActive(false);

        if (panelFinJuego != null)
            panelFinJuego.SetActive(false);

        if (barraEnergia != null)
        {
            barraEnergia.minValue = 0f;
            barraEnergia.maxValue = 100f;
        }

        if (audioAlerta != null)
        {
            audioAlerta.playOnAwake = false;
            audioAlerta.loop = false;
            audioAlerta.spatialBlend = 0f;
        }

        ActualizarBarra();
    }

    void Update()
    {
        if (energiaJugador == null || juegoTerminado)
            return;

        float porcentaje = energiaJugador.PorcentajeEnergia;

        ActualizarBarra();

        bool jugando = ControlPartida.Instancia != null &&
                       ControlPartida.Instancia.EstaJugando;

        bool energiaBaja = porcentaje > 0f &&
                           porcentaje <= energiaCritica;

        if (porcentaje > energiaCritica)
        {
            alertaMostrada = false;
            tiempoAlerta = 0f;
            tiempoPitido = 0f;

            if (alertaEnergia != null)
                alertaEnergia.SetActive(false);
        }

        if (energiaBaja && jugando)
        {
            if (!alertaMostrada)
            {
                alertaMostrada = true;
                tiempoAlerta = duracionAlerta;
                tiempoPitido = 0f;

                if (alertaEnergia != null)
                    alertaEnergia.SetActive(true);
            }

            ActualizarPitido();
            ActualizarParpadeo();
        }
        else
        {
            DetenerPitido();
        }

        if (alertaEnergia != null && alertaEnergia.activeSelf)
        {
            if (jugando)
                tiempoAlerta -= Time.deltaTime;

            if (tiempoAlerta <= 0f)
                alertaEnergia.SetActive(false);
        }

        if (porcentaje <= 0f)
        {
            juegoTerminado = true;

            DetenerPitido();

            if (alertaEnergia != null)
                alertaEnergia.SetActive(false);

            if (panelFinJuego != null)
                panelFinJuego.SetActive(true);

            if (ControlPartida.Instancia != null)
            {
                ControlPartida.Instancia.Perder();
            }
            else
            {
                Debug.LogError("No se encontro ControlPartida");
                Time.timeScale = 0f;
            }
        }
    }

    private void ActualizarPitido()
    {
        if (audioAlerta == null || sonidoAlerta == null)
            return;

        tiempoPitido -= Time.deltaTime;

        if (tiempoPitido <= 0f)
        {
            audioAlerta.PlayOneShot(sonidoAlerta);
            tiempoPitido = Mathf.Max(0.1f, intervaloPitido);
        }
    }

    private void DetenerPitido()
    {
        if (audioAlerta != null && audioAlerta.isPlaying)
            audioAlerta.Stop();

        tiempoPitido = 0f;
    }

    private void ActualizarParpadeo()
    {
        if (rellenoBarra == null)
            return;

        Color color = rellenoBarra.color;

        float intensidad = Mathf.Lerp(
            brilloMinimo,
            1f,
            (Mathf.Sin(Time.time * velocidadParpadeo) + 1f) / 2f
        );

        color.a = intensidad;
        rellenoBarra.color = color;
    }

    public void ConfigurarColores(bool dificil)
    {
        dificultadDificil = dificil;
        ActualizarBarra();
    }

    void ActualizarBarra()
    {
        if (energiaJugador == null ||
            barraEnergia == null ||
            rellenoBarra == null)
            return;

        float porcentaje = energiaJugador.PorcentajeEnergia;

        barraEnergia.value = porcentaje * 100f;

        Color color;

        if (dificultadDificil)
        {
            if (porcentaje >= 0.5f)
                color = colorRosaDificil;
            else if (porcentaje >= 0.25f)
                color = colorNaranjaDificil;
            else
                color = colorRojoDificil;
        }
        else
        {
            if (porcentaje >= 0.5f)
                color = colorVerde;
            else if (porcentaje >= 0.25f)
                color = colorNaranja;
            else
                color = colorRojo;
        }

        color.a = 1f;
        rellenoBarra.color = color;
    }

    public void ReiniciarTiempo()
    {
        Time.timeScale = 1f;
    }

    private void OnDisable()
    {
        DetenerPitido();
    }
}
