using FishNet;
using FishNet.Managing;
using Steamworks;
using UnityEngine;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManager;

    private FishySteamworks.FishySteamworks SteamTransport
    {
        get
        {
            if (networkManager == null)
                return null;
            return networkManager.GetComponent<FishySteamworks.FishySteamworks>()
                ?? networkManager.GetComponentInChildren<FishySteamworks.FishySteamworks>();
        }
    }

    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam not initialized");
            return;
        }

        if (networkManager == null)
            networkManager = InstanceFinder.NetworkManager;
    }

    public void StartHost()
    {
        if (networkManager == null || !SteamManager.Initialized)
            return;

        Debug.Log("Starting host...");
        networkManager.ServerManager.StartConnection();
        networkManager.ClientManager.StartConnection();

        Debug.Log($"Host started with Steam ID: {SteamUser.GetSteamID()}");
    }

    public void JoinGame(CSteamID hostSteamID)
    {
        if (networkManager == null || !SteamManager.Initialized)
            return;

        // Get transport when needed
        var transport = SteamTransport;
        if (transport == null)
        {
            Debug.LogError("FishySteamworks transport not found on NetworkManager");
            return;
        }

        Debug.Log($"Joining game hosted by {hostSteamID}...");
        transport.SetClientAddress(hostSteamID.m_SteamID.ToString());
        networkManager.ClientManager.StartConnection();
    }

    public void StopConnection()
    {
        if (networkManager == null)
            return;

        if (networkManager.IsServer)
            networkManager.ServerManager.StopConnection(true);

        if (networkManager.IsClient)
            networkManager.ClientManager.StopConnection();

        Debug.Log("Connection stopped");
    }
}
