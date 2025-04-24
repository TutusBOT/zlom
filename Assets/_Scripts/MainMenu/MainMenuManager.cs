using System;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    private static MainMenuManager instance;

    [SerializeField]
    private GameObject menuScreen,
        lobbyScreen;

    [SerializeField]
    private TMP_InputField lobbyInput;

    [SerializeField]
    private TextMeshProUGUI lobbyTitle,
        lobbyIDText;

    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private TextMeshProUGUI lobbyPlayerListText;

    [SerializeField]
    private Button leaveLobbyButton;

    [SerializeField]
    private Button copyLobbyIDButton;

#pragma warning disable IDE0052 // Remove unread private member - needs to be kept for Steam callback
    private Callback<LobbyChatUpdate_t> lobbyChatUpdate;
#pragma warning restore IDE0052

    private void Awake() => instance = this;

    private void Start()
    {
        OpenMainMenu();
        lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    public void CreateLobby()
    {
        BootstrapManager.CreateLobby();
    }

    public void OpenMainMenu()
    {
        CloseAllScreens();
        menuScreen.SetActive(true);
    }

    public void OpenLobby()
    {
        CloseAllScreens();
        lobbyScreen.SetActive(true);
    }

    public static void LobbyEntered(string lobbyName, bool isHost)
    {
        instance.lobbyTitle.text = lobbyName;

        instance.startGameButton.gameObject.SetActive(isHost);
        instance.startGameButton.onClick.AddListener(instance.StartGame);
        instance.leaveLobbyButton.onClick.AddListener(instance.LeaveLobby);
        instance.leaveLobbyButton.onClick.AddListener(instance.LeaveLobby);

        instance.lobbyIDText.text = BootstrapManager.CurrentLobbyID.ToString();
        instance.OpenLobby();
        instance.UpdateLobbyPlayerList();
    }

    void CloseAllScreens()
    {
        menuScreen.SetActive(false);
        lobbyScreen.SetActive(false);
    }

    public void JoinLobby()
    {
        CSteamID steamID = new CSteamID(Convert.ToUInt64(lobbyInput.text));
        BootstrapManager.JoinByID(steamID);
    }

    public void LeaveLobby()
    {
        BootstrapManager.LeaveLobby();
        OpenMainMenu();
    }

    public void StartGame()
    {
        string[] scenesToClose = new string[] { "Menu" };
        BootstrapNetworkManager.ChangeNetworkScene("Dungeon3D", scenesToClose);
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        UpdateLobbyPlayerList();
    }

    public void UpdateLobbyPlayerList()
    {
        if (!SteamManager.Initialized || BootstrapManager.CurrentLobbyID == 0)
        {
            lobbyPlayerListText.text = "No players in lobby.";
            return;
        }

        CSteamID lobbyID = new CSteamID(BootstrapManager.CurrentLobbyID);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyID);

        string playerList = "";
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberSteamID = SteamMatchmaking.GetLobbyMemberByIndex(lobbyID, i);
            string name = SteamFriends.GetFriendPersonaName(memberSteamID);
            playerList += name + "\n";
        }

        lobbyPlayerListText.text = playerList;
    }

    public void CopyLobbyIDToClipboard()
    {
        if (BootstrapManager.CurrentLobbyID != 0)
        {
            string lobbyID = BootstrapManager.CurrentLobbyID.ToString();
            GUIUtility.systemCopyBuffer = lobbyID;

            Debug.Log("Lobby ID copied to clipboard: " + lobbyID);
        }
    }
}
