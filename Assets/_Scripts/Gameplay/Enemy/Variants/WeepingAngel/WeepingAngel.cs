using UnityEngine;

public class WeepingAngel : Enemy
{
    [Header("Vision Detection")]
    [SerializeField]
    private float playerDetectionDistance = 20f;

    [SerializeField]
    private float playerViewAngle = 120f; // Field of view angle to detect the angel

    [SerializeField]
    private float beingWatchedCooldown = 0.5f; // Cooldown when no longer being watched

    [SerializeField]
    private LayerMask playerLayers;

    [SerializeField]
    private LayerMask obstacleLayer;

    [SerializeField]
    private bool debugMode = false;

    private bool _beingWatched = false;
    private bool _hasBeenActivated = false;
    private Animator _animator;
    private Vector3 _lastMovementPosition;

    [Header("Sound")]
    [SerializeField]
    private AudioClip stoneMovementSound;

    [SerializeField]
    private AudioClip playerDetectedSound;

    [SerializeField]
    private AudioClip angrySound;

    // Cache components
    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _lastMovementPosition = transform.position;
    }

    protected override void Update()
    {
        base.Update();

        bool currentlyBeingWatched = IsBeingWatched();
        TargetClosestPlayer();

        if (!_hasBeenActivated)
        {
            Player[] nearbyPlayers = PlayerManager
                .Instance.GetPlayersInRange(transform.position, 10f)
                .ToArray();

            if (nearbyPlayers.Length > 0)
            {
                Activate();
            }
        }

        if (currentlyBeingWatched != _beingWatched)
        {
            if (currentlyBeingWatched)
            {
                OnBeingWatched();
            }
            else
            {
                OnNoLongerWatched();
            }

            _beingWatched = currentlyBeingWatched;
        }
    }

    private void Activate()
    {
        _hasBeenActivated = true;

        behaviorGraph.SetVariableValue("MovementState", MovementState.Chasing);
        behaviorGraph.SetVariableValue("ShouldPerformAction", true);

        if (debugMode)
            Debug.Log("Angel activated by proximity without being seen!");
    }

    private void TargetClosestPlayer()
    {
        Player[] players = PlayerManager.Instance.GetAllPlayers().ToArray();
        GameObject closestPlayer = null;
        float closestDistance = Mathf.Infinity;
        foreach (Player player in players)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPlayer = player.gameObject;
            }
        }

        behaviorGraph.SetVariableValue("Target", closestPlayer);
    }

    private bool IsBeingWatched()
    {
        Player[] playersInRange = PlayerManager
            .Instance.GetPlayersInRange(transform.position, playerDetectionDistance)
            .ToArray();

        foreach (Player player in playersInRange)
        {
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera == null)
                continue;

            float distanceToPlayer = Vector3.Distance(
                transform.position,
                playerCamera.transform.position
            );

            Vector3 angelEyePosition = transform.position + Vector3.up * 1.7f;

            Vector3 directionToAngel = (
                angelEyePosition - playerCamera.transform.position
            ).normalized;

            float dotProduct = Vector3.Dot(playerCamera.transform.forward, directionToAngel);

            // SPECIAL CASE: If player is very close (< 3m) and angel is in front (dotProduct > 0)
            if (distanceToPlayer < 3f && dotProduct > 0)
            {
                // At very close range, we use a simpler check - if angel is in front at all
                if (HasLineOfSightToPlayer(playerCamera.transform.position, angelEyePosition))
                {
                    if (debugMode)
                    {
                        Debug.DrawLine(
                            playerCamera.transform.position,
                            angelEyePosition,
                            Color.magenta
                        );
                        Debug.Log($"Angel is being watched at CLOSE RANGE by {player.name}");
                    }
                    return true;
                }
                continue; // Check next player if this one can't see the angel
            }

            // For normal distances, do the angle check
            // Clamp dot product to avoid NaN errors
            dotProduct = Mathf.Clamp(dotProduct, -1f, 1f);

            // Calculate angle between player's view and direction to angel
            float angleToAngel = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;

            // DISTANCE-BASED VIEW ANGLE: Wider at close range, narrower at far range
            float effectiveViewAngle = playerViewAngle;
            if (distanceToPlayer < 10f)
            {
                // Gradually increase view angle as distance decreases
                effectiveViewAngle = Mathf.Lerp(
                    playerViewAngle,
                    150f,
                    1f - (distanceToPlayer / 10f)
                );
            }

            if (debugMode)
            {
                Debug.Log(
                    $"Player: {player.name}, Angle: {angleToAngel}, Threshold: {effectiveViewAngle * 0.5f}, Dot: {dotProduct}"
                );
            }

            if (
                angleToAngel <= effectiveViewAngle * 0.5f
                && HasLineOfSightToPlayer(playerCamera.transform.position, angelEyePosition)
            )
            {
                if (debugMode)
                {
                    Debug.DrawLine(playerCamera.transform.position, angelEyePosition, Color.green);
                    Debug.Log(
                        $"Angel is being watched by {player.name}, angle: {angleToAngel}, distance: {distanceToPlayer}"
                    );
                }
                return true;
            }
            else if (debugMode)
            {
                Debug.DrawLine(playerCamera.transform.position, angelEyePosition, Color.yellow);
                Debug.Log(
                    $"Angel outside view angle for {player.name}: {angleToAngel} > {effectiveViewAngle * 0.5f}"
                );
            }
        }

        // No one is watching
        return false;
    }

    private bool HasLineOfSightToPlayer(Vector3 playerPosition, Vector3 angelPosition)
    {
        Vector3 directionToPlayer = playerPosition - angelPosition;
        float distanceToPlayer = directionToPlayer.magnitude;

        Ray ray = new Ray(angelPosition, directionToPlayer.normalized);

        if (debugMode)
        {
            Debug.DrawRay(angelPosition, directionToPlayer, Color.blue, 0.1f);
        }

        bool hitObstacle = Physics.Raycast(
            ray,
            out RaycastHit obstacleHit,
            distanceToPlayer,
            obstacleLayer
        );

        if (hitObstacle)
        {
            if (debugMode)
            {
                Debug.Log(
                    $"Ray hit obstacle: {obstacleHit.collider.name} at distance {obstacleHit.distance}"
                );
                Debug.DrawLine(angelPosition, obstacleHit.point, Color.red, 0.1f);
            }
            return false;
        }

        // If we get here, there are no obstacles - the player can see the angel
        return true;
    }

    private void OnBeingWatched()
    {
        SetFrozenState(true);

        if (debugMode)
        {
            Debug.Log("Angel is now being watched and frozen!");
        }
    }

    private void OnNoLongerWatched()
    {
        SetFrozenState(false);

        if (debugMode)
        {
            Debug.Log("Angel is no longer being watched, cooldown started");
        }
    }

    private void SetFrozenState(bool isFrozen)
    {
        if (_animator != null)
        {
            _animator.SetBool("IsFrozen", isFrozen);
        }

        behaviorGraph.SetVariableValue("IsFrozen", isFrozen);
        behaviorGraph.SetVariableValue(
            "MovementState",
            isFrozen ? MovementState.Idle : MovementState.Chasing
        );
        if (isFrozen)
        {
            behaviorGraph.SetVariableValue("ShouldPerformAction", false);
        }

        // Play sound if we just moved and are now frozen
        if (isFrozen && Vector3.Distance(transform.position, _lastMovementPosition) > 0.1f)
        {
            AudioManager.Instance.PlaySoundAtPosition(stoneMovementSound, transform.position);
            _lastMovementPosition = transform.position;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionDistance);

        if (debugMode)
        {
            Gizmos.color = Color.blue;
            Vector3 rightDir = Quaternion.Euler(0, playerViewAngle * 0.5f, 0) * Vector3.forward;
            Vector3 leftDir = Quaternion.Euler(0, -playerViewAngle * 0.5f, 0) * Vector3.forward;
            Gizmos.DrawRay(transform.position + Vector3.up, rightDir * playerDetectionDistance);
            Gizmos.DrawRay(transform.position + Vector3.up, leftDir * playerDetectionDistance);
        }
    }
}
