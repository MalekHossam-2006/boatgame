using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Top-down follow camera. Looks down at a fixed angle (does not rotate with the ship),
/// leads slightly ahead of the target's movement, and zooms with the mouse wheel.
/// </summary>
public class TopDownCamera : MonoBehaviour
{
    public Transform target;
    [Range(30f, 90f)] public float pitch = 65f;
    public float distance = 38f;
    public float minDistance = 18f;
    public float maxDistance = 70f;
    public float followSmoothing = 3f;
    public float lookAhead = 1.2f;   // seconds of velocity to lead by

    Vector3 focus;
    Vector3 lastTargetPos;

    void Start()
    {
        if (target) focus = lastTargetPos = target.position;
        Apply();
    }

    void LateUpdate()
    {
        if (!target) return;
        float dt = Mathf.Max(Time.deltaTime, 0.0001f);

        var mouse = Mouse.current;
        if (mouse != null)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance * (scroll > 0 ? 0.9f : 1.1f), minDistance, maxDistance);
        }

        var velocity = (target.position - lastTargetPos) / dt;
        lastTargetPos = target.position;
        var desired = target.position + velocity * lookAhead;
        focus = Vector3.Lerp(focus, desired, 1f - Mathf.Exp(-followSmoothing * dt));
        Apply();
    }

    void Apply()
    {
        var rot = Quaternion.Euler(pitch, 0f, 0f);
        transform.rotation = rot;
        transform.position = focus - rot * Vector3.forward * distance;
    }
}
