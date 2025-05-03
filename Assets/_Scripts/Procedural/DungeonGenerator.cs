using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Object;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
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

public class DungeonGenerator : NetworkBehaviour
{
    public static DungeonGenerator Instance { get; private set; }
    public static event Action DungeonGenerated;
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
    public List<Room> Rooms => rooms;

    [Header("Enemy Spawning")]
    [SerializeField]
    private EnemySpawnController enemySpawner;

    [SerializeField]
    private bool spawnEnemiesInRooms = true;

    [SerializeField]
    private int minEnemiesPerRoom = 0;

    [SerializeField]
    private int maxEnemiesPerRoom = 3;

    [SerializeField]
    private int seed;

    [SerializeField]
    GameObject doorPrefab;

    private void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (!debug)
            return;

        Debug.Log("=== ROOM VARIANT CONFIGURATIONS ===");

        foreach (var entry in roomVariantsWrapper.roomVariantsList)
        {
            RoomSize size = entry.size;
            RoomVariantData data = entry.variantData;

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
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Scene dungeonScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Dungeon3D");
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(dungeonScene);

        if (!IsServerInitialized)
            return;

        if (seed == 0)
        {
            seed = Random.Range(1, 100000);
        }

        GenerateDungeon(seed);
    }

    [ObserversRpc(BufferLast = true)]
    void GenerateDungeon(int seed)
    {
        Random.InitState(seed);
        grid = new CellType[gridSizeX, gridSizeZ];
        rooms.Clear();

        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }

        PlaceRooms();

        RenderDungeon();

        SetupRoomLighting();

        if (valuableSpawner != null && IsServerInitialized)
        {
            valuableSpawner.SpawnValuablesInRooms();
        }

        if (spawnEnemiesInRooms && enemySpawner != null)
        {
            StartCoroutine(GenerateNavMeshDelayed());
            StartCoroutine(SpawnEnemiesInRooms());
        }

        Random.InitState((int)DateTime.Now.Ticks);
        DungeonGenerated?.Invoke();
    }

    void PlaceRooms()
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

    void RenderDungeon()
    {
        Dictionary<RoomSize, RoomVariantData> roomVariants = roomVariantsWrapper.ToDictionary();

        Room startingRoom = rooms[0];
        float offsetX = -((startingRoom.xOrigin + (startingRoom.width / 2f)) * gridUnitSize);
        float offsetZ = -((startingRoom.zOrigin + (startingRoom.length / 2f)) * gridUnitSize);

        for (int i = 0; i < rooms.Count; i++)
        {
            Room room = rooms[i];
            bool isStartingRoom = (i == 0);
            GameObject renderedRoom = null;

            Vector3 roomCenter = new Vector3(
                (room.xOrigin + (room.width / 2f)) * gridUnitSize + offsetX,
                0,
                (room.zOrigin + (room.length / 2f)) * gridUnitSize + offsetZ
            );

            // Create the room prefab (starting room or normal room)
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

            // After the room prefab is instantiated, place doors based on connections
            if (renderedRoom != null)
            {
                RoomController controller = renderedRoom.GetComponent<RoomController>();
                if (controller != null)
                {
                    controller.SetupDoorsFromConnections(room.connectionPoints);

                    // Spawn doors at the connection points
                    if (IsServerInitialized)
                        SpawnDoors(room, renderedRoom);
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

    // This method handles door spawning for the room based on its connection points


    private HashSet<Vector3> occupiedDoorPositions = new HashSet<Vector3>();

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
            GameObject door = Instantiate(doorPrefab, adjustedPosition, doorAnchor.rotation);

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

    private IEnumerator GenerateNavMeshDelayed()
    {
        yield return new WaitForSeconds(0.5f);

        NavMeshSurface surface = gameObject.AddComponent<NavMeshSurface>();

        surface.collectObjects = CollectObjects.Children;
        surface.layerMask = LayerMask.GetMask("Walkable");

        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;

        surface.defaultArea = 0;

        surface.BuildNavMesh();

        if (debug)
        {
            Debug.Log("NavMesh generation complete");
        }
    }

    private IEnumerator SpawnEnemiesInRooms()
    {
        if (!IsServerInitialized)
            yield break;

        yield return new WaitForSeconds(3f);

        List<Transform> roomsList = new List<Transform>();

        for (int i = 1; i < transform.childCount; i++)
        {
            Transform roomTransform = transform.GetChild(i);
            roomsList.Add(roomTransform);
        }

        int roomCount = roomsList.Count;
        List<Transform> lastThreeRooms = roomsList.GetRange(roomCount - 3, 3);

        Transform[] roomsToSpawn = lastThreeRooms.ToArray();

        enemySpawner.SpawnEnemies(roomsToSpawn, 3);
    }

    private void SetupRoomLighting()
    {
        int litRooms = 0;

        for (int i = 2; i < transform.childCount; i++)
        {
            RoomController room = transform.GetChild(i).GetComponent<RoomController>();
            if (room == null || room.lightingType == RoomController.RoomLightingType.Unimplemented)
                continue;

            if (
                (Random.value < 0.35f && litRooms < 3)
                || (litRooms == 0 && i == transform.childCount - 1)
            )
            {
                room.lightingType = RoomController.RoomLightingType.Enabled;
                room.SetupLightSwitch();
                continue;
            }

            room.lightingType = RoomController.RoomLightingType.Disabled;
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

        if (rooms != null)
        {
            for (int i = 0; i < rooms.Count; i++)
            {
                Room room = rooms[i];
                Gizmos.color = (i == 0) ? Color.yellow : Color.green;
                Vector3 roomCenter = new Vector3(
                    (room.xOrigin + room.width / 2f) * gridUnitSize,
                    0.01f,
                    (room.zOrigin + room.length / 2f) * gridUnitSize
                );
                Vector3 roomSize = new Vector3(
                    room.width * gridUnitSize,
                    0.01f,
                    room.length * gridUnitSize
                );

                Gizmos.DrawCube(roomCenter, roomSize);
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
                Gizmos.color = Color.red;
                foreach (ConnectionPoint cp in room.connectionPoints)
                {
                    Vector3 doorPos = new Vector3(
                        (room.xOrigin + cp.localPosition.x) * gridUnitSize,
                        0.05f,
                        (room.zOrigin + cp.localPosition.y) * gridUnitSize
                    );
                    Gizmos.DrawSphere(doorPos, 0.2f * gridUnitSize);
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
