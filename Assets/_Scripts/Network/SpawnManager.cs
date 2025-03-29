using Alteruna;
using Alteruna.Trinity;
using UnityEngine;

public class SpawnManager : AttributesSync
{
    // Replace transform reference with direct Vector3 coordinates
    [Header("Spawn Settings")]
    public Vector3 spawnPosition = new Vector3(130, 100, 0);
    public Vector3 spawnRotationEuler = new Vector3(0, 0, 0);
    
    void Start()
    {
        SpawnPlayer();
    }

    [SynchronizableMethod]
    public void SpawnPlayer()
    {
        // Convert Euler angles to Quaternion
        Debug.Log($"Spawning player at position: {spawnPosition}, rotation: {spawnRotationEuler}");
        Quaternion spawnRotation = Quaternion.Euler(spawnRotationEuler);
        
        // Use the direct coordinates without any Transform references
        Multiplayer.SpawnAvatar(spawnPosition, spawnRotation);
        
        Debug.Log($"Spawning player at position: {spawnPosition}, rotation: {spawnRotationEuler}");
    }
}