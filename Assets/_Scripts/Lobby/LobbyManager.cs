using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Steamworks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyManager : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private Button startGameButton;

    [SerializeField]
    private Transform playerListContainer;

    [SerializeField]
    private GameObject playerEntryPrefab;

    [SerializeField]
    private TMP_Text lobbyCodeText;

    [Header("Scene Settings")]
    [SerializeField]
    private string gameSceneName = "Game";

    [SerializeField]
    private GameObject playerPrefab; // Reference to player prefab

    // Store the original player prefab for when we want to start spawning
    private static GameObject _storedPlayerPrefab;

    private readonly SyncList<PlayerInfo> _connectedPlayers = new();

    // Local cache of UI elements
    private Dictionary<ulong, GameObject> _playerListItems = new Dictionary<ulong, GameObject>();

    // Structure to store player information
    public struct PlayerInfo
    {
        public ulong SteamId;
        public string PlayerName;
        public bool IsHost;

        public PlayerInfo(ulong steamId, string playerName, bool isHost)
        {
            SteamId = steamId;
            PlayerName = playerName;
            IsHost = isHost;
        }
    }

    private void Awake()
    {
        // Initialize SyncList callbacks
        _connectedPlayers.OnChange += OnPlayerListChanged;

        // Store the player prefab when first loaded (if not already stored)
        if (_storedPlayerPrefab == null)
        {
            _storedPlayerPrefab = playerPrefab;
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Set up events
        if (IsServerInitialized)
        {
            NetworkManager.ServerManager.OnRemoteConnectionState +=
                ServerManager_OnRemoteConnectionState;

            // Disable automatic spawning when the server starts
            ConfigureSpawning(false);
        }

        // Set up local player on startup
        if (SteamManager.Initialized)
        {
            RegisterLocalPlayer();
        }

        // Configure UI based on server/client status
        ConfigureUI();
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        if (IsServerInitialized)
        {
            NetworkManager.ServerManager.OnRemoteConnectionState -=
                ServerManager_OnRemoteConnectionState;
        }

        // Remove callback to prevent memory leaks
        _connectedPlayers.OnChange -= OnPlayerListChanged;
    }

    private void ConfigureSpawning(bool enable)
    {
        if (enable)
        {
            Debug.Log("Enabling player spawning");
            // If you need to re-enable automatic player spawning, implement here
        }
        else
        {
            Debug.Log("Disabling player spawning");
            // If you need to disable automatic player spawning, implement here
        }
    }

    private void ConfigureUI()
    {
        // Only host can see and use the start button
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(IsServerInitialized);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        // Show lobby code if we're the host
        if (lobbyCodeText != null && IsServerInitialized && SteamManager.Initialized)
        {
            lobbyCodeText.text = $"Lobby Code: {SteamUser.GetSteamID()}";
        }
    }

    private void RegisterLocalPlayer()
    {
        // If server, add ourselves directly, otherwise request through RPC
        if (IsServerInitialized)
        {
            AddPlayer(SteamUser.GetSteamID().m_SteamID, SteamFriends.GetPersonaName(), true);
        }
        else
        {
            RegisterPlayerServerRpc(
                SteamUser.GetSteamID().m_SteamID,
                SteamFriends.GetPersonaName()
            );
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RegisterPlayerServerRpc(ulong steamId, string playerName)
    {
        // Add the player to our sync list
        AddPlayer(steamId, playerName, false);
    }

    private void ServerManager_OnRemoteConnectionState(
        NetworkConnection conn,
        RemoteConnectionStateArgs args
    )
    {
        if (args.ConnectionState == RemoteConnectionState.Started)
        {
            // A new client connected, they'll register themselves
            Debug.Log($"Client connected: {conn.ClientId}");
        }
        else if (args.ConnectionState == RemoteConnectionState.Stopped)
        {
            // Find and remove disconnected player
            for (int i = 0; i < _connectedPlayers.Count; i++)
            {
                // Fix the type comparison by casting
                if (_connectedPlayers[i].SteamId == (ulong)conn.ClientId)
                {
                    _connectedPlayers.RemoveAt(i);
                    break;
                }
            }
        }
    }

    // Add a player to the sync list (server only)
    private void AddPlayer(ulong steamId, string playerName, bool isHost)
    {
        if (!IsServerInitialized)
            return;

        // Create player info
        PlayerInfo newPlayer = new PlayerInfo(steamId, playerName, isHost);

        // Add to sync list
        _connectedPlayers.Add(newPlayer);

        Debug.Log($"Added player to lobby: {playerName} (Host: {isHost})");
    }

    // Called when the player list changes (on all clients)
    private void OnPlayerListChanged(
        SyncListOperation op,
        int index,
        PlayerInfo oldItem,
        PlayerInfo newItem,
        bool asServer
    )
    {
        // Update UI based on the operation
        switch (op)
        {
            case SyncListOperation.Add:
                CreatePlayerListItem(newItem);
                break;

            case SyncListOperation.RemoveAt:
                RemovePlayerListItem(oldItem.SteamId);
                break;

            case SyncListOperation.Complete:
                // Refresh entire list
                RefreshPlayerList();
                break;
        }
    }

    // Creates a UI element for a player
    private void CreatePlayerListItem(PlayerInfo player)
    {
        if (playerEntryPrefab == null || playerListContainer == null)
            return;

        // Create the UI element
        GameObject newEntry = Instantiate(playerEntryPrefab, playerListContainer);

        // Set up the UI with player info
        TMP_Text nameText = newEntry.GetComponentInChildren<TMP_Text>();
        if (nameText != null)
        {
            nameText.text = player.IsHost ? $"{player.PlayerName} (Host)" : player.PlayerName;
        }

        // Store reference for later
        _playerListItems[player.SteamId] = newEntry;
    }

    // Removes a player's UI element
    private void RemovePlayerListItem(ulong steamId)
    {
        if (_playerListItems.TryGetValue(steamId, out GameObject entry))
        {
            Destroy(entry);
            _playerListItems.Remove(steamId);
        }
    }

    // Refreshes the entire player list
    private void RefreshPlayerList()
    {
        // Clear existing items
        foreach (var entry in _playerListItems.Values)
        {
            Destroy(entry);
        }
        _playerListItems.Clear();

        // Create new items
        foreach (var player in _connectedPlayers)
        {
            CreatePlayerListItem(player);
        }
    }

    // Start game button handler
    private void OnStartGameClicked()
    {
        if (IsServerInitialized)
        {
            StartGame();
        }
    }

    // Direct method to start the game (server only)
    private void StartGame()
    {
        if (!IsServerInitialized)
            return;

        // Notify all clients to load the game scene
        StartGameClientRpc();

        // Enable player spawning
        EnablePlayerSpawning();
    }

    [ObserversRpc(RunLocally = true)]
    private void StartGameClientRpc()
    {
        SceneLoadData sld = new SceneLoadData(gameSceneName);
        sld.ReplaceScenes = ReplaceOption.All;
        SceneManager.LoadGlobalScenes(sld);
    }

    // Re-enable player spawning before switching scenes
    private void EnablePlayerSpawning()
    {
        if (_storedPlayerPrefab == null)
        {
            Debug.LogError("No stored player prefab found! Players won't spawn.");
            return;
        }

        // Enable player spawning
        ConfigureSpawning(true);

        // Manually spawn players - implementation depends on your FishNet setup
        if (IsServerInitialized)
        {
            Debug.Log("Spawning players for all connections");
            foreach (var client in NetworkManager.ServerManager.Clients.Values)
            {
                SpawnPlayerManually(client);
            }
        }
    }

    private void SpawnPlayerManually(NetworkConnection conn)
    {
        try
        {
            Vector3 spawnPosition = new Vector3(0, 1, 0);
            GameObject playerObj = Instantiate(_storedPlayerPrefab, spawnPosition, Quaternion.identity);
            NetworkObject nob = playerObj.GetComponent<NetworkObject>();
            if (nob != null)
            {
                // Spawn the player and give ownership to the connection
                NetworkManager.ServerManager.Spawn(playerObj, conn);
                Debug.Log($"Spawned player for connection {conn.ClientId}");
            }
            else
            {
                Debug.LogError("Player prefab missing NetworkObject component!");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error spawning player: {ex.Message}");
        }
    }
}
