using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class ChaseComponent : NetworkBehaviour
{
    [Header("Chase Settings")]
    [SerializeField]
    private float updatePathInterval = 0.2f;

    [SerializeField]
    private float maxChaseDistance = 30f;

    [SerializeField]
    private float minDistanceToTarget = 1.5f;
    private float walkSpeed = 3f;
    private float chaseSpeed = 5f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string chaseAnimParam = "IsChasing";

    [SerializeField]
    private string speedParam = "MoveSpeed";

    // Internal state
    protected NavMeshAgent _agent;
    protected GameObject _target;
    private Vector3 _lastKnownPosition;
    private float _pathUpdateTimer;
    protected bool _isActive = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void Initialize(float walkSpeed, float chaseSpeed)
    {
        this.walkSpeed = walkSpeed;
        this.chaseSpeed = chaseSpeed;
    }

    public void StartChasing(GameObject target)
    {
        if (target == null)
            return;

        _target = target;
        _lastKnownPosition = _target.transform.position;
        _agent.speed = chaseSpeed;
        _isActive = true;
        _pathUpdateTimer = 0f; // Force immediate update

        if (animator != null)
            animator.SetBool(chaseAnimParam, true);
    }

    public void StopChasing()
    {
        _isActive = false;
        _target = null;
        _agent.speed = walkSpeed;

        if (animator != null)
            animator.SetBool(chaseAnimParam, false);

        if (_agent != null)
            _agent.ResetPath();
    }

    public bool IsTargetReached()
    {
        if (_target == null || !_isActive)
            return false;

        return Vector3.Distance(transform.position, _target.transform.position)
            <= minDistanceToTarget;
    }

    public bool IsTargetLost()
    {
        return !_isActive || _target == null;
    }

    public void UpdateLastKnownPosition(Vector3 position)
    {
        _lastKnownPosition = position;
    }

    protected virtual void Update()
    {
        if (!IsServerInitialized)
            return;

        if (!_isActive || _agent == null)
            return;

        // if (animator != null)
        //     animator.SetFloat(speedParam, _agent.velocity.magnitude);

        if (_target != null)
        {
            float distanceToTarget = Vector3.Distance(
                transform.position,
                _target.transform.position
            );

            // Update target position (the target reference exists)
            _lastKnownPosition = _target.transform.position;

            // If we're too far from target, stop chasing
            if (distanceToTarget > maxChaseDistance)
            {
                StopChasing();
                return;
            }

            // Update path periodically
            _pathUpdateTimer -= Time.deltaTime;
            if (_pathUpdateTimer <= 0)
            {
                _agent.SetDestination(_lastKnownPosition);
                _pathUpdateTimer = updatePathInterval;
            }
        }
        else
        {
            // Head to last known position if target reference is lost
            if (Vector3.Distance(transform.position, _lastKnownPosition) < minDistanceToTarget)
            {
                StopChasing();
            }
        }
    }
}
