using FishNet.Object;
using UnityEngine;
using UnityEngine.UI;

public class PauseManager : NetworkBehaviour
{
    public static PauseManager Instance { get; private set; }

    [Header("Pause Menu")]
    [SerializeField]
    private GameObject _pauseMenuPanel;

    [SerializeField]
    private Button _resumeButton;

    [SerializeField]
    private Button _settingsButton;

    [SerializeField]
    private Button _quitButton;

    [SerializeField]
    private Button _returnToLobbyButton;

    private bool _isPaused = false;
    public bool IsPaused => _isPaused;

    private void Awake()
    {
        Instance = this;

        // Initialize pause menu in hidden state
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(false);

        // Set up button listeners
        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(ResumeGame);

        if (_settingsButton != null)
            _settingsButton.onClick.AddListener(OpenSettings);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(QuitGame);

        if (_returnToLobbyButton != null)
            _returnToLobbyButton.onClick.AddListener(ReturnToLobby);
    }

    private void OnDestroy()
    {
        // Clean up listeners
        if (_resumeButton != null)
            _resumeButton.onClick.RemoveListener(ResumeGame);

        if (_settingsButton != null)
            _settingsButton.onClick.RemoveListener(OpenSettings);

        if (_quitButton != null)
            _quitButton.onClick.RemoveListener(QuitGame);

        if (_returnToLobbyButton != null)
            _returnToLobbyButton.onClick.RemoveListener(ReturnToLobby);
    }

    private void Update()
    {
        if (!IsOwner)
            return;

        if (InputBindingManager.Instance.IsActionTriggered(InputActions.Pause))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (_isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    public void PauseGame()
    {
        _isPaused = true;

        // Show cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show pause menu
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(true);
    }

    public void ResumeGame()
    {
        _isPaused = false;

        // Hide cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Hide pause menu
        if (_pauseMenuPanel != null)
            _pauseMenuPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        // TODO: Open settings panel
        Debug.Log("Settings menu not yet implemented");
    }

    public void ReturnToLobby()
    {
        // Only the server can change scenes
        if (IsServerInitialized)
        {
            // Load lobby scene directly
            SceneController.Instance.LoadScene("Lobby");
        }
        else
        {
            // Client requests server to change scene
            RequestSceneChangeServerRpc("Lobby");
        }
    }

    [ServerRpc]
    private void RequestSceneChangeServerRpc(string sceneName)
    {
        // Security check - only allow certain scenes
        if (sceneName == "Lobby")
        {
            SceneController.Instance.LoadScene(sceneName);
        }
    }

    public void QuitGame()
    {
        // If we're in the editor
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If we're in a build
        Application.Quit();
#endif
    }
}
