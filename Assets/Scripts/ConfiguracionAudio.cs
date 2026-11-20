
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;

public class ConfiguracionAudio : MonoBehaviour
{
    [Header("Audio Mixer")]
    public AudioMixer mixer;

    [Header("Barras")]
    public Slider sliderMusica;
    public Slider sliderEfectos;

    [Header("Marco Silenciar")]
    public GameObject marcoSilenciar;

    [Header("Boton Silenciar")]
    public TextMeshProUGUI textoSilencio;

    private float volumenMusica;
    private float volumenEfectos;
    private bool silenciado;

    private void Start()
    {
        volumenMusica = PlayerPrefs.GetFloat("VolumenMusica", 0.7f);
        volumenEfectos = PlayerPrefs.GetFloat("VolumenEfectos", 1f);
        silenciado = PlayerPrefs.GetInt("SilenciarTodo", 0) == 1;

        if (sliderMusica != null)
        {
            sliderMusica.SetValueWithoutNotify(volumenMusica);
            sliderMusica.onValueChanged.AddListener(CambiarVolumenMusica);
        }

        if (sliderEfectos != null)
        {
            sliderEfectos.SetValueWithoutNotify(volumenEfectos);
            sliderEfectos.onValueChanged.AddListener(CambiarVolumenEfectos);
        }

        AplicarVolumenes();
    }

    public void CambiarVolumenMusica(float valor)
    {
        volumenMusica = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenMusica", volumenMusica);
        AplicarVolumenes();
    }

    public void CambiarVolumenEfectos(float valor)
    {
        volumenEfectos = Mathf.Clamp01(valor);
        PlayerPrefs.SetFloat("VolumenEfectos", volumenEfectos);
        AplicarVolumenes();
    }

    public void SilenciarTodo()
    {
        silenciado = !silenciado;

        PlayerPrefs.SetInt("SilenciarTodo", silenciado ? 1 : 0);
        PlayerPrefs.Save();

        AplicarVolumenes();
    }

    private void AplicarVolumenes()
    {
        if (marcoSilenciar != null)
            marcoSilenciar.SetActive(silenciado);

        if (mixer == null)
            return;

        float musica = silenciado ? 0f : volumenMusica;
        float efectos = silenciado ? 0f : volumenEfectos;

        mixer.SetFloat("VolumenMusica", ConvertirDecibelios(musica));
        mixer.SetFloat("VolumenEfectos", ConvertirDecibelios(efectos));
    }

    private float ConvertirDecibelios(float volumen)
    {
        return volumen <= 0.0001f
            ? -80f
            : Mathf.Log10(volumen) * 20f;
    }

    private void OnDisable()
    {
        if (sliderMusica != null)
            sliderMusica.onValueChanged.RemoveListener(CambiarVolumenMusica);

        if (sliderEfectos != null)
            sliderEfectos.onValueChanged.RemoveListener(CambiarVolumenEfectos);

        PlayerPrefs.Save();
    }
}
