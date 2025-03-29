using System.Collections.Generic;
using UnityEngine;

public class RoomController : MonoBehaviour
{
    public GameObject[] northWalls;
    public GameObject[] southWalls;
    public GameObject[] eastWalls;
    public GameObject[] westWalls;

    public GameObject[] northDoors;
    public GameObject[] southDoors;
    public GameObject[] eastDoors;
    public GameObject[] westDoors;

    public void SetupDoorsFromConnections(List<ConnectionPoint> connectionPoints)
    {
        Debug.Log($"Room at {transform.position}: Setting up {connectionPoints.Count} doors");

        DisableAllDoors();

        // Only enable doors that match connection points
        foreach (ConnectionPoint cp in connectionPoints)
        {
            Debug.Log(
                $"  Enabling door at local ({cp.localPosition.x}, {cp.localPosition.y}) facing {cp.direction}"
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
                Debug.Log($"Disabled wall at {localPos} facing {direction}");
            }
            return true;
        }

        Debug.LogError($"No door found for position {localPos} and direction {direction}");
        return false;
    }
}
