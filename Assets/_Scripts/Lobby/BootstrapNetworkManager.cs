using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapNetworkManager : NetworkBehaviour
{
    [SerializeField]
    private NetworkObject _playerPrefab;
    public static BootstrapNetworkManager instance;

    private void Awake() => instance = this;

    public static void ChangeNetworkScene(string sceneName, string[] scenesToClose)
    {
        instance.CloseScenes(scenesToClose);

        SceneLoadData sld = new SceneLoadData(sceneName);
        NetworkConnection[] conns = instance.ServerManager.Clients.Values.ToArray();
        instance.SceneManager.LoadConnectionScenes(conns, sld);
    }

    [ServerRpc(RequireOwnership = false)]
    void CloseScenes(string[] scenesToClose)
    {
        CloseScenesObserver(scenesToClose);
    }

    [ObserversRpc]
    void CloseScenesObserver(string[] scenesToClose)
    {
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
        if (!IsServerInitialized)
            return;

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
                break;
            }
        }
    }

    private void OnDungeonGenerated()
    {
        Vector3 spawnPosition = new Vector3(0, 1, 0);

        foreach (var conn in InstanceFinder.ServerManager.Clients.Values)
        {
            instance.SpawnPlayer(conn, spawnPosition, Quaternion.identity);
        }
    }
}
