using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal in-game HUD (IMGUI): money, barrels collected, speed, controls,
/// a floating "+10" over every barrel picked up, and a message when all barrels are collected.
/// </summary>
public class GameHud : MonoBehaviour
{
    public PlayerWallet wallet;
    public ShipController ship;
    public float popupTime = 1.2f;

    class Popup { public Vector3 pos; public int amount; public float age; }
    readonly List<Popup> popups = new List<Popup>();

    Camera cam;
    GUIStyle label, small, big, popupStyle;

    static readonly Color Panel = new Color(0.05f, 0.1f, 0.15f, 0.65f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

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
            popupStyle = new GUIStyle(label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        }

        float s = Screen.height / 1080f;
        label.fontSize = Mathf.RoundToInt(26 * s);
        small.fontSize = Mathf.RoundToInt(17 * s);
        big.fontSize = Mathf.RoundToInt(48 * s);
        popupStyle.fontSize = Mathf.RoundToInt(30 * s);

        int total = MoneyBarrel.Remaining + wallet.BarrelsCollected;

        var r = new Rect(20 * s, 20 * s, 340 * s, 170 * s);
        Fill(r, Panel);
        float x = r.x + 16 * s, y = r.y + 12 * s, w = r.width - 32 * s, line = 38 * s;
        GUI.Label(new Rect(x, y, w, line), $"<b>Money</b>  <color=#FFD84D>{wallet.Money}</color>", label); y += line;
        GUI.Label(new Rect(x, y, w, line), $"<b>Barrels</b>  {wallet.BarrelsCollected} / {total}", label); y += line;
        if (ship) GUI.Label(new Rect(x, y, w, line), $"<b>Speed</b>  {Mathf.Abs(ship.Speed) * 1.94f:0} knots", label);
        y += line;
        GUI.Label(new Rect(x, y, w, line), "W/S move   A/D steer   R restart", small);

        DrawPopups(s);

        if (total > 0 && MoneyBarrel.Remaining == 0)
        {
            GUI.color = Gold;
            GUI.Label(new Rect(0, Screen.height * 0.38f, Screen.width, 70 * s), "<b>All barrels collected!</b>", big);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, Screen.height * 0.38f + 70 * s, Screen.width, 50 * s),
                      $"Total money: {wallet.Money}   -   press R to play again",
                      new GUIStyle(small) { alignment = TextAnchor.MiddleCenter, fontSize = Mathf.RoundToInt(24 * s) });
        }
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
