using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The game flow: Start screen -> Playing -> Won (all barrels collected) or Lost (caught by pirates).
/// Space / Enter starts the game, R restarts the level at any time.
/// After a restart the start screen is skipped and the game begins right away.
/// </summary>
public class GameState : MonoBehaviour
{
    public enum Phase { Start, Playing, Won, Lost }

    public static GameState Instance { get; private set; }
    public static bool IsPlaying => Instance != null && Instance.Current == Phase.Playing;

    public Phase Current { get; private set; } = Phase.Start;

    static bool skipIntro;

    // statics survive between Play sessions when domain reload is off, so reset them on startup
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => skipIntro = false;

    void Awake()
    {
        Instance = this;
        if (skipIntro) Current = Phase.Playing;
        skipIntro = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (Current == Phase.Start && (kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame))
                Current = Phase.Playing;
            if (kb.rKey.wasPressedThisFrame)
                Restart();
        }

        var wallet = PlayerWallet.Instance;
        if (Current == Phase.Playing && wallet && wallet.BarrelsCollected > 0 && MoneyBarrel.Remaining == 0)
            Current = Phase.Won;
    }

    /// <summary>Called by the enemy when it catches the player.</summary>
    public void Lose(ShipController playerShip)
    {
        if (Current != Phase.Playing) return;
        Current = Phase.Lost;
        if (playerShip) playerShip.Sink();
    }

    public static void Restart()
    {
        skipIntro = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
