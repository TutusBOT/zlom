using System.Collections.Generic;
using UnityEngine;
using static DungeonGenerator;

struct PossibleRoomPlacement
{
    public int x,
        z;
    public RoomSize size;
    public Direction direction;
    public Vector2Int connectionPoint;
}

public class RoomPlacer
{
    public int gridSizeX;
    public int gridSizeZ;
    private CellType[,] grid;
    public RoomVariantsWrapper roomVariantsWrapper = new RoomVariantsWrapper();

    public RoomPlacer(int gridSizeX, int gridSizeZ)
    {
        this.gridSizeX = gridSizeX;
        this.gridSizeZ = gridSizeZ;
        grid = new CellType[gridSizeX, gridSizeZ];
    }

    public void PlaceRooms(List<Room> rooms, int roomCount, bool debug)
    {
        if (roomCount <= 0)
            return;

        List<RoomSize> availableSizes = new List<RoomSize>(roomVariantsWrapper.ToDictionary().Keys);
        if (availableSizes.Count == 0)
        {
            availableSizes.Add(new RoomSize { width = 1, length = 1 });
        }

        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        RoomSize startingSize = new RoomSize { width = 1, length = 1 };
        int startX = (gridSizeX - startingSize.width) / 2;
        int startZ = 0;

        Room startingRoom = new Room(startX, startZ, startingSize.width, startingSize.length);
        rooms.Add(startingRoom);
        MarkRoomInGrid(startingRoom);

        List<PossibleRoomPlacement> startingRoomPlacements = GetPossibleRoomPlacements(
            startingRoom,
            availableSizes
        );

        if (startingRoomPlacements.Count > 0)
        {
            List<PossibleRoomPlacement> validPlacements = new List<PossibleRoomPlacement>();

            foreach (var placement in startingRoomPlacements)
            {
                RoomSize placementSize = placement.size;

                bool isValidPlacement = true;
                if (
                    roomVariants.TryGetValue(placementSize, out RoomVariantData variantData)
                    && !variantData.allowDoorsAnywhere
                )
                {
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

            if (validPlacements.Count > 0)
            {
                PossibleRoomPlacement placement = validPlacements[
                    Random.Range(0, validPlacements.Count)
                ];

                Room secondRoom = new Room(
                    placement.x,
                    placement.z,
                    placement.size.width,
                    placement.size.length
                );
                rooms.Add(secondRoom);
                MarkRoomInGrid(secondRoom);

                CreateRoomConnection(startingRoom, secondRoom, placement);

                int placedRooms = 2;
                int attempts = 0;
                int maxAttempts = roomCount * 10;

                while (placedRooms < roomCount && attempts < maxAttempts)
                {
                    attempts++;

                    Room currentRoom = rooms[Random.Range(1, rooms.Count)];

                    List<PossibleRoomPlacement> possiblePlacements = GetPossibleRoomPlacements(
                        currentRoom,
                        availableSizes
                    );
                    validPlacements.Clear();

                    foreach (var p in possiblePlacements)
                    {
                        RoomSize placementSize = p.size;

                        bool isValidPlacement = true;
                        if (
                            roomVariants.TryGetValue(
                                placementSize,
                                out RoomVariantData pVariantData
                            ) && !pVariantData.allowDoorsAnywhere
                        )
                        {
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

                        if (
                            roomVariants.TryGetValue(
                                new RoomSize { width = newRoom.width, length = newRoom.length },
                                out RoomVariantData checkVariantData
                            ) && !checkVariantData.allowDoorsAnywhere
                        )
                        {
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

                if (debug)
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

    private void CreateRoomConnection(
        Room sourceRoom,
        Room targetRoom,
        PossibleRoomPlacement placement
    )
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
                targetLocalPos = new Vector2Int(placement.connectionPoint.x - placement.x, 0);
                break;
            case Direction.South:
                targetLocalPos = new Vector2Int(
                    placement.connectionPoint.x - placement.x,
                    placement.size.length - 1
                );
                break;
            case Direction.East:
                targetLocalPos = new Vector2Int(0, placement.connectionPoint.y - placement.z);
                break;
            case Direction.West:
                targetLocalPos = new Vector2Int(
                    placement.size.width - 1,
                    placement.connectionPoint.y - placement.z
                );
                break;
        }

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

    private Direction GetOppositeDirection(Direction dir)
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

    private void MarkRoomInGrid(Room room)
    {
        for (int x = room.xOrigin; x < room.xOrigin + room.width; x++)
        {
            for (int z = room.zOrigin; z < room.zOrigin + room.length; z++)
            {
                grid[x, z] = CellType.Room;
            }
        }
    }

    private bool CanPlaceRoom(int x, int z, int width, int length)
    {
        if (x < 0 || z < 0 || x + width > gridSizeX || z + length > gridSizeZ)
        {
            return false;
        }

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

    private List<PossibleRoomPlacement> GetPossibleRoomPlacements(
        Room room,
        List<RoomSize> availableSizes
    )
    {
        List<PossibleRoomPlacement> placements = new List<PossibleRoomPlacement>();
        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        foreach (RoomSize size in availableSizes)
        {
            bool hasVariantData = roomVariants.TryGetValue(size, out RoomVariantData variantData);

            bool canPlaceNorth = true;
            bool canPlaceSouth = true;
            bool canPlaceEast = true;
            bool canPlaceWest = true;

            if (hasVariantData && !variantData.allowDoorsAnywhere)
            {
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
            }

            if (canPlaceNorth)
            {
                for (int x = room.xOrigin; x < room.xOrigin + room.width; x++)
                {
                    int z = room.zOrigin + room.length;

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

                    int newRoomX = x - (size.width / 2);
                    newRoomX = Mathf.Clamp(newRoomX, 0, gridSizeX - size.width);

                    Vector2Int connectionPoint = new Vector2Int(x, z - 1);

                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(x - newRoomX, 0);

                        if (
                            variantData.allowedSouthDoors == null
                            || variantData.allowedSouthDoors.Count == 0
                        )
                        {
                            targetRoomValid = false;
                        }
                        else if (!variantData.allowedSouthDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false;
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

                    int newRoomX = x - (size.width / 2);
                    newRoomX = Mathf.Clamp(newRoomX, 0, gridSizeX - size.width);

                    Vector2Int connectionPoint = new Vector2Int(x, room.zOrigin);

                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(x - newRoomX, size.length - 1);

                        if (
                            variantData.allowedNorthDoors == null
                            || variantData.allowedNorthDoors.Count == 0
                        )
                        {
                            targetRoomValid = false;
                        }
                        else if (!variantData.allowedNorthDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false;
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

                    int newRoomZ = z - (size.length / 2);
                    newRoomZ = Mathf.Clamp(newRoomZ, 0, gridSizeZ - size.length);

                    Vector2Int connectionPoint = new Vector2Int(x - 1, z);

                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(0, z - newRoomZ);

                        if (
                            variantData.allowedWestDoors == null
                            || variantData.allowedWestDoors.Count == 0
                        )
                        {
                            targetRoomValid = false;
                        }
                        else if (!variantData.allowedWestDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false;
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

                    int newRoomZ = z - (size.length / 2);
                    newRoomZ = Mathf.Clamp(newRoomZ, 0, gridSizeZ - size.length);

                    Vector2Int connectionPoint = new Vector2Int(room.xOrigin, z);

                    bool targetRoomValid = true;
                    if (hasVariantData && !variantData.allowDoorsAnywhere)
                    {
                        Vector2Int targetLocalPos = new Vector2Int(size.width - 1, z - newRoomZ);

                        if (
                            variantData.allowedEastDoors == null
                            || variantData.allowedEastDoors.Count == 0
                        )
                        {
                            targetRoomValid = false;
                        }
                        else if (!variantData.allowedEastDoors.Contains(targetLocalPos))
                        {
                            targetRoomValid = false;
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

        return placements;
    }
}
