using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controls for the player ship.
/// Keyboard (PC): W / Up = forward, S / Down = slow down and reverse, A/D or Left/Right = steer.
/// Touch (mobile): push the <see cref="TouchJoystick"/> toward where you want to sail.
/// The ship turns toward that direction and the push distance sets the speed.
/// The camera never rotates, so "up" on the joystick is always "up" on the screen.
/// The ship only answers while the game is being played (see <see cref="GameState"/>).
/// </summary>
[RequireComponent(typeof(ShipController))]
public class PlayerShipInput : MonoBehaviour
{
    [Tooltip("Steering angle (degrees) that gives full rudder when following the joystick.")]
    public float fullSteerAngle = 30f;

    ShipController ship;

    void Awake()
    {
        ship = GetComponent<ShipController>();
    }

    void Update()
    {
        ship.Throttle = 0f;
        ship.SteerInput = 0f;
        if (!GameState.IsPlaying) return;

        var joy = TouchJoystick.Active;
        if (joy && joy.Value != Vector2.zero)
        {
            FollowJoystick(joy.Value);
            return;
        }

        var kb = Keyboard.current;
        if (kb == null) return;

        float throttle = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle -= 1f;
        ship.Throttle = throttle;

        float steer = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
        ship.SteerInput = steer;
    }

    void FollowJoystick(Vector2 stick)
    {
        var wanted = new Vector3(stick.x, 0f, stick.y);
        float angle = Vector3.SignedAngle(transform.forward, wanted, Vector3.up);
        ship.SteerInput = Mathf.Clamp(angle / fullSteerAngle, -1f, 1f);
        // slow down while turning hard so the ship can come around
        ship.Throttle = stick.magnitude * Mathf.Lerp(1f, 0.35f, Mathf.Abs(angle) / 180f);
    }
}
