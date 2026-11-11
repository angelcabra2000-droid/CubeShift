using UnityEngine;

/// <summary>
/// Genera por código un cielo de espacio (estrellas + nebulosa opcional) y lo usa como Skybox.
/// No necesita ninguna textura importada: al dar Play crea un Cubemap con estrellas aleatorias.
///
/// --- CONFIGURACIÓN ---
/// 1. Crea un material (Create > Material) con el shader "Skybox/Cubemap" y arrástralo a "Material Skybox".
///    (Se usa una COPIA en runtime, así que el archivo del material no se modifica.)
/// 2. Pon este script en un GameObject vacío (por ejemplo "FondoEspacial").
/// 3. Dale Play. El cielo aparece en la vista Game (y en Scene mientras corre el juego).
/// </summary>
[DisallowMultipleComponent]
public class FondoEspacial : MonoBehaviour
{
    [Header("Material")]
    [Tooltip("Material con shader Skybox/Cubemap.")]
    [SerializeField] private Material materialSkybox;

    [Header("Calidad")]
    [Tooltip("Resolución de cada cara del cubo. 512 se ve bien; 256 genera más rápido.")]
    [SerializeField] private int resolucion = 512;
    [SerializeField] private int semilla = 12345;

    [Header("Estrellas")]
    [SerializeField] private int cantidadEstrellas = 2500;
    [Tooltip("Mayor = menos estrellas brillantes y más tenues (más realista).")]
    [SerializeField] private float exponenteBrillo = 8f;
    [Tooltip("Tamaño base de cada estrella en píxeles.")]
    [SerializeField] private float tamanoBase = 0.6f;

    [Header("Fondo y nebulosa")]
    [SerializeField] private Color colorFondo = new Color(0.004f, 0.006f, 0.018f);
    [Tooltip("0 = sin nebulosa.")]
    [SerializeField, Range(0f, 0.6f)] private float intensidadNebulosa = 0.15f;
    [SerializeField] private float escalaNebulosa = 1.8f;
    [SerializeField] private Color colorNebulosaA = new Color(0.10f, 0.25f, 0.55f);
    [SerializeField] private Color colorNebulosaB = new Color(0.45f, 0.12f, 0.50f);

    [Header("Movimiento")]
    [Tooltip("Giro lento del cielo en grados por segundo. 0 = quieto.")]
    [SerializeField] private float velocidadRotacion = 0.3f;

    [Header("Cielo (material)")]
    [SerializeField] private float exposicion = 1.3f;

    private Material materialInstancia;
    private float giro;

    void Awake()
    {
        if (materialSkybox == null)
        {
            Shader sh = Shader.Find("Skybox/Cubemap");
            if (sh == null)
            {
                Debug.LogError("[FondoEspacial] Asigna un material con shader Skybox/Cubemap.", this);
                enabled = false;
                return;
            }
            Debug.LogWarning("[FondoEspacial] No asignaste material; se creó uno temporal. Asigna uno para builds.", this);
            materialInstancia = new Material(sh);
        }
        else
        {
            materialInstancia = new Material(materialSkybox);
        }

        Cubemap cubo = GenerarCubemap();
        materialInstancia.SetTexture("_Tex", cubo);
        materialInstancia.SetFloat("_Exposure", exposicion);
        RenderSettings.skybox = materialInstancia;

        Camera cam = Camera.main;
        if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
    }

    void Update()
    {
        if (materialInstancia == null || Mathf.Approximately(velocidadRotacion, 0f)) return;
        giro = (giro + velocidadRotacion * Time.deltaTime) % 360f;
        materialInstancia.SetFloat("_Rotation", giro);
    }

    void OnDestroy()
    {
        if (materialInstancia != null) Destroy(materialInstancia);
    }

    // ---------------------------------------------------------------------

    private Cubemap GenerarCubemap()
    {
        int size = Mathf.Max(64, resolucion);
        System.Random rng = new System.Random(semilla);

        // 6 caras, todas con el color de fondo
        Color[][] caras = new Color[6][];
        for (int f = 0; f < 6; f++)
        {
            caras[f] = new Color[size * size];
            for (int i = 0; i < caras[f].Length; i++) caras[f][i] = new Color(colorFondo.r, colorFondo.g, colorFondo.b, 1f);
        }

        if (intensidadNebulosa > 0f) PintarNebulosa(caras, size, rng);
        PintarEstrellas(caras, size, rng);

        Cubemap cubo = new Cubemap(size, TextureFormat.RGBA32, false);
        cubo.wrapMode = TextureWrapMode.Clamp;
        cubo.filterMode = FilterMode.Bilinear;
        for (int f = 0; f < 6; f++) cubo.SetPixels(caras[f], (CubemapFace)f);
        cubo.Apply();
        return cubo;
    }

    private void PintarEstrellas(Color[][] caras, int size, System.Random rng)
    {
        Color azul = new Color(0.70f, 0.82f, 1.00f);
        Color blanco = Color.white;
        Color calido = new Color(1.00f, 0.88f, 0.68f);

        for (int s = 0; s < cantidadEstrellas; s++)
        {
            // Dirección uniforme sobre la esfera
            float z = (float)(rng.NextDouble() * 2.0 - 1.0);
            float fi = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float r = Mathf.Sqrt(1f - z * z);
            Vector3 dir = new Vector3(r * Mathf.Cos(fi), r * Mathf.Sin(fi), z);

            DirACara(dir, out int cara, out float u, out float v);
            float cx = u * (size - 1);
            float cy = v * (size - 1);

            float brillo = Mathf.Lerp(0.18f, 1f, Mathf.Pow((float)rng.NextDouble(), exponenteBrillo));
            float sigma = tamanoBase + brillo * 0.9f;

            // Color: azulado - blanco - cálido
            float t = (float)rng.NextDouble();
            Color col = t < 0.5f ? Color.Lerp(azul, blanco, t * 2f) : Color.Lerp(blanco, calido, (t - 0.5f) * 2f);

            int rad = Mathf.CeilToInt(sigma * 3f);
            int ix = Mathf.RoundToInt(cx), iy = Mathf.RoundToInt(cy);
            for (int dy = -rad; dy <= rad; dy++)
            {
                int y = iy + dy;
                if (y < 0 || y >= size) continue;
                for (int dx = -rad; dx <= rad; dx++)
                {
                    int x = ix + dx;
                    if (x < 0 || x >= size) continue;
                    float d2 = (x - cx) * (x - cx) + (y - cy) * (y - cy);
                    float w = brillo * Mathf.Exp(-d2 / (2f * sigma * sigma));
                    int idx = y * size + x;
                    Color p = caras[cara][idx];
                    caras[cara][idx] = new Color(
                        Mathf.Min(1f, p.r + col.r * w),
                        Mathf.Min(1f, p.g + col.g * w),
                        Mathf.Min(1f, p.b + col.b * w),
                        1f);
                }
            }
        }
    }

    private void PintarNebulosa(Color[][] caras, int size, System.Random rng)
    {
        float off = (float)(rng.NextDouble() * 100.0) + 50f;

        for (int f = 0; f < 6; f++)
        {
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float v = (y + 0.5f) / size;
                    Vector3 d = CaraADir(f, u, v) * escalaNebulosa;

                    float n = Ruido3(d.x + off, d.y + off, d.z + off);
                    n = Mathf.Clamp01((n - 0.48f) * 2.4f);
                    if (n <= 0f) continue;

                    float mezcla = Ruido3(d.x + off * 2f + 7f, d.y + off * 2f + 7f, d.z + off * 2f + 7f);
                    Color c = Color.Lerp(colorNebulosaA, colorNebulosaB, mezcla) * (n * n * intensidadNebulosa);

                    int idx = y * size + x;
                    Color p = caras[f][idx];
                    caras[f][idx] = new Color(p.r + c.r, p.g + c.g, p.b + c.b, 1f);
                }
            }
        }
    }

    private static float Ruido3(float x, float y, float z)
    {
        float ab = Mathf.PerlinNoise(x, y);
        float bc = Mathf.PerlinNoise(y, z);
        float ca = Mathf.PerlinNoise(z, x);
        return (ab + bc + ca) / 3f;
    }

    // Dirección -> (cara del cubo, u, v) con la convención estándar de cubemaps
    private static void DirACara(Vector3 d, out int cara, out float u, out float v)
    {
        float ax = Mathf.Abs(d.x), ay = Mathf.Abs(d.y), az = Mathf.Abs(d.z);
        float sc, tc, ma;

        if (ax >= ay && ax >= az)
        {
            ma = ax;
            if (d.x > 0f) { cara = 0; sc = -d.z; tc = -d.y; }
            else          { cara = 1; sc =  d.z; tc = -d.y; }
        }
        else if (ay >= az)
        {
            ma = ay;
            if (d.y > 0f) { cara = 2; sc = d.x; tc =  d.z; }
            else          { cara = 3; sc = d.x; tc = -d.z; }
        }
        else
        {
            ma = az;
            if (d.z > 0f) { cara = 4; sc =  d.x; tc = -d.y; }
            else          { cara = 5; sc = -d.x; tc = -d.y; }
        }

        u = (sc / ma + 1f) * 0.5f;
        v = (tc / ma + 1f) * 0.5f;
    }

    // (cara, u, v) -> dirección (inversa de la anterior)
    private static Vector3 CaraADir(int cara, float u, float v)
    {
        float sc = u * 2f - 1f;
        float tc = v * 2f - 1f;
        Vector3 d;
        switch (cara)
        {
            case 0: d = new Vector3(1f, -tc, -sc); break;
            case 1: d = new Vector3(-1f, -tc, sc); break;
            case 2: d = new Vector3(sc, 1f, tc); break;
            case 3: d = new Vector3(sc, -1f, -tc); break;
            case 4: d = new Vector3(sc, -tc, 1f); break;
            default: d = new Vector3(-sc, -tc, -1f); break;
        }
        return d.normalized;
    }
}
