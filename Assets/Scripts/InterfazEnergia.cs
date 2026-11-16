
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

    [Header("Colores")]
    public Color colorVerde = Color.green;
    public Color colorNaranja = new Color(1f, 0.5f, 0f);
    public Color colorRojo = Color.red;

    private bool juegoTerminado = false;
    private bool alertaMostrada = false;
    private float tiempoAlerta = 0f;

    void Start()
    {
        alertaEnergia.SetActive(false);
        panelFinJuego.SetActive(false);

        barraEnergia.minValue = 0f;
        barraEnergia.maxValue = 100f;

        ActualizarBarra();
    }

    void Update()
    {
        if (energiaJugador == null || juegoTerminado)
            return;

        float porcentaje = energiaJugador.PorcentajeEnergia;

        ActualizarBarra();

        // Reiniciar alerta cuando se recupera energia
        if (porcentaje >= 0.25f)
        {
            alertaMostrada = false;
            alertaEnergia.SetActive(false);
            tiempoAlerta = 0f;
        }

        // Mostrar alerta una sola vez
        if (porcentaje > 0f &&
            porcentaje < 0.25f &&
            !alertaMostrada)
        {
            alertaMostrada = true;
            tiempoAlerta = duracionAlerta;
            alertaEnergia.SetActive(true);
        }

        // Ocultar alerta despues de unos segundos
        if (alertaEnergia.activeSelf)
        {
            tiempoAlerta -= Time.deltaTime;

            if (tiempoAlerta <= 0f)
            {
                alertaEnergia.SetActive(false);
            }
        }

        // Fin del juego
        if (porcentaje <= 0f)
        {
            juegoTerminado = true;

            alertaEnergia.SetActive(false);
            panelFinJuego.SetActive(true);

            Time.timeScale = 0f;
        }
    }

    void ActualizarBarra()
    {
        if (energiaJugador == null)
            return;

        float porcentaje = energiaJugador.PorcentajeEnergia;

        barraEnergia.value = porcentaje * 100f;

        if (porcentaje >= 0.5f)
        {
            rellenoBarra.color = colorVerde;
        }
        else if (porcentaje >= 0.25f)
        {
            rellenoBarra.color = colorNaranja;
        }
        else
        {
            rellenoBarra.color = colorRojo;
        }
    }

    public void ReiniciarTiempo()
    {
        Time.timeScale = 1f;
    }
}
