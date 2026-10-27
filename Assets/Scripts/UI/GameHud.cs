using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game UI (uGUI + TextMeshPro). The objects are created by SeaMapBuilder and wired here:
/// money and barrel counters, a short hint at the start, a "+10" popup over every collected barrel,
/// a red banner while the pirates chase you, the pause panel (Esc) and the win / lose panel.
/// </summary>
public class GameHud : MonoBehaviour
{
    [Header("Counters")]
    public TMP_Text moneyText;
    public TMP_Text barrelsText;
    public RectTransform moneyPill;

    [Header("Feedback")]
    public CanvasGroup hint;
    public float hintSeconds = 6f;
    public CanvasGroup chaseBanner;
    public TMP_Text popupTemplate;
    public float popupSeconds = 1.1f;

    [Header("Pause panel")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button pauseRestartButton;
    public Button pauseMenuButton;

    [Header("End panel")]
    public GameObject endPanel;
    public TMP_Text endTitle;
    public TMP_Text endMoney;
    public TMP_Text endBest;
    public Button playAgainButton;
    public Button endMenuButton;

    public Color winColor = new Color(1f, 0.79f, 0.29f);
    public Color loseColor = new Color(1f, 0.42f, 0.36f);

    class Popup { public TMP_Text text; public Vector3 world; public float age; }
    readonly List<Popup> popups = new List<Popup>();

    PlayerWallet wallet;
    Camera cam;
    float shownMoney;
    float punch;
    float time;

    void Start()
    {
        wallet = FindAnyObjectByType<PlayerWallet>();
        if (wallet) wallet.Collected += OnCollected;
        cam = Camera.main;

        resumeButton.onClick.AddListener(() => GameState.Instance.Resume());
        pauseRestartButton.onClick.AddListener(GameState.Restart);
        pauseMenuButton.onClick.AddListener(GameState.ToMenu);
        playAgainButton.onClick.AddListener(GameState.Restart);
        endMenuButton.onClick.AddListener(GameState.ToMenu);

        popupTemplate.gameObject.SetActive(false);
        pausePanel.SetActive(false);
        endPanel.SetActive(false);
        chaseBanner.alpha = 0f;
    }

    void OnDestroy()
    {
        if (wallet) wallet.Collected -= OnCollected;
    }

    void OnCollected(int amount, Vector3 world)
    {
        var t = Instantiate(popupTemplate, popupTemplate.transform.parent);
        t.text = "+" + amount;
        t.gameObject.SetActive(true);
        popups.Add(new Popup { text = t, world = world });
        punch = 1f;
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        time += dt;
        var phase = GameState.Instance ? GameState.Instance.Current : GameState.Phase.Playing;

        // counters: the money number counts up instead of jumping, and the pill pops on every pickup
        if (wallet)
        {
            shownMoney = Mathf.MoveTowards(shownMoney, wallet.Money, Mathf.Max(60f, (wallet.Money - shownMoney) * 6f) * dt);
            moneyText.text = Mathf.RoundToInt(shownMoney).ToString();
            barrelsText.text = $"{wallet.BarrelsCollected} / {MoneyBarrel.Remaining + wallet.BarrelsCollected}";
        }
        punch = Mathf.MoveTowards(punch, 0f, dt * 4f);
        moneyPill.localScale = Vector3.one * (1f + 0.12f * punch);

        hint.alpha = Mathf.Clamp01((hintSeconds - time) / 1f);

        float chase = EnemyShipAI.AnyChasing && phase == GameState.Phase.Playing ? 1f : 0f;
        float pulse = 0.75f + 0.25f * Mathf.Sin(time * 8f);
        chaseBanner.alpha = Mathf.MoveTowards(chaseBanner.alpha, chase * pulse, dt * 4f);

        UpdatePopups(dt);

        pausePanel.SetActive(phase == GameState.Phase.Paused);
        bool ended = phase == GameState.Phase.Won || phase == GameState.Phase.Lost;
        if (ended && !endPanel.activeSelf) ShowEnd(phase == GameState.Phase.Won);
    }

    void UpdatePopups(float dt)
    {
        if (!cam) cam = Camera.main;
        for (int i = popups.Count - 1; i >= 0; i--)
        {
            var p = popups[i];
            p.age += dt;
            float t = p.age / popupSeconds;
            if (t >= 1f || !cam)
            {
                Destroy(p.text.gameObject);
                popups.RemoveAt(i);
                continue;
            }
            var sp = cam.WorldToScreenPoint(p.world + Vector3.up * (3f + t * 4f));
            p.text.rectTransform.position = sp;
            p.text.alpha = 1f - t * t;
        }
    }

    void ShowEnd(bool won)
    {
        endPanel.SetActive(true);
        endTitle.text = won ? "YOU WIN!" : "SHIP SUNK!";
        endTitle.color = won ? winColor : loseColor;
        int money = wallet ? wallet.Money : 0;
        int total = wallet ? MoneyBarrel.Remaining + wallet.BarrelsCollected : 0;
        endMoney.text = $"Money  <color=#FFC94A>{money}</color>     Barrels  {(wallet ? wallet.BarrelsCollected : 0)} / {total}";
        bool newBest = GameState.Instance && GameState.Instance.NewBest;
        endBest.text = newBest ? "New best score!" : $"Best  {PlayerPrefs.GetInt(GameState.BestKey, 0)}";
        playAgainButton.Select();
    }
}
