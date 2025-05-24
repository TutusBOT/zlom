using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using UnityEngine;
using static DungeonGenerator;

public class RoomRenderer
{
    private RoomVariantsWrapper roomVariantsWrapper;
    private float gridUnitSize;
    private Transform parentTransform;
    private GameObject startingRoomPrefab;
    private GameObject doorPrefab;
    private HashSet<Vector3> occupiedDoorPositions = new HashSet<Vector3>();
    private bool debug;

    public RoomRenderer(
        RoomVariantsWrapper roomVariantsWrapper,
        float gridUnitSize,
        Transform parentTransform,
        GameObject startingRoomPrefab,
        GameObject doorPrefab,
        bool debug
    )
    {
        this.roomVariantsWrapper = roomVariantsWrapper;
        this.gridUnitSize = gridUnitSize;
        this.parentTransform = parentTransform;
        this.startingRoomPrefab = startingRoomPrefab;
        this.doorPrefab = doorPrefab;
        this.debug = debug;
    }

    public void RenderDungeon(
        List<Room> rooms,
        List<GameObject> waypoints,
        bool isServerInitialized
    )
    {
        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        Room startingRoom = rooms[0];
        float offsetX = -((startingRoom.xOrigin + (startingRoom.width / 2f)) * gridUnitSize);
        float offsetZ = -((startingRoom.zOrigin + (startingRoom.length / 2f)) * gridUnitSize);

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            bool isStartingRoom = i == 0;
            GameObject renderedRoom = null;

            // Create the room prefab (starting room or normal room)
            if (isStartingRoom && startingRoomPrefab != null)
            {
                renderedRoom = PlaceRoom(
                    startingRoomPrefab,
                    room,
                    offsetX,
                    offsetZ,
                    "StartingRoom"
                );
            }
            else
            {
                RoomSize size = new RoomSize { width = room.width, length = room.length };

                if (roomVariants.ContainsKey(size))
                {
                    RoomVariantData variantData = roomVariants[size];

                    if (variantData.normalVariants != null && variantData.normalVariants.Count > 0)
                    {
                        GameObject prefab = variantData.normalVariants[
                            Random.Range(0, variantData.normalVariants.Count)
                        ];

                        renderedRoom = PlaceRoom(prefab, room, offsetX, offsetZ);
                    }
                    else
                    {
                        Debug.LogWarning(
                            $"No room variants found for size: Width={size.width}, Length={size.length}"
                        );
                    }
                }
                else
                {
                    Debug.LogWarning(
                        $"No room variants defined for size: Width={room.width}, Length={room.length}"
                    );
                }
            }

            // After the room prefab is instantiated, place doors based on connections
            if (renderedRoom == null)
            {
                Debug.LogWarning($"Failed to instantiate room prefab for room: {room}");
                continue;
            }

            RoomController controller = renderedRoom.GetComponent<RoomController>();
            if (controller == null)
            {
                Debug.LogWarning(
                    $"Room at ({room.xOrigin}, {room.zOrigin}) has no RoomController component"
                );
                continue;
            }

            waypoints.AddRange(controller.waypoints);
            controller.SetupDoorsFromConnections(room.connectionPoints);

            if (isServerInitialized)
                SpawnDoors(room, renderedRoom);
        }
    }

    GameObject PlaceRoom(
        GameObject prefab,
        Room room,
        float offsetX,
        float offsetZ,
        string name = null
    )
    {
        Vector3 roomCenter = new Vector3(
            (room.xOrigin + (room.width / 2f)) * gridUnitSize + offsetX,
            0,
            (room.zOrigin + (room.length / 2f)) * gridUnitSize + offsetZ
        );

        GameObject placedRoom = Object.Instantiate(
            prefab,
            roomCenter,
            Quaternion.identity,
            parentTransform
        );
        placedRoom.name = name ?? $"Room_{room.xOrigin}_{room.zOrigin}";

        RoomController controller = placedRoom.GetComponent<RoomController>();
        if (controller != null)
        {
            room.roomController = controller;
            controller.SpawnNetworkObjects();
        }

        return placedRoom;
    }

    void SpawnDoors(Room room, GameObject roomObject)
    {
        if (room.connectionPoints == null || room.connectionPoints.Count == 0)
            return;

        foreach (ConnectionPoint cp in room.connectionPoints)
        {
            string dirName = cp.direction.ToString(); // "North", "South", etc.
            Transform doorAnchor = roomObject.transform.Find($"Doors/{dirName}");

            if (doorAnchor == null)
            {
                Debug.LogWarning($"Missing door anchor '{dirName}' in room: {roomObject.name}");
                continue;
            }

            // Check if any doors exist within a radius of 5 units
            if (IsDoorInRadius(doorAnchor.position, 5f))
            {
                if (debug)
                    Debug.Log(
                        $"Skipping door instantiation, nearby door already exists within 5 units."
                    );
                continue;
            }

            // Mark the door position as occupied
            occupiedDoorPositions.Add(doorAnchor.position);
            Vector3 adjustedPosition = doorAnchor.position + new Vector3(0, 1.5f, 0);
            // Spawn door at this anchor
            GameObject door = Object.Instantiate(doorPrefab, adjustedPosition, doorAnchor.rotation);
            Debug.Log(door);

            NetworkObject networkObject = door.GetComponent<NetworkObject>();
            InstanceFinder.ServerManager.Spawn(networkObject);
            door.name = $"Door_{dirName}";
        }
    }

    bool IsDoorInRadius(Vector3 position, float radius)
    {
        foreach (Vector3 occupiedPosition in occupiedDoorPositions)
        {
            // If any door is within the radius, return true
            if (Vector3.Distance(position, occupiedPosition) < radius)
            {
                return true;
            }
        }
        return false;
    }
}
