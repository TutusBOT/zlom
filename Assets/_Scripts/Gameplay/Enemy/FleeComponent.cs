using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class FleeComponent : MonoBehaviour
{
    [Header("Flee Settings")]
    [SerializeField]
    private float fleeDistance = 25f;

    [SerializeField]
    private float minFleeDistanceFromSource = 12f;

    [SerializeField]
    private float pathUpdateInterval = 1.0f;

    [SerializeField]
    private int maxAttempts = 5;

    [SerializeField]
    private float fleeSuccessDistance = 13f;

    [SerializeField]
    private float maxFleeTime = 10f;

    [Header("Room Navigation")]
    [SerializeField]
    private string roomTag = "Room";

    [SerializeField]
    private bool prioritizeRooms = true;

    [SerializeField]
    private int navMeshSampleAttempts = 5;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string fleeingParam = "IsFleeing";

    [SerializeField]
    private string speedParam = "MoveSpeed";

    private NavMeshAgent _agent;
    private Transform _fleeFromTarget;
    private Vector3 _fleeDestination;
    private bool _isFleeing;
    private float _fleeStartTime;
    private bool _hasValidPath;
    private float _nextPathUpdateTime;
    private int _attemptsMade;

    // Cache for performance
    private NavMeshPath _path;
    private List<Room> _nearbyRooms = new List<Room>();
    private Dictionary<Collider, bool> _roomLightCache = new Dictionary<Collider, bool>();

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();

        _path = new NavMeshPath();
    }

    private void Update()
    {
        if (!_isFleeing)
            return;

        if (animator != null && _agent != null)
            animator.SetFloat(speedParam, _agent.velocity.magnitude);

        if (Time.time >= _nextPathUpdateTime && _attemptsMade < maxAttempts)
        {
            _attemptsMade++;
            FindAndSetFleeDestination();
            _nextPathUpdateTime = Time.time + pathUpdateInterval;
        }
    }

    public void StartFleeing(Transform fleeFromTarget)
    {
        if (fleeFromTarget == null || _agent == null)
            return;

        _fleeFromTarget = fleeFromTarget;
        _isFleeing = true;
        _fleeStartTime = Time.time;
        _hasValidPath = false;
        _nextPathUpdateTime = 0f;
        _attemptsMade = 0;
        _roomLightCache.Clear();

        if (animator != null)
            animator.SetBool(fleeingParam, true);

        FindAndSetFleeDestination();
    }

    public void StopFleeing()
    {
        _isFleeing = false;
        _fleeFromTarget = null;
        _roomLightCache.Clear();

        if (animator != null)
            animator.SetBool(fleeingParam, false);

        if (_agent != null)
            _agent.ResetPath();
    }

    public bool IsFleeingComplete()
    {
        if (!_isFleeing || _fleeFromTarget == null)
            return true;

        if (_hasValidPath && Vector3.Distance(transform.position, _fleeDestination) < 1.0f)
            return true;

        float distanceFromSource = Vector3.Distance(transform.position, _fleeFromTarget.position);
        if (distanceFromSource > fleeSuccessDistance)
            return true;

        if (Time.time - _fleeStartTime > maxFleeTime)
            return true;

        return false;
    }

    private void FindAndSetFleeDestination()
    {
        if (prioritizeRooms && TryFindRoomFleePoint(out Vector3 roomPoint))
        {
            _fleeDestination = roomPoint;
            _hasValidPath = true;
            _agent.SetDestination(_fleeDestination);
            return;
        }

        _hasValidPath = FindDirectionalFleePoint(out Vector3 directionalPoint);
        if (_hasValidPath)
        {
            _fleeDestination = directionalPoint;
            _agent.SetDestination(_fleeDestination);
        }
    }

    private bool TryFindRoomFleePoint(out Vector3 fleePoint)
    {
        fleePoint = Vector3.zero;

        // Get rooms from DungeonGenerator
        _nearbyRooms = DungeonGenerator.Instance.Rooms;
        Debug.Log($"Found {_nearbyRooms.Count} rooms for fleeing.");

        if (_nearbyRooms.Count == 0)
            return false;

        // Determine which room the agent is currently in
        Room currentRoom = null;
        foreach (Room room in _nearbyRooms)
        {
            // Calculate room bounds in world space
            Bounds roomBounds = GetRoomBounds(room);

            if (roomBounds.Contains(transform.position))
            {
                currentRoom = room;
                break;
            }
        }

        // Sort rooms - prioritizing different rooms over current room and unlit over lit
        _nearbyRooms.Sort(
            (a, b) =>
            {
                // First priority: Different room over current room
                bool aIsCurrent = (a == currentRoom);
                bool bIsCurrent = (b == currentRoom);

                if (!aIsCurrent && bIsCurrent)
                    return -1; // A is better (not current room)
                if (aIsCurrent && !bIsCurrent)
                    return 1; // B is better (not current room)

                // Second priority: Unlit over lit
                bool isRoomALit = IsRoomLit(a);
                bool isRoomBLit = IsRoomLit(b);

                if (!isRoomALit && isRoomBLit)
                    return -1; // A is better (unlit)
                if (isRoomALit && !isRoomBLit)
                    return 1; // B is better (unlit)

                // Third priority: Distance from danger
                Bounds boundsA = GetRoomBounds(a);
                Bounds boundsB = GetRoomBounds(b);
                float distA = Vector3.Distance(boundsA.center, _fleeFromTarget.position);
                float distB = Vector3.Distance(boundsB.center, _fleeFromTarget.position);
                return distB.CompareTo(distA); // Further is better
            }
        );

        // Try unlit rooms (non-current first)
        foreach (Room room in _nearbyRooms)
        {
            // Skip current room in first pass
            if (room == currentRoom)
                continue;

            if (IsRoomLit(room))
                continue;

            Bounds roomBounds = GetRoomBounds(room);
            if (TryGetRandomNavPointInBounds(roomBounds, out Vector3 navPoint))
            {
                fleePoint = navPoint;
                Debug.DrawLine(transform.position, fleePoint, Color.blue, 2f);
                return true;
            }
        }

        // If we got here and still haven't found a room, try any distant point in current room
        // Only if it's unlit
        if (currentRoom != null && !IsRoomLit(currentRoom))
        {
            Bounds roomBounds = GetRoomBounds(currentRoom);

            // Get a point that's significantly far from current position
            for (int i = 0; i < navMeshSampleAttempts; i++)
            {
                Vector3 randomPoint = new Vector3(
                    Random.Range(roomBounds.min.x, roomBounds.max.x),
                    transform.position.y,
                    Random.Range(roomBounds.min.z, roomBounds.max.z)
                );

                // Check if point is far enough from current position
                if (Vector3.Distance(randomPoint, transform.position) > fleeDistance * 0.5f)
                {
                    if (
                        NavMesh.SamplePosition(
                            randomPoint,
                            out NavMeshHit hit,
                            2f,
                            NavMesh.AllAreas
                        )
                    )
                    {
                        if (
                            _agent.CalculatePath(hit.position, _path)
                            && _path.status == NavMeshPathStatus.PathComplete
                        )
                        {
                            fleePoint = hit.position;
                            Debug.DrawLine(transform.position, fleePoint, Color.cyan, 2f);
                            return true;
                        }
                    }
                }
            }
        }

        // Fall back to lit rooms far from danger (excluding current room if possible)
        foreach (Room room in _nearbyRooms)
        {
            Bounds roomBounds = GetRoomBounds(room);

            // Skip the room with the light source
            if (roomBounds.Contains(_fleeFromTarget.position))
                continue;

            // Skip current room in second pass too unless it's the last resort
            if (room == currentRoom && _nearbyRooms.Count > 1)
                continue;

            float distanceFromDanger = Vector3.Distance(
                roomBounds.center,
                _fleeFromTarget.position
            );

            if (distanceFromDanger < minFleeDistanceFromSource)
                continue;

            if (TryGetRandomNavPointInBounds(roomBounds, out Vector3 navPoint))
            {
                fleePoint = navPoint;
                Debug.DrawLine(transform.position, fleePoint, Color.yellow, 2f);
                return true;
            }
        }

        return false;
    }

    // Helper to get room bounds in world space based on DungeonGenerator's Room struct
    private Bounds GetRoomBounds(Room room)
    {
        float gridUnitSize = DungeonGenerator.Instance.gridUnitSize;

        // Calculate actual world positions
        float offsetX = -(
            (
                DungeonGenerator.Instance.Rooms[0].xOrigin
                + (DungeonGenerator.Instance.Rooms[0].width / 2f)
            ) * gridUnitSize
        );
        float offsetZ = -(
            (
                DungeonGenerator.Instance.Rooms[0].zOrigin
                + (DungeonGenerator.Instance.Rooms[0].length / 2f)
            ) * gridUnitSize
        );

        Vector3 center = new Vector3(
            (room.xOrigin + (room.width / 2f)) * gridUnitSize + offsetX,
            0,
            (room.zOrigin + (room.length / 2f)) * gridUnitSize + offsetZ
        );

        Vector3 size = new Vector3(
            room.width * gridUnitSize,
            4f, // Room height - adjust if needed
            room.length * gridUnitSize
        );

        return new Bounds(center, size);
    }

    // Modified IsRoomLit to work with Room struct
    private bool IsRoomLit(Room room)
    {
        // First check if this room contains the light source
        Bounds roomBounds = GetRoomBounds(room);
        if (_fleeFromTarget != null && roomBounds.Contains(_fleeFromTarget.position))
            return true;

        // Otherwise, try to find the actual GameObject for this room to check its controller
        for (int i = 0; i < DungeonGenerator.Instance.transform.childCount; i++)
        {
            Transform child = DungeonGenerator.Instance.transform.GetChild(i);
            RoomController controller = child.GetComponent<RoomController>();

            if (controller != null)
            {
                // Try to match the room by position
                Bounds transformBounds = new Bounds(child.position, Vector3.one * 0.1f);
                if (roomBounds.Intersects(transformBounds))
                {
                    // Cache result if using collider-based caching
                    return controller.IsLit;
                }
            }
        }

        // Default to false if we couldn't determine lighting
        return false;
    }

    private bool TryGetRandomNavPointInBounds(Bounds bounds, out Vector3 result)
    {
        result = Vector3.zero;

        for (int i = 0; i < navMeshSampleAttempts; i++)
        {
            // Keep y coordinate consistent with agent's current height
            Vector3 randomPoint = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                transform.position.y,
                Random.Range(bounds.min.z, bounds.max.z)
            );

            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                if (
                    _agent.CalculatePath(hit.position, _path)
                    && _path.status == NavMeshPathStatus.PathComplete
                )
                {
                    result = hit.position;
                    return true;
                }
            }
        }

        return false;
    }

    private bool FindDirectionalFleePoint(out Vector3 fleePoint)
    {
        fleePoint = Vector3.zero;

        if (_fleeFromTarget == null)
            return false;

        Vector3 fleeDirection = (transform.position - _fleeFromTarget.position).normalized;

        // Try cardinal directions first (more efficient)
        int[] angles = { 0, 45, 90, 135, 180, 225, 270, 315 };
        foreach (int angle in angles)
        {
            Vector3 attemptDir = Quaternion.Euler(0, angle, 0) * fleeDirection;
            Vector3 targetPosition = transform.position + attemptDir * fleeDistance;

            if (
                _agent.CalculatePath(targetPosition, _path)
                && _path.status == NavMeshPathStatus.PathComplete
            )
            {
                Vector3 finalPoint = _path.corners[_path.corners.Length - 1];
                float distToTarget = Vector3.Distance(finalPoint, _fleeFromTarget.position);

                if (distToTarget > minFleeDistanceFromSource)
                {
                    fleePoint = finalPoint;
                    Debug.DrawLine(transform.position, fleePoint, Color.green, 2f);
                    return true;
                }
            }
        }

        // Random fallback (fewer attempts for better performance)
        for (int i = 0; i < 6; i++)
        {
            Vector3 randomDir = new Vector3(
                Random.Range(-1f, 1f),
                0,
                Random.Range(-1f, 1f)
            ).normalized;
            Vector3 targetPosition = transform.position + randomDir * (fleeDistance * 0.7f);

            if (
                _agent.CalculatePath(targetPosition, _path)
                && _path.status == NavMeshPathStatus.PathComplete
            )
            {
                fleePoint = _path.corners[_path.corners.Length - 1];
                Debug.DrawLine(transform.position, fleePoint, Color.yellow, 2f);
                return true;
            }
        }

        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_fleeFromTarget != null && _isFleeing)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _fleeFromTarget.position);

            Gizmos.color = _hasValidPath ? Color.green : Color.yellow;
            Gizmos.DrawSphere(_fleeDestination, 0.5f);
        }
    }
}
