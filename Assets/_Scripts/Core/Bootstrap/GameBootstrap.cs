using System.Collections;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

/// <summary>
/// Single entry point for initializing all game systems.
/// Add this to your initial scene and it will ensure all required
/// managers and controllers are created and initialized properly.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    public static GameBootstrap Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Should this object persist between scenes?")]
    public bool dontDestroyOnLoad = true;

    [Tooltip("Should systems be initialized on Awake or manually?")]
    public bool initializeOnAwake = true;

    [Header("Required Prefabs")]
    [Tooltip("Prefabs for systems that should be instantiated if not found")]
    public List<GameObject> systemPrefabs = new List<GameObject>();

    [Tooltip("Prefabs that should be spawned over the network (server only)")]
    public List<GameObject> networkSystemPrefabs = new List<GameObject>();

    private List<GameObject> _instantiatedSystems = new List<GameObject>();
    private bool _initialized = false;
    private bool _networkInitialized = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            if (initializeOnAwake)
                InitializeSystems();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState +=
                ServerManager_OnServerConnectionState;
        }
        else
        {
            Debug.LogWarning("ServerManager not found, waiting for it to initialize");
            StartCoroutine(WaitForNetworkManager());
        }
    }

    private IEnumerator WaitForNetworkManager()
    {
        // Wait until NetworkManager is available
        while (InstanceFinder.NetworkManager == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        InstanceFinder.ServerManager.OnServerConnectionState +=
            ServerManager_OnServerConnectionState;
    }

    private void OnDestroy()
    {
        // Clean up event subscriptions
        if (InstanceFinder.ServerManager != null)
        {
            InstanceFinder.ServerManager.OnServerConnectionState -=
                ServerManager_OnServerConnectionState;
        }
    }

    private void ServerManager_OnServerConnectionState(
        FishNet.Transporting.ServerConnectionStateArgs args
    )
    {
        // When server starts, initialize network systems
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Started)
        {
            InitializeNetworkSystems();
        }
    }

    /// <summary>
    /// Initialize all game systems in the proper order
    /// </summary>
    public void InitializeSystems()
    {
        if (_initialized)
            return;

        foreach (GameObject prefab in systemPrefabs)
        {
            if (prefab == null)
                continue;

            var components = prefab.GetComponents<MonoBehaviour>();
            if (components.Length == 0)
                continue;

            // Use the first MonoBehaviour as the system type
            System.Type systemType = components[0].GetType();

            if (FindFirstObjectByType(systemType) == null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name;

                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(instance);

                _instantiatedSystems.Add(instance);
            }
            else
            {
                Debug.Log($"System already exists: {systemType.Name}");
            }
        }

        // Now force initialization order for any managers with InitializeSystem method
        foreach (GameObject system in _instantiatedSystems)
        {
            // Look for an initialization method
            var initializables = system.GetComponents<IInitializable>();
            foreach (var initializable in initializables)
            {
                initializable.Initialize();
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// Initialize network-based systems (server-only)
    /// </summary>
    public void InitializeNetworkSystems()
    {
        if (_networkInitialized)
            return;

        // Only run on the server
        if (InstanceFinder.ServerManager == null || !InstanceFinder.ServerManager.Started)
        {
            return;
        }

        foreach (GameObject prefab in networkSystemPrefabs)
        {
            if (prefab == null)
                continue;

            var components = prefab.GetComponents<MonoBehaviour>();
            if (components.Length == 0)
                continue;

            // Use the first MonoBehaviour as the system type
            System.Type systemType = components[0].GetType();

            if (FindFirstObjectByType(systemType) == null)
            {
                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name;

                NetworkObject networkObject = instance.GetComponent<NetworkObject>();

                if (networkObject != null)
                {
                    InstanceFinder.ServerManager.Spawn(instance);
                }
                else
                {
                    Debug.LogError($"Prefab {prefab.name} doesn't have a NetworkObject component!");
                    Destroy(instance);
                }
            }
            else
            {
                Debug.Log($"Network system already exists: {systemType.Name}");
            }
        }

        _networkInitialized = true;
    }
}

/// <summary>
/// Interface for systems that need special initialization
/// </summary>
public interface IInitializable
{
    void Initialize();
}
