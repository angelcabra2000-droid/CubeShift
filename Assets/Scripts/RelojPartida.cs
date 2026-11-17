
using UnityEngine;
using TMPro;

public class RelojPartida : MonoBehaviour
{
    public TMP_Text textoTiempo;

    void Update()
    {
        if (ControlPartida.Instancia == null ||
            textoTiempo == null)
            return;

        float tiempo = ControlPartida.Instancia.tiempoPartida;

        int minutos = Mathf.FloorToInt(tiempo / 60f);
        int segundos = Mathf.FloorToInt(tiempo % 60f);

        textoTiempo.text = string.Format(
            "{0:00}:{1:00}",
            minutos,
            segundos
        );
    }
}
