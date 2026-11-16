
using UnityEngine;

public class MovimientoBooster : MonoBehaviour
{
    public float velocidadRotacion = 90f;
    public float altura = 0.3f;
    public float velocidadFlotacion = 2f;

    private Vector3 posicionInicial;

    void Start()
    {
        posicionInicial = transform.localPosition;
    }

    void Update()
    {
        transform.Rotate(0, velocidadRotacion * Time.deltaTime, 0);

        float movimiento = Mathf.Sin(Time.time * velocidadFlotacion) * altura;

        transform.localPosition = posicionInicial + Vector3.up * movimiento;
    }
}
