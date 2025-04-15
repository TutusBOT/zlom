using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private ConnectionManager connectionManager;

    [Header("UI Elements")]
    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private TMP_InputField steamIDInput;

    [SerializeField]
    private Button joinButton;

    private void Start()
    {
        if (hostButton)
            hostButton.onClick.AddListener(() =>
            {
                connectionManager.StartHost();
                SceneManager.LoadScene("Lobby");
            });

        if (joinButton)
            joinButton.onClick.AddListener(() =>
            {
                if (ulong.TryParse(steamIDInput.text, out ulong id))
                {
                    connectionManager.JoinGame(new CSteamID(id));
                    SceneManager.LoadScene("Lobby");
                }
            });
    }
}
