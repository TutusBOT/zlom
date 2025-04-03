using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Single entry point for initializing all game systems.
/// Add this to your initial scene and it will ensure all required
/// managers and controllers are created and initialized properly.
/// </summary>
public class GameBootstrap : MonoBehaviour
{
    // Singleton instance
    public static GameBootstrap Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Should this object persist between scenes?")]
    public bool dontDestroyOnLoad = true;

    [Tooltip("Should systems be initialized on Awake or manually?")]
    public bool initializeOnAwake = true;

    [Header("Required Prefabs")]
    [Tooltip("Prefabs for systems that should be instantiated if not found")]
    public List<GameObject> systemPrefabs = new List<GameObject>();

    // Track created systems
    private List<GameObject> _instantiatedSystems = new List<GameObject>();
    private bool _initialized = false;

    private void Awake()
    {
        // Singleton pattern
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

    /// <summary>
    /// Initialize all game systems in the proper order
    /// </summary>
    public void InitializeSystems()
    {
        if (_initialized)
            return;

        Debug.Log("Initializing game systems...");

        // Create all required systems if they don't already exist
        foreach (GameObject prefab in systemPrefabs)
        {
            if (prefab == null)
                continue;

            // Get the type of system from the prefab
            var components = prefab.GetComponents<MonoBehaviour>();
            if (components.Length == 0)
                continue;

            // Use the first MonoBehaviour as the system type
            System.Type systemType = components[0].GetType();

            // Check if this system already exists
            if (FindObjectOfType(systemType) == null)
            {
                // System doesn't exist, create it
                GameObject instance = Instantiate(prefab);
                instance.name = prefab.name;

                if (dontDestroyOnLoad)
                    DontDestroyOnLoad(instance);

                _instantiatedSystems.Add(instance);
                Debug.Log($"Created system: {prefab.name}");
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
        Debug.Log("All game systems initialized");
    }
}

/// <summary>
/// Interface for systems that need special initialization
/// </summary>
public interface IInitializable
{
    void Initialize();
}
