using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class ValuableSpawner : NetworkBehaviour
{
    [System.Serializable]
    public class ValuablePrefab
    {
        public GameObject prefab;
        public ValuableSize size;
        public float spawnWeight = 1f;
    }

    [Header("Valuable Settings")]
    public List<ValuablePrefab> valuablePrefabs = new List<ValuablePrefab>();

    [Header("Spawn Settings")]
    public float spawnDensity = 0.5f; // Overall percentage of spawn points to use
    public bool debug = false;

    private List<GameObject> spawnedValuables = new List<GameObject>();

    public void ClearValuables()
    {
        foreach (GameObject valuable in spawnedValuables)
        {
            if (valuable != null)
                Destroy(valuable);
        }

        spawnedValuables.Clear();
    }

    public void SpawnValuablesInRooms()
    {
        GameObject[] roomObjects = GameObject.FindGameObjectsWithTag("Room");
        SpawnValuablesInRooms(roomObjects);
    }

    public void SpawnValuablesInRooms(GameObject[] roomObjects)
    {
        if (roomObjects == null || roomObjects.Length == 0)
        {
            Debug.LogWarning("No rooms provided to spawn valuables in");
            return;
        }

        foreach (GameObject roomObject in roomObjects)
        {
            SpawnValuablesInRoom(roomObject);
        }
    }

    public void SpawnValuablesInRoom(GameObject roomObject)
    {
        if (roomObject == null)
        {
            Debug.LogWarning("Room object is null");
            return;
        }

        if (!IsServerInitialized)
        {
            Debug.LogWarning("Attempted to spawn valuables from client");
            return;
        }

        ValuableSpawnPoint[] spawnPoints = roomObject.GetComponentsInChildren<ValuableSpawnPoint>();

        if (spawnPoints.Length == 0)
        {
            Debug.LogWarning($"No spawn points found in room: {roomObject.name}");
            return;
        }

        ShuffleArray(spawnPoints);

        // Determine how many to fill based on density
        int pointsToFill = Mathf.FloorToInt(spawnPoints.Length * spawnDensity);

        for (int i = 0; i < pointsToFill; i++)
        {
            if (i >= spawnPoints.Length)
                break;

            ValuableSpawnPoint spawnPoint = spawnPoints[i];

            // Find valid valuable for this spawn point
            GameObject valuablePrefab = GetRandomValuableForSize(
                spawnPoint.allowedSize,
                spawnPoint.allowSmallerSizes
            );

            if (valuablePrefab != null)
            {
                // First instantiate locally
                GameObject valuable = Instantiate(
                    valuablePrefab,
                    spawnPoint.transform.position,
                    Quaternion.Euler(0, Random.Range(0, 360), 0)
                );

                valuable.layer = LayerMask.NameToLayer("Interactable");

                // Then spawn on the network
                NetworkObject networkObject = valuable.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    InstanceFinder.ServerManager.Spawn(valuable);
                }
                else
                {
                    Debug.LogError(
                        $"Valuable prefab {valuablePrefab.name} does not have a NetworkObject component!"
                    );
                }

                // Track spawned valuable
                spawnedValuables.Add(valuable);
            }
        }
    }

    private GameObject GetRandomValuableForSize(ValuableSize size, bool allowSmallerSizes)
    {
        List<ValuablePrefab> validValuables = new List<ValuablePrefab>();

        foreach (ValuablePrefab valuable in valuablePrefabs)
        {
            if (valuable.size == size || (allowSmallerSizes && valuable.size < size))
            {
                validValuables.Add(valuable);
            }
        }

        if (validValuables.Count == 0)
            return null;

        // Get weighted random selection
        float totalWeight = 0;
        foreach (var valuable in validValuables)
            totalWeight += valuable.spawnWeight;

        float random = Random.Range(0, totalWeight);
        float weightSum = 0;

        foreach (var valuable in validValuables)
        {
            weightSum += valuable.spawnWeight;
            if (random <= weightSum)
                return valuable.prefab;
        }

        return validValuables[0].prefab;
    }

    private void ShuffleArray<T>(T[] array)
    {
        for (int i = 0; i < array.Length; i++)
        {
            int randomIndex = Random.Range(i, array.Length);
            T temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}
