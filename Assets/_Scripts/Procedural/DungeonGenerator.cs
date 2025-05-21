using System;
using System.Collections;
using System.Collections.Generic;
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
    private EnemiesManager enemiesManager;

    [SerializeField]
    private bool spawnEnemiesInRooms = true;

    [SerializeField]
    private int minEnemiesPerRoom = 0;

    [SerializeField]
    private int maxEnemiesPerRoom = 3;

    [SerializeField]
    public List<GameObject> waypoints = new List<GameObject>();

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

        DebugRoomVariantConfiguration();
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

        RoomPlacer roomPlacer = new RoomPlacer(gridSizeX, gridSizeZ);
        roomPlacer.PlaceRooms(rooms, roomCount, true);

        RoomRenderer roomRenderer = new RoomRenderer(
            roomVariantsWrapper,
            gridUnitSize,
            transform,
            startingRoomPrefab,
            doorPrefab,
            debug
        );
        roomRenderer.RenderDungeon(rooms, waypoints, IsServerInitialized);

        SetupRoomLighting();

        if (valuableSpawner != null && IsServerInitialized)
        {
            valuableSpawner.SpawnValuablesInRooms();
        }

        if (spawnEnemiesInRooms && enemiesManager != null)
        {
            StartCoroutine(GenerateNavMeshDelayed());
            StartCoroutine(SpawnEnemiesInRooms());
        }

        InitializeWaypoints();

        Random.InitState((int)DateTime.Now.Ticks);
        DungeonGenerated?.Invoke();
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
        enemiesManager.SpawnEnemies(roomsToSpawn, waypoints, 3);
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

    private void InitializeWaypoints()
    {
        WaypointPlacer waypointPlacer = new WaypointPlacer();
        waypointPlacer.InitializeWaypoints(transform);
    }

    //
    // -----------------------------------------------------------------------
    // Debug
    //

    private void DebugRoomVariantConfiguration()
    {
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
