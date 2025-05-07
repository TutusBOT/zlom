using System.Collections.Generic;
using UnityEngine;

public class WaypointPlacer
{
    private bool debug = true;

    public void InitializeWaypoints(Transform dungeonRoot)
    {
        if (DungeonGenerator.Instance == null)
        {
            Debug.LogError("DungeonGenerator instance not found");
            return;
        }

        // Get the list of rooms with connection data
        List<Room> rooms = DungeonGenerator.Instance.Rooms;

        // Connect waypoints directly using room controllers
        ConnectWaypointsThroughRoomConnections(rooms);

        // Refresh the waypoint manager's list
        if (WaypointManager.Instance != null)
        {
            WaypointManager.Instance.RefreshWaypointsList();
        }
    }

    private void ConnectWaypointsThroughRoomConnections(List<Room> rooms)
    {
        int connectionsProcessed = 0;

        // Process each room's connections
        foreach (Room room in rooms)
        {
            // Skip if room doesn't have a controller
            if (room.roomController == null)
                continue;

            // Process each connection point
            foreach (ConnectionPoint connectionPoint in room.connectionPoints)
            {
                // Skip if there's no connected room
                Room connectedRoom = connectionPoint.connectedRoom;
                if (connectedRoom == null || connectedRoom.roomController == null)
                    continue;

                // Find the connection position
                Vector3 connectionPos = GetConnectionPosition(room, connectionPoint);

                // Find closest waypoint in each room
                Waypoint waypoint1 = FindClosestWaypoint(room.roomController, connectionPos);
                Waypoint waypoint2 = FindClosestWaypoint(
                    connectedRoom.roomController,
                    connectionPos
                );

                // Connect the waypoints if both exist
                if (waypoint1 != null && waypoint2 != null)
                {
                    waypoint1.AddConnection(waypoint2);
                    waypoint2.AddConnection(waypoint1);
                    connectionsProcessed++;

                    if (debug)
                        Debug.Log(
                            $"Connected waypoint in room at ({room.xOrigin},{room.zOrigin}) to waypoint in room at ({connectedRoom.xOrigin},{connectedRoom.zOrigin})"
                        );
                }
                else if (debug)
                {
                    Debug.LogWarning(
                        $"Could not find waypoints for connection between room at ({room.xOrigin},{room.zOrigin}) and room at ({connectedRoom.xOrigin},{connectedRoom.zOrigin})"
                    );
                }
            }
        }

        if (debug)
            Debug.Log($"Connected {connectionsProcessed} waypoints between rooms");
    }

    // Rest of your methods remain the same
    private Vector3 GetConnectionPosition(Room room, ConnectionPoint connectionPoint)
    {
        float gridSize = DungeonGenerator.Instance.gridUnitSize;
        return new Vector3(
            (room.xOrigin + connectionPoint.localPosition.x) * gridSize,
            0,
            (room.zOrigin + connectionPoint.localPosition.y) * gridSize
        );
    }

    private Waypoint FindClosestWaypoint(RoomController room, Vector3 position)
    {
        Waypoint[] waypoints = room.GetComponentsInChildren<Waypoint>();
        if (waypoints.Length == 0)
        {
            if (debug)
                Debug.LogWarning($"Room {room.name} has no waypoints");
            return null;
        }

        Waypoint closest = null;
        float closestDistance = float.MaxValue;

        foreach (Waypoint waypoint in waypoints)
        {
            float distance = Vector3.Distance(position, waypoint.transform.position);
            if (distance < closestDistance)
            {
                closest = waypoint;
                closestDistance = distance;
            }
        }

        return closest;
    }
}
