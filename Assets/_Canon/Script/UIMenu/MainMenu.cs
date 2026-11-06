using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Main menu controller.
/// Attach to an empty GameObject (e.g. "MainMenu") in your menu scene.
/// Assign buttons in the Inspector. Anything left unassigned is simply skipped,
/// so the scene runs fine with no Play button (or any other button) assigned.
/// </summary>
public class MainMenu : MonoBehaviour
{
    [Header("Buttons (all optional)")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;
    [Tooltip("Placed inside the options panel. Closes it.")]
    [SerializeField] private Button backButton;

    [Header("Play")]
    [Tooltip("Scene name to load when Play is pressed. Leave empty to do nothing.")]
    [SerializeField] private string gameSceneName = "";

    [Header("Panels (optional)")]
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        // Null-checked everywhere: missing references never throw.
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayPressed);
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsPressed);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitPressed);
        if (backButton != null)
            backButton.onClick.AddListener(OnBackFromSettings);

        // Main menu stays visible; options starts closed.
        if (mainPanel != null)
            mainPanel.SetActive(true);
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playButton != null)
            playButton.onClick.RemoveListener(OnPlayPressed);
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsPressed);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitPressed);
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackFromSettings);
    }

    // ---- Button handlers (public so you can also wire them via the Inspector's onClick) ----

    public void OnPlayPressed()
    {
        if (string.IsNullOrEmpty(gameSceneName))
        {
            Debug.LogWarning("MainMenu: No game scene assigned. Play does nothing for now.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogWarning($"MainMenu: Scene '{gameSceneName}' is not in Build Settings.");
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// Placeholder. Wire your own logic to the settings Button's built-in
    /// onClick event in the Inspector; this only toggles an optional panel.
    /// </summary>
    public void OnSettingsPressed()
    {
        // Opens the options panel on top; the main menu stays active.
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnBackFromSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnQuitPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
