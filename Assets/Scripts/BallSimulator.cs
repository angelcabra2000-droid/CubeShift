using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pelota con física propia (sin Rigidbody) dentro del Mundo que rota.
///
/// Se simula en LateUpdate (después de RotadorMundo y MovimientoJugador) y cada
/// frame se divide en subpasos. Dentro del frame se interpolan la pose del Mundo
/// y la posición del Jugador; de esa interpolación sale la VELOCIDAD de cada punto
/// de pared (v = ω × r + v_traslación), que es lo que hace que la caja "empuje"
/// y "arrastre" a la pelota: rebote y fricción se calculan con la velocidad
/// RELATIVA pelota-pared.
///
/// Colocar sobre la esfera. La esfera NO debe ser hija del Mundo, NO debe
/// tener Rigidbody y NO debe estar en el Layer "Paredes".
///
/// Materiales: cada pared puede tener su Physic Material (fricción estática,
/// dinámica y rebote). Si no tiene, se usan los valores "Paredes sin Physic
/// Material". Mezcla: fricción = media geométrica (pelota, pared);
/// rebote = producto (pelota * pared).
/// </summary>
public class BallSimulator : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El GameObject 'Mundo' (el que tiene RotadorMundo).")]
    [SerializeField] private Transform mundo;
    [Tooltip("CapsuleCollider del Jugador (se usa como cuerpo con el que choca la pelota).")]
    [SerializeField] private CapsuleCollider player;

    [Header("Cuerpo")]
    [Tooltip("Si está activo, el radio se toma de la escala de la esfera (escala/2).")]
    [SerializeField] private bool radioDesdeEscala = true;
    [SerializeField] private float radius = 0.5f;
    [Tooltip("Densidad en kg/m³. La masa sale de aquí (esfera sólida). Goma ~1100, madera ~700, pelota hueca ~100. " +
             "La masa solo afecta a la resistencia del aire y a AddImpulse; el rebote/fricción no dependen de ella.")]
    [SerializeField] private float density = 1100f;

    [Header("Ambiente")]
    [Tooltip("Con la pelota a escala de juego (radio 0.5) un valor de 15-25 'se siente' mejor que 9.81.")]
    [SerializeField] private float gravity = 20f;
    [SerializeField] private bool useDrag = true;
    [SerializeField] private float airDensity = 1.2f;
    [SerializeField] private float dragCoefficient = 0.47f;
    [Tooltip("Amortiguación del giro por el aire (1/s). Muy pequeña.")]
    [SerializeField] private float airSpinDamping = 0.02f;

    [Header("Pelota (material)")]
    [SerializeField, Range(0f, 1f)] private float restitution = 0.7f;
    [Tooltip("Bajo esta velocidad normal de impacto no hay rebote (evita temblor en reposo).")]
    [SerializeField] private float restitutionThreshold = 1f;
    [SerializeField] private float muStatic = 0.6f;
    [SerializeField] private float muKinetic = 0.4f;
    [Tooltip("Coeficiente de resistencia a la rodadura (adimensional). Goma sobre piso duro: 0.01-0.05.")]
    [SerializeField] private float rollingResistance = 0.02f;
    [SerializeField] private float positionSlop = 0.001f;

    [Header("Paredes sin Physic Material")]
    [Tooltip("Se multiplica por el rebote de la pelota. 1 = la pared no resta rebote.")]
    [SerializeField, Range(0f, 1f)] private float wallRestitution = 1f;
    [SerializeField] private float wallMuStatic = 0.6f;
    [SerializeField] private float wallMuKinetic = 0.4f;

    [Header("Integración")]
    [Tooltip("Tiempo máximo de cada subpaso (s).")]
    [SerializeField] private float maxPasoTiempo = 0.005f;
    [Tooltip("Máximo desplazamiento relativo por subpaso, en radios de la pelota. Evita atravesar paredes delgadas.")]
    [SerializeField] private float maxMovPorPasoRadios = 0.25f;
    [SerializeField] private int maxSubpasos = 300;
    [SerializeField] private int solverIterations = 4;
    [Tooltip("Si un frame dura más que esto, se simula solo este tiempo (evita saltos tras una pausa).")]
    [SerializeField] private float maxDtFrame = 0.05f;
    [Tooltip("Tope de seguridad de velocidad (u/s). Una pared lejos del jugador puede moverse muy rápido al rotar.")]
    [SerializeField] private float maxSpeed = 100f;

    [Header("Reposo (sleep)")]
    [SerializeField] private float sleepSpeed = 0.05f;
    [SerializeField] private float sleepSpin = 0.1f;

    [Header("Debug")]
    [SerializeField] private bool mostrarDebug = false;

    // Estado
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    private Vector3 pos;
    private Quaternion orientation = Quaternion.identity;
    private float mass = 1f;

    public float Mass => mass;
    /// <summary>Radio real que usa la física (lo lee MetaPelota para detectar el contacto con el jugador).</summary>
    public float Radius => radius;
    public float Speed => Velocity.magnitude;
    private float Inertia => 0.4f * mass * radius * radius;   // esfera sólida
    public float KineticEnergy => 0.5f * mass * Velocity.sqrMagnitude + 0.5f * Inertia * AngularVelocity.sqrMagnitude;

    // Cajas en el marco RÍGIDO del Mundo (posición/rotación del Mundo, sin escala)
    private struct Box
    {
        public Vector3 center; public Quaternion rot; public Vector3 half;
        public float boundR;                    // radio de la esfera que envuelve la caja
        public float muS, muK, e;               // material de la pared
    }
    private struct Contact
    {
        public Vector3 n, point, surfaceVel; public float depth;
        public float muS, muK, e;
    }

    private readonly List<Box> boxes = new();
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
    private float stepNormalImpulse;   // impulso normal acumulado en el subpaso (para la rodadura)

    // Solo para debug
    private int ultimosSubpasos;
    private float ultimaVelMundo;

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

        mass = Mathf.Max(1e-4f, density * (4f / 3f) * Mathf.PI * radius * radius * radius);

        Vector3 wp = mundo.position;
        Quaternion wr = mundo.rotation;
        Quaternion wrInv = Quaternion.Inverse(wr);

        int nCajas = 0, nAprox = 0;
        foreach (var col in mundo.GetComponentsInChildren<Collider>())
        {
            // Los colliders con Is Trigger TAMBIÉN cuentan como pared (en este proyecto
            // el sensor y el jugador detectan las paredes con raycasts, así que es normal
            // que sean triggers). Ojo: cualquier trigger hijo del Mundo (meta, zonas...)
            // también se vuelve sólido para la pelota.
            if (col == player) continue;
            if (col.transform.IsChildOf(transform)) continue;     // colliders de la propia pelota

            LeerMaterial(col, out float mS, out float mK, out float e);

            if (col is BoxCollider bc)
            {
                Vector3 cW = bc.transform.TransformPoint(bc.center);
                Vector3 half = Vector3.Scale(bc.size, bc.transform.lossyScale) * 0.5f;
                half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
                AddBox(wrInv * (cW - wp), wrInv * bc.transform.rotation, half, mS, mK, e);
                nCajas++;
            }
            else
            {
                // Otro tipo de collider (Plane, Quad, Mesh...): se aproxima con su caja envolvente.
                Bounds b = col.bounds;
                AddBox(wrInv * (b.center - wp), wrInv, b.extents, mS, mK, e);
                nAprox++;
                Debug.LogWarning($"[BallSimulator] '{col.name}' no es BoxCollider ({col.GetType().Name}); se aproxima con su caja envolvente. Mejor usa un Cube con BoxCollider.", col);
            }
        }

        float area = Mathf.PI * radius * radius;
        float vTerminal = useDrag ? Mathf.Sqrt(2f * mass * gravity / (airDensity * dragCoefficient * area)) : float.PositiveInfinity;
        Debug.Log($"[BallSimulator] Paredes: {nCajas} BoxCollider + {nAprox} aproximadas. Radio={radius:F3}, masa={mass:F2} kg, velocidad terminal={vTerminal:F1} u/s");
        if (nCajas + nAprox == 0)
            Debug.LogError("[BallSimulator] No encontré NINGÚN collider hijo de 'Mundo'. La pelota atravesará todo.", this);

        prevFrameWPos = wp;
        prevFrameWRot = wr;
        prevFramePPos = player != null ? player.transform.position : Vector3.zero;
    }

    void LeerMaterial(Collider col, out float mS, out float mK, out float e)
    {
        var m = col.sharedMaterial;   // PhysicMaterial (Unity <=2022) o PhysicsMaterial (Unity 6)
        if (m != null) { mS = m.staticFriction; mK = m.dynamicFriction; e = m.bounciness; }
        else { mS = wallMuStatic; mK = wallMuKinetic; e = wallRestitution; }
    }

    void AddBox(Vector3 center, Quaternion rot, Vector3 half, float mS, float mK, float e)
    {
        boxes.Add(new Box { center = center, rot = rot, half = half, boundR = half.magnitude, muS = mS, muK = mK, e = e });
    }

    static float MixFriccion(float a, float b) => Mathf.Sqrt(Mathf.Max(0f, a) * Mathf.Max(0f, b));

    // LateUpdate: ya se movieron el Mundo (RotadorMundo) y el Jugador (MovimientoJugador) en este frame.
    void LateUpdate()
    {
        float dt = Mathf.Min(Time.deltaTime, maxDtFrame);
        if (dt <= 1e-6f) return;   // juego en pausa

        Vector3 wPos1 = mundo.position;
        Quaternion wRot1 = mundo.rotation;
        Vector3 pPos1 = player != null ? player.transform.position : Vector3.zero;

        // Cuánto se movieron las paredes CERCANAS a la pelota + la pelota + el jugador -> cuántos subpasos hacen falta
        float alcance = Velocity.magnitude * dt;
        float movMundo = MaxDesplazamientoCercano(prevFrameWPos, prevFrameWRot, wPos1, wRot1, alcance);
        Vector3 dPlayer = pPos1 - prevFramePPos;
        playerVel = dPlayer / dt;

        float movTotal = alcance + movMundo + dPlayer.magnitude;
        int n = Mathf.CeilToInt(Mathf.Max(dt / maxPasoTiempo, movTotal / (radius * maxMovPorPasoRadios)));
        n = Mathf.Clamp(n, 1, maxSubpasos);
        float h = dt / n;
        stepH = h;
        ultimosSubpasos = n;
        ultimaVelMundo = movMundo / dt;

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
            stepNormalImpulse = 0f;
            Integrate(h);            // gravedad + aire
            SolveContacts();         // normal + fricción (paredes y jugador)
            AplicarRodadura();       // resistencia a la rodadura (proporcional a la fuerza normal)
            IntegrateOrientation(h);
        }

        // Reposo: si todo está quieto, se apaga el micro-movimiento residual.
        bool mundoQuieto = (wPos1 - prevFrameWPos).sqrMagnitude < 1e-10f && Quaternion.Angle(prevFrameWRot, wRot1) < 0.001f;
        if (grounded && mundoQuieto && dPlayer.sqrMagnitude < 1e-8f
            && Velocity.sqrMagnitude < sleepSpeed * sleepSpeed
            && AngularVelocity.sqrMagnitude < sleepSpin * sleepSpin)
        {
            Velocity = Vector3.zero;
            AngularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(pos, orientation);

        prevFrameWPos = wPos1;
        prevFrameWRot = wRot1;
        prevFramePPos = pPos1;
    }

    // Cota superior del desplazamiento de las cajas que podrían tocar a la pelota en este frame.
    // Desplazamiento de cualquier punto de la caja <= desplazamiento de su centro + boundR * cuerda(θ).
    float MaxDesplazamientoCercano(Vector3 p0, Quaternion r0, Vector3 p1, Quaternion r1, float alcanceBola)
    {
        float theta = Quaternion.Angle(r0, r1) * Mathf.Deg2Rad;
        float chord = 2f * Mathf.Sin(theta * 0.5f);
        float m = 0f;
        foreach (var b in boxes)
        {
            Vector3 c0 = p0 + r0 * b.center;
            Vector3 c1 = p1 + r1 * b.center;
            float d = (c1 - c0).magnitude + b.boundR * chord;
            float reach = b.boundR + radius + d + alcanceBola;
            float reach2 = reach * reach;
            if ((pos - c0).sqrMagnitude > reach2 && (pos - c1).sqrMagnitude > reach2) continue;
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

        if (Velocity.sqrMagnitude > maxSpeed * maxSpeed)
            Velocity = Velocity.normalized * maxSpeed;

        pos += Velocity * h;
    }

    void IntegrateOrientation(float h)
    {
        if (useDrag) AngularVelocity *= Mathf.Max(0f, 1f - airSpinDamping * h);

        float w = AngularVelocity.magnitude;
        if (w > 1e-5f)
            orientation = Quaternion.AngleAxis(w * Mathf.Rad2Deg * h, AngularVelocity / w) * orientation;
    }

    // Resistencia a la rodadura: par opuesto al giro, de magnitud Crr * N * r.
    // Con N = impulso normal del subpaso, así es 0 en el aire y crece con la fuerza de contacto.
    void AplicarRodadura()
    {
        if (stepNormalImpulse <= 0f || rollingResistance <= 0f) return;
        float w = AngularVelocity.magnitude;
        if (w < 1e-5f) return;
        float dw = rollingResistance * stepNormalImpulse * radius / Inertia;
        AngularVelocity *= Mathf.Max(0f, 1f - dw / w);
    }

    // ---------- Detección ----------
    void GatherContacts(bool conParedes, bool conJugador)
    {
        contacts.Clear();

        // Paredes: esfera vs caja orientada (OBB), en el marco de cada caja
        if (conParedes)
            foreach (var b in boxes)
            {
                Vector3 cW = wPos + wRot * b.center;

                // Descarte rápido: la esfera no llega a la esfera envolvente de la caja
                float reach = b.boundR + radius;
                if ((pos - cW).sqrMagnitude > reach * reach) continue;

                Quaternion rW = wRot * b.rot;
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
                    muS = b.muS,
                    muK = b.muK,
                    e = b.e
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
                    surfaceVel = playerVel,
                    muS = wallMuStatic,
                    muK = wallMuKinetic,
                    e = wallRestitution
                });
            }
        }
    }

    // ---------- Respuesta ----------
    void SolveContacts()
    {
        // Máximo que el jugador puede empujar la pelota POR POSICIÓN en cada iteración.
        // Así, si el jugador la aprieta contra una pared, nunca la clava tan hondo
        // en la pared como para sacarla por el otro lado.
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

        // Rebote: producto de los coeficientes (pelota * pared), solo sobre el umbral
        float e = (-vn > restitutionThreshold) ? restitution * c.e : 0f;
        float jn = -(1f + e) * vn * mass;           // impulso normal
        stepNormalImpulse += jn;

        Vector3 J = jn * c.n;

        // Fricción de Coulomb con la velocidad tangencial RELATIVA (incluye el giro de la caja)
        float muS = MixFriccion(muStatic, c.muS);
        float muK = Mathf.Min(MixFriccion(muKinetic, c.muK), muS);

        Vector3 vt = vRel - vn * c.n;
        float vtMag = vt.magnitude;
        if (vtMag > 1e-5f)
        {
            Vector3 t = vt / vtMag;
            float jStick = vtMag * mass * 2f / 7f;  // anula el deslizamiento (esfera sólida)
            float jt = (jStick <= muS * jn) ? jStick : muK * jn;
            J -= jt * t;
        }

        Velocity += J / mass;
        AngularVelocity += Vector3.Cross(rc, J) / Inertia;
    }

    // API para impulsos externos (power-ups, empujones, etc.)
    public void AddImpulse(Vector3 impulse) => Velocity += impulse / mass;
    public void AddForce(Vector3 force, float dt) => Velocity += force / mass * dt;

    void OnGUI()
    {
        if (!mostrarDebug) return;
        GUI.Label(new Rect(10, 10, 700, 24),
            $"v={Velocity.magnitude:F2} u/s   ω={AngularVelocity.magnitude:F2} rad/s   E={KineticEnergy:F1} J   " +
            $"subpasos={ultimosSubpasos}   v.pared.max={ultimaVelMundo:F1} u/s");
    }
}