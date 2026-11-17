
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class MenuVictoria : MonoBehaviour
{
    [Header("Interfaz")]
    public GameObject panelVictoria;
    public GameObject nuevoRecord;
    public TMP_Text textoTiempo;
    public TMP_Text textoRecord;

    [Header("Musica")]
    public ControladorMusica controladorMusica;

    [Header("Sonidos")]
    public AudioSource audioEfectos;
    public AudioClip sonidoVictoria;
    public AudioClip sonidoNuevoRecord;

    public void MostrarVictoria(float tiempo)
    {
        float recordAnterior = PlayerPrefs.GetFloat(
            "RecordCubeShift", -1f
        );
        

        bool esNuevoRecord =
            recordAnterior < 0f || tiempo < recordAnterior;

        if (esNuevoRecord)
        {
            PlayerPrefs.SetFloat("RecordCubeShift", tiempo);
            PlayerPrefs.Save();
        }

        float mejorTiempo = esNuevoRecord
            ? tiempo
            : recordAnterior;

        panelVictoria.SetActive(true);

        textoTiempo.text = FormatearTiempo(tiempo);
        textoRecord.text = FormatearTiempo(mejorTiempo);

        nuevoRecord.SetActive(esNuevoRecord);

        if (controladorMusica != null)
        {
            controladorMusica.CambiarAVictoria();
        }

     
    }

    private string FormatearTiempo(float tiempo)
    {
        int minutos = Mathf.FloorToInt(tiempo / 60f);
        int segundos = Mathf.FloorToInt(tiempo % 60f);

        return $"{minutos:00}:{segundos:00}";
    }

    public void ReiniciarJuego()
    {
        PlayerPrefs.SetInt("ReinicioDirecto", 1);
        PlayerPrefs.Save();

        Time.timeScale = 1f;

        Scene escena = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escena.buildIndex);
    }
    public void DetenerMusicaNivel()
    {
        if (controladorMusica != null)
            controladorMusica.DetenerMusica();
    }
    public void ReproducirSonidoVictoria(float tiempo)
    {
        float recordAnterior = PlayerPrefs.GetFloat("RecordCubeShift", -1f);

        bool esNuevoRecord = recordAnterior < 0f || tiempo < recordAnterior;

        AudioClip sonido = esNuevoRecord
            ? sonidoNuevoRecord
            : sonidoVictoria;

        if (audioEfectos != null && sonido != null)
            audioEfectos.PlayOneShot(sonido);
    }

    public void VolverMenuPrincipal()
    {
        PlayerPrefs.SetInt("ReinicioDirecto", 0);
        PlayerPrefs.Save();

        Time.timeScale = 1f;

        Scene escena = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escena.buildIndex);
    }
}
