using UnityEngine;
using UnityEngine.AI;

public class ChaseComponent : MonoBehaviour
{
    [Header("Chase Settings")]
    [SerializeField]
    private float updatePathInterval = 0.2f;

    [SerializeField]
    private float maxChaseDistance = 30f;

    [SerializeField]
    private float minDistanceToTarget = 1.5f;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string chaseAnimParam = "IsChasing";

    [SerializeField]
    private string speedParam = "MoveSpeed";

    // Internal state
    private NavMeshAgent _agent;
    private Transform _target;
    private Vector3 _lastKnownPosition;
    private float _pathUpdateTimer;
    private bool _isActive = false;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void StartChasing(Transform targetTransform)
    {
        if (targetTransform == null)
            return;

        _target = targetTransform;
        _lastKnownPosition = _target.position;
        _isActive = true;
        _pathUpdateTimer = 0f; // Force immediate update

        // Set animation
        if (animator != null)
            animator.SetBool(chaseAnimParam, true);
    }

    public void StopChasing()
    {
        _isActive = false;
        _target = null;

        // Reset animation
        if (animator != null)
            animator.SetBool(chaseAnimParam, false);

        // Stop the agent
        if (_agent != null)
            _agent.ResetPath();
    }

    // Check if target is close enough for other actions (like attacking)
    public bool IsTargetReached()
    {
        if (_target == null || !_isActive)
            return false;

        return Vector3.Distance(transform.position, _target.position) <= minDistanceToTarget;
    }

    // Check if we've lost the target completely
    public bool IsTargetLost()
    {
        return !_isActive || _target == null;
    }

    // Update the last known position of the target
    // Called by external systems when target is spotted
    public void UpdateLastKnownPosition(Vector3 position)
    {
        _lastKnownPosition = position;
    }

    private void Update()
    {
        if (!_isActive || _agent == null)
            return;

        // Update animation speed
        if (animator != null)
            animator.SetFloat(speedParam, _agent.velocity.magnitude);

        // Check for target proximity
        if (_target != null)
        {
            float distanceToTarget = Vector3.Distance(transform.position, _target.position);

            // Update target position (the target reference exists)
            _lastKnownPosition = _target.position;

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
                // We've reached last known position and target is gone
                StopChasing();
            }
        }
    }
}
