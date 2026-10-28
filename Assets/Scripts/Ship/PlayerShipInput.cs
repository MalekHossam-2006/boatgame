using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Keyboard controls for the player ship.
/// W / Up = forward, S / Down = slow down and reverse, A/D or Left/Right = steer.
/// The ship only answers while the game is being played (see <see cref="GameState"/>).
/// </summary>
[RequireComponent(typeof(ShipController))]
public class PlayerShipInput : MonoBehaviour
{
    ShipController ship;

    void Awake()
    {
        ship = GetComponent<ShipController>();
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null || !GameState.IsPlaying)
        {
            ship.Throttle = 0f;
            ship.SteerInput = 0f;
            return;
        }

        float throttle = 0f;
        if (kb.wKey.isPressed || kb.upArrowKey.isPressed) throttle += 1f;
        if (kb.sKey.isPressed || kb.downArrowKey.isPressed) throttle -= 1f;
        ship.Throttle = throttle;

        float steer = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) steer -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) steer += 1f;
        ship.SteerInput = steer;
    }
}
