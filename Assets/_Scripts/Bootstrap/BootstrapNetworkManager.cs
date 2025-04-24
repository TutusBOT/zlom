using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;

public class BootstrapNetworkManager : NetworkBehaviour
{
    [SerializeField]
    private NetworkObject _playerPrefab;
    public static BootstrapNetworkManager instance;

    private Dictionary<int, bool> _clientSceneLoadStatus = new Dictionary<int, bool>();

    private string _currentLoadingScene = string.Empty;

    private void Awake() => instance = this;

    public static void ChangeNetworkScene(string sceneName, string[] scenesToClose)
    {
        instance._clientSceneLoadStatus.Clear();
        instance._currentLoadingScene = sceneName;

        foreach (NetworkConnection conn in instance.NetworkObject.Observers)
        {
            instance._clientSceneLoadStatus[conn.ClientId] = false;
        }

        instance.CloseScenesObserver(scenesToClose);

        SceneLoadData sld = new SceneLoadData(sceneName);
        NetworkConnection[] conns = instance.ServerManager.Clients.Values.ToArray();
        instance.SceneManager.LoadConnectionScenes(conns, sld);
    }

    [ObserversRpc]
    void CloseScenesObserver(string[] scenesToClose)
    {
        Debug.Log("Closing scenes: " + string.Join(", ", scenesToClose));
        foreach (var sceneName in scenesToClose)
        {
            UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(sceneName);
        }
    }

    public void SpawnPlayer(NetworkConnection conn, Vector3 position, Quaternion rotation)
    {
        if (!IsServerInitialized)
            return;

        NetworkManager nm = InstanceFinder.NetworkManager;

        NetworkObject nob = nm.GetPooledInstantiated(_playerPrefab, position, rotation, true);
        nm.ServerManager.Spawn(nob, conn);

        // OnSpawned?.Invoke(nob);
    }

    private void OnEnable()
    {
        InstanceFinder.SceneManager.OnLoadEnd += OnSceneLoaded;
    }

    private void OnDisable()
    {
        InstanceFinder.SceneManager.OnLoadEnd -= OnSceneLoaded;
    }

    void OnSceneLoaded(SceneLoadEndEventArgs args)
    {
        if (!IsServerInitialized && args.LoadedScenes.Length > 0)
        {
            string loadedScene = args.LoadedScenes[0].name;
            NotifyServerSceneLoadedServerRpc(loadedScene);
        }

        if (IsServerInitialized)
        {
            foreach (var scene in args.LoadedScenes)
            {
                if (scene.name == "Dungeon3D")
                {
                    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);

                    DungeonGenerator dg = FindFirstObjectByType<DungeonGenerator>();
                    if (dg != null)
                    {
                        DungeonGenerator.DungeonGenerated += OnDungeonGenerated;
                    }

                    // Mark the server (client ID 0) as having loaded the scene
                    if (_clientSceneLoadStatus.ContainsKey(0))
                        _clientSceneLoadStatus[0] = true;

                    CheckAllClientsLoaded();

                    break;
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerSceneLoadedServerRpc(string sceneName, NetworkConnection sender = null)
    {
        if (sender == null)
            return;

        int clientId = sender.ClientId;

        if (_clientSceneLoadStatus.ContainsKey(clientId) && sceneName == _currentLoadingScene)
        {
            _clientSceneLoadStatus[clientId] = true;

            CheckAllClientsLoaded();
        }
    }

    private void CheckAllClientsLoaded()
    {
        // If all clients have loaded the scene, proceed with setting the active scene
        bool allLoaded = _clientSceneLoadStatus.All(kvp => kvp.Value);

        if (allLoaded)
        {
            SetActiveSceneObserver(_currentLoadingScene);
        }
    }

    private void OnDungeonGenerated()
    {
        Vector3[] spawnPositions = new Vector3[]
        {
            new Vector3(0, 1, 0),
            new Vector3(2, 1, 0),
            new Vector3(0, 1, 2),
            new Vector3(-2, 1, 0),
            new Vector3(0, 1, -2),
            new Vector3(2, 1, 2),
            new Vector3(-2, 1, -2),
            new Vector3(2, 1, -2),
            new Vector3(-2, 1, 2),
        };

        int spawnIndex = 0;
        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            // Use modulo to cycle through spawn positions if more players than positions
            Vector3 spawnPosition = spawnPositions[spawnIndex % spawnPositions.Length];
            instance.SpawnPlayer(conn, spawnPosition, Quaternion.identity);

            spawnIndex++;
        }

        DungeonGenerator.DungeonGenerated -= OnDungeonGenerated;
    }

    [ObserversRpc]
    void SetActiveSceneObserver(string sceneName)
    {
        Debug.Log($"Setting active scene to: {sceneName}");
        var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(sceneName);
        if (scene.IsValid())
        {
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
            Debug.Log($"Successfully set active scene to: {sceneName}");
        }
        else
        {
            Debug.LogError($"Failed to set active scene: {sceneName} is not loaded!");
        }
    }
}
