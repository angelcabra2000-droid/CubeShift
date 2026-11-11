using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Fin del juego: cuando el Jugador encuentra (toca) la pelota, el juego se
/// detiene y aparece un mensaje en pantalla. Con la tecla R se reinicia la escena.
///
/// El radio de la pelota se lee de BallSimulator (el mismo que usa la física),
/// así que ambos scripts siempre coinciden aunque cambies el tamaño o desactives
/// "Radio Desde Escala".
///
/// --- CONFIGURACIÓN ---
/// Colocar sobre el objeto "Setup" (o cualquier objeto de la escena) y asignar:
///   Jugador : el CapsuleCollider del Player
///   Pelota  : el Transform de la Pelota (la de la ESCENA, no el prefab)
///
/// Para que R funcione, la escena debe estar en File > Build Profiles
/// (Scene List). Si no lo está, el juego igual termina, pero R solo avisa
/// en la Console.
/// </summary>
public class MetaPelota : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CapsuleCollider jugador;
    [SerializeField] private Transform pelota;

    [Header("Detección")]
    [Tooltip("Distancia extra (además de que se toquen) para considerar que la encontró.")]
    [SerializeField] private float margen = 0.15f;

    [Header("Mensaje")]
    [SerializeField] private string mensaje = "¡Encontraste la pelota!";

    public bool Terminado { get; private set; }
    public event System.Action OnPelotaEncontrada;

    private BallSimulator simuladorPelota;
    private float radioPelota = 0.5f;   // respaldo si la pelota no tiene BallSimulator

    private void Start()
    {
        if (jugador == null) Debug.LogError("[MetaPelota] Falta asignar 'Jugador' (CapsuleCollider del Player).", this);
        if (pelota == null) Debug.LogError("[MetaPelota] Falta asignar 'Pelota'.", this);

        if (pelota != null)
        {
            simuladorPelota = pelota.GetComponent<BallSimulator>();

            Vector3 s = pelota.lossyScale;
            radioPelota = 0.5f * Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }
    }

    private void Update()
    {
        if (Terminado)
        {
            if (Input.GetKeyDown(KeyCode.R)) Reiniciar();
            return;
        }

        if (jugador == null || pelota == null) return;

        if (PelotaTocaJugador()) Terminar();
    }

    // Esfera (pelota) vs cápsula (jugador): distancia al segmento central de la cápsula.
    private bool PelotaTocaJugador()
    {
        Transform pt = jugador.transform;
        Vector3 ls = pt.lossyScale;
        Vector3 eje;
        float radioJ, escalaLargo;

        switch (jugador.direction)
        {
            case 0: eje = pt.right; radioJ = jugador.radius * Mathf.Max(ls.y, ls.z); escalaLargo = ls.x; break;
            case 2: eje = pt.forward; radioJ = jugador.radius * Mathf.Max(ls.x, ls.y); escalaLargo = ls.z; break;
            default: eje = pt.up; radioJ = jugador.radius * Mathf.Max(ls.x, ls.z); escalaLargo = ls.y; break;
        }

        Vector3 centro = pt.TransformPoint(jugador.center);
        float mitad = Mathf.Max(0f, jugador.height * escalaLargo * 0.5f - radioJ);
        Vector3 a = centro - eje * mitad;
        Vector3 b = centro + eje * mitad;

        Vector3 ab = b - a;
        float t = ab.sqrMagnitude > 1e-8f
            ? Mathf.Clamp01(Vector3.Dot(pelota.position - a, ab) / ab.sqrMagnitude)
            : 0f;
        Vector3 masCercano = a + ab * t;

        // Mismo radio que usa la física de la pelota
        float radioP = simuladorPelota != null ? simuladorPelota.Radius : radioPelota;

        float distancia = Vector3.Distance(pelota.position, masCercano);
        return distancia <= radioJ + radioP + margen;
    }

    private void Terminar()
    {
        Terminado = true;
        Time.timeScale = 0f;   // congela movimiento, física de la bola y rotaciones
        OnPelotaEncontrada?.Invoke();
        Debug.Log("[MetaPelota] Pelota encontrada: fin del juego.");
    }

    private void Reiniciar()
    {
        Time.timeScale = 1f;
        Scene escena = SceneManager.GetActiveScene();

        if (escena.buildIndex >= 0)
            SceneManager.LoadScene(escena.buildIndex);
        else
            Debug.LogWarning("[MetaPelota] La escena no está en Build Profiles (Scene List); no puedo reiniciarla. Agrégala y vuelve a probar.");
    }

    private void OnDestroy()
    {
        // Por si se sale del Play con el juego congelado.
        Time.timeScale = 1f;
    }

    private void OnGUI()
    {
        if (!Terminado) return;

        var estilo = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 36,
            fontStyle = FontStyle.Bold
        };
        estilo.normal.textColor = Color.white;

        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none);
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 60), mensaje, estilo);

        estilo.fontSize = 20;
        estilo.fontStyle = FontStyle.Normal;
        GUI.Label(new Rect(0, Screen.height * 0.35f + 70, Screen.width, 40), "Presiona R para reiniciar", estilo);
    }
}