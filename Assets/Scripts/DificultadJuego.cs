
using UnityEngine;

public static class DificultadJuego
{
    public enum Nivel
    {
        Facil,
        Dificil
    }

    public static Nivel dificultadActual = Nivel.Facil;

    public static void SeleccionarFacil()
    {
        dificultadActual = Nivel.Facil;
    }

    public static void SeleccionarDificil()
    {
        dificultadActual = Nivel.Dificil;
    }
}
