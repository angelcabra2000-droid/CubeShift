using System.Collections.Generic;
using UnityEngine;

public class BallSimulator : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private WorldRotator world;
    [SerializeField] private CapsuleCollider player;      // solo se usa como dato geométrico

    [Header("Cuerpo")]
    [SerializeField] private float mass = 1f;
    [SerializeField] private float radius = 0.5f;         // = escala / 2 (esfera por defecto: 0.5)

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
    [SerializeField] private int substeps = 4;
    [SerializeField] private int solverIterations = 4;

    // Estado
    public Vector3 Velocity;
    public Vector3 AngularVelocity;
    private Vector3 pos;
    private Quaternion orientation = Quaternion.identity;

    private struct Box { public Vector3 min, max; }
    private struct Contact { public Vector3 n, point, surfaceVel; public float depth; }

    private readonly List<Box> boxes = new();
    private readonly List<Contact> contacts = new();
    private bool grounded;

    void Start()
    {
        pos = transform.position;
        orientation = transform.rotation;

        // Cajas de las paredes en el espacio LOCAL del Mundo.
        // Supone: escala 1 en el Mundo y BoxColliders sin rotar respecto a él.
        foreach (var bc in world.GetComponentsInChildren<BoxCollider>())
        {
            Vector3 c = world.transform.InverseTransformPoint(bc.transform.TransformPoint(bc.center));
            Vector3 half = Vector3.Scale(bc.size, bc.transform.lossyScale) * 0.5f;
            half = new Vector3(Mathf.Abs(half.x), Mathf.Abs(half.y), Mathf.Abs(half.z));
            boxes.Add(new Box { min = c - half, max = c + half });
        }
    }

    void FixedUpdate()
    {
        float h = Time.fixedDeltaTime / substeps;
        grounded = false;

        for (int i = 0; i < substeps; i++)
        {
            world.Advance(h);        // 1) mover las paredes
            Integrate(h);            // 2) gravedad + aire
            SolveContacts();         // 3) normal + fricción
            if (grounded)            // 4) resistencia a la rodadura
                AngularVelocity *= Mathf.Max(0f, 1f - rollingDamping * h);
            IntegrateOrientation(h);
        }

        transform.SetPositionAndRotation(pos, orientation);
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
    void GatherContacts()
    {
        contacts.Clear();

        // Paredes: esfera vs caja, en el espacio local del Mundo
        Vector3 c = world.transform.InverseTransformPoint(pos);
        foreach (var b in boxes)
        {
            Vector3 q = new Vector3(
                Mathf.Clamp(c.x, b.min.x, b.max.x),
                Mathf.Clamp(c.y, b.min.y, b.max.y),
                Mathf.Clamp(c.z, b.min.z, b.max.z));
            Vector3 d = c - q;
            float dist2 = d.sqrMagnitude;
            if (dist2 > radius * radius) continue;

            Vector3 nLocal; float depth;
            if (dist2 > 1e-10f)
            {
                float dist = Mathf.Sqrt(dist2);
                nLocal = d / dist;
                depth = radius - dist;
            }
            else
            {
                // Centro dentro de la caja: salir por la cara más cercana
                Vector3 lo = c - b.min, hi = b.max - c;
                float m = lo.x; nLocal = Vector3.left;
                if (hi.x < m) { m = hi.x; nLocal = Vector3.right; }
                if (lo.y < m) { m = lo.y; nLocal = Vector3.down; }
                if (hi.y < m) { m = hi.y; nLocal = Vector3.up; }
                if (lo.z < m) { m = lo.z; nLocal = Vector3.back; }
                if (hi.z < m) { m = hi.z; nLocal = Vector3.forward; }
                depth = radius + m;
            }

            Vector3 n = world.transform.TransformDirection(nLocal);
            Vector3 p = pos - n * radius;
            contacts.Add(new Contact
            {
                n = n,
                point = p,
                depth = depth,
                surfaceVel = world.SurfaceVelocityAt(p)
            });
        }

        // Jugador: esfera vs cápsula (el jugador está fijo en el marco inercial)
        if (player != null)
        {
            Transform pt = player.transform;
            Vector3 up = pt.up;
            Vector3 center = pt.TransformPoint(player.center);
            float pr = player.radius * Mathf.Max(pt.lossyScale.x, pt.lossyScale.z);
            float half = Mathf.Max(0f, player.height * pt.lossyScale.y * 0.5f - pr);
            Vector3 a = center - up * half, bPt = center + up * half;

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
                    surfaceVel = Vector3.zero
                });
            }
        }
    }

    // ---------- Respuesta ----------
    void SolveContacts()
    {
        for (int it = 0; it < solverIterations; it++)
        {
            GatherContacts();
            if (contacts.Count == 0) return;
            foreach (var c in contacts) Resolve(c);
        }
    }

    void Resolve(Contact c)
    {
        // Corrección de penetración
        pos += c.n * Mathf.Max(c.depth - positionSlop, 0f);
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