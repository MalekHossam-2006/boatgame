using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal in-game HUD (IMGUI): money, barrels collected, speed, controls,
/// a floating "+10" over every barrel picked up, and the start / win / lose screens
/// for the current <see cref="GameState"/>.
/// </summary>
public class GameHud : MonoBehaviour
{
    public PlayerWallet wallet;
    public ShipController ship;
    public float popupTime = 1.2f;

    class Popup { public Vector3 pos; public int amount; public float age; }
    readonly List<Popup> popups = new List<Popup>();

    Camera cam;
    GUIStyle label, small, big, center, popupStyle;

    static readonly Color Panel = new Color(0.05f, 0.1f, 0.15f, 0.65f);
    static readonly Color Shade = new Color(0.02f, 0.06f, 0.1f, 0.6f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Danger = new Color(1f, 0.35f, 0.3f);

    void Start()
    {
        if (!wallet) wallet = FindFirstObjectByType<PlayerWallet>();
        if (!ship && wallet) ship = wallet.GetComponent<ShipController>();
        if (wallet) wallet.Collected += OnCollected;
        cam = Camera.main;
    }

    void OnDestroy()
    {
        if (wallet) wallet.Collected -= OnCollected;
    }

    void OnCollected(int amount, Vector3 pos) => popups.Add(new Popup { pos = pos, amount = amount });

    void Update()
    {
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            popups[i].age += Time.deltaTime;
            if (popups[i].age > popupTime) popups.RemoveAt(i);
        }
    }

    void OnGUI()
    {
        if (!wallet) return;
        if (label == null)
        {
            label = new GUIStyle(GUI.skin.label) { richText = true, normal = { textColor = Color.white } };
            small = new GUIStyle(label);
            big = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter };
            center = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, wordWrap = true };
            popupStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        float s = Screen.height / 1080f;
        label.fontSize = Mathf.RoundToInt(26 * s);
        small.fontSize = Mathf.RoundToInt(17 * s);
        big.fontSize = Mathf.RoundToInt(56 * s);
        center.fontSize = Mathf.RoundToInt(26 * s);
        popupStyle.fontSize = Mathf.RoundToInt(30 * s);

        var phase = GameState.Instance ? GameState.Instance.Current : GameState.Phase.Playing;
        if (phase == GameState.Phase.Start)
        {
            DrawStart(s);
            return;
        }

        DrawStats(s);
        DrawPopups(s);

        if (phase == GameState.Phase.Won)
            DrawEnd(s, "<b>You collected every barrel!</b>", Gold);
        else if (phase == GameState.Phase.Lost)
            DrawEnd(s, "<b>The pirates sank your ship!</b>", Danger);
    }

    void DrawStats(float s)
    {
        int total = MoneyBarrel.Remaining + wallet.BarrelsCollected;
        var r = new Rect(20 * s, 20 * s, 340 * s, 170 * s);
        Fill(r, Panel);
        float x = r.x + 16 * s, y = r.y + 12 * s, w = r.width - 32 * s, line = 38 * s;
        GUI.Label(new Rect(x, y, w, line), $"<b>Money</b>  <color=#FFD84D>{wallet.Money}</color>", label); y += line;
        GUI.Label(new Rect(x, y, w, line), $"<b>Barrels</b>  {wallet.BarrelsCollected} / {total}", label); y += line;
        if (ship) GUI.Label(new Rect(x, y, w, line), $"<b>Speed</b>  {Mathf.Abs(ship.Speed) * 1.94f:0} knots", label);
        y += line;
        GUI.Label(new Rect(x, y, w, line), "W/S move   A/D steer   R restart", small);

        if (EnemyShipAI.AnyChasing && GameState.IsPlaying)
        {
            float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * 8f);
            GUI.color = new Color(Danger.r, Danger.g, Danger.b, pulse);
            GUI.Label(new Rect(0, Screen.height * 0.08f, Screen.width, 70 * s),
                      "<b>PIRATES ARE CHASING YOU!</b>", new GUIStyle(big) { fontSize = Mathf.RoundToInt(40 * s) });
            GUI.color = Color.white;
        }
    }

    void DrawStart(float s)
    {
        Fill(new Rect(0, 0, Screen.width, Screen.height), Shade);
        float w = 900 * s, x = (Screen.width - w) / 2f, y = Screen.height * 0.18f;
        GUI.color = Gold;
        GUI.Label(new Rect(x, y, w, 80 * s), "<b>BOAT GAME</b>", big); y += 110 * s;
        GUI.color = Color.white;
        GUI.Label(new Rect(x, y, w, 120 * s),
            "Collect the floating money barrels.\n" +
            "Small barrels near the start: <color=#FFD84D>+10</color>.   Big spinning barrels far out: <color=#FFD84D>+50</color>.", center);
        y += 130 * s;
        GUI.Label(new Rect(x, y, w, 90 * s),
            "<color=#FF7A70>A pirate ship guards the open sea. If it catches you, you sink.</color>\n" +
            "Inside the ring of red buoys you are safe.", center);
        y += 120 * s;
        GUI.Label(new Rect(x, y, w, 50 * s), "W/S move   -   A/D steer   -   mouse wheel zoom   -   R restart", center);
        y += 80 * s;
        float pulse = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f);
        GUI.color = new Color(1f, 1f, 1f, pulse);
        GUI.Label(new Rect(x, y, w, 50 * s), "<b>Press SPACE to start</b>", center);
        GUI.color = Color.white;
    }

    void DrawEnd(float s, string title, Color color)
    {
        float y = Screen.height * 0.36f;
        Fill(new Rect(0, y - 20 * s, Screen.width, 190 * s), Shade);
        GUI.color = color;
        GUI.Label(new Rect(0, y, Screen.width, 80 * s), title, big);
        GUI.color = Color.white;
        GUI.Label(new Rect(0, y + 85 * s, Screen.width, 50 * s),
                  $"Money: <color=#FFD84D>{wallet.Money}</color>   -   press R to play again", center);
    }

    void DrawPopups(float s)
    {
        if (!cam) cam = Camera.main;
        if (!cam) return;
        foreach (var p in popups)
        {
            float t = p.age / popupTime;
            var sp = cam.WorldToScreenPoint(p.pos + Vector3.up * (3f + t * 4f));
            if (sp.z < 0) continue;
            GUI.color = new Color(Gold.r, Gold.g, Gold.b, 1f - t * t);
            GUI.Label(new Rect(sp.x - 80 * s, Screen.height - sp.y - 25 * s, 160 * s, 50 * s), $"+{p.amount}", popupStyle);
        }
        GUI.color = Color.white;
    }

    void Fill(Rect r, Color c)
    {
        var old = GUI.color;
        GUI.color = c;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = old;
    }
}
