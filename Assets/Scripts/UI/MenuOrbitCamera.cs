using UnityEngine;

/// <summary>Slowly orbits a point; used as the animated background of the main menu.
/// Copied from our earlier pirate prototype.</summary>
public class MenuOrbitCamera : MonoBehaviour
{
    public Vector3 focus = new Vector3(0f, 2f, 0f);
    public float distance = 26f;
    public float height = 11f;
    public float degreesPerSecond = 4f;
    public float startAngle = 210f;

    void LateUpdate() => Apply(startAngle + Time.time * degreesPerSecond);

    public void Apply(float degrees)
    {
        float a = degrees * Mathf.Deg2Rad;
        transform.position = focus + new Vector3(Mathf.Sin(a) * distance, height, Mathf.Cos(a) * distance);
        transform.LookAt(focus);
    }
}
