// Create NetworkUI.cs
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
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

    [SerializeField]
    private Button disconnectButton;

    [SerializeField]
    private TextMeshProUGUI statusText;

    private void Start()
    {
        if (hostButton)
            hostButton.onClick.AddListener(() =>
            {
                connectionManager.StartHost();
                UpdateStatusText();
            });

        if (joinButton)
            joinButton.onClick.AddListener(() =>
            {
                if (ulong.TryParse(steamIDInput.text, out ulong id))
                {
                    connectionManager.JoinGame(new CSteamID(id));
                }
            });

        if (disconnectButton)
            disconnectButton.onClick.AddListener(() =>
            {
                connectionManager.StopConnection();
                statusText.text = "Disconnected";
            });

        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText && SteamManager.Initialized)
        {
            string name = SteamFriends.GetPersonaName();
            ulong id = SteamUser.GetSteamID().m_SteamID;
            statusText.text = $"User: {name}\nSteam ID: {id}";
        }
    }
}
