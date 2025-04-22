using FishNet;
using FishNet.Managing;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private ConnectionManager connectionManager;

    [SerializeField]
    private bool useSteam = true;

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
                if (!useSteam)
                {
                    NetworkManager networkManager = InstanceFinder.NetworkManager;
                    networkManager.ServerManager.StartConnection();
                    SceneManager.LoadScene("Lobby");
                    return;
                }
                connectionManager.StartHost();
                SceneManager.LoadScene("Lobby");
            });

        if (joinButton)
            joinButton.onClick.AddListener(() =>
            {
                if (!useSteam)
                {
                    NetworkManager networkManager = InstanceFinder.NetworkManager;
                    networkManager.ClientManager.StartConnection();
                    SceneManager.LoadScene("Lobby");
                    return;
                }

                if (ulong.TryParse(steamIDInput.text, out ulong id))
                {
                    connectionManager.JoinGame(new CSteamID(id));
                    SceneManager.LoadScene("Lobby");
                }
            });
    }
}
