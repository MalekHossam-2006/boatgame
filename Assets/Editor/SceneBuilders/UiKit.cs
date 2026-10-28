using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Small helpers the scene builders use to create the UI: a canvas, rounded cards,
/// text and buttons, all in one consistent style (dark navy cards, gold accents, white text).
/// Fonts come from the TextMesh Pro examples that ship with the project
/// (Bangers for titles and numbers, Oswald Bold for the rest).
/// </summary>
public static class UiKit
{
    public static readonly Color Card = new Color(0.04f, 0.12f, 0.18f, 0.88f);
    public static readonly Color CardLight = new Color(0.04f, 0.12f, 0.18f, 0.6f);
    public static readonly Color Gold = new Color(1f, 0.79f, 0.29f);
    public static readonly Color GoldDark = new Color(0.86f, 0.62f, 0.16f);
    public static readonly Color Ink = new Color(0.1f, 0.07f, 0.03f);
    public static readonly Color Soft = new Color(0.86f, 0.93f, 0.97f);
    public static readonly Color Danger = new Color(0.86f, 0.22f, 0.2f, 0.92f);

    const string FontDir = "Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/";
    const string UiDir = "Assets/UI";

    public static TMP_FontAsset TitleFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Bangers SDF.asset");
    public static TMP_FontAsset BodyFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontDir + "Oswald Bold SDF.asset");

    // ------------------------------------------------------------------ canvas
    public static Transform Canvas(string name)
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return go.transform;
    }

    public static void EventSystem()
    {
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // ------------------------------------------------------------------ layout
    /// <summary>New rect at an anchor point (0..1 of the parent) with a pivot, offset and size.</summary>
    public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = anchor;
        r.pivot = pivot;
        r.anchoredPosition = pos;
        r.sizeDelta = size;
        return r;
    }

    /// <summary>Rect that fills its parent.</summary>
    public static RectTransform Stretch(string name, Transform parent)
    {
        var r = Rect(name, parent, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        return r;
    }

    public static Image Panel(RectTransform r, Color color, bool round = true)
    {
        var img = r.gameObject.AddComponent<Image>();
        if (round)
        {
            img.sprite = RoundedSprite();
            img.type = Image.Type.Sliced;
        }
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static Image Circle(RectTransform r, Color color)
    {
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = CircleSprite();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    // ------------------------------------------------------------------ text and buttons
    public static TextMeshProUGUI Text(RectTransform r, string text, float size, Color color, bool title = false,
                                       TextAlignmentOptions align = TextAlignmentOptions.Center)
    {
        var t = r.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = title ? TitleFont : BodyFont;
        t.text = text;
        t.fontSize = size;
        t.color = color;
        t.alignment = align;
        t.raycastTarget = false;
        if (title) t.characterSpacing = 4;
        return t;
    }

    /// <summary>Rounded button. Primary = gold with dark text, otherwise dark with white text.</summary>
    public static Button Button(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size, bool primary)
    {
        var r = Rect(name, parent, anchor, new Vector2(0.5f, 0.5f), pos, size);
        var img = r.gameObject.AddComponent<Image>();
        img.sprite = RoundedSprite();
        img.type = Image.Type.Sliced;
        img.color = Color.white;

        var btn = r.gameObject.AddComponent<Button>();
        var c = btn.colors;
        c.normalColor = primary ? Gold : new Color(1f, 1f, 1f, 0.14f);
        c.highlightedColor = primary ? new Color(1f, 0.87f, 0.5f) : new Color(1f, 1f, 1f, 0.26f);
        c.selectedColor = c.highlightedColor;
        c.pressedColor = primary ? GoldDark : new Color(1f, 1f, 1f, 0.35f);
        c.fadeDuration = 0.08f;
        btn.colors = c;

        var t = Text(Stretch("Label", r), label, size.y * 0.42f, primary ? Ink : Color.white);
        t.characterSpacing = 6;
        return btn;
    }

    // ------------------------------------------------------------------ generated sprites
    /// <summary>White rounded rectangle, 9-sliced so it scales to any size.</summary>
    public static Sprite RoundedSprite() => GeneratedSprite("Rounded.png", 96, 30f, new Vector4(32, 32, 32, 32));

    public static Sprite CircleSprite() => GeneratedSprite("Circle.png", 128, 64f, Vector4.zero);

    static Sprite GeneratedSprite(string file, int size, float radius, Vector4 border)
    {
        string path = $"{UiDir}/{file}";
        if (!File.Exists(path))
        {
            if (!AssetDatabase.IsValidFolder(UiDir)) AssetDatabase.CreateFolder("Assets", "UI");
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // signed distance to a rounded square, 1px soft edge
                float px = Mathf.Abs(x + 0.5f - size / 2f) - (size / 2f - radius);
                float py = Mathf.Abs(y + 0.5f - size / 2f) - (size / 2f - radius);
                float d = new Vector2(Mathf.Max(px, 0), Mathf.Max(py, 0)).magnitude + Mathf.Min(Mathf.Max(px, py), 0) - radius;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(0.5f - d)));
            }
            File.WriteAllBytes(path, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(path);
            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.spriteBorder = border;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
}
