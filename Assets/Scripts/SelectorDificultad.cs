
using UnityEngine;
using UnityEngine.UI;

public class SelectorDificultad : MonoBehaviour
{
    [Header("Referencias")]
    public EnergiaJugador energiaJugador;
    public GeneradorBoosters generadorBoosters;
    public InterfazEnergia interfazEnergia;
    public MovimientoJugador movimientoJugador;

    [Header("Laberinto")]
    public Transform mundo;

    [Header("Materiales faciles")]
    public Material azulOriginal;
    public Material cianOriginal;

    [Header("Materiales dificiles")]
    public Material rojoPared;
    public Material rojoNeon;

    [Header("Imagenes de interfaz")]
    public Image marcoEnergia;
    public Image botonPausa;
    public Image marcoReloj;

    [Header("Sprites faciles")]
    public Sprite marcoEnergiaAzul;
    public Sprite botonPausaAzul;
    public Sprite marcoRelojAzul;

    [Header("Sprites dificiles")]
    public Sprite marcoEnergiaRojo;
    public Sprite botonPausaRojo;
    public Sprite marcoRelojRojo;

    private void Start()
    {
        RestaurarDificultad();
    }

    public void SeleccionarFacil()
    {
        DificultadJuego.SeleccionarFacil();
        RestaurarDificultad();
    }

    public void SeleccionarDificil()
    {
        DificultadJuego.SeleccionarDificil();
        RestaurarDificultad();
    }

    public void RestaurarDificultad()
    {
        bool dificil =
            DificultadJuego.dificultadActual ==
            DificultadJuego.Nivel.Dificil;

        AplicarDificultad();
        CambiarColorLaberinto(dificil);
        CambiarInterfaz(dificil);

        if (movimientoJugador != null)
            movimientoJugador.ConfigurarResaltado(dificil);
    }

    private void AplicarDificultad()
    {
        if (energiaJugador != null)
            energiaJugador.ConfigurarDificultad();

        if (generadorBoosters != null)
            generadorBoosters.ConfigurarDificultad();
    }

    private void CambiarInterfaz(bool dificil)
    {
        if (marcoEnergia != null)
        {
            Sprite imagen = dificil
                ? marcoEnergiaRojo
                : marcoEnergiaAzul;

            if (imagen != null)
                marcoEnergia.sprite = imagen;
        }

        if (botonPausa != null)
        {
            Sprite imagen = dificil
                ? botonPausaRojo
                : botonPausaAzul;

            if (imagen != null)
                botonPausa.sprite = imagen;
        }

        if (marcoReloj != null)
        {
            Sprite imagen = dificil
                ? marcoRelojRojo
                : marcoRelojAzul;

            if (imagen != null)
                marcoReloj.sprite = imagen;
        }

        if (interfazEnergia != null)
            interfazEnergia.ConfigurarColores(dificil);
    }

    private void CambiarColorLaberinto(bool dificil)
    {
        if (mundo == null)
        {
            Debug.LogWarning("No se asigno el Mundo.");
            return;
        }

        Renderer[] renderers =
            mundo.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer render in renderers)
        {
            Material[] materiales = render.sharedMaterials;
            bool cambiar = false;

            for (int i = 0; i < materiales.Length; i++)
            {
                Material material = materiales[i];

                if (material == null)
                    continue;

                if (material == azulOriginal ||
                    material == rojoPared)
                {
                    materiales[i] = dificil
                        ? rojoPared
                        : azulOriginal;

                    cambiar = true;
                }
                else if (material == cianOriginal ||
                         material == rojoNeon)
                {
                    materiales[i] = dificil
                        ? rojoNeon
                        : cianOriginal;

                    cambiar = true;
                }
            }

            if (cambiar)
                render.sharedMaterials = materiales;
        }
    }
}
