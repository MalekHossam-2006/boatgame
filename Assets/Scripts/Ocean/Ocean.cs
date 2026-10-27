using UnityEngine;

/// <summary>
/// Low-poly animated ocean. Builds a flat-shaded grid that follows a target
/// (snapped to the grid so the waves don't slide) and displaces it with a few
/// summed sine waves. Ships sample the same wave function via <see cref="HeightAt"/>.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class Ocean : MonoBehaviour
{
    public static Ocean Instance { get; private set; }

    [Header("Grid")]
    [Tooltip("Object the ocean follows (usually the player ship).")]
    public Transform follow;
    public int cells = 70;
    public float cellSize = 2.5f;

    [Header("Waves")]
    public float amplitude = 0.35f;
    public float wavelength = 14f;
    public float speed = 1.2f;

    Mesh mesh;
    Vector3[] baseVerts;
    Vector3[] verts;

    // direction (normalized), relative wavelength, relative amplitude
    static readonly Vector4[] Waves =
    {
        new Vector4(1f, 0f, 1.00f, 1.00f),
        new Vector4(0.6f, 0.8f, 0.63f, 0.55f),
        new Vector4(-0.4f, 0.92f, 0.41f, 0.35f),
        new Vector4(0.9f, -0.44f, 0.27f, 0.20f),
    };

    void OnEnable()
    {
        Instance = this;
        Build();
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Water surface height (world Y) at a world XZ position.</summary>
    public float HeightAt(Vector3 world) => WaveHeight(world.x, world.z, Time.time);

    float WaveHeight(float x, float z, float t)
    {
        float h = 0f;
        foreach (var w in Waves)
        {
            float k = 2f * Mathf.PI / (wavelength * w.z);
            float phase = k * (w.x * x + w.y * z) - t * speed * (1.6f - 0.6f * w.z);
            h += Mathf.Sin(phase) * amplitude * w.w;
        }
        return h;
    }

    void Build()
    {
        // flat shading: every triangle gets its own 3 vertices
        int n = cells;
        float half = n * cellSize * 0.5f;
        baseVerts = new Vector3[n * n * 6];
        var tris = new int[baseVerts.Length];
        int i = 0;
        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            var a = new Vector3(x * cellSize - half, 0, z * cellSize - half);
            var b = a + new Vector3(cellSize, 0, 0);
            var c = a + new Vector3(0, 0, cellSize);
            var d = a + new Vector3(cellSize, 0, cellSize);
            bool flip = ((x + z) & 1) == 0; // alternate diagonals for a nicer facet pattern
            var quad = flip ? new[] { a, c, b, b, c, d } : new[] { a, c, d, a, d, b };
            foreach (var v in quad)
            {
                baseVerts[i] = v;
                tris[i] = i;
                i++;
            }
        }
        verts = (Vector3[])baseVerts.Clone();
        mesh = new Mesh { name = "OceanMesh", hideFlags = HideFlags.DontSave, indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
        mesh.MarkDynamic();
        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.bounds = new Bounds(Vector3.zero, new Vector3(n * cellSize, 10, n * cellSize));
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }

    void LateUpdate()
    {
        if (follow)
        {
            // snap to 2 cells so the alternating diagonal pattern stays stable
            float snap = cellSize * 2f;
            var p = follow.position;
            transform.position = new Vector3(Mathf.Round(p.x / snap) * snap, 0, Mathf.Round(p.z / snap) * snap);
        }

        float t = Time.time;
        var origin = transform.position;
        for (int i = 0; i < verts.Length; i++)
        {
            var b = baseVerts[i];
            verts[i].x = b.x;
            verts[i].z = b.z;
            verts[i].y = WaveHeight(b.x + origin.x, b.z + origin.z, t);
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }
}
