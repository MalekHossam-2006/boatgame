using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu: Play loads the game scene, How to Play opens the rules panel, Quit closes the game.
/// Shows the best money score saved by <see cref="GameState"/>.
/// </summary>
public class MainMenu : MonoBehaviour
{
    public string gameScene = "SeaMap";

    public Button playButton;
    public Button howToButton;
    public Button quitButton;
    public Button backButton;
    public GameObject mainPanel;
    public GameObject howToPanel;
    public TMP_Text bestText;

    void Start()
    {
        Time.timeScale = 1f;
        playButton.onClick.AddListener(() => SceneManager.LoadScene(gameScene));
        howToButton.onClick.AddListener(() => ShowHowTo(true));
        backButton.onClick.AddListener(() => ShowHowTo(false));
        quitButton.onClick.AddListener(Quit);

        int best = PlayerPrefs.GetInt(GameState.BestKey, 0);
        bestText.text = best > 0 ? $"Best score  <color=#FFC94A>{best}</color>" : "";
        ShowHowTo(false);
    }

    void ShowHowTo(bool show)
    {
        mainPanel.SetActive(!show);
        howToPanel.SetActive(show);
        (show ? backButton : playButton).Select();
    }

    static void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
