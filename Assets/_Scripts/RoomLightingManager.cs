using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public interface ILightingStateReceiver
{
    void OnLightingStateChanged(bool isPowered, bool hasWorkingLights);
}

public class RoomLightingManager : NetworkBehaviour
{
    private static RoomLightingManager _instance;
    public static RoomLightingManager Instance => _instance;

    // Dictionary mapping room IDs to their power state
    private readonly SyncDictionary<int, bool> _roomPowerStates = new SyncDictionary<int, bool>();

    // Dictionary mapping light IDs to their destroyed state
    private readonly SyncDictionary<int, bool> _destroyedLights = new SyncDictionary<int, bool>();
    private readonly SyncDictionary<int, bool> _roomCanBePowered = new SyncDictionary<int, bool>();

    // Track all room controllers
    private Dictionary<int, RoomController> _roomControllers =
        new Dictionary<int, RoomController>();
    private Dictionary<int, RoomLightSource> _lightSources = new Dictionary<int, RoomLightSource>();

    [SerializeField]
    private bool debug;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DungeonGenerator.DungeonGenerated += OnDungeonGenerated;
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();

        // Listen for changes
        _roomPowerStates.OnChange += OnRoomPowerStateChanged;
        _destroyedLights.OnChange += OnDestroyedLightChanged;
    }

    public override void OnStopNetwork()
    {
        base.OnStopNetwork();

        // Unsubscribe
        _roomPowerStates.OnChange -= OnRoomPowerStateChanged;
        _destroyedLights.OnChange -= OnDestroyedLightChanged;
    }

    private void OnDestroy()
    {
        DungeonGenerator.DungeonGenerated -= OnDungeonGenerated;
    }

    private void OnDungeonGenerated()
    {
        RegisterAllRoomsAndLights();
    }

    public void RegisterAllRoomsAndLights()
    {
        RoomController[] rooms = FindObjectsByType<RoomController>(FindObjectsSortMode.None);
        int implementedCount = 0;

        foreach (var room in rooms)
        {
            // Generate a unique ID based on position
            int roomId = GenerateRoomId(room);

            // Track if room can be powered based on its type
            bool canBePowered = room.CanBePowered;

            // Register room controller
            _roomControllers[roomId] = room;
            if (room.HasLightingImplementation)
            {
                implementedCount++;

                // Find and register all lights in this room
                RoomLightSource[] lights = room.GetComponentsInChildren<RoomLightSource>(true);
                foreach (var light in lights)
                {
                    int lightId = GenerateLightId(light);
                    _lightSources[lightId] = light;

                    // Default to not destroyed unless already set
                    if (!_destroyedLights.ContainsKey(lightId))
                    {
                        _destroyedLights.Add(lightId, false);
                    }
                }
            }

            if (!IsServerInitialized)
                continue;

            // Register power ability
            if (_roomCanBePowered.ContainsKey(roomId))
            {
                _roomCanBePowered[roomId] = canBePowered;
            }
            else
            {
                _roomCanBePowered.Add(roomId, canBePowered);
            }

            // Default to powered off unless already set
            if (!_roomPowerStates.ContainsKey(roomId))
            {
                _roomPowerStates.Add(roomId, false);
            }
        }

        if (debug)
            Debug.Log(
                $"RoomLightingManager registered {implementedCount} of {rooms.Length} rooms with lighting "
                    + $"({_lightSources.Count} lights total)"
            );
    }

    private int GenerateRoomId(RoomController room) // ID is based on position
    {
        Vector3 pos = room.transform.position;
        return Mathf.RoundToInt(pos.x * 1000)
            + Mathf.RoundToInt(pos.y * 100)
            + Mathf.RoundToInt(pos.z * 10);
    }

    private int GenerateLightId(RoomLightSource light) // ID is based on position
    {
        Vector3 pos = light.transform.position;
        return Mathf.RoundToInt(pos.x * 10000)
            + Mathf.RoundToInt(pos.y * 1000)
            + Mathf.RoundToInt(pos.z * 100);
    }

    private void OnRoomPowerStateChanged(
        SyncDictionaryOperation op,
        int key,
        bool value,
        bool asServer
    )
    {
        if (_roomControllers.TryGetValue(key, out RoomController room))
        {
            if (room is ILightingStateReceiver receiver)
            {
                bool hasWorkingLights = CheckRoomHasWorkingLights(room);
                receiver.OnLightingStateChanged(value, hasWorkingLights);
            }

            UpdateRoomLights(room, value);
        }
    }

    private void OnDestroyedLightChanged(
        SyncDictionaryOperation op,
        int key,
        bool value,
        bool asServer
    )
    {
        if (_lightSources.TryGetValue(key, out RoomLightSource light))
        {
            // Update light state based on destroyed status
            if (value) // If destroyed
            {
                light.SetLightState(RoomLightSource.LightState.Destroyed);
            }
            else
            {
                // If not destroyed, set based on room power
                RoomController room = light.GetComponentInParent<RoomController>();
                if (room != null)
                {
                    int roomId = GenerateRoomId(room);
                    bool isPowered =
                        _roomPowerStates.TryGetValue(roomId, out bool powered) && powered;

                    light.SetLightState(
                        isPowered ? RoomLightSource.LightState.On : RoomLightSource.LightState.Off
                    );
                }
            }

            // Update room lighting status
            RoomController lightRoom = light.GetComponentInParent<RoomController>();
            if (lightRoom != null && lightRoom is ILightingStateReceiver receiver)
            {
                int roomId = GenerateRoomId(lightRoom);
                bool isPowered = _roomPowerStates.TryGetValue(roomId, out bool powered) && powered;
                bool hasWorkingLights = CheckRoomHasWorkingLights(lightRoom);

                receiver.OnLightingStateChanged(isPowered, hasWorkingLights);
            }
        }
    }

    private void UpdateRoomLights(RoomController room, bool isPowered)
    {
        RoomLightSource[] lights = room.GetComponentsInChildren<RoomLightSource>(true);

        foreach (var light in lights)
        {
            int lightId = GenerateLightId(light);
            bool isDestroyed =
                _destroyedLights.TryGetValue(lightId, out bool destroyed) && destroyed;

            if (isDestroyed)
            {
                light.SetLightState(RoomLightSource.LightState.Destroyed);
            }
            else if (isPowered)
            {
                light.SetLightState(RoomLightSource.LightState.On);
            }
            else
            {
                light.SetLightState(RoomLightSource.LightState.Off);
            }
        }
    }

    private bool CheckRoomHasWorkingLights(RoomController room)
    {
        RoomLightSource[] lights = room.GetComponentsInChildren<RoomLightSource>(true);

        foreach (var light in lights)
        {
            int lightId = GenerateLightId(light);
            bool isDestroyed =
                _destroyedLights.TryGetValue(lightId, out bool destroyed) && destroyed;

            if (!isDestroyed)
                return true;
        }

        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetRoomPowerServerRpc(Vector3 roomPosition, bool isPowered)
    {
        RoomController room = FindRoomAtPosition(roomPosition);
        if (room == null)
            return;

        int roomId = GenerateRoomId(room);

        // Only allow powering if room can be powered (Enabled type)
        bool canBePowered = _roomCanBePowered.TryGetValue(roomId, out bool powerable) && powerable;

        if (!canBePowered)
        {
            Debug.Log($"Cannot power room at {roomPosition} - room is disabled or unimplemented");
            return;
        }

        if (_roomPowerStates.ContainsKey(roomId))
        {
            _roomPowerStates[roomId] = isPowered;
        }
        else
        {
            _roomPowerStates.Add(roomId, isPowered);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DestroyLightServerRpc(Vector3 lightPosition)
    {
        if (!IsServerInitialized)
            return;

        RoomLightSource light = FindLightAtPosition(lightPosition);
        if (light != null)
        {
            int lightId = GenerateLightId(light);
            if (_destroyedLights.ContainsKey(lightId))
            {
                _destroyedLights[lightId] = true;
            }
            else
            {
                _destroyedLights.Add(lightId, true);
            }
        }
    }

    private RoomController FindRoomAtPosition(Vector3 position)
    {
        foreach (var kvp in _roomControllers)
        {
            if (Vector3.Distance(kvp.Value.transform.position, position) < 0.5f)
            {
                return kvp.Value;
            }
        }
        return null;
    }

    private RoomLightSource FindLightAtPosition(Vector3 position)
    {
        foreach (var kvp in _lightSources)
        {
            if (Vector3.Distance(kvp.Value.transform.position, position) < 0.5f)
            {
                return kvp.Value;
            }
        }
        return null;
    }

    public bool IsRoomPowered(RoomController room)
    {
        if (room == null)
            return false;

        int roomId = GenerateRoomId(room);
        return _roomPowerStates.TryGetValue(roomId, out bool powered) && powered;
    }

    public bool IsLightDestroyed(RoomLightSource light)
    {
        if (light == null)
            return false;

        int lightId = GenerateLightId(light);
        return _destroyedLights.TryGetValue(lightId, out bool destroyed) && destroyed;
    }
}
