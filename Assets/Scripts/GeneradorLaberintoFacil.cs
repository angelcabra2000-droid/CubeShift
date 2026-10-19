using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Genera las paredes del laberinto del NIVEL FÁCIL a partir del diseño 2D
/// (vista cenital). Cada pared es un rectángulo medido en píxeles sobre la
/// imagen original (1024x1024) y se convierte a coordenadas locales del Mundo.
///
/// Colocar sobre el GameObject "Mundo" (o sobre un hijo suyo). Las paredes se
/// crean como hijas de un objeto "Laberinto", así rotan junto con el Mundo.
///
/// Uso: clic derecho sobre el título del componente > "Generar laberinto"
/// (funciona en modo edición, queda guardado en la escena) o activar
/// 'generarAlIniciar'. "Limpiar laberinto" lo borra.
///
/// Orientación: la parte de ARRIBA de la imagen = +Z (lejos de la cámara de
/// la vista 1 de CamaraVertices), la DERECHA de la imagen = +X.
/// </summary>
public class GeneradorLaberintoFacil : MonoBehaviour
{
    private enum Tipo { Bajo, Medio, Alto }

    private struct Muro
    {
        public float x1, y1, x2, y2; // píxeles de la imagen
        public Tipo tipo;
        public bool exterior;
        public Muro(float x1, float y1, float x2, float y2, Tipo tipo, bool exterior = false)
        { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; this.tipo = tipo; this.exterior = exterior; }
    }

    [Header("Dimensiones")]
    [Tooltip("Lado (en unidades locales del Mundo) del cuadrado que ocupa el laberinto, medido de borde exterior a borde exterior.")]
    [SerializeField] private float tamanoLaberinto = 30f;

    [Tooltip("Altura local del piso (donde se apoyan las paredes). Ajustala a la cara interior del piso de tu cubo.")]
    [SerializeField] private float alturaPiso = 0f;

    [Header("Alturas")]
    [SerializeField] private float alturaBaja = 1f;
    [SerializeField] private float alturaMedia = 2.5f;
    [Tooltip("No pases la altura interior del cubo.")]
    [SerializeField] private float alturaAlta = 5f;

    [Header("Opciones")]
    [Tooltip("Si tu cubo ya tiene sus propias paredes exteriores, desactivá esto.")]
    [SerializeField] private bool incluirMuroExterior = true;

    [Tooltip("Layer 'Paredes' (créalo en Tags and Layers). Lo usan todos tus scripts.")]
    [SerializeField] private string nombreLayer = "Paredes";

    [SerializeField] private bool generarAlIniciar = false;

    [Header("Materiales (opcionales)")]
    [SerializeField] private Material materialAlto;
    [SerializeField] private Material materialMedio;
    [SerializeField] private Material materialBajo;

    // Medidas del recuadro exterior en la imagen original.
    private const float X_MIN = 123f, X_MAX = 901f, Y_MIN = 82f, Y_MAX = 901f;

    private static readonly Muro[] muros = new Muro[]
    {
        // --- Perímetro (azul). Huecos: START arriba-izq (x144-185), EXIT abajo-der (y819-880) ---
        new Muro(123,  82, 144, 901, Tipo.Alto, true),   // izquierda
        new Muro(185,  82, 901, 102, Tipo.Alto, true),   // arriba
        new Muro(880,  82, 901, 819, Tipo.Alto, true),   // derecha
        new Muro(144, 880, 880, 901, Tipo.Alto, true),   // abajo

        // --- Azules interiores (altas) ---
        new Muro(491, 102, 532, 164, Tipo.Alto),         // tope superior centro
        new Muro(491, 245, 532, 348, Tipo.Alto),         // columna central
        new Muro(470, 348, 553, 368, Tipo.Alto),         // base en T
        new Muro(347, 348, 451, 368, Tipo.Alto),         // corchete izq, horizontal
        new Muro(347, 368, 368, 491, Tipo.Alto),         // corchete izq, vertical
        new Muro(347, 532, 368, 656, Tipo.Alto),         // izq inferior, vertical
        new Muro(347, 656, 410, 676, Tipo.Alto),         // izq inferior, pie
        new Muro(573, 348, 655, 368, Tipo.Alto),         // corchete der, horizontal
        new Muro(655, 348, 676, 676, Tipo.Alto),         // corchete der, vertical
        new Muro(614, 656, 655, 676, Tipo.Alto),         // der inferior, pie
        new Muro(491, 676, 532, 778, Tipo.Alto),         // columna inferior centro
        new Muro(532, 656, 553, 676, Tipo.Alto),         // aleta derecha de la columna inferior
        new Muro(491, 656, 532, 676, Tipo.Alto),         // aleta (parte superior columna inferior)

        // --- Rojas (medias) ---
        new Muro(328, 143, 410, 164, Tipo.Medio),
        new Muro(614, 143, 696, 164, Tipo.Medio),
        new Muro(246, 245, 430, 266, Tipo.Medio),        // L izq, horizontal
        new Muro(246, 266, 266, 368, Tipo.Medio),        // L izq, vertical
        new Muro(594, 245, 778, 266, Tipo.Medio),        // L der, horizontal
        new Muro(757, 266, 778, 451, Tipo.Medio),        // L der, vertical
        new Muro(144, 594, 246, 614, Tipo.Medio),        // T izq, horizontal
        new Muro(246, 573, 266, 655, Tipo.Medio),        // T izq, vertical
        new Muro(778, 594, 880, 614, Tipo.Medio),        // T der, horizontal
        new Muro(757, 573, 778, 614, Tipo.Medio),        // T der, vertical
        new Muro(328, 676, 369, 696, Tipo.Medio),
        new Muro(655, 676, 717, 696, Tipo.Medio),
        new Muro(369, 758, 410, 778, Tipo.Medio),

        // --- Amarillas (bajas) ---
        new Muro(144, 348, 205, 368, Tipo.Bajo),
        new Muro(819, 348, 880, 368, Tipo.Bajo),
        new Muro(246, 430, 287, 451, Tipo.Bajo),
        new Muro(246, 696, 266, 778, Tipo.Bajo),         // L izq, vertical
        new Muro(266, 778, 328, 798, Tipo.Bajo),         // L izq, horizontal
        new Muro(757, 696, 778, 778, Tipo.Bajo),         // L der, vertical
        new Muro(696, 778, 778, 798, Tipo.Bajo),         // L der, horizontal
        new Muro(614, 778, 656, 798, Tipo.Bajo),
    };

    private const string NOMBRE_CONTENEDOR = "Laberinto";

    private void Start()
    {
        if (generarAlIniciar && Application.isPlaying) Generar();
    }

    [ContextMenu("Generar laberinto")]
    public void Generar()
    {
        Limpiar();

        int layer = LayerMask.NameToLayer(nombreLayer);
        if (layer < 0)
            Debug.LogWarning($"[GeneradorLaberintoFacil] No existe el layer '{nombreLayer}'. Créalo en Tags and Layers; por ahora las paredes quedan en Default.", this);

        Transform contenedor = new GameObject(NOMBRE_CONTENEDOR).transform;
        contenedor.SetParent(transform, false);

        float anchoPx = X_MAX - X_MIN;
        float altoPx = Y_MAX - Y_MIN;
        float escalaX = tamanoLaberinto / anchoPx;   // por separado: el diseño es cuadrado,
        float escalaZ = tamanoLaberinto / altoPx;    // aunque en píxeles mida unos px de diferencia
        float cx = (X_MIN + X_MAX) * 0.5f;
        float cy = (Y_MIN + Y_MAX) * 0.5f;

        int n = 0;
        foreach (Muro m in muros)
        {
            if (m.exterior && !incluirMuroExterior) continue;

            float h = m.tipo == Tipo.Alto ? alturaAlta : m.tipo == Tipo.Medio ? alturaMedia : alturaBaja;

            float ancho = (m.x2 - m.x1) * escalaX;
            float profundo = (m.y2 - m.y1) * escalaZ;
            float px = ((m.x1 + m.x2) * 0.5f - cx) * escalaX;
            float pz = (cy - (m.y1 + m.y2) * 0.5f) * escalaZ;   // eje Y de la imagen va hacia abajo

            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube); // ya trae BoxCollider + Renderer
            go.name = $"Pared_{m.tipo}_{n++}";
            if (layer >= 0) go.layer = layer;

            Transform t = go.transform;
            t.SetParent(contenedor, false);
            t.localScale = new Vector3(ancho, h, profundo);
            t.localPosition = new Vector3(px, alturaPiso + h * 0.5f, pz);

            Material mat = m.tipo == Tipo.Alto ? materialAlto : m.tipo == Tipo.Medio ? materialMedio : materialBajo;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        // Posiciones útiles para colocar al Jugador y una meta (coordenadas locales del Mundo).
        Debug.Log($"[GeneradorLaberintoFacil] {n} paredes creadas. " +
                  $"START (local): {PxALocal(164, 120, escalaX, escalaZ, cx, cy)}  |  " +
                  $"EXIT (local): {PxALocal(890, 850, escalaX, escalaZ, cx, cy)}  (sumá alturaPiso + media cápsula en Y).", this);
    }

    [ContextMenu("Limpiar laberinto")]
    public void Limpiar()
    {
        Transform viejo = transform.Find(NOMBRE_CONTENEDOR);
        if (viejo == null) return;
        if (Application.isPlaying) Destroy(viejo.gameObject);
        else DestroyImmediate(viejo.gameObject);
    }
    /// <summary>Punto del mundo (ya con la escala/rotación de Mundo) sobre el piso, dado un píxel del diseño.</summary>
    public Vector3 PuntoEnPiso(float px, float py)
    {
        float ex = tamanoLaberinto / (X_MAX - X_MIN);
        float ez = tamanoLaberinto / (Y_MAX - Y_MIN);
        float cx = (X_MIN + X_MAX) * 0.5f, cy = (Y_MIN + Y_MAX) * 0.5f;
        return transform.TransformPoint(new Vector3((px - cx) * ex, alturaPiso, (cy - py) * ez));
    }

    public Vector3 PuntoInicio() => PuntoEnPiso(164, 125);   // esquina START (arriba-izquierda)
    public Vector3 PuntoSalida() => PuntoEnPiso(850, 850);   // esquina EXIT (abajo-derecha)

    private static Vector3 PxALocal(float px, float py, float ex, float ez, float cx, float cy)
        => new Vector3((px - cx) * ex, 0f, (cy - py) * ez);
}
