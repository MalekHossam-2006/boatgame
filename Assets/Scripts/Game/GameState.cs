using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The game flow inside the level: Playing -> Won (all barrels collected) or Lost (caught by pirates).
/// Esc pauses / resumes. The main menu scene comes before this one and explains the rules.
/// The best money score is saved with PlayerPrefs and shown in the main menu.
/// </summary>
public class GameState : MonoBehaviour
{
    public enum Phase { Playing, Paused, Won, Lost }

    public const string MenuScene = "MainMenu";
    public const string BestKey = "BestMoney";

    public static GameState Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.Current == Phase.Playing;

    public Phase Current { get; private set; } = Phase.Playing;
    /// <summary>True when the last finished run beat the saved best score.</summary>
    public bool NewBest { get; private set; }

    void Awake()
    {
        Instance = this;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.escapeKey.wasPressedThisFrame)
            {
                if (Current == Phase.Playing) Pause();
                else if (Current == Phase.Paused) Resume();
            }
            if (kb.rKey.wasPressedThisFrame) Restart();
        }

        var wallet = PlayerWallet.Instance;
        if (Current == Phase.Playing && wallet && wallet.BarrelsCollected > 0 && MoneyBarrel.Remaining == 0)
            Finish(Phase.Won);
    }

    /// <summary>Called by the enemy when it catches the player.</summary>
    public void Lose(ShipController playerShip)
    {
        if (Current != Phase.Playing) return;
        Finish(Phase.Lost);
        if (playerShip) playerShip.Sink();
    }

    void Finish(Phase result)
    {
        Current = result;
        int money = PlayerWallet.Instance ? PlayerWallet.Instance.Money : 0;
        NewBest = money > PlayerPrefs.GetInt(BestKey, 0);
        if (NewBest)
        {
            PlayerPrefs.SetInt(BestKey, money);
            PlayerPrefs.Save();
        }
    }

    public void Pause()
    {
        if (Current != Phase.Playing) return;
        Current = Phase.Paused;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        if (Current != Phase.Paused) return;
        Current = Phase.Playing;
        Time.timeScale = 1f;
    }

    public static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public static void ToMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(MenuScene);
    }
}
