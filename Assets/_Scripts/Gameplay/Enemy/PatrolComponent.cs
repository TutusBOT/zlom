using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class PatrolComponent : NetworkBehaviour
{
    private enum PatrolState
    {
        Moving,
        Waiting,
        PathFinding,
    }

    [Header("Patrol Settings")]
    [SerializeField]
    private float waypointWaitTime = 1.0f;

    [SerializeField]
    private float distanceThreshold = 0.5f;
    private float patrolSpeed = 3f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string moveSpeedParam = "MoveSpeed";

    private NavMeshAgent _agent;
    private Waypoint _currentWaypoint;
    private Waypoint _previousWaypoint;
    private bool _isActive = false;
    private float _waitTimer;

    private PatrolState _currentState = PatrolState.Waiting;
    private float _pathRetryTimer = 0f;
    private const float PATH_RETRY_DELAY = 0.5f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Initialize(float patrolSpeed)
    {
        Debug.Log($"Patrol started");
        this.patrolSpeed = patrolSpeed;
        if (_agent != null)
        {
            _agent.speed = patrolSpeed;
        }
    }

    public void StartPatrolling(GameObject startWaypoint)
    {
        if (startWaypoint == null && WaypointManager.Instance != null)
        {
            Waypoint nearest = WaypointManager.Instance.GetNearestWaypoint(transform.position);
            if (nearest != null)
                startWaypoint = nearest.gameObject;
            else
            {
                Debug.LogError("No waypoint found to start patrolling");
                return;
            }
        }

        Waypoint waypoint = startWaypoint?.GetComponent<Waypoint>();
        if (waypoint == null)
        {
            Debug.LogError("StartWaypoint doesn't have a Waypoint component");
            return;
        }

        _currentWaypoint = waypoint;
        _previousWaypoint = null;
        _isActive = true;
        _agent.speed = patrolSpeed;

        // Set initial destination directly instead of calling MoveToNextWaypoint
        Debug.Log($"Starting patrol at waypoint: {_currentWaypoint.transform.position}");
        _agent.SetDestination(_currentWaypoint.transform.position);

        // Switch to pathfinding state
        _currentState = PatrolState.PathFinding;
        _pathRetryTimer = PATH_RETRY_DELAY;
    }

    public void StopPatrolling()
    {
        _isActive = false;
        _currentState = PatrolState.Waiting;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    public bool HasReachedDestination()
    {
        if (!_isActive || _agent == null || _currentWaypoint == null)
            return false;

        // Use a more reliable check that avoids false positives
        if (
            _agent.pathStatus == NavMeshPathStatus.PathInvalid
            || _agent.pathStatus == NavMeshPathStatus.PathPartial
        )
        {
            return false;
        }

        // Don't check immediately after setting a path
        if (_currentState == PatrolState.PathFinding)
        {
            return false;
        }

        bool pathFinished =
            !_agent.pathPending
            && _agent.hasPath
            && (_agent.remainingDistance <= distanceThreshold || _agent.remainingDistance == 0);

        bool directDistance =
            Vector3.Distance(transform.position, _currentWaypoint.transform.position)
            <= distanceThreshold;

        return pathFinished || directDistance;
    }

    private void Update()
    {
        if (!IsServerInitialized || !_isActive || _agent == null)
            return;

        switch (_currentState)
        {
            case PatrolState.Waiting:
                if (_waitTimer > 0)
                    _waitTimer -= Time.deltaTime;

                // Add logging to help debug
                if (_waitTimer <= 0.1f)
                {
                    Debug.Log($"Wait timer nearly expired: {_waitTimer}s remaining");
                }

                if (_waitTimer <= 0f)
                {
                    Debug.Log(
                        $"Wait timer expired, moving from waypoint after {waypointWaitTime}s wait"
                    );
                    MoveToNextWaypoint(false);
                }
                break;

            case PatrolState.PathFinding:
                _pathRetryTimer -= Time.deltaTime;
                if (_pathRetryTimer <= 0)
                {
                    // Switch back to moving state after short delay
                    _currentState = PatrolState.Moving;
                }
                break;

            case PatrolState.Moving:
                if (HasReachedDestination())
                {
                    // Switch to waiting state
                    _currentState = PatrolState.Waiting;
                    _waitTimer = waypointWaitTime;
                    _agent.ResetPath();

                    Debug.Log(
                        $"Reached waypoint: {_currentWaypoint.transform.position}, waiting for {waypointWaitTime}s"
                    );
                }
                else if (!_agent.hasPath && !_agent.pathPending && _currentWaypoint != null)
                {
                    // Path failed, retry
                    Debug.Log($"Path to {_currentWaypoint.transform.position} failed, retrying...");
                    _agent.SetDestination(_currentWaypoint.transform.position);
                }
                break;
        }
    }

    private void MoveToNextWaypoint(bool isInitialMove = false)
    {
        if (_currentWaypoint == null)
            return;

        if (!isInitialMove) // Only get next waypoint if not initial move
        {
            _previousWaypoint = _currentWaypoint;
            _currentWaypoint = _previousWaypoint.GetNextWaypoint(_previousWaypoint);

            if (_currentWaypoint == null)
            {
                Debug.LogWarning(
                    $"No connected waypoints available from {_previousWaypoint.transform.position}"
                );
                StopPatrolling();
                return;
            }
        }

        // Set the destination
        Debug.Log(
            $"Moving to waypoint: {_currentWaypoint.transform.position}"
                + (_previousWaypoint != null ? $" from {_previousWaypoint.transform.position}" : "")
        );

        Vector3 targetPos = _currentWaypoint.transform.position;
        Vector3 finalDestination = targetPos;

        _agent.SetDestination(finalDestination);

        _currentState = PatrolState.PathFinding;
        _pathRetryTimer = PATH_RETRY_DELAY;
    }

    public GameObject CurrentWaypoint => _currentWaypoint?.gameObject;

    public void SetWaitTime(float seconds)
    {
        waypointWaitTime = seconds;
    }

    public void SetDistanceThreshold(float distance)
    {
        distanceThreshold = distance;
        if (_agent != null)
            _agent.stoppingDistance = distance * 0.8f;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _agent == null || _currentWaypoint == null)
            return;

        // Draw line to destination
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, _currentWaypoint.transform.position);

        // Draw agent path
        if (_agent.hasPath)
        {
            Gizmos.color = Color.green;
            Vector3[] corners = _agent.path.corners;
            for (int i = 0; i < corners.Length - 1; i++)
            {
                Gizmos.DrawLine(corners[i], corners[i + 1]);
            }
        }
    }
}
