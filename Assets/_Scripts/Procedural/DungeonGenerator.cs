using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

public enum CellType
{
    Empty,
    Room,
}

public enum Direction
{
    North,
    South,
    East,
    West,
}

public class Room
{
    public int xOrigin,
        zOrigin;
    public int width,
        length;
    public List<Room> connections;
    public List<ConnectionPoint> connectionPoints;

    public Room(int x, int z, int w, int l)
    {
        xOrigin = x;
        zOrigin = z;
        width = w;
        length = l;
        connections = new List<Room>();
        connectionPoints = new List<ConnectionPoint>();
    }
}

public struct ConnectionPoint
{
    public Room connectedRoom;
    public Vector2Int localPosition;
    public Direction direction;
}

public class DungeonGenerator : MonoBehaviour
{
    public const int DEFAULT_GRID_SIZE_X = 50;
    public const int DEFAULT_GRID_SIZE_Z = 50;
    public const int DEFAULT_ROOM_COUNT = 8;

    [Header("Valuable Spawning")]
    public ValuableSpawner valuableSpawner;

    [Header("Room Scaling")]
    public float gridUnitSize = 5f;

    public int gridSizeX = DEFAULT_GRID_SIZE_X;
    public int gridSizeZ = DEFAULT_GRID_SIZE_Z;
    public int roomCount = DEFAULT_ROOM_COUNT;
    public GameObject startingRoomPrefab;
    public bool debug;

    [Serializable]
    public struct RoomSize
    {
        public int width;
        public int length;

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            RoomSize other = (RoomSize)obj;
            return (width == other.width) && (length == other.length);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(width, length);
        }
    }

    struct PossibleRoomPlacement
    {
        public int x,
            z;
        public RoomSize size;
        public Direction direction;
        public Vector2Int connectionPoint;
    }

    public RoomVariantsWrapper roomVariantsWrapper = new RoomVariantsWrapper();

    private CellType[,] grid;
    private List<Room> rooms = new List<Room>();

    void Start()
    {
        Debug.Log("=== ROOM VARIANT CONFIGURATIONS ===");
        foreach (var entry in roomVariantsWrapper.roomVariantsList)
        {
            RoomSize size = entry.size;
            RoomVariantData data = entry.variantData;

            if (!debug)
                continue;

            Debug.Log(
                $"Room {size.width}x{size.length}: "
                    + $"allowDoorsAnywhere={data.allowDoorsAnywhere}, "
                    + $"N:{data.allowedNorthDoors?.Count ?? 0}, "
                    + $"S:{data.allowedSouthDoors?.Count ?? 0}, "
                    + $"E:{data.allowedEastDoors?.Count ?? 0}, "
                    + $"W:{data.allowedWestDoors?.Count ?? 0}"
            );

            if (data.allowedNorthDoors != null)
            {
                foreach (var pos in data.allowedNorthDoors)
                {
                    Debug.Log($"  North door at: ({pos.x}, {pos.y})");
                }
            }

            if (data.allowedEastDoors != null)
            {
                foreach (var pos in data.allowedEastDoors)
                {
                    Debug.Log($"  East door at: ({pos.x}, {pos.y})");
                }
            }

            if (data.allowedSouthDoors != null)
            {
                foreach (var pos in data.allowedSouthDoors)
                {
                    Debug.Log($"  South door at: ({pos.x}, {pos.y})");
                }
            }

            if (data.allowedWestDoors != null)
            {
                foreach (var pos in data.allowedWestDoors)
                {
                    Debug.Log($"  West door at: ({pos.x}, {pos.y})");
                }
            }
        }

        GenerateDungeon();
    }

    void GenerateDungeon()
    {
        grid = new CellType[gridSizeX, gridSizeZ];
        rooms.Clear();

        // Clear any existing dungeon objects. Important to do this when regenerating.
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        PlaceRooms();
        ConnectRooms();
        RenderDungeon();

        if (valuableSpawner != null)
        {
            valuableSpawner.SpawnValuablesInRooms();
        }

        // foreach (Room room in rooms)
        // {
        //     Debug.Log(
        //         $"Room at ({room.xOrigin}, {room.zOrigin}) with size {room.width}x{room.length}"
        //     );
        //     foreach (Room connection in room.connections)
        //     {
        //         Debug.Log($"  Connected to room at ({connection.xOrigin}, {connection.zOrigin})");
        //     }
        // }
    }

    void PlaceRooms()
    {
        if (roomCount <= 0)
            return;

        List<RoomSize> availableSizes = new List<RoomSize>(roomVariantsWrapper.ToDictionary().Keys);
        if (availableSizes.Count == 0)
        {
            Debug.LogWarning("No room variants defined. Using 1x1 rooms as fallback.");
            availableSizes.Add(new RoomSize { width = 1, length = 1 });
        }

        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        RoomSize startingSize = new RoomSize { width = 1, length = 1 }; // Default starting room size
        int startX = (gridSizeX - startingSize.width) / 2;
        int startZ = 0;

        Room startingRoom = new Room(startX, startZ, startingSize.width, startingSize.length);
        rooms.Add(startingRoom);
        MarkRoomInGrid(startingRoom);

        Debug.Log(
            $"Placed starting room at ({startX}, {startZ}) with size {startingSize.width}x{startingSize.length}"
        );

        // Place the first room connected to the starting room
        List<PossibleRoomPlacement> startingRoomPlacements = GetPossibleRoomPlacements(
            startingRoom,
            availableSizes
        );

        if (startingRoomPlacements.Count > 0)
        {
            // For the second room placement, verify door restrictions explicitly
            List<PossibleRoomPlacement> validPlacements = new List<PossibleRoomPlacement>();

            foreach (var placement in startingRoomPlacements)
            {
                RoomSize placementSize = placement.size;

                // Check if this room placement would violate door restrictions
                bool isValidPlacement = true;
                if (
                    roomVariants.TryGetValue(placementSize, out RoomVariantData variantData)
                    && !variantData.allowDoorsAnywhere
                )
                {
                    // Verify this room can actually have a door in the direction needed
                    switch (GetOppositeDirection(placement.direction))
                    {
                        case Direction.North:
                            isValidPlacement =
                                variantData.allowedNorthDoors != null
                                && variantData.allowedNorthDoors.Count > 0;
                            break;
                        case Direction.South:
                            isValidPlacement =
                                variantData.allowedSouthDoors != null
                                && variantData.allowedSouthDoors.Count > 0;
                            break;
                        case Direction.East:
                            isValidPlacement =
                                variantData.allowedEastDoors != null
                                && variantData.allowedEastDoors.Count > 0;
                            break;
                        case Direction.West:
                            isValidPlacement =
                                variantData.allowedWestDoors != null
                                && variantData.allowedWestDoors.Count > 0;
                            break;
                    }
                }

                if (isValidPlacement)
                {
                    validPlacements.Add(placement);
                }
            }

            // Only use valid placements that respect door restrictions
            if (validPlacements.Count > 0)
            {
                PossibleRoomPlacement placement = validPlacements[
                    Random.Range(0, validPlacements.Count)
                ];

                // CREATE AND PLACE THE SECOND ROOM FIRST
                Room secondRoom = new Room(
                    placement.x,
                    placement.z,
                    placement.size.width,
                    placement.size.length
                );
                rooms.Add(secondRoom);
                MarkRoomInGrid(secondRoom);

                // Connect the first and second rooms
                CreateRoomConnection(startingRoom, secondRoom, placement);

                Debug.Log(
                    $"Placed second room at ({placement.x},{placement.z}) with size {placement.size.width}x{placement.size.length}"
                );

                int placedRooms = 2;
                int attempts = 0;
                int maxAttempts = roomCount * 10;

                while (placedRooms < roomCount && attempts < maxAttempts)
                {
                    attempts++;

                    // IMPORTANT: Select a random room EXCEPT the starting room (index 1 and above)
                    Room currentRoom = rooms[Random.Range(1, rooms.Count)];

                    List<PossibleRoomPlacement> possiblePlacements = GetPossibleRoomPlacements(
                        currentRoom,
                        availableSizes
                    );
                    validPlacements.Clear();

                    // Filter placements to only those that respect door restrictions
                    foreach (var p in possiblePlacements)
                    {
                        RoomSize placementSize = p.size;

                        // Check if this room placement would violate door restrictions
                        bool isValidPlacement = true;
                        if (
                            roomVariants.TryGetValue(
                                placementSize,
                                out RoomVariantData pVariantData
                            ) && !pVariantData.allowDoorsAnywhere
                        )
                        {
                            // Verify this room can actually have a door in the direction needed
                            switch (GetOppositeDirection(p.direction))
                            {
                                case Direction.North:
                                    isValidPlacement =
                                        pVariantData.allowedNorthDoors != null
                                        && pVariantData.allowedNorthDoors.Count > 0;
                                    break;
                                case Direction.South:
                                    isValidPlacement =
                                        pVariantData.allowedSouthDoors != null
                                        && pVariantData.allowedSouthDoors.Count > 0;
                                    break;
                                case Direction.East:
                                    isValidPlacement =
                                        pVariantData.allowedEastDoors != null
                                        && pVariantData.allowedEastDoors.Count > 0;
                                    break;
                                case Direction.West:
                                    isValidPlacement =
                                        pVariantData.allowedWestDoors != null
                                        && pVariantData.allowedWestDoors.Count > 0;
                                    break;
                            }
                        }

                        if (isValidPlacement)
                        {
                            validPlacements.Add(p);
                        }
                    }

                    if (validPlacements.Count > 0)
                    {
                        placement = validPlacements[Random.Range(0, validPlacements.Count)];

                        Debug.Log(
                            $"Placing room {placedRooms + 1}: {placement.size.width}x{placement.size.length} "
                                + $"at ({placement.x},{placement.z}) with connection on {placement.direction} wall"
                        );

                        Room newRoom = new Room(
                            placement.x,
                            placement.z,
                            placement.size.width,
                            placement.size.length
                        );
                        rooms.Add(newRoom);
                        MarkRoomInGrid(newRoom);
                        placedRooms++;

                        CreateRoomConnection(currentRoom, newRoom, placement);

                        // Double-check that no room has doors it shouldn't have
                        if (
                            roomVariants.TryGetValue(
                                new RoomSize { width = newRoom.width, length = newRoom.length },
                                out RoomVariantData checkVariantData
                            ) && !checkVariantData.allowDoorsAnywhere
                        )
                        {
                            // Check all connection points to ensure they're allowed
                            foreach (ConnectionPoint cp in newRoom.connectionPoints)
                            {
                                bool isAllowed = false;

                                switch (cp.direction)
                                {
                                    case Direction.North:
                                        isAllowed =
                                            checkVariantData.allowedNorthDoors != null
                                            && checkVariantData.allowedNorthDoors.Count > 0
                                            && checkVariantData.allowedNorthDoors.Contains(
                                                cp.localPosition
                                            );
                                        break;
                                    case Direction.South:
                                        isAllowed =
                                            checkVariantData.allowedSouthDoors != null
                                            && checkVariantData.allowedSouthDoors.Count > 0
                                            && checkVariantData.allowedSouthDoors.Contains(
                                                cp.localPosition
                                            );
                                        break;
                                    case Direction.East:
                                        isAllowed =
                                            checkVariantData.allowedEastDoors != null
                                            && checkVariantData.allowedEastDoors.Count > 0
                                            && checkVariantData.allowedEastDoors.Contains(
                                                cp.localPosition
                                            );
                                        break;
                                    case Direction.West:
                                        isAllowed =
                                            checkVariantData.allowedWestDoors != null
                                            && checkVariantData.allowedWestDoors.Count > 0
                                            && checkVariantData.allowedWestDoors.Contains(
                                                cp.localPosition
                                            );
                                        break;
                                }

                                if (!isAllowed)
                                {
                                    Debug.LogError(
                                        $"ERROR: Room at ({newRoom.xOrigin},{newRoom.zOrigin}) has invalid door on {cp.direction} wall at {cp.localPosition}"
                                    );
                                }
                            }
                        }
                    }
                    else if (attempts % (rooms.Count - 1) == 0)
                    {
                        Debug.LogWarning(
                            $"Struggling to place rooms. Placed {placedRooms} of {roomCount} after {attempts} attempts."
                        );
                    }
                }

                Debug.Log(
                    $"Room placement complete. Placed {placedRooms} rooms out of {roomCount} after {attempts} attempts."
                );
            }
            else
            {
                Debug.LogWarning(
                    "No valid placements for second room that respect door restrictions."
                );
            }
        }
        else
        {
            Debug.LogWarning("Could not place a second room connected to the starting room.");
        }
    }

    void CreateRoomConnection(Room sourceRoom, Room targetRoom, PossibleRoomPlacement placement)
    {
        sourceRoom.connections.Add(targetRoom);
        targetRoom.connections.Add(sourceRoom);

        Vector2Int sourceLocalPos = new Vector2Int(
            placement.connectionPoint.x - sourceRoom.xOrigin,
            placement.connectionPoint.y - sourceRoom.zOrigin
        );

        Vector2Int targetLocalPos = Vector2Int.zero;
        Direction oppositeDir = GetOppositeDirection(placement.direction);

        switch (placement.direction)
        {
            case Direction.North:
                targetLocalPos = new Vector2Int(
                    placement.connectionPoint.x - placement.x,
                    0 // Bottom edge of target room
                );
                break;
            case Direction.South:
                targetLocalPos = new Vector2Int(
                    placement.connectionPoint.x - placement.x,
                    placement.size.length - 1 // Top edge of target room
                );
                break;
            case Direction.East:
                targetLocalPos = new Vector2Int(
                    0, // Left edge of target room
                    placement.connectionPoint.y - placement.z
                );
                break;
            case Direction.West:
                targetLocalPos = new Vector2Int(
                    placement.size.width - 1, // Right edge of target room
                    placement.connectionPoint.y - placement.z
                );
                break;
        }

        // Add connection points to both rooms
        sourceRoom.connectionPoints.Add(
            new ConnectionPoint
            {
                connectedRoom = targetRoom,
                localPosition = sourceLocalPos,
                direction = placement.direction,
            }
        );

        targetRoom.connectionPoints.Add(
            new ConnectionPoint
            {
                connectedRoom = sourceRoom,
                localPosition = targetLocalPos,
                direction = oppositeDir,
            }
        );
    }

    Direction GetOppositeDirection(Direction dir)
    {
        switch (dir)
        {
            case Direction.North:
                return Direction.South;
            case Direction.South:
                return Direction.North;
            case Direction.East:
                return Direction.West;
            case Direction.West:
                return Direction.East;
            default:
                return dir;
        }
    }

    void MarkRoomInGrid(Room room)
    {
        for (int x = room.xOrigin; x < room.xOrigin + room.width; x++)
        {
            for (int z = room.zOrigin; z < room.zOrigin + room.length; z++)
            {
                grid[x, z] = CellType.Room;
            }
        }
    }

    bool CanPlaceRoom(int x, int z, int width, int length)
    {
        if (x < 0 || z < 0 || x + width > gridSizeX || z + length > gridSizeZ)
        {
            return false;
        }

        // Check if all cells are empty
        for (int i = x; i < x + width; i++)
        {
            for (int j = z; j < z + length; j++)
            {
                if (grid[i, j] != CellType.Empty)
                {
                    return false;
                }
            }
        }

        return true;
    }

    List<PossibleRoomPlacement> GetPossibleRoomPlacements(Room room, List<RoomSize> availableSizes)
    {
        List<PossibleRoomPlacement> placements = new List<PossibleRoomPlacement>();
        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        Debug.Log(
            $"Getting room placements for room at ({room.xOrigin}, {room.zOrigin}) with {availableSizes.Count} possible sizes"
        );

        foreach (RoomSize size in availableSizes)
        {
            bool hasVariantData = roomVariants.TryGetValue(size, out RoomVariantData variantData);

            // Check door restrictions UP FRONT for entire walls
            bool canPlaceNorth = true;
            bool canPlaceSouth = true;
            bool canPlaceEast = true;
            bool canPlaceWest = true;

            if (hasVariantData && !variantData.allowDoorsAnywhere)
            {
                // If doors not allowed anywhere, check if each wall has ANY allowed door positions
                canPlaceNorth =
                    variantData.allowedNorthDoors != null
                    && variantData.allowedNorthDoors.Count > 0;
                canPlaceSouth =
                    variantData.allowedSouthDoors != null
                    && variantData.allowedSouthDoors.Count > 0;
                canPlaceEast =
                    variantData.allowedEastDoors != null && variantData.allowedEastDoors.Count > 0;
                canPlaceWest =
                    variantData.allowedWestDoors != null && variantData.allowedWestDoors.Count > 0;

                Debug.Log(
                    $"For size {size.width}x{size.length}: N={canPlaceNorth}, S={canPlaceSouth}, E={canPlaceEast}, W={canPlaceWest}"
                );
            }

            if (canPlaceNorth)
            {
                for (int x = room.xOrigin; x < room.xOrigin + room.width; x++)
                {
                    int z = room.zOrigin + room.length;

                    // Source room door position check
                    Vector2Int localPos = new Vector2Int(x - room.xOrigin, room.length - 1);
                    if (
                        hasVariantData
                        && !variantData.allowDoorsAnywhere
                        && variantData.allowedNorthDoors != null
                        && variantData.allowedNorthDoors.Count > 0
                        && !variantData.allowedNorthDoors.Contains(localPos)
                    )
                    {
                        continue;
                    }

                    // Calculate new room position
                    int newRoomX = x - (size.width / 2);
                    newRoomX = Mathf.Clamp(newRoomX, 0, gridSizeX - size.width);

                    // Connection point is on the top edge of current room
                    Vector2Int connectionPoint = new Vector2Int(x, z - 1);

                    // Target room door position check
                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(
                            x - newRoomX,
                            0 // Bottom edge (South wall) of target
                        );

                        if (
                            variantData.allowedSouthDoors == null
                            || variantData.allowedSouthDoors.Count == 0
                        )
                        {
                            targetRoomValid = false; // No south doors allowed at all
                        }
                        else if (!variantData.allowedSouthDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false; // This position isn't in the allowed list
                        }
                    }

                    if (targetRoomValid && CanPlaceRoom(newRoomX, z, size.width, size.length))
                    {
                        placements.Add(
                            new PossibleRoomPlacement
                            {
                                x = newRoomX,
                                z = z,
                                size = size,
                                direction = Direction.North,
                                connectionPoint = connectionPoint,
                            }
                        );
                    }
                }
            }

            if (canPlaceSouth)
            {
                for (int x = room.xOrigin; x < room.xOrigin + room.width; x++)
                {
                    int z = room.zOrigin - size.length;

                    // Source room door position check
                    Vector2Int localPos = new Vector2Int(x - room.xOrigin, 0);
                    if (
                        hasVariantData
                        && !variantData.allowDoorsAnywhere
                        && variantData.allowedSouthDoors != null
                        && variantData.allowedSouthDoors.Count > 0
                        && !variantData.allowedSouthDoors.Contains(localPos)
                    )
                    {
                        continue;
                    }

                    // Calculate new room position
                    int newRoomX = x - (size.width / 2);
                    newRoomX = Mathf.Clamp(newRoomX, 0, gridSizeX - size.width);

                    // Connection point is on the bottom edge of current room
                    Vector2Int connectionPoint = new Vector2Int(x, room.zOrigin);

                    // Target room door position check
                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(
                            x - newRoomX,
                            size.length - 1 // Top edge (North wall) of target
                        );

                        if (
                            variantData.allowedNorthDoors == null
                            || variantData.allowedNorthDoors.Count == 0
                        )
                        {
                            targetRoomValid = false; // No north doors allowed at all
                        }
                        else if (!variantData.allowedNorthDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false; // This position isn't in the allowed list
                        }
                    }

                    if (targetRoomValid && CanPlaceRoom(newRoomX, z, size.width, size.length))
                    {
                        placements.Add(
                            new PossibleRoomPlacement
                            {
                                x = newRoomX,
                                z = z,
                                size = size,
                                direction = Direction.South,
                                connectionPoint = connectionPoint,
                            }
                        );
                    }
                }
            }

            if (canPlaceEast)
            {
                for (int z = room.zOrigin; z < room.zOrigin + room.length; z++)
                {
                    int x = room.xOrigin + room.width;

                    // Source room door position check
                    Vector2Int localPos = new Vector2Int(room.width - 1, z - room.zOrigin);
                    if (
                        hasVariantData
                        && !variantData.allowDoorsAnywhere
                        && variantData.allowedEastDoors != null
                        && variantData.allowedEastDoors.Count > 0
                        && !variantData.allowedEastDoors.Contains(localPos)
                    )
                    {
                        continue;
                    }

                    // Calculate new room position
                    int newRoomZ = z - (size.length / 2);
                    newRoomZ = Mathf.Clamp(newRoomZ, 0, gridSizeZ - size.length);

                    // Connection point is on the right edge of current room
                    Vector2Int connectionPoint = new Vector2Int(x - 1, z);

                    // Target room door position check
                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(
                            0, // Left edge (West wall) of target
                            z - newRoomZ
                        );

                        if (
                            variantData.allowedWestDoors == null
                            || variantData.allowedWestDoors.Count == 0
                        )
                        {
                            targetRoomValid = false; // No west doors allowed at all
                        }
                        else if (!variantData.allowedWestDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false; // This position isn't in the allowed list
                        }
                    }

                    if (targetRoomValid && CanPlaceRoom(x, newRoomZ, size.width, size.length))
                    {
                        placements.Add(
                            new PossibleRoomPlacement
                            {
                                x = x,
                                z = newRoomZ,
                                size = size,
                                direction = Direction.East,
                                connectionPoint = connectionPoint,
                            }
                        );
                    }
                }
            }

            if (canPlaceWest)
            {
                for (int z = room.zOrigin; z < room.zOrigin + room.length; z++)
                {
                    int x = room.xOrigin - size.width;

                    // Source room door position check
                    Vector2Int localPos = new Vector2Int(0, z - room.zOrigin);
                    if (
                        hasVariantData
                        && !variantData.allowDoorsAnywhere
                        && variantData.allowedWestDoors != null
                        && variantData.allowedWestDoors.Count > 0
                        && !variantData.allowedWestDoors.Contains(localPos)
                    )
                    {
                        continue;
                    }

                    // Calculate new room position
                    int newRoomZ = z - (size.length / 2);
                    newRoomZ = Mathf.Clamp(newRoomZ, 0, gridSizeZ - size.length);

                    // Connection point is on the left edge of current room
                    Vector2Int connectionPoint = new Vector2Int(room.xOrigin, z);

                    // Target room door position check
                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(
                            size.width - 1, // Right edge (East wall) of target
                            z - newRoomZ
                        );

                        if (
                            variantData.allowedEastDoors == null
                            || variantData.allowedEastDoors.Count == 0
                        )
                        {
                            targetRoomValid = false; // No east doors allowed at all
                        }
                        else if (!variantData.allowedEastDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false; // This position isn't in the allowed list
                        }
                    }

                    if (targetRoomValid && CanPlaceRoom(x, newRoomZ, size.width, size.length))
                    {
                        placements.Add(
                            new PossibleRoomPlacement
                            {
                                x = x,
                                z = newRoomZ,
                                size = size,
                                direction = Direction.West,
                                connectionPoint = connectionPoint,
                            }
                        );
                    }
                }
            }
        }

        Debug.Log($"Found {placements.Count} possible placements");
        return placements;
    }

    void ConnectRooms()
    {
        return;
        // This should ONLY connect rooms that meet the door placement requirements
        // foreach (Room roomA in rooms) {
        //   foreach (Room roomB in rooms) {
        //     if (roomA != roomB && !roomA.connections.Contains(roomB)) {
        //       // Check if they're adjacent
        //       if (AreRoomsAdjacent(roomA, roomB)) {
        //         // Don't automatically connect - check if doors are allowed here
        //         Vector2Int connectionPoint = GetAdjacentConnectionPoint(roomA, roomB);

        //         // Only connect if BOTH rooms allow doors at these positions
        //         if (IsValidDoorPosition(roomA, connectionPoint, GetDirectionFromRoomToRoom(roomA, roomB)) &&
        //             IsValidDoorPosition(roomB, connectionPoint, GetDirectionFromRoomToRoom(roomB, roomA))) {

        //           roomA.connections.Add(roomB);
        //           roomB.connections.Add(roomA);

        //           // Also add connection points for these automatically connected rooms
        //           Direction dirFromAtoB = GetDirectionFromRoomToRoom(roomA, roomB);
        //           Direction dirFromBtoA = GetOppositeDirection(dirFromAtoB);

        //           // Add connection points
        //           Vector2Int localPosInA = GetLocalPosition(connectionPoint, roomA);
        //           Vector2Int localPosInB = GetLocalPosition(connectionPoint, roomB);

        //           roomA.connectionPoints.Add(new ConnectionPoint {
        //             connectedRoom = roomB,
        //             localPosition = localPosInA,
        //             direction = dirFromAtoB
        //           });

        //           roomB.connectionPoints.Add(new ConnectionPoint {
        //             connectedRoom = roomA,
        //             localPosition = localPosInB,
        //             direction = dirFromBtoA
        //           });
        //         }
        //       }
        //     }
        //   }
        // }
    }

    Direction GetDirectionFromRoomToRoom(Room source, Room target)
    {
        if (source.zOrigin + source.length == target.zOrigin)
            return Direction.North;
        if (target.zOrigin + target.length == source.zOrigin)
            return Direction.South;
        if (source.xOrigin + source.width == target.xOrigin)
            return Direction.East;
        if (target.xOrigin + target.width == source.xOrigin)
            return Direction.West;

        // Default (shouldn't happen for adjacent rooms)
        return Direction.North;
    }

    bool IsValidDoorPosition(Room room, Vector2Int worldPosition, Direction direction)
    {
        Vector2Int localPos = GetLocalPosition(worldPosition, room);

        RoomSize size = new RoomSize { width = room.width, length = room.length };
        bool hasVariantData = roomVariantsWrapper
            .ToDictionary()
            .TryGetValue(size, out RoomVariantData variantData);

        if (!hasVariantData || variantData.allowDoorsAnywhere)
            return true;

        switch (direction)
        {
            case Direction.North:
                return variantData.allowedNorthDoors != null
                    && variantData.allowedNorthDoors.Count > 0
                    && variantData.allowedNorthDoors.Contains(localPos);
            case Direction.South:
                return variantData.allowedSouthDoors != null
                    && variantData.allowedSouthDoors.Count > 0
                    && variantData.allowedSouthDoors.Contains(localPos);
            case Direction.East:
                return variantData.allowedEastDoors != null
                    && variantData.allowedEastDoors.Count > 0
                    && variantData.allowedEastDoors.Contains(localPos);
            case Direction.West:
                return variantData.allowedWestDoors != null
                    && variantData.allowedWestDoors.Count > 0
                    && variantData.allowedWestDoors.Contains(localPos);
            default:
                return false;
        }
    }

    Vector2Int GetLocalPosition(Vector2Int worldPosition, Room room)
    {
        return new Vector2Int(worldPosition.x - room.xOrigin, worldPosition.y - room.zOrigin);
    }

    Vector2Int GetAdjacentConnectionPoint(Room roomA, Room roomB)
    {
        // North/South connection
        if (roomA.zOrigin + roomA.length == roomB.zOrigin)
        {
            // Find overlapping x range
            int minX = Mathf.Max(roomA.xOrigin, roomB.xOrigin);
            int maxX = Mathf.Min(roomA.xOrigin + roomA.width, roomB.xOrigin + roomB.width) - 1;
            int x = (minX + maxX) / 2;
            return new Vector2Int(x, roomA.zOrigin + roomA.length - 1);
        }

        // South/North connection
        if (roomB.zOrigin + roomB.length == roomA.zOrigin)
        {
            int minX = Mathf.Max(roomA.xOrigin, roomB.xOrigin);
            int maxX = Mathf.Min(roomA.xOrigin + roomA.width, roomB.xOrigin + roomB.width) - 1;
            int x = (minX + maxX) / 2;
            return new Vector2Int(x, roomA.zOrigin);
        }

        // East/West connection
        if (roomA.xOrigin + roomA.width == roomB.xOrigin)
        {
            int minZ = Mathf.Max(roomA.zOrigin, roomB.zOrigin);
            int maxZ = Mathf.Min(roomA.zOrigin + roomA.length, roomB.zOrigin + roomB.length) - 1;
            int z = (minZ + maxZ) / 2;
            return new Vector2Int(roomA.xOrigin + roomA.width - 1, z);
        }

        // West/East connection
        if (roomB.xOrigin + roomB.width == roomA.xOrigin)
        {
            int minZ = Mathf.Max(roomA.zOrigin, roomB.zOrigin);
            int maxZ = Mathf.Min(roomA.zOrigin + roomA.length, roomB.zOrigin + roomB.length) - 1;
            int z = (minZ + maxZ) / 2;
            return new Vector2Int(roomA.xOrigin, z);
        }

        // Default fallback (shouldn't reach here)
        return new Vector2Int(0, 0);
    }

    bool AreRoomsAdjacent(Room roomA, Room roomB)
    {
        // Check if roomB is to the right of roomA
        if (
            roomA.xOrigin + roomA.width == roomB.xOrigin
            && !(
                roomA.zOrigin + roomA.length <= roomB.zOrigin
                || roomB.zOrigin + roomB.length <= roomA.zOrigin
            )
        )
        {
            return true;
        }

        // Check if roomB is to the left of roomA
        if (
            roomB.xOrigin + roomB.width == roomA.xOrigin
            && !(
                roomA.zOrigin + roomA.length <= roomB.zOrigin
                || roomB.zOrigin + roomB.length <= roomA.zOrigin
            )
        )
        {
            return true;
        }

        // Check if roomB is in front of roomA
        if (
            roomA.zOrigin + roomA.length == roomB.zOrigin
            && !(
                roomA.xOrigin + roomA.width <= roomB.xOrigin
                || roomB.xOrigin + roomB.width <= roomA.xOrigin
            )
        )
        {
            return true;
        }

        // Check if roomB is behind roomA
        if (
            roomB.zOrigin + roomB.length == roomA.zOrigin
            && !(
                roomA.xOrigin + roomA.width <= roomB.xOrigin
                || roomB.xOrigin + roomB.width <= roomA.xOrigin
            )
        )
        {
            return true;
        }

        return false; // Not adjacent
    }

    void RenderDungeon()
    {
        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        // Calculate offset to place starting room at world origin
        Room startingRoom = rooms[0];
        float offsetX = -((startingRoom.xOrigin + (startingRoom.width / 2f)) * gridUnitSize);
        float offsetZ = -((startingRoom.zOrigin + (startingRoom.length / 2f)) * gridUnitSize);

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            bool isStartingRoom = (i == 0);
            GameObject renderedRoom = null;

            // Calculate the scaled room center position with offset
            Vector3 roomCenter = new Vector3(
                (room.xOrigin + (room.width / 2f)) * gridUnitSize + offsetX,
                0,
                (room.zOrigin + (room.length / 2f)) * gridUnitSize + offsetZ
            );

            if (isStartingRoom && startingRoomPrefab != null)
            {
                renderedRoom = Instantiate(
                    startingRoomPrefab,
                    roomCenter,
                    Quaternion.identity,
                    transform
                );
                renderedRoom.name = "StartingRoom";
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

                        renderedRoom = Instantiate(
                            prefab,
                            roomCenter,
                            Quaternion.identity,
                            transform
                        );
                        renderedRoom.name = $"Room_{room.xOrigin}_{room.zOrigin}";
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

            if (renderedRoom != null)
            {
                RoomController controller = renderedRoom.GetComponent<RoomController>();
                if (controller != null)
                {
                    controller.SetupDoorsFromConnections(room.connectionPoints);
                }
                else
                {
                    Debug.LogWarning(
                        $"Room at ({room.xOrigin}, {room.zOrigin}) has no RoomController component"
                    );
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (grid == null || !debug)
            return;

        Vector3 bottomLeft = new Vector3(0, 0, 0);
        Vector3 bottomRight = new Vector3(gridSizeX * gridUnitSize, 0, 0);
        Vector3 topLeft = new Vector3(0, 0, gridSizeZ * gridUnitSize);
        Vector3 topRight = new Vector3(gridSizeX * gridUnitSize, 0, gridSizeZ * gridUnitSize);

        Gizmos.color = Color.gray;
        Gizmos.DrawLine(bottomLeft, bottomRight);
        Gizmos.DrawLine(bottomRight, topRight);
        Gizmos.DrawLine(topRight, topLeft);
        Gizmos.DrawLine(topLeft, bottomLeft);

        // Draw grid cells
        for (int x = 0; x <= gridSizeX; x++)
        {
            Gizmos.DrawLine(
                new Vector3(x * gridUnitSize, 0, 0),
                new Vector3(x * gridUnitSize, 0, gridSizeZ * gridUnitSize)
            );
        }

        for (int z = 0; z <= gridSizeZ; z++)
        {
            Gizmos.DrawLine(
                new Vector3(0, 0, z * gridUnitSize),
                new Vector3(gridSizeX * gridUnitSize, 0, z * gridUnitSize)
            );
        }

        // Draw rooms with different colors
        if (rooms != null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];

                // Starting room is yellow, others are green
                Gizmos.color = (i == 0) ? Color.yellow : Color.green;

                // Draw room area
                Vector3 roomCenter = new Vector3(
                    (room.xOrigin + room.width / 2f) * gridUnitSize,
                    0.01f, // Slightly above grid
                    (room.zOrigin + room.length / 2f) * gridUnitSize
                );
                Vector3 roomSize = new Vector3(
                    room.width * gridUnitSize,
                    0.01f,
                    room.length * gridUnitSize
                );

                Gizmos.DrawCube(roomCenter, roomSize);

                // Draw room outline in black
                Gizmos.color = Color.black;
                Vector3 roomMin = new Vector3(
                    room.xOrigin * gridUnitSize,
                    0.02f,
                    room.zOrigin * gridUnitSize
                );
                Vector3 roomMax = new Vector3(
                    (room.xOrigin + room.width) * gridUnitSize,
                    0.02f,
                    (room.zOrigin + room.length) * gridUnitSize
                );

                Gizmos.DrawLine(roomMin, new Vector3(roomMax.x, roomMin.y, roomMin.z));
                Gizmos.DrawLine(
                    new Vector3(roomMax.x, roomMin.y, roomMin.z),
                    new Vector3(roomMax.x, roomMin.y, roomMax.z)
                );
                Gizmos.DrawLine(
                    new Vector3(roomMax.x, roomMin.y, roomMax.z),
                    new Vector3(roomMin.x, roomMin.y, roomMax.z)
                );
                Gizmos.DrawLine(new Vector3(roomMin.x, roomMin.y, roomMax.z), roomMin);

                // Draw doors with red spheres
                Gizmos.color = Color.red;
                foreach (ConnectionPoint cp in room.connectionPoints)
                {
                    // Calculate world position of door
                    Vector3 doorPos = new Vector3(
                        (room.xOrigin + cp.localPosition.x) * gridUnitSize,
                        0.05f,
                        (room.zOrigin + cp.localPosition.y) * gridUnitSize
                    );

                    // Draw door position
                    Gizmos.DrawSphere(doorPos, 0.2f * gridUnitSize);

                    // Draw a line showing the door direction
                    Vector3 dirVector = Vector3.zero;
                    switch (cp.direction)
                    {
                        case Direction.North:
                            dirVector = Vector3.forward;
                            break;
                        case Direction.South:
                            dirVector = Vector3.back;
                            break;
                        case Direction.East:
                            dirVector = Vector3.right;
                            break;
                        case Direction.West:
                            dirVector = Vector3.left;
                            break;
                    }

                    Gizmos.DrawLine(doorPos, doorPos + dirVector * gridUnitSize * 0.5f);
                }
            }
        }
    }
}
