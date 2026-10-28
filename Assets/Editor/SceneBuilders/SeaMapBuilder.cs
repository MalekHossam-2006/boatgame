using System.Collections.Generic;
using System.Linq;
using TMPro;
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
    public const string Kit = "Assets/ThirdParty/KenneyPirateKit/";
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
        CreateSafeZone();
        PlaceBarrels(world);
        CreateEnemy(ship.transform);

        var cam = CreateCamera();
        var follow = cam.gameObject.AddComponent<TopDownCamera>();
        follow.target = ship.transform;
        cam.transform.rotation = Quaternion.Euler(follow.pitch, 0f, 0f);
        cam.transform.position = ship.transform.position - cam.transform.forward * follow.distance;

        CreatePostVolume();
        new GameObject("GameState").AddComponent<GameState>();
        CreateHud();
        UiKit.EventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        SetBuildScenes();
        AssetDatabase.SaveAssets();
        return "built " + ScenePath;
    }

    /// <summary>Build order: MainMenu first, then the game. The old Roll-a-Ball scene is left out.</summary>
    public static void SetBuildScenes()
    {
        var order = new[] { MainMenuBuilder.ScenePath, ScenePath }.Where(System.IO.File.Exists).ToList();
        var scenes = order.Select(p => new EditorBuildSettingsScene(p, true)).ToList();
        scenes.AddRange(EditorBuildSettings.scenes.Where(s => !order.Contains(s.path) && s.path != "Assets/Scenes/SampleScene.unity"));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------------------------------ HUD
    /// <summary>
    /// In-game UI: money pill (top left), barrel counter (top right), start hint (bottom),
    /// chase banner (top), popup layer, pause panel and end panel. All wired to <see cref="GameHud"/>.
    /// </summary>
    static void CreateHud()
    {
        var canvas = UiKit.Canvas("HUD");
        var hud = canvas.gameObject.AddComponent<GameHud>();
        var topLeft = new Vector2(0, 1);
        var topRight = new Vector2(1, 1);

        // money: gold coin + big number
        var pill = UiKit.Rect("MoneyPill", canvas, topLeft, new Vector2(0, 1), new Vector2(36, -32), new Vector2(270, 88));
        UiKit.Panel(pill, UiKit.Card);
        var coin = UiKit.Rect("Coin", pill, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(46, 0), new Vector2(54, 54));
        UiKit.Circle(coin, UiKit.Gold);
        UiKit.Text(UiKit.Stretch("Sign", coin), "$", 38, UiKit.Ink, true);
        var money = UiKit.Rect("Money", pill, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(88, 2), new Vector2(170, 80));
        hud.moneyText = UiKit.Text(money, "0", 60, Color.white, true, TextAlignmentOptions.Left);
        hud.moneyPill = pill;

        // barrels collected
        var barrels = UiKit.Rect("BarrelsPill", canvas, topRight, new Vector2(1, 1), new Vector2(-36, -32), new Vector2(330, 88));
        UiKit.Panel(barrels, UiKit.Card);
        UiKit.Text(UiKit.Rect("Label", barrels, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(26, 0), new Vector2(110, 60)),
                   "BARRELS", 24, UiKit.Soft, false, TextAlignmentOptions.Left);
        hud.barrelsText = UiKit.Text(UiKit.Rect("Count", barrels, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-24, 2), new Vector2(120, 80)),
                                     "0 / 28", 44, Color.white, true, TextAlignmentOptions.Right);

        // short hint at the start of the level
        var hint = UiKit.Rect("Hint", canvas, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 48), new Vector2(1080, 70));
        UiKit.Panel(hint, UiKit.CardLight);
        UiKit.Text(UiKit.Stretch("Text", hint), "Collect barrels   |   Avoid the pirate ship   |   Red buoys = safe zone   |   Esc = pause", 28, Color.white);
        hud.hint = hint.gameObject.AddComponent<CanvasGroup>();

        // chase warning
        var banner = UiKit.Rect("ChaseBanner", canvas, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(640, 80));
        UiKit.Panel(banner, UiKit.Danger);
        UiKit.Text(UiKit.Stretch("Text", banner), "PIRATES ARE CHASING YOU!", 46, Color.white, true);
        hud.chaseBanner = banner.gameObject.AddComponent<CanvasGroup>();
        hud.chaseBanner.blocksRaycasts = false;

        // "+10" popups are copies of this template
        var popups = UiKit.Stretch("Popups", canvas);
        var popup = UiKit.Rect("PopupTemplate", popups, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 80));
        hud.popupTemplate = UiKit.Text(popup, "+10", 58, UiKit.Gold, true);
        hud.popupTemplate.outlineWidth = 0.2f;
        hud.popupTemplate.outlineColor = new Color32(40, 25, 5, 255);

        // pause panel (Esc)
        var pause = Overlay(canvas, "PausePanel", 560, 470, "PAUSED", UiKit.Gold, out _);
        hud.pausePanel = pause.parent.gameObject;   // the dim layer, so it hides too
        hud.resumeButton = UiKit.Button(pause, "Resume", "RESUME", new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(360, 84), true);
        hud.pauseRestartButton = UiKit.Button(pause, "Restart", "RESTART", new Vector2(0.5f, 0.5f), new Vector2(0, -80), new Vector2(360, 72), false);
        hud.pauseMenuButton = UiKit.Button(pause, "Menu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0, -168), new Vector2(360, 72), false);

        // end panel (win / lose)
        var end = Overlay(canvas, "EndPanel", 720, 520, "YOU WIN!", UiKit.Gold, out var title);
        hud.endPanel = end.parent.gameObject;
        hud.endTitle = title;
        hud.endMoney = UiKit.Text(UiKit.Rect("Money", end, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(640, 60)),
                                  "Money 0", 36, Color.white);
        hud.endBest = UiKit.Text(UiKit.Rect("Best", end, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -5), new Vector2(640, 44)),
                                 "Best 0", 28, UiKit.Soft);
        hud.playAgainButton = UiKit.Button(end, "PlayAgain", "PLAY AGAIN", new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(380, 84), true);
        hud.endMenuButton = UiKit.Button(end, "Menu", "MAIN MENU", new Vector2(0.5f, 0.5f), new Vector2(0, -192), new Vector2(380, 72), false);
    }

    /// <summary>Full-screen dim layer with a centered card and a title; returns the card.</summary>
    static RectTransform Overlay(Transform canvas, string name, float w, float h, string titleText, Color titleColor, out TMPro.TextMeshProUGUI title)
    {
        var dim = UiKit.Stretch(name, canvas);
        var dimImg = UiKit.Panel(dim, new Color(0.01f, 0.04f, 0.07f, 0.55f), round: false);
        dimImg.raycastTarget = true;   // block clicks on the game behind
        var card = UiKit.Rect("Card", dim, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(w, h));
        UiKit.Panel(card, UiKit.Card);
        title = UiKit.Text(UiKit.Rect("Title", card, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(w - 60, 110)),
                           titleText, 96, titleColor, true);
        return card;
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

    // ------------------------------------------------------------------ enemy and safe zone
    const float SafeZoneRadius = 28f;
    const float PatrolRadius = 80f;

    /// <summary>A pirate ship that patrols the outer ring, where the big barrels are.</summary>
    static void CreateEnemy(Transform player)
    {
        var root = new GameObject("PirateShip");
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Kit + "ship-pirate-medium.fbx"), root.transform);
        visual.name = "Visual";
        root.transform.position = new Vector3(0f, 0f, PatrolRadius);
        root.transform.rotation = Quaternion.LookRotation(Vector3.left);   // tangent to the patrol circle

        var controller = root.AddComponent<ShipController>();
        controller.visual = visual.transform;
        controller.mapRadius = MapRadius;
        controller.maxSpeed = 8f;          // slower than the player (10) so escaping is possible
        controller.turnRate = 55f;
        controller.hullLength = 9f;

        var ai = root.AddComponent<EnemyShipAI>();
        ai.player = player;
        ai.patrolRadius = PatrolRadius;
        ai.safeZoneRadius = SafeZoneRadius;
    }

    /// <summary>Red buoys in a circle around the start: inside it the pirates will not follow you.</summary>
    public static void CreateSafeZone()
    {
        var parent = new GameObject("SafeZone").transform;
        var red = LitMaterial("Buoy", new Color(0.85f, 0.18f, 0.15f), 0.4f);
        var white = LitMaterial("BuoyStripe", new Color(0.95f, 0.95f, 0.92f), 0.4f);
        const int count = 20;
        for (int i = 0; i < count; i++)
        {
            float a = i * Mathf.PI * 2f / count;
            var buoy = new GameObject("Buoy").transform;
            buoy.SetParent(parent);
            buoy.position = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * SafeZoneRadius;
            Part(buoy, PrimitiveType.Cylinder, new Vector3(0f, 0.3f, 0f), new Vector3(1.1f, 0.6f, 1.1f), red);
            Part(buoy, PrimitiveType.Cylinder, new Vector3(0f, 1.0f, 0f), new Vector3(0.5f, 0.25f, 0.5f), white);
            Part(buoy, PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0f), Vector3.one * 0.45f, red);
            buoy.gameObject.AddComponent<Bobber>().offset = -0.2f;
        }
    }

    static void Part(Transform parent, PrimitiveType type, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(go.GetComponent<Collider>());   // buoys are only a marker, the ship sails through
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
    public static void SetupLighting()
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

    public static Ocean CreateOcean()
    {
        var oceanGo = new GameObject("Ocean", typeof(MeshFilter), typeof(MeshRenderer));
        var ocean = oceanGo.AddComponent<Ocean>();
        var mr = oceanGo.GetComponent<MeshRenderer>();
        mr.sharedMaterial = LitMaterial("Ocean", new Color(0.13f, 0.52f, 0.68f), 0.7f);
        mr.shadowCastingMode = ShadowCastingMode.Off;
        return ocean;
    }

    /// <summary>Islands and rocks inside the map, plus a ring of rocks that marks the map edge.</summary>
    public static Transform CreateWorld()
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

    public static Camera CreateCamera()
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

    public static void CreatePostVolume()
    {
        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = PostProfile();
    }

    // ------------------------------------------------------------------ helpers
    public static GameObject Place(string path, Transform parent, Vector3 pos, float yaw, float scale)
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
