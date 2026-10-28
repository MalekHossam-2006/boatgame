using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Builds Assets/Scenes/MainMenu.unity: the game's sea, islands and ships as a slowly orbiting
/// backdrop, with the title, Play / How to Play / Quit buttons, the best score and a rules panel.
/// Based on the main menu builder of our earlier pirate prototype.
/// Menu: Tools/Boat Game/Build Main Menu
/// </summary>
public static class MainMenuBuilder
{
    public const string ScenePath = "Assets/Scenes/MainMenu.unity";

    [MenuItem("Tools/Boat Game/Build Main Menu")]
    public static string Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ---------------------------------------------------------------- backdrop
        SeaMapBuilder.SetupLighting();
        var ocean = SeaMapBuilder.CreateOcean();
        SeaMapBuilder.CreateWorld();
        SeaMapBuilder.CreateSafeZone();

        var hero = Ship("MenuShip", "ship-pirate-small.fbx", new Vector3(0, 0, 0), 35f);
        ocean.follow = hero.transform;
        Ship("MenuPirates", "ship-pirate-medium.fbx", new Vector3(-17f, 0, 15f), 160f);
        foreach (var p in new[] { new Vector3(7, 0, 6), new Vector3(-6, 0, -8), new Vector3(10, 0, -5) })
            SeaMapBuilder.Place(SeaMapBuilder.Kit + "barrel.fbx", null, p + Vector3.down * 0.3f, p.x * 20f, 1.2f)
                         .AddComponent<Bobber>();

        var cam = SeaMapBuilder.CreateCamera();
        cam.gameObject.AddComponent<MenuOrbitCamera>().Apply(210f);
        SeaMapBuilder.CreatePostVolume();

        // ---------------------------------------------------------------- UI
        var canvas = UiKit.Canvas("Canvas");
        var menu = canvas.gameObject.AddComponent<MainMenu>();

        // main panel on the left
        var main = UiKit.Rect("MainPanel", canvas, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(620, 760));
        UiKit.Panel(main, UiKit.Card);
        menu.mainPanel = main.gameObject;
        var title = UiKit.Text(UiKit.Rect("Title", main, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(560, 200)),
                               "BOAT\nGAME", 124, UiKit.Gold, true);
        title.lineSpacing = -30;
        UiKit.Text(UiKit.Rect("Tagline", main, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -262), new Vector2(560, 50)),
                   "Grab the loot. Outrun the pirates.", 30, UiKit.Soft);

        var c = new Vector2(0.5f, 0.5f);
        menu.playButton = UiKit.Button(main, "Play", "PLAY", c, new Vector2(0, -40), new Vector2(420, 96), true);
        menu.howToButton = UiKit.Button(main, "HowTo", "HOW TO PLAY", c, new Vector2(0, -150), new Vector2(420, 78), false);
        menu.quitButton = UiKit.Button(main, "Quit", "QUIT", c, new Vector2(0, -246), new Vector2(420, 78), false);
        menu.bestText = UiKit.Text(UiKit.Rect("Best", main, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 34), new Vector2(560, 44)),
                                   "", 28, UiKit.Soft);

        // how to play panel in the center
        var how = UiKit.Rect("HowToPanel", canvas, c, c, Vector2.zero, new Vector2(1180, 840));
        UiKit.Panel(how, UiKit.Card);
        menu.howToPanel = how.gameObject;
        UiKit.Text(UiKit.Rect("Title", how, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -36), new Vector2(1000, 110)),
                   "HOW TO PLAY", 92, UiKit.Gold, true);
        Column(how, -280, "GOAL",
            "Sail around the sea and collect the floating money barrels.\n\n" +
            "<color=#FFC94A>Small barrels  +10</color>  near the start.\n" +
            "<color=#FFC94A>Big spinning barrels  +50</color>  far out, near the edge.\n\n" +
            "<color=#FF7A70>A pirate ship guards the open sea.</color> If it catches you, your ship sinks.\n" +
            "Inside the ring of red buoys you are safe.\n\n" +
            "Collect every barrel to win.");
        Column(how, 330, "CONTROLS",
            "<color=#FFC94A>W / S</color>   sail forward / back\n" +
            "<color=#FFC94A>A / D</color>   steer\n" +
            "<color=#FFC94A>Mouse wheel</color>   zoom\n" +
            "<color=#FFC94A>Esc</color>   pause\n" +
            "<color=#FFC94A>R</color>   restart\n\n" +
            "ON A PHONE\n" +
            "<color=#FFC94A>Joystick</color> (left thumb)   push where\nyou want to sail\n" +
            "<color=#FFC94A>II</color> button   pause");
        menu.backButton = UiKit.Button(how, "Back", "BACK", new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(320, 80), true);

        var credit = UiKit.Text(UiKit.Rect("Credit", canvas, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-36, 24), new Vector2(700, 36)),
                                "3D models: Kenney.nl (CC0)    |    Made with Unity", 22, new Color(1, 1, 1, 0.7f), false, TextAlignmentOptions.Right);
        credit.outlineWidth = 0.15f;
        credit.outlineColor = new Color32(0, 0, 0, 160);

        UiKit.EventSystem();

        EditorSceneManager.SaveScene(scene, ScenePath);
        SeaMapBuilder.SetBuildScenes();
        AssetDatabase.SaveAssets();
        return "built " + ScenePath;
    }

    /// <summary>A ship model that floats on the waves but does not move.</summary>
    static GameObject Ship(string name, string model, Vector3 pos, float yaw)
    {
        var root = new GameObject(name);
        var visual = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(SeaMapBuilder.Kit + model), root.transform);
        visual.name = "Visual";
        root.transform.SetPositionAndRotation(pos, Quaternion.Euler(0, yaw, 0));
        root.AddComponent<ShipController>().visual = visual.transform;
        return root;
    }

    static void Column(Transform panel, float x, string heading, string body)
    {
        UiKit.Text(UiKit.Rect(heading, panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, -170), new Vector2(520, 50)),
                   heading, 34, Color.white, false, TextAlignmentOptions.Left);
        var t = UiKit.Text(UiKit.Rect(heading + "Text", panel, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(x, -230), new Vector2(520, 400)),
                           body, 25, UiKit.Soft, false, TextAlignmentOptions.TopLeft);
        t.rectTransform.pivot = new Vector2(0.5f, 1);
    }
}
