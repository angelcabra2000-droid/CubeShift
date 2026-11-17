
using System.Collections.Generic;
using UnityEngine;

public class GeneradorBoosters : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject prefabBooster;
    public Transform mundo;
    public LayerMask capaParedes;

    [Header("Generacion")]
    public float tiempoGeneracion = 15f;
    public int maximoBoosters = 3;
    public float alturaSobreSuelo = 0.6f;
    public float radioLibre = 0.3f;
    public float margenBorde = 0.5f;
    public int intentosMaximos = 100;

    private float temporizador;
    private readonly List<GameObject> boosters = new List<GameObject>();
    private readonly List<Collider> superficies = new List<Collider>();

    private Bounds limitesMundo;

    void Start()
    {
        if (mundo == null || prefabBooster == null)
        {
            Debug.LogError("Faltan referencias.");
            enabled = false;
            return;
        }

        Collider[] colliders = mundo.GetComponentsInChildren<Collider>();

        bool primero = true;

        foreach (Collider col in colliders)
        {
            if (!col.enabled || col.isTrigger)
                continue;

            if (((1 << col.gameObject.layer) & capaParedes.value) == 0)
                continue;

            superficies.Add(col);

            if (primero)
            {
                limitesMundo = col.bounds;
                primero = false;
            }
            else
            {
                limitesMundo.Encapsulate(col.bounds);
            }
        }

        if (superficies.Count == 0)
        {
            Debug.LogError("No se encontraron superficies.");
            enabled = false;
        }
    }

    void Update()
    {
        if (ControlPartida.Instancia == null ||
            !ControlPartida.Instancia.EstaJugando)
            return;

        boosters.RemoveAll(b => b == null);

        temporizador += Time.deltaTime;

        if (temporizador >= tiempoGeneracion)
        {
            temporizador = 0f;

            if (boosters.Count < maximoBoosters)
                GenerarBooster();
        }
    }


    void GenerarBooster()
    {
        for (int i = 0; i < intentosMaximos; i++)
        {
            Collider superficie = superficies[
                Random.Range(0, superficies.Count)
            ];

            Bounds b = superficie.bounds;

            float x = Random.Range(
                b.min.x + margenBorde,
                b.max.x - margenBorde
            );

            float z = Random.Range(
                b.min.z + margenBorde,
                b.max.z - margenBorde
            );

            Vector3 origen = new Vector3(
                x,
                b.max.y + 0.2f,
                z
            );

            if (!superficie.Raycast(
                new Ray(origen, Vector3.down),
                out RaycastHit hit,
                b.size.y + 1f))
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.9f)
                continue;

            Vector3 posicion =
                hit.point + Vector3.up * alturaSobreSuelo;

            // Evitar la zona exterior superior del mundo
            if (posicion.y >= limitesMundo.max.y - margenBorde)
                continue;

            if (posicion.y <= limitesMundo.min.y + margenBorde)
                continue;

            // Comprobar espacio libre
            if (Physics.CheckSphere(
                posicion,
                radioLibre,
                capaParedes,
                QueryTriggerInteraction.Ignore))
                continue;

            bool ocupado = false;

            foreach (GameObject booster in boosters)
            {
                if (booster != null &&
                    Vector3.Distance(
                        booster.transform.position,
                        posicion) < 2f)
                {
                    ocupado = true;
                    break;
                }
            }

            if (ocupado)
                continue;

            GameObject nuevo = Instantiate(
                prefabBooster,
                posicion,
                Quaternion.identity,
                mundo
            );

            nuevo.SetActive(true);
            boosters.Add(nuevo);

            Debug.Log("Booster generado: " + posicion);
            return;
        }

        Debug.LogWarning("No se encontro posicion valida.");
    }
}
