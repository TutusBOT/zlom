using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Managing;
using FishNet;


public class EnemySpawnController : NetworkBehaviour
{
    [Tooltip("Enemy spawn configurations")]
    [SerializeField]
    private List<EnemySpawnConfig> enemyPrefabs = new List<EnemySpawnConfig>();

    [Header("Spawn Settings")]
    [Tooltip("Maximum number of enemies that can be spawned at once")]
    [SerializeField]
    private int maxEnemies = 10;

    [Tooltip("Delay between enemy spawns in seconds")]
    [SerializeField]
    private float spawnInterval = 5f;

    [Tooltip("Min distance from player to spawn enemies")]
    [SerializeField]
    private float minDistanceFromPlayer = 10f;

    [Tooltip("Max distance from player to spawn enemies")]
    [SerializeField]
    private float maxDistanceFromPlayer = 50f;

    [SerializeField]
    private bool debugMode = false;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Transform playerTransform;
    private bool isSpawning = false;

    [Serializable]
    public class EnemySpawnConfig
    {
        public GameObject enemyPrefab;

        [Range(0f, 100f)]
        [Tooltip("Chance of this enemy spawning (0-100%)")]
        public float spawnChance = 25f;

        [Tooltip("Maximum number of this enemy type that can exist at once")]
        public int maxCount = 5;

        [HideInInspector]
        public int currentCount = 0;
    }

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Begin spawning loop if player found
        if (playerTransform != null)
        {
            StartCoroutine(SpawnRoutine());
        }
        else
        {
            Debug.LogWarning("EnemySpawnController: No player found with tag 'Player'");
        }
    }

    


    private IEnumerator SpawnRoutine()
    {
        isSpawning = true;

        while (isSpawning)
        {
            // Wait for the spawn interval
            yield return new WaitForSeconds(spawnInterval);

            // Clean up destroyed enemies
            CleanupDestroyedEnemies();

            // Check if we can spawn more enemies
            if (activeEnemies.Count < maxEnemies)
            {
                TrySpawnEnemy();
            }
        }
    }

    private void CleanupDestroyedEnemies()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                activeEnemies.RemoveAt(i);

                // Reduce the count for that enemy type
                foreach (var config in enemyPrefabs)
                {
                    if (config.currentCount > 0)
                    {
                        config.currentCount--;
                        break;
                    }
                }
            }
        }
    }


    private void TrySpawnEnemy()
    {

        if (playerTransform == null)
            return;

        Vector3 spawnPosition = FindSpawnPosition();

        if (spawnPosition != Vector3.zero)
        {
            GameObject enemyPrefab = SelectRandomEnemyPrefab();

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);

            var enemyNetObj = enemy.GetComponent<NetworkObject>();

            InstanceFinder.ServerManager.Spawn(enemyNetObj);

            activeEnemies.Add(enemy);

            foreach (var config in enemyPrefabs)
            {
                if (config.enemyPrefab == enemyPrefab)
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
    }

    private Vector3 FindSpawnPosition()
    {
        // Try to find a valid position on NavMesh 30 times
        for (int i = 0; i < 30; i++)
        {
            // Random angle around player
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(minDistanceFromPlayer, maxDistanceFromPlayer);

            // Calculate position
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 potentialPosition = playerTransform.position + direction * distance;

            // Sample NavMesh to find closest valid position
            UnityEngine.AI.NavMeshHit hit;
            if (
                UnityEngine.AI.NavMesh.SamplePosition(
                    potentialPosition,
                    out hit,
                    5f,
                    UnityEngine.AI.NavMesh.AllAreas
                )
            )
            {
                // There should be a check if this point is out of player's sight (optional)

                return hit.position;
            }
        }

        return Vector3.zero; // No valid position found
    }

    private GameObject SelectRandomEnemyPrefab()
    {
        // Make sure there are prefabs to spawn
        if (enemyPrefabs.Count == 0)
            return null;

        // Calculate total weight (considering max count limits)
        float totalWeight = 0f;
        List<EnemySpawnConfig> availablePrefabs = new List<EnemySpawnConfig>();

        foreach (var config in enemyPrefabs)
        {
            if (config.enemyPrefab != null && config.currentCount < config.maxCount)
            {
                totalWeight += config.spawnChance;
                availablePrefabs.Add(config);
            }
        }

        if (availablePrefabs.Count == 0)
            return null;

        // Select a random enemy based on weights
        float randomValue = Random.Range(0f, totalWeight);
        float currentWeight = 0f;

        foreach (var config in availablePrefabs)
        {
            currentWeight += config.spawnChance;

            if (randomValue <= currentWeight)
            {
                return config.enemyPrefab;
            }
        }

        // Fallback to first available prefab
        return availablePrefabs[0].enemyPrefab;
    }

    public void StopSpawning()
    {
        isSpawning = false;
    }

    public void StartSpawning()
    {
        if (!isSpawning)
        {
            StartCoroutine(SpawnRoutine());
        }
    }

    public GameObject SpawnSpecificEnemy(int enemyIndex, Vector3 position)
    {
        if (enemyIndex >= 0 && enemyIndex < enemyPrefabs.Count)
        {
            EnemySpawnConfig config = enemyPrefabs[enemyIndex];

            if (config.currentCount < config.maxCount)
            {
                GameObject enemy = Instantiate(config.enemyPrefab, position, Quaternion.identity);
                activeEnemies.Add(enemy);
                config.currentCount++;
                return enemy;
            }
        }
        return null;
    }

    public void SpawnEnemiesInRoom(Transform roomTransform, int minEnemies, int maxEnemies)
    {

        int enemiesToSpawn = Random.Range(minEnemies, maxEnemies + 1);
        Debug.Log("Enemies to spawn: " + enemiesToSpawn);

        // Get the room bounds
        Bounds roomBounds = new Bounds(roomTransform.position, Vector3.zero);
        var renderers = roomTransform.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            if (r != null)
                roomBounds.Encapsulate(r.bounds);
        }

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPosition = FindPositionInRoom(roomBounds);
            if (spawnPosition == Vector3.zero)
            {
                continue;  // If invalid position, skip this enemy spawn
            }

            GameObject enemyPrefab = SelectRandomEnemyPrefab();
            if (enemyPrefab == null)
            {
                continue;
            }

            GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
            enemy.SetActive(true); // Ensure the enemy is active
            base.Spawn(enemy);
            activeEnemies.Add(enemy);

            foreach (var config in enemyPrefabs)
            {
                if (config.enemyPrefab == enemyPrefab)
                {
                    config.currentCount++;
                    break;
                }
            }
        }
    }

    private Vector3 FindPositionInRoom(Bounds roomBounds)
    {
        for (int i = 0; i < 30; i++) // Try 30 times
        {
            // Random position within bounds
            float x = Random.Range(roomBounds.min.x + 1f, roomBounds.max.x - 1f); // Buffer from walls
            float z = Random.Range(roomBounds.min.z + 1f, roomBounds.max.z - 1f);

            Vector3 potentialPosition = new Vector3(x, roomBounds.center.y, z);

            // Sample NavMesh
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

        Debug.LogWarning($"[FindPositionInRoom] Nie znaleziono pozycji w pokoju: {roomBounds}. Sprawdź NavMesh!");
        return Vector3.zero;
    }
}
