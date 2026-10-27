using UnityEngine;

/// <summary>
/// Pirate ship that guards the outer sea.
/// Patrol: sails around a circle of <see cref="patrolRadius"/>.
/// Chase: when the player comes within <see cref="detectRange"/> (and is outside the safe zone).
/// Give up: when the player gets further than <see cref="giveUpRange"/> or reaches the safe zone.
/// Catching the player (closer than <see cref="catchRadius"/>) sinks the player and loses the game.
/// Uses the same <see cref="ShipController"/> as the player, only slower.
/// </summary>
[RequireComponent(typeof(ShipController))]
public class EnemyShipAI : MonoBehaviour
{
    public enum Mode { Patrol, Chase }

    public Transform player;

    [Header("Patrol")]
    public float patrolRadius = 80f;
    [Tooltip("1 = counter-clockwise, -1 = clockwise.")]
    public float patrolDirection = 1f;
    [Range(0f, 1f)] public float patrolThrottle = 0.6f;

    [Header("Chase")]
    public float detectRange = 25f;
    public float giveUpRange = 45f;
    public float catchRadius = 4.5f;
    [Tooltip("The enemy never enters this circle around the start, and stops chasing a player inside it.")]
    public float safeZoneRadius = 28f;

    [Header("Avoidance")]
    public float lookAhead = 14f;
    public LayerMask obstacleMask = ~0;

    /// <summary>True while at least one enemy is chasing the player (shown on the HUD).</summary>
    public static bool AnyChasing { get; private set; }

    public Mode Current { get; private set; } = Mode.Patrol;

    ShipController ship;
    ShipController playerShip;

    void Awake()
    {
        ship = GetComponent<ShipController>();
        if (player) playerShip = player.GetComponent<ShipController>();
    }

    void OnDisable() => AnyChasing = false;

    void Update()
    {
        if (!GameState.IsPlaying || !player)
        {
            ship.Throttle = 0f;
            ship.SteerInput = 0f;
            AnyChasing = false;
            return;
        }

        var toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float dist = toPlayer.magnitude;
        bool playerSafe = Flat(player.position).magnitude < safeZoneRadius;

        if (Current == Mode.Patrol && dist < detectRange && !playerSafe) Current = Mode.Chase;
        else if (Current == Mode.Chase && (dist > giveUpRange || playerSafe)) Current = Mode.Patrol;
        AnyChasing = Current == Mode.Chase;

        if (Current == Mode.Chase && dist < catchRadius)
        {
            GameState.Instance.Lose(playerShip);
            return;
        }

        Vector3 target;
        float throttle;
        if (Current == Mode.Chase)
        {
            target = player.position;
            throttle = 1f;
        }
        else
        {
            // aim at a point a little further along the patrol circle
            var p = Flat(transform.position);
            float angle = Mathf.Atan2(p.z, p.x) + 0.35f * patrolDirection;
            target = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * patrolRadius;
            throttle = patrolThrottle;
        }

        // never sail into the safe zone
        if (Flat(transform.position).magnitude < safeZoneRadius + 6f)
            target = Flat(transform.position).normalized * (safeZoneRadius + 20f);

        ship.Throttle = throttle;
        ship.SteerInput = SteerTowards(target) + AvoidObstacles();
    }

    float SteerTowards(Vector3 target)
    {
        var to = Flat(target - transform.position);
        if (to.sqrMagnitude < 0.01f) return 0f;
        float angle = Vector3.SignedAngle(transform.forward, to, Vector3.up);
        return Mathf.Clamp(angle / 30f, -1f, 1f);
    }

    /// <summary>Looks ahead for islands and rocks and steers away from them.</summary>
    float AvoidObstacles()
    {
        var origin = transform.position + Vector3.up;
        if (!Physics.SphereCast(origin, 2.5f, transform.forward, out var hit, lookAhead, obstacleMask, QueryTriggerInteraction.Ignore))
            return 0f;
        if (hit.transform.IsChildOf(transform)) return 0f;
        var away = Flat(origin - hit.point);
        float side = Vector3.Dot(away, transform.right) >= 0f ? 1f : -1f;
        return side * 1.5f * (1f - hit.distance / lookAhead);
    }

    static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
}
