using UnityEngine;

public class WorldRotator : MonoBehaviour
{
    [SerializeField] private Transform pivot;      // el Jugador
    [SerializeField] private float duration = 0.8f;

    public bool IsRotating { get; private set; }
    public Vector3 AngularVelocity { get; private set; }   // Ω del mundo (rad/s, marco inercial)

    private Vector3 axis, startPos, pivotPos;
    private Quaternion startRot;
    private float totalDeg, t;

    public void Rotate(Vector3 worldAxis, float degrees)
    {
        if (IsRotating) return;
        axis = worldAxis.normalized;
        totalDeg = degrees;
        t = 0f;
        pivotPos = pivot.position;
        startPos = transform.position;
        startRot = transform.rotation;
        IsRotating = true;
    }

    // Lo llama BallSimulator en cada subpaso
    public void Advance(float dt)
    {
        if (!IsRotating) { AngularVelocity = Vector3.zero; return; }

        t = Mathf.Min(t + dt / duration, 1f);
        float s = t * t * (3f - 2f * t);            // posición angular normalizada
        float sDot = 6f * t * (1f - t) / duration;     // derivada

        Quaternion d = Quaternion.AngleAxis(totalDeg * s, axis);
        transform.SetPositionAndRotation(pivotPos + d * (startPos - pivotPos), d * startRot);
        AngularVelocity = axis * (totalDeg * Mathf.Deg2Rad * sDot);

        if (t >= 1f) { IsRotating = false; AngularVelocity = Vector3.zero; }
    }

    // Velocidad de un punto de la pared (v = Ω × r, con r desde el pivote)
    public Vector3 SurfaceVelocityAt(Vector3 p) => Vector3.Cross(AngularVelocity, p - pivotPos);
}