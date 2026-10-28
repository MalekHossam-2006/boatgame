using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// On-screen joystick for touch devices. Put it on a UI area (the touch zone);
/// <see cref="knobBase"/> jumps to where the finger lands and <see cref="knob"/> follows the drag.
/// <see cref="Value"/> is the stick direction (-1..1 on each axis, length up to 1).
/// Only shown on mobile, so the PC version is unchanged.
/// </summary>
public class TouchJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public static TouchJoystick Active { get; private set; }

    public RectTransform knobBase;
    public RectTransform knob;
    [Tooltip("How far the knob can move from the center, in canvas units.")]
    public float radius = 110f;
    [Range(0f, 0.5f)] public float deadZone = 0.12f;

    public Vector2 Value { get; private set; }
    public bool Held { get; private set; }

    RectTransform area;
    Vector2 restPosition;
    Canvas canvas;

    /// <summary>True on phones and tablets (and in the Device Simulator).</summary>
    public static bool UseTouchControls => Application.isMobilePlatform;

    void Awake()
    {
        area = (RectTransform)transform;
        canvas = GetComponentInParent<Canvas>();
        restPosition = knobBase.anchoredPosition;
        if (!UseTouchControls) gameObject.SetActive(false);
    }

    void OnEnable() => Active = this;

    void OnDisable()
    {
        if (Active == this) Active = null;
        Release();
    }

    public void OnPointerDown(PointerEventData e)
    {
        Held = true;
        knobBase.anchoredPosition = ToLocal(e);
        OnDrag(e);
    }

    public void OnDrag(PointerEventData e)
    {
        var offset = Vector2.ClampMagnitude(ToLocal(e) - knobBase.anchoredPosition, radius);
        knob.anchoredPosition = offset;
        var v = offset / radius;
        Value = v.magnitude < deadZone ? Vector2.zero : v;
    }

    public void OnPointerUp(PointerEventData e) => Release();

    void Release()
    {
        Held = false;
        Value = Vector2.zero;
        if (knob) knob.anchoredPosition = Vector2.zero;
        if (knobBase) knobBase.anchoredPosition = restPosition;
    }

    Vector2 ToLocal(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(area, e.position,
            canvas && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null, out var p);
        // knobBase is anchored to the area's bottom-left corner
        return p - area.rect.min;
    }
}
