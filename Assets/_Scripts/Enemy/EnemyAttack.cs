using UnityEngine;
using UnityEngine.AI;
using FishNet.Object;

public class EnemyAttack : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackCooldown = 1.5f;
    [SerializeField] private float damage = 20f;

    [Header("Detectors")]
    [SerializeField] private LineOfSightDetector lineOfSightDetector;
    [SerializeField] private RangeDetector rangeDetector;

    private NavMeshAgent agent;
    private float attackTimer;
    private GameObject targetPlayer;
    private float timeSinceLastSeen;
    private float forgetTargetTime = 2f;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        attackTimer += Time.deltaTime;

        if (targetPlayer == null)
        {
            FindTarget();
        }
        else
        {
            if (CanSeeTarget(targetPlayer))
            {
                timeSinceLastSeen = 0f;
            }
            else
            {
                timeSinceLastSeen += Time.deltaTime;
                if (timeSinceLastSeen >= forgetTargetTime)
                {
                    targetPlayer = null;
                    return;
                }
            }

            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

            if (distance <= attackRange)
            {
                if (agent.hasPath)
                    agent.ResetPath();

                TryAttack();
            }
            else
            {
                agent.SetDestination(targetPlayer.transform.position);
            }
        }
    }

    private void FindTarget()
    {
        GameObject player = null;

        if (lineOfSightDetector != null)
        {
            player = lineOfSightDetector.PerformDetection();
        }

        if (player == null && rangeDetector != null)
        {
            player = rangeDetector.UpdateDetector();
        }

        if (player != null)
        {
            targetPlayer = player;
            timeSinceLastSeen = 0f;
        }
    }

    private bool CanSeeTarget(GameObject target)
    {
        if (lineOfSightDetector == null || target == null)
            return false;

        GameObject seenPlayer = lineOfSightDetector.PerformDetection();
        return seenPlayer == target;
    }

    private void TryAttack()
    {
        if (attackTimer < attackCooldown)
            return;

        if (targetPlayer != null)
        {
            PlayerHealth playerHealth = targetPlayer.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                if (!playerHealth.IsDead)
                {
                    playerHealth.TakeDamage(damage, gameObject);
                }
                else
                {
                    targetPlayer = null;
                }
                attackTimer = 0f;
            }
        }
    }
}
