using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;

public class RoomController : MonoBehaviour, ILightingStateReceiver
{
    public GameObject[] northWalls;
    public GameObject[] southWalls;
    public GameObject[] eastWalls;
    public GameObject[] westWalls;

    public GameObject[] northDoors;
    public GameObject[] southDoors;
    public GameObject[] eastDoors;
    public GameObject[] westDoors;

    public enum RoomLightingType
    {
        Unimplemented, // No lighting system
        Disabled, // Has lights, permanently disabled
        Enabled, // Has lights that can be powered
    }

    [SerializeField]
    public GameObject[] waypoints;

    [Header("Lighting")]
    [SerializeField]
    public RoomLightingType lightingType = RoomLightingType.Unimplemented;

    [SerializeField]
    private GameObject lightSwitchPrefab;

    [SerializeField]
    private Transform lightSwitchMountPoint;
    private LightSwitch _roomLightSwitch;
    [System.Serializable]
    public class NetworkSpawnData
    {
        public NetworkObject prefab;
        public Transform[] spawnPoints;
    }
    [Header("Networked Objects To Spawn")]
    [SerializeField]
    private NetworkSpawnData[] networkSpawns;
    // Lighting state
    private bool _isPowered = false;
    private bool _hasWorkingLights = false;

    // Properties for external access
    public bool HasLightingImplementation => lightingType != RoomLightingType.Unimplemented;
    public bool CanBePowered => lightingType == RoomLightingType.Enabled;
    public bool IsPowered => _isPowered && CanBePowered;
    public bool IsLit => IsPowered && _hasWorkingLights;

    public void SpawnNetworkObjects()
{
    if (networkSpawns == null)
        return;

    foreach (var spawnData in networkSpawns)
    {
        if (spawnData.prefab == null || spawnData.spawnPoints == null)
            continue;

        foreach (var point in spawnData.spawnPoints)
        {
            if (point == null) continue;

            NetworkObject obj = Instantiate(spawnData.prefab, point.position, point.rotation, transform);
            InstanceFinder.ServerManager.Spawn(obj);
        }
    }
}
    public void SetupDoorsFromConnections(List<ConnectionPoint> connectionPoints)
    {
        Debug.Log($"Room at {transform.position}: Setting up {connectionPoints.Count} doors");

        DisableAllDoors();

        // Only enable doors that match connection points
        foreach (ConnectionPoint cp in connectionPoints)
        {
            Debug.Log(
                $"Enabling door at local ({cp.localPosition.x}, {cp.localPosition.y}) facing {cp.direction}"
            );
            bool success = EnableDoorAtPosition(cp.localPosition, cp.direction);
            if (!success)
            {
                Debug.LogError(
                    $"Failed to enable door at {cp.localPosition} facing {cp.direction}"
                );
            }
        }
    }

    private void DisableAllDoors()
    {
        foreach (var door in northDoors)
            if (door != null)
                door.SetActive(false);
        foreach (var door in southDoors)
            if (door != null)
                door.SetActive(false);
        foreach (var door in eastDoors)
            if (door != null)
                door.SetActive(false);
        foreach (var door in westDoors)
            if (door != null)
                door.SetActive(false);
    }

    private bool EnableDoorAtPosition(Vector2Int localPos, Direction direction)
    {
        GameObject door = null;
        GameObject wall = null;

        switch (direction)
        {
            case Direction.North:
                // Find the north door and wall at this position
                if (northDoors != null && localPos.x < northDoors.Length)
                {
                    door = northDoors[localPos.x];

                    // Also get the corresponding wall to disable
                    if (northWalls != null && localPos.x < northWalls.Length)
                    {
                        wall = northWalls[localPos.x];
                    }
                }
                break;
            case Direction.South:
                if (southDoors != null && localPos.x < southDoors.Length)
                {
                    door = southDoors[localPos.x];

                    // Also get the corresponding wall
                    if (southWalls != null && localPos.x < southWalls.Length)
                    {
                        wall = southWalls[localPos.x];
                    }
                }
                break;
            case Direction.East:
                if (eastDoors != null && localPos.y < eastDoors.Length)
                {
                    door = eastDoors[localPos.y];

                    // Also get the corresponding wall
                    if (eastWalls != null && localPos.y < eastWalls.Length)
                    {
                        wall = eastWalls[localPos.y];
                    }
                }
                break;
            case Direction.West:
                if (westDoors != null && localPos.y < westDoors.Length)
                {
                    door = westDoors[localPos.y];

                    // Also get the corresponding wall
                    if (westWalls != null && localPos.y < westWalls.Length)
                    {
                        wall = westWalls[localPos.y];
                    }
                }
                break;
        }

        if (door != null)
        {
            door.SetActive(true);

            if (wall != null)
            {
                wall.SetActive(false);
            }
            return true;
        }

        Debug.LogError($"No door found for position {localPos} and direction {direction}");
        return false;
    }

    public void OnLightingStateChanged(bool isPowered, bool hasWorkingLights)
    {
        // Disabled rooms can never be powered
        if (lightingType == RoomLightingType.Disabled)
            isPowered = false;

        bool wasLit = _isPowered && _hasWorkingLights;

        _isPowered = isPowered;
        _hasWorkingLights = hasWorkingLights;

        bool isLit = _isPowered && _hasWorkingLights;

        if (wasLit != isLit && HasLightingImplementation)
        {
            if (isLit)
            {
                Debug.Log($"Room at {transform.position} is now lit");
            }
            else
            {
                Debug.Log($"Room at {transform.position} is now dark");
            }
        }
    }

    public void SetupLightSwitch()
    {
        if (!HasLightingImplementation)
            return;

        if (lightSwitchMountPoint == null)
        {
            Debug.LogError(
                $"Room at {transform.position} does not have a light switch mount point."
            );
            return;
        }

        GameObject switchObj = Instantiate(
            lightSwitchPrefab,
            lightSwitchMountPoint.position,
            lightSwitchMountPoint.rotation,
            transform
        );
        _roomLightSwitch = switchObj.GetComponent<LightSwitch>();

        if (_roomLightSwitch != null)
        {
            _roomLightSwitch.SetTargetRoom(this);
        }
    }

    public void ReportLightDestroyed(RoomLightSource light)
    {
        if (RoomLightingManager.Instance != null)
        {
            RoomLightingManager.Instance.DestroyLightServerRpc(light.transform.position);
        }
    }
}
