using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Builds Assets/Scenes/SeaMap.unity from scratch: lighting, animated ocean, islands and rocks,
/// the player ship, the top-down camera and the HUD. Safe to re-run (overwrites the scene).
/// Adapted from the ShipDemoBuilder of our earlier pirate prototype.
/// Menu: Tools/Boat Game/Build Sea Map
/// </summary>
public static class SeaMapBuilder
{
    public const string ScenePath = "Assets/Scenes/SeaMap.unity";
    const string Kit = "Assets/ThirdParty/KenneyPirateKit/";
    const string MatDir = "Assets/Materials";
    const string PostProfilePath = "Assets/Settings/SeaMapPost.asset";
    const float MapRadius = 95f;

    static readonly Color Sky = new Color(0.55f, 0.80f, 0.92f);

    [MenuItem("Tools/Boat Game/Build Sea Map")]
    public static string Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        SetupLighting();
        var ocean = CreateOcean();
        var ship = CreatePlayerShip();
        ocean.follow = ship.transform;
        var world = CreateWorld();
        PlaceBarrels(world);

        var cam = CreateCamera();
        var follow = cam.gameObject.AddComponent<TopDownCamera>();
        follow.target = ship.transform;
        cam.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);
        cam.transform.position = ship.transform.position - cam.transform.forward * follow.distance;

        CreatePostVolume();
        var hud = new GameObject("HUD").AddComponent<GameHud>();
        hud.wallet = ship.GetComponent<PlayerWallet>();
        hud.ship = ship.GetComponent<ShipController>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        // SeaMap becomes the first scene in the build
        var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        AssetDatabase.SaveAssets();
        return "built " + ScenePath;
    }

    // ------------------------------------------------------------------ ship
    static GameObject CreatePlayerShip()
    {
        var root = new GameObject("PlayerShip");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "ship-pirate-small.fbx"), root.transform);
        visual.name = "Visual";

        var controller = root.AddComponent<ShipController>();
        controller.visual = visual.transform;
        controller.mapRadius = MapRadius;
        root.AddComponent<PlayerShipInput>();
        root.AddComponent<PlayerWallet>();
        return root;
    }

    // ------------------------------------------------------------------ money barrels
    const int SmallBarrels = 22;
    const int BigBarrels = 6;

    /// <summary>
    /// Scatters money barrels over the map. Small barrels (+10) are everywhere;
    /// big barrels (+50) are placed far from the start, close to the map edge, so the
    /// player has to decide whether the long trip is worth it.
    /// A fixed random seed keeps the layout the same every time the scene is built.
    /// </summary>
    static void PlaceBarrels(Transform world)
    {
        var parent = new GameObject("MoneyBarrels").transform;
        var blockers = world.GetComponentsInChildren<Collider>();
        var placed = new List<Vector3>();
        var rng = new System.Random(7);

        int made = 0, tries = 0;
        while (made < SmallBarrels + BigBarrels && tries++ < 5000)
        {
            bool big = made >= SmallBarrels;
            float minR = big ? 60f : 12f, maxR = big ? MapRadius - 8f : 70f;
            float a = (float)rng.NextDouble() * Mathf.PI * 2f;
            float r = Mathf.Lerp(minR, maxR, Mathf.Sqrt((float)rng.NextDouble()));
            var pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);

            if (placed.Any(p => (p - pos).sqrMagnitude < 10f * 10f)) continue;
            if (blockers.Any(c => (c.ClosestPoint(pos + Vector3.up) - (pos + Vector3.up)).sqrMagnitude < 5f * 5f)) continue;

            var go = Place(Kit + "barrel.fbx", parent, pos + Vector3.down * 0.3f, a * Mathf.Rad2Deg, big ? 2f : 1.2f);
            go.name = big ? "BigMoneyBarrel" : "MoneyBarrel";
            var bob = go.AddComponent<Bobber>();
            bob.spin = big ? 40f : 8f;          // big barrels spin fast so they stand out
            var barrel = go.AddComponent<MoneyBarrel>();
            barrel.value = big ? 50 : 10;
            barrel.pickupRadius = big ? 3.8f : 3.2f;
            placed.Add(pos);
            made++;
        }
    }

    // ------------------------------------------------------------------ environment
    static void SetupLighting()
    {
        var sun = new GameObject("Sun").AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.95f, 0.85f);
        sun.intensity = 1.6f;
        sun.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        sun.gameObject.AddComponent<UniversalAdditionalLightData>();

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.62f, 0.78f, 0.90f);
        RenderSettings.ambientEquatorColor = new Color(0.45f, 0.62f, 0.70f);
        RenderSettings.ambientGroundColor = new Color(0.20f, 0.32f, 0.38f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = Sky;
        RenderSettings.fogStartDistance = 70f;
        RenderSettings.fogEndDistance = 130f;
    }

    static Ocean CreateOcean()
    {
        var oceanGo = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
        var ocean = oceanGo.AddComponent<Ocean>();
        var mr = oceanGo.GetComponent<MeshRenderer>();
        mr.sharedMaterial = LitMaterial("Ocean", new Color(0.13f, 0.52f, 0.68f), 0.7f);
        mr.shadowCastingMode = ShadowCastingMode.Off;
        return ocean;
    }

    /// <summary>Islands and rocks inside the map, plus a ring of rocks that marks the map edge.</summary>
    static Transform CreateWorld()
    {
        var world = new GameObject("World").transform;
        Island(world, new Vector3(28, 0, 34), 2.6f, 20f, 3);
        Island(world, new Vector3(-36, 0, 12), 2.0f, 110f, 2);
        Island(world, new Vector3(-6, 0, -42), 2.3f, 250f, 3);
        Island(world, new Vector3(48, 0, -20), 1.7f, 160f, 2);
        Island(world, new Vector3(-55, 0, -50), 2.2f, 300f, 3);
        Island(world, new Vector3(10, 0, 70), 1.9f, 45f, 2);
        Rock(world, "rocks-a.fbx", new Vector3(10, -0.6f, 18), 30f, 1.2f);
        Rock(world, "rocks-b.fbx", new Vector3(-20, -0.6f, -16), 200f, 1.4f);
        Rock(world, "rocks-a.fbx", new Vector3(22, -0.6f, -8), 95f, 0.9f);
        Rock(world, "rocks-b.fbx", new Vector3(-14, -0.6f, 38), 300f, 1.1f);
        Rock(world, "rocks-a.fbx", new Vector3(60, -0.6f, 30), 140f, 1.3f);
        Rock(world, "rocks-b.fbx", new Vector3(30, -0.6f, -60), 20f, 1.2f);

        // map edge: the ship stops at MapRadius, these rocks just show where that is
        var edge = new GameObject("MapEdge").transform;
        edge.SetParent(world);
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI * 2f / 24f;
            var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (MapRadius + 6f) + Vector3.down * 0.6f;
            Place(Kit + (i % 2 == 0 ? "rocks-a.fbx" : "rocks-b.fbx"), edge, pos, i * 53f, 1.3f + (i % 3) * 0.2f);
        }
        return world;
    }

    static Camera CreateCamera()
    {
        var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Sky;
        cam.fieldOfView = 40f;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 300f;
        camGo.AddComponent<AudioListener>();
        var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
        camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        return cam;
    }

    static void CreatePostVolume()
    {
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = PostProfile();
    }

    // ------------------------------------------------------------------ helpers
    static GameObject Place(string path, Transform parent, Vector3 pos, float yaw, float scale)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0, yaw, 0);
        go.transform.localScale = Vector3.one * scale;
        return go;
    }

    static void Island(Transform parent, Vector3 pos, float scale, float yaw, int palms)
    {
        var root = new GameObject("Island").transform;
        root.SetParent(parent);
        root.localPosition = pos;
        root.localRotation = Quaternion.Euler(0, yaw, 0);

        // sand sits slightly below the wave crests so the waves lap at the shore
        Place(Kit + "patch-sand.fbx", root, new Vector3(0, -0.15f, 0), 0, scale);
        Place(Kit + "patch-sand-foliage.fbx", root, new Vector3(1.5f * scale, -0.05f, -0.8f * scale), 40, scale * 0.6f);
        Place(Kit + "rocks-sand-a.fbx", root, new Vector3(-2.2f * scale, -0.2f, 1.2f * scale), 70, scale * 0.6f);
        var spots = new List<Vector3> { new Vector3(0.4f, 0, 0.6f), new Vector3(-1.2f, 0, -0.4f), new Vector3(1.4f, 0, 1.2f) };
        for (int i = 0; i < palms; i++)
            Place(Kit + (i % 2 == 0 ? "palm-bend.fbx" : "palm-straight.fbx"), root, spots[i] * scale, i * 97f, scale * 0.55f);

        var col = root.gameObject.AddComponent<CapsuleCollider>();
        col.radius = 2.9f * scale;
        col.height = 10f;
        col.center = new Vector3(0, 2f, 0);
    }

    static void Rock(Transform parent, string model, Vector3 pos, float yaw, float scale)
    {
        var go = Place(Kit + model, parent, pos, yaw, scale);
        var col = go.AddComponent<CapsuleCollider>();
        col.radius = 2.2f;
        col.height = 8f;
        col.center = new Vector3(0, 2f, 0);
    }

    static Material LitMaterial(string name, Color color, float smoothness)
    {
        if (!AssetDatabase.IsValidFolder(MatDir)) AssetDatabase.CreateFolder("Assets", "Materials");
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (!mat)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        mat.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static VolumeProfile PostProfile()
    {
        var existing = AssetDatabase.LoadAssetAtPath<VolumeProfile>(PostProfilePath);
        if (existing) return existing;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, PostProfilePath);
        var tone = profile.Add<Tonemapping>(true);
        tone.mode.value = TonemappingMode.Neutral;
        var color = profile.Add<ColorAdjustments>(true);
        color.saturation.value = 12f;
        color.contrast.value = 8f;
        color.postExposure.value = 0.1f;
        var bloom = profile.Add<Bloom>(true);
        bloom.intensity.value = 0.3f;
        bloom.threshold.value = 1.1f;
        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.value = 0.22f;

        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }
}
