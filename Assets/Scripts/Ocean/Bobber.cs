using UnityEngine;

/// <summary>Makes a small floating prop follow the ocean surface and drift-rotate.</summary>
public class Bobber : MonoBehaviour
{
    public float offset = -0.3f;
    public float spin = 8f;

    void Update()
    {
        if (Ocean.Instance == null) return;
        var p = transform.position;
        p.y = Ocean.Instance.HeightAt(p) + offset;
        transform.position = p;
        transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.World);
    }
}
