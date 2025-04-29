using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;
using Random = UnityEngine.Random;

public class EnemySpawnController : NetworkBehaviour
{
    [Tooltip("Enemy spawn configurations")]
    [SerializeField]
    private List<EnemySpawnConfig> enemyPrefabs = new List<EnemySpawnConfig>();

    [Tooltip("Min distance from player to spawn enemies")]
    [SerializeField]
    private float minDistanceFromPlayer = 10f;

    [Tooltip("Max distance from player to spawn enemies")]
    [SerializeField]
    private float maxDistanceFromPlayer = 50f;

    [SerializeField]
    private bool debugMode = false;

    private List<GameObject> activeEnemies = new List<GameObject>();

    [Serializable]
    public class EnemySpawnConfig
    {
        public GameObject prefab;

        [Range(0f, 100f)]
        [Tooltip("Chance of this enemy spawning (0-100%)")]
        public float spawnChance = 25f;

        [Tooltip("Maximum number of this enemy type that can exist at once")]
        public int maxCount = 5;

        [Tooltip("Enemy tier (1-3), higher tier enemies are stronger but cost more power")]
        [Range(1, 3)]
        public int tier = 1;

        [HideInInspector]
        public int currentCount = 0;
    }

    private EnemySpawnConfig SelectRandomEnemy()
    {
        if (enemyPrefabs.Count == 0)
            throw new Exception("No enemy prefabs assigned!");

        float totalWeight = 0f;
        List<EnemySpawnConfig> availablePrefabs = new List<EnemySpawnConfig>();

        foreach (var config in enemyPrefabs)
        {
            if (config.prefab != null && config.currentCount < config.maxCount)
            {
                totalWeight += config.spawnChance;
                availablePrefabs.Add(config);
            }
        }

        if (availablePrefabs.Count == 0)
            throw new Exception("No available enemy prefabs to spawn!");

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var config in availablePrefabs)
        {
            currentWeight += config.spawnChance;

            if (randomValue <= currentWeight)
            {
                return config;
            }
        }

        return availablePrefabs[0];
    }

    private EnemySpawnConfig SelectRandomEnemy(int powerBudget)
    {
        if (enemyPrefabs.Count == 0)
            throw new Exception("No enemy prefabs assigned!");

        float totalWeight = 0f;
        List<EnemySpawnConfig> availablePrefabs = new List<EnemySpawnConfig>();

        foreach (var config in enemyPrefabs)
        {
            if (
                config.prefab != null
                && config.currentCount < config.maxCount
                && config.tier <= powerBudget
            )
            {
                totalWeight += config.spawnChance;
                availablePrefabs.Add(config);
            }
        }

        if (availablePrefabs.Count == 0)
            throw new Exception("No available enemy prefabs to spawn!");

        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var config in availablePrefabs)
        {
            currentWeight += config.spawnChance;

            if (randomValue <= currentWeight)
            {
                return config;
            }
        }

        return availablePrefabs[0];
    }

    public void SpawnEnemies(Transform[] roomsTransform)
    {
        if (roomsTransform == null || roomsTransform.Length == 0)
        {
            Debug.LogWarning("No rooms provided for enemy spawning");
            return;
        }

        Bounds[] roomsBounds = new Bounds[roomsTransform.Length];
        for (int i = 0; i < roomsTransform.Length; i++)
        {
            roomsBounds[i] = GetRoomBounds(roomsTransform[i]);
        }

        if (debugMode)
        {
            Debug.Log($"Spawning 3 enemies in the first room from the provided rooms");
        }

        for (int i = 0; i < 3; i++)
        {
            Vector3 spawnPosition = FindPositionInRoom(
                roomsBounds[Random.Range(0, roomsBounds.Length)]
            );
            if (spawnPosition == Vector3.zero)
            {
                spawnPosition = roomsBounds[0].center; // Fallback to the first room center
            }

            GameObject enemyPrefab = SelectRandomEnemy().prefab;

            SpawnEnemy(enemyPrefab, spawnPosition);

            if (debugMode)
            {
                Debug.Log($"Spawned enemy {i + 1} of 3 in the first room");
            }
        }
    }

    public void SpawnEnemies(Transform[] roomsTransform, int powerBudget)
    {
        if (roomsTransform == null || roomsTransform.Length == 0)
        {
            Debug.LogWarning("No rooms provided for enemy spawning");
            return;
        }

        Bounds[] roomsBounds = new Bounds[roomsTransform.Length];
        for (int i = 0; i < roomsTransform.Length; i++)
        {
            roomsBounds[i] = GetRoomBounds(roomsTransform[i]);
        }

        if (debugMode)
        {
            Debug.Log($"Spawning 3 enemies in the first room from the provided rooms");
        }
        // Spawn enemies based on power budget
        int remainingPower = powerBudget;

        while (remainingPower > 0)
        {
            Vector3 spawnPosition = FindPositionInRoom(
                roomsBounds[Random.Range(0, roomsBounds.Length)]
            );
            if (spawnPosition == Vector3.zero)
            {
                spawnPosition = roomsBounds[0].center; // Fallback to the first room center
            }

            EnemySpawnConfig enemy = SelectRandomEnemy(remainingPower);
            remainingPower -= enemy.tier;
            if (debugMode)
            {
                Debug.Log("-------------------------------------------------------");
                Debug.Log($"Enemy tier: {enemy.tier}");
                Debug.Log($"Remaining power: {remainingPower}");
            }

            SpawnEnemy(enemy.prefab, spawnPosition);
        }
    }

    public void SpawnEnemy(GameObject enemyPrefab, Vector3 spawnPosition)
    {
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

        var enemyNetObj = enemy.GetComponent<NetworkObject>();

        InstanceFinder.ServerManager.Spawn(enemyNetObj);

        activeEnemies.Add(enemy);

        foreach (var config in enemyPrefabs)
        {
            if (config.prefab == enemyPrefab)
            {
                config.currentCount++;
                break;
            }
        }

        if (debugMode)
        {
            Debug.Log($"[Server] Enemy spawned: {enemy.name} at {spawnPosition}");
        }
    }

    private Bounds GetRoomBounds(Transform roomTransform)
    {
        Bounds roomBounds = new Bounds(roomTransform.position, Vector3.zero);
        var renderers = roomTransform.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r != null)
                roomBounds.Encapsulate(r.bounds);
        }

        return roomBounds;
    }

    private Vector3 FindPositionInRoom(Bounds roomBounds)
    {
        for (int i = 0; i < 30; i++) // Try 30 times
        {
            // Random position within bounds
            float x = Random.Range(roomBounds.min.x + 1f, roomBounds.max.x - 1f); // Buffer from walls
            float z = Random.Range(roomBounds.min.z + 1f, roomBounds.max.z - 1f);

            Vector3 potentialPosition = new Vector3(x, roomBounds.center.y, z);

            UnityEngine.AI.NavMeshHit hit;
            if (
                UnityEngine.AI.NavMesh.SamplePosition(
                    potentialPosition,
                    out hit,
                    2f,
                    UnityEngine.AI.NavMesh.AllAreas
                )
            )
            {
                return hit.position;
            }
        }

        Debug.LogWarning($"[FindPositionInRoom] Position not found in room: {roomBounds}");
        return Vector3.zero;
    }
}
