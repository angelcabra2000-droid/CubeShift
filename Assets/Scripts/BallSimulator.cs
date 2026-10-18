using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelota con física propia (sin Rigidbody) dentro del Mundo que rota.
///
/// NO necesita WorldRotator: lee directamente la pose (posición/rotación)
/// del Transform "Mundo", que es la que mueve RotadorMundo. Así RotadorMundo,
/// MovimientoJugador y el resto de scripts quedan exactamente como están.
///
/// Se simula en LateUpdate (después de que RotadorMundo y MovimientoJugador
/// ya movieron al Mundo y al Jugador en este frame) y cada frame se divide en
/// subpasos. Dentro del frame se interpolan la pose del Mundo y la posición del
/// Jugador, y de esa interpolación sale la VELOCIDAD de cada pared (para que
/// la fricción y el rebote contra paredes en movimiento sean correctos).
///
/// Colocar sobre la esfera. La esfera NO debe ser hija del Mundo, NO debe
/// tener Rigidbody y NO debe estar en el Layer "Paredes".
/// </summary>
public class BallSimulator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El GameObject 'Mundo' (el que tiene RotadorMundo).")]
    [SerializeField] private Transform mundo;
    [Tooltip("CapsuleCollider del Jugador (se usa como cuerpo con el que choca la pelota).")]
    [SerializeField] private CapsuleCollider player;

    [Header("Cuerpo")]
    [SerializeField] private float mass = 1f;
    [Tooltip("Si está activo, el radio se toma de la escala de la esfera (escala/2).")]
    [SerializeField] private bool radioDesdeEscala = true;
    [SerializeField] private float radius = 0.5f;

    [Header("Ambiente")]
    [SerializeField] private float gravity = 20f;
    [SerializeField] private bool useDrag = true;
    [SerializeField] private float airDensity = 1.2f;
    [SerializeField] private float dragCoefficient = 0.47f;

    [Header("Contacto")]
    [SerializeField, Range(0f, 1f)] private float restitution = 0.5f;
    [SerializeField] private float restitutionThreshold = 1f;   // bajo esta v_n no hay rebote
    [SerializeField] private float muStatic = 0.6f;
    [SerializeField] private float muKinetic = 0.4f;
    [SerializeField] private float rollingDamping = 0.3f;       // 1/s, resistencia a la rodadura
    [SerializeField] private float positionSlop = 0.001f;

    [Header("Integración")]
    [Tooltip("Tiempo máximo de cada subpaso (s).")]
    [SerializeField] private float maxPasoTiempo = 0.005f;
    [Tooltip("Máximo desplazamiento relativo por subpaso, en radios de la pelota. Evita atravesar paredes delgadas.")]
    [SerializeField] private float maxMovPorPasoRadios = 0.25f;
    [SerializeField] private int maxSubpasos = 300;
    [SerializeField] private int solverIterations = 4;
    [Tooltip("Si un frame dura más que esto, se simula solo este tiempo (evita saltos tras una pausa).")]
    [SerializeField] private float maxDtFrame = 0.05f;

    // Estado
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    private Vector3 pos;
    private Quaternion orientation = Quaternion.identity;

    // Cajas en el marco RÍGIDO del Mundo (posición/rotación del Mundo, sin escala)
    private struct Box { public Vector3 center; public Quaternion rot; public Vector3 half; }
    private struct Contact { public Vector3 n, point, surfaceVel; public float depth; public bool wall; }

    private readonly List<Box> boxes = new();
    private readonly List<Vector3> corners = new();
    private readonly List<Contact> contacts = new();

    // Pose del Mundo en el subpaso actual y en el anterior
    private Vector3 wPos, wPosPrev;
    private Quaternion wRot = Quaternion.identity, wRotPrev = Quaternion.identity;
    // Pose al final del frame anterior
    private Vector3 prevFrameWPos, prevFramePPos;
    private Quaternion prevFrameWRot = Quaternion.identity;

    private Vector3 playerOffset;   // cuánto falta para llegar a la posición final del Jugador en este frame
    private Vector3 playerVel;
    private float stepH = 0.005f;
    private bool grounded;

    void Start()
    {
        if (mundo == null)
        {
            Debug.LogError("[BallSimulator] Falta asignar 'Mundo' en el Inspector.", this);
            enabled = false;
            return;
        }

        pos = transform.position;
        orientation = transform.rotation;

        if (radioDesdeEscala)
        {
            Vector3 s = transform.lossyScale;
            radius = 0.5f * Mathf.Max(s.x, Mathf.Max(s.y, s.z));
        }

        Vector3 wp = mundo.position;
        Quaternion wr = mundo.rotation;
        Quaternion wrInv = Quaternion.Inverse(wr);

        int nCajas = 0, nAprox = 0;
        foreach (var col in mundo.GetComponentsInChildren<Collider>())
        {
            // Los colliders con Is Trigger TAMBIÉN cuentan como pared: en este proyecto
            // el sensor y el jugador ya detectan las paredes con raycasts, así que es
            // normal que sean triggers.
            if (col == player) continue;
            if (col.transform.IsChildOf(transform)) continue;     // colliders de la propia pelota

            if (col is BoxCollider bc)
            {
                Vector3 cW = bc.transform.TransformPoint(bc.center);
                Vector3 half = Vector3.Scale(bc.size, bc.transform.lossyScale) * 0.5f;
                half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
                AddBox(wrInv * (cW - wp), wrInv * bc.transform.rotation, half);
                nCajas++;
            }
            else
            {
                // Otro tipo de collider (Plane, Quad, Mesh...): se aproxima con su caja envolvente.
                Bounds b = col.bounds;
                AddBox(wrInv * (b.center - wp), wrInv, b.extents);
                nAprox++;
                Debug.LogWarning($"[BallSimulator] '{col.name}' no es BoxCollider ({col.GetType().Name}); se aproxima con su caja envolvente. Mejor usa un Cube con BoxCollider.", col);
            }
        }

        Debug.Log($"[BallSimulator] Paredes cargadas: {nCajas} BoxCollider + {nAprox} aproximadas. Radio = {radius:F3}");
        if (nCajas + nAprox == 0)
            Debug.LogError("[BallSimulator] No encontré NINGÚN collider hijo de 'Mundo'. La pelota atravesará todo.", this);

        prevFrameWPos = wp;
        prevFrameWRot = wr;
        prevFramePPos = player != null ? player.transform.position : Vector3.zero;
    }

    void AddBox(Vector3 center, Quaternion rot, Vector3 half)
    {
        boxes.Add(new Box { center = center, rot = rot, half = half });
        for (int sx = -1; sx <= 1; sx += 2)
            for (int sy = -1; sy <= 1; sy += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    corners.Add(center + rot * Vector3.Scale(half, new Vector3(sx, sy, sz)));
    }

    // LateUpdate: ya se movieron el Mundo (RotadorMundo) y el Jugador (MovimientoJugador) en este frame.
    void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDtFrame);
        if (dt <= 1e-6f) return;   // juego en pausa

        Vector3 wPos1 = mundo.position;
        Quaternion wRot1 = mundo.rotation;
        Vector3 pPos1 = player != null ? player.transform.position : Vector3.zero;

        // Cuánto se movió la pared que más se movió + la pelota + el jugador -> cuántos subpasos hacen falta
        float movMundo = MaxCornerDisplacement(prevFrameWPos, prevFrameWRot, wPos1, wRot1);
        Vector3 dPlayer = pPos1 - prevFramePPos;
        playerVel = dPlayer / dt;

        float movTotal = Velocity.magnitude * dt + movMundo + dPlayer.magnitude;
        int n = Mathf.CeilToInt(Mathf.Max(dt / maxPasoTiempo, movTotal / (radius * maxMovPorPasoRadios)));
        n = Mathf.Clamp(n, 1, maxSubpasos);
        float h = dt / n;
        stepH = h;

        wPos = prevFrameWPos;
        wRot = prevFrameWRot;

        for (int i = 1; i <= n; i++)
        {
            float s = (float)i / n;
            wPosPrev = wPos;
            wRotPrev = wRot;
            wPos = Vector3.Lerp(prevFrameWPos, wPos1, s);
            wRot = Quaternion.Slerp(prevFrameWRot, wRot1, s);
            playerOffset = Vector3.Lerp(prevFramePPos, pPos1, s) - pPos1;

            grounded = false;
            Integrate(h);            // gravedad + aire
            SolveContacts();         // normal + fricción (paredes y jugador)
            if (grounded)            // resistencia a la rodadura
                AngularVelocity *= Mathf.Max(0f, 1f - rollingDamping * h);
            IntegrateOrientation(h);
        }

        transform.SetPositionAndRotation(pos, orientation);

        prevFrameWPos = wPos1;
        prevFrameWRot = wRot1;
        prevFramePPos = pPos1;
    }

    float MaxCornerDisplacement(Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1)
    {
        float m = 0f;
        foreach (var L in corners)
        {
            float d = ((p1 + r1 * L) - (p0 + r0 * L)).magnitude;
            if (d > m) m = d;
        }
        return m;
    }

    // Velocidad de un punto del Mundo (mundial) en el subpaso actual
    Vector3 SurfaceVelocityAt(Vector3 p)
    {
        Vector3 L = Quaternion.Inverse(wRot) * (p - wPos);   // punto fijo al Mundo
        Vector3 antes = wPosPrev + wRotPrev * L;
        return (p - antes) / stepH;
    }

    // ---------- Integración (Euler semi-implícito) ----------
    void Integrate(float h)
    {
        Vector3 a = Vector3.down * gravity;
        if (useDrag)
        {
            float k = 0.5f * airDensity * dragCoefficient * Mathf.PI * radius * radius;
            a -= (k / mass) * Velocity.magnitude * Velocity;
        }
        Velocity += a * h;
        pos += Velocity * h;
    }

    void IntegrateOrientation(float h)
    {
        float w = AngularVelocity.magnitude;
        if (w > 1e-5f)
            orientation = Quaternion.AngleAxis(w * Mathf.Rad2Deg * h, AngularVelocity / w) * orientation;
    }

    // ---------- Detección ----------
    void GatherContacts(bool conParedes, bool conJugador)
    {
        contacts.Clear();

        // Paredes: esfera vs caja orientada (OBB), en el marco de cada caja
        if (conParedes)
            foreach (var b in boxes)
            {
                Quaternion rW = wRot * b.rot;
                Vector3 cW = wPos + wRot * b.center;
                Vector3 c = Quaternion.Inverse(rW) * (pos - cW);

                Vector3 q = new Vector3(
                    Mathf.Clamp(c.x, -b.half.x, b.half.x),
                    Mathf.Clamp(c.y, -b.half.y, b.half.y),
                    Mathf.Clamp(c.z, -b.half.z, b.half.z));
                Vector3 d = c - q;
                float dist2 = d.sqrMagnitude;
                if (dist2 > radius * radius) continue;

                Vector3 nBox; float depth;
                if (dist2 > 1e-10f)
                {
                    float dist = Mathf.Sqrt(dist2);
                    nBox = d / dist;
                    depth = radius - dist;
                }
                else
                {
                    // Centro dentro de la caja: salir por la cara más cercana
                    Vector3 lo = c + b.half, hi = b.half - c;
                    float m = lo.x; nBox = Vector3.left;
                    if (hi.x < m) { m = hi.x; nBox = Vector3.right; }
                    if (lo.y < m) { m = lo.y; nBox = Vector3.down; }
                    if (hi.y < m) { m = hi.y; nBox = Vector3.up; }
                    if (lo.z < m) { m = lo.z; nBox = Vector3.back; }
                    if (hi.z < m) { m = hi.z; nBox = Vector3.forward; }
                    depth = radius + m;
                }

                Vector3 n = rW * nBox;
                Vector3 p = pos - n * radius;
                contacts.Add(new Contact
                {
                    n = n,
                    point = p,
                    depth = depth,
                    surfaceVel = SurfaceVelocityAt(p),
                    wall = true
                });
            }

        // Jugador: esfera vs cápsula. El jugador se mueve (MovimientoJugador), así que
        // su velocidad se transfiere a la pelota al empujarla.
        if (conJugador && player != null)
        {
            Transform pt = player.transform;
            Vector3 ls = pt.lossyScale;
            Vector3 axis; float pr, lenScale;
            switch (player.direction)
            {
                case 0: axis = pt.right; pr = player.radius * Mathf.Max(ls.y, ls.z); lenScale = ls.x; break;
                case 2: axis = pt.forward; pr = player.radius * Mathf.Max(ls.x, ls.y); lenScale = ls.z; break;
                default: axis = pt.up; pr = player.radius * Mathf.Max(ls.x, ls.z); lenScale = ls.y; break;
            }

            Vector3 center = pt.TransformPoint(player.center) + playerOffset;
            float half = Mathf.Max(0f, player.height * lenScale * 0.5f - pr);
            Vector3 a = center - axis * half, bPt = center + axis * half;

            Vector3 ab = bPt - a;
            float tt = ab.sqrMagnitude > 1e-8f ? Mathf.Clamp01(Vector3.Dot(pos - a, ab) / ab.sqrMagnitude) : 0f;
            Vector3 q = a + ab * tt;
            Vector3 d = pos - q;
            float dist = d.magnitude, sum = radius + pr;
            if (dist < sum && dist > 1e-6f)
            {
                Vector3 n = d / dist;
                contacts.Add(new Contact
                {
                    n = n,
                    point = pos - n * radius,
                    depth = sum - dist,
                    surfaceVel = playerVel
                });
            }
        }
    }

    // ---------- Respuesta ----------
    void SolveContacts()
    {
        // Máximo que el jugador puede empujar la pelota POR POSICIÓN en cada iteración.
        // Así, si el jugador la aprieta contra una pared (el hueco entre él y la pared
        // es menor que el diámetro de la pelota), nunca la clava tan hondo en la pared
        // como para sacarla por el otro lado.
        float maxPushJugador = radius * 0.1f;

        for (int it = 0; it < solverIterations; it++)
        {
            // 1) Jugador (contacto recalculado con la posición actual)
            GatherContacts(false, true);
            foreach (var c in contacts) Resolve(c, maxPushJugador);

            // 2) Paredes (contactos recalculados DESPUÉS de empujar el jugador):
            //    la pared siempre tiene la última palabra.
            GatherContacts(true, false);
            foreach (var c in contacts) Resolve(c, float.MaxValue);
        }
    }

    void Resolve(Contact c, float maxPush)
    {
        // Corrección de penetración (limitada por maxPush)
        pos += c.n * Mathf.Min(Mathf.Max(c.depth - positionSlop, 0f), maxPush);
        grounded = true;

        Vector3 rc = -c.n * radius;
        Vector3 vRel = Velocity + Vector3.Cross(AngularVelocity, rc) - c.surfaceVel;
        float vn = Vector3.Dot(vRel, c.n);
        if (vn >= 0f) return;                       // ya se separan

        float I = 0.4f * mass * radius * radius;    // esfera sólida
        float e = (-vn > restitutionThreshold) ? restitution : 0f;
        float jn = -(1f + e) * vn * mass;           // impulso normal

        Vector3 J = jn * c.n;

        Vector3 vt = vRel - vn * c.n;
        float vtMag = vt.magnitude;
        if (vtMag > 1e-5f)
        {
            Vector3 t = vt / vtMag;
            float jStick = vtMag * mass * 2f / 7f;  // anula el deslizamiento
            float jt = (jStick <= muStatic * jn) ? jStick : muKinetic * jn;
            J -= jt * t;
        }

        Velocity += J / mass;
        AngularVelocity += Vector3.Cross(rc, J) / I;
    }

    // API para impulsos externos (power-ups, empujones, etc.)
    public void AddImpulse(Vector3 impulse) => Velocity += impulse / mass;
    public void AddForce(Vector3 force, float dt) => Velocity += force / mass * dt;
}