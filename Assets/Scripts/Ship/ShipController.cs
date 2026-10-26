using UnityEngine;

/// <summary>
/// Simple arcade boat movement.
/// Driven by <see cref="Throttle"/> (-1 reverse .. 1 full ahead) and <see cref="SteerInput"/> (-1..1),
/// which are set by <see cref="PlayerShipInput"/>.
/// The ship turns faster when it is moving, and stays inside the circular map.
/// The visual child bobs and rolls on the <see cref="Ocean"/> waves.
/// Adapted from the ShipController of our earlier pirate prototype (sail levels removed).
/// </summary>
public class ShipController : MonoBehaviour
{
    [Header("Movement")]
    public float maxSpeed = 10f;
    public float reverseSpeed = 3f;
    public float acceleration = 4f;
    public float deceleration = 3f;
    public float turnRate = 70f;          // deg/sec at full steering, full speed
    public float minTurnFactor = 0.35f;   // fraction of turn rate available when almost stopped

    [Header("Map")]
    [Tooltip("The ship cannot leave this circle (centered at the world origin).")]
    public float mapRadius = 95f;

    [Header("Floating")]
    [Tooltip("Ship model, a child of this object. Its local position/rotation are driven by the waves.")]
    public Transform visual;
    public float draft = 0.9f;            // how deep the hull sits in the water
    public float hullLength = 7.5f;
    public float hullWidth = 4f;
    public float floatSmoothing = 4f;
    public float bankAngle = 6f;          // lean into turns

    [Header("Obstacles")]
    public float collisionRadius = 2.4f;
    public LayerMask obstacleMask = ~0;

    public float Throttle { get; set; }
    public float SteerInput { get; set; }
    public float Speed { get; private set; }

    float steer;
    Vector3 visualBaseOffset;

    void Start()
    {
        if (visual) visualBaseOffset = visual.localPosition;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        steer = Mathf.MoveTowards(steer, Mathf.Clamp(SteerInput, -1f, 1f), dt * 4f);
        Move(dt);
        Float(dt);
    }

    void Move(float dt)
    {
        float throttle = Mathf.Clamp(Throttle, -1f, 1f);
        float targetSpeed = throttle >= 0f ? throttle * maxSpeed : throttle * reverseSpeed;
        float rate = Mathf.Abs(targetSpeed) > Mathf.Abs(Speed) ? acceleration : deceleration;
        Speed = Mathf.MoveTowards(Speed, targetSpeed, rate * dt);

        float speed01 = Mathf.Abs(Speed) / maxSpeed;
        float turn = steer * turnRate * Mathf.Lerp(minTurnFactor, 1f, Mathf.Sqrt(speed01));
        transform.Rotate(0f, turn * dt, 0f, Space.World);

        var next = transform.position + transform.forward * (Speed * dt);

        // simple obstacle handling: push out of anything we overlap, and lose speed
        foreach (var col in Physics.OverlapSphere(next + Vector3.up, collisionRadius, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (col.transform.IsChildOf(transform)) continue;
            var closest = col.ClosestPoint(next + Vector3.up);
            var away = next + Vector3.up - closest;
            away.y = 0f;
            float dist = away.magnitude;
            if (dist < 0.0001f) away = -transform.forward; else away /= dist;
            next += away * Mathf.Max(0f, collisionRadius - dist);
            Speed *= 0.9f;
        }

        // keep the ship inside the map
        next.y = 0f;
        if (next.magnitude > mapRadius)
        {
            next = next.normalized * mapRadius;
            Speed *= 0.95f;
        }
        transform.position = next;
    }

    void Float(float dt)
    {
        if (!visual || Ocean.Instance == null) return;
        var ocean = Ocean.Instance;
        var p = transform.position;
        var f = transform.forward * (hullLength * 0.5f);
        var r = transform.right * (hullWidth * 0.5f);

        float bow = ocean.HeightAt(p + f), stern = ocean.HeightAt(p - f);
        float port = ocean.HeightAt(p - r), starboard = ocean.HeightAt(p + r);
        float height = (bow + stern + port + starboard) * 0.25f - draft;
        float pitch = Mathf.Atan2(stern - bow, hullLength) * Mathf.Rad2Deg;
        float roll = Mathf.Atan2(port - starboard, hullWidth) * Mathf.Rad2Deg
                     - steer * bankAngle * Mathf.Clamp01(Mathf.Abs(Speed) / 4f);

        float k = 1f - Mathf.Exp(-floatSmoothing * dt);
        visual.localPosition = Vector3.Lerp(visual.localPosition, visualBaseOffset + Vector3.up * height, k);
        visual.localRotation = Quaternion.Slerp(visual.localRotation, Quaternion.Euler(pitch, 0f, roll), k);
    }
}
