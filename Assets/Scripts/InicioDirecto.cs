using UnityEngine;

public class InicioDirecto : MonoBehaviour
{
    public GameObject menuPrincipal;
    public GameObject panelDificultad;
    public GameObject historia;

    void Start()
    {
        if (PlayerPrefs.GetInt("ReinicioDirecto", 0) != 1)
            return;

        PlayerPrefs.SetInt("ReinicioDirecto", 0);
        PlayerPrefs.Save();

        if (menuPrincipal != null)
            menuPrincipal.SetActive(false);

        if (panelDificultad != null)
            panelDificultad.SetActive(false);

        if (historia != null)
            historia.SetActive(false);

        if (ControlPartida.Instancia != null)
            ControlPartida.Instancia.PrepararPartida();
    }
}