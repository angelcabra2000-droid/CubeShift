using UnityEngine;

public class ColocarInicioYMeta : MonoBehaviour
{
    [SerializeField] private GeneradorLaberintoFacil generador;
    [SerializeField] private Transform jugador;
    [SerializeField] private Transform pelota;
    [Tooltip("Mitad de la altura de la cápsula (en unidades reales).")]
    [SerializeField] private float mitadAlturaJugador = 1f;
    [SerializeField] private float radioPelota = 0.5f;

    private void Start()
    {
        jugador.position = generador.PuntoInicio() + Vector3.up * (mitadAlturaJugador + 0.05f);
        pelota.position = generador.PuntoSalida() + Vector3.up * (radioPelota + 0.05f);
    }
}