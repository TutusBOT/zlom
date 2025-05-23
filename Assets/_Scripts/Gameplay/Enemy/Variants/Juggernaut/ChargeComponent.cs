using System.Collections;
using FishNet.Object;
using UnityEngine;
using UnityEngine.AI;

public class ChargeComponent : NetworkBehaviour
{
    [Header("Charge Settings")]
    [SerializeField]
    private float chargeSpeed = 10f;

    [SerializeField]
    private float chargeDuration = 5f;

    [SerializeField]
    private float prepareTime = 0.5f;

    [SerializeField]
    private float cooldownTime = 5f;

    [SerializeField]
    private float wallDetectionDistance = 1.5f;

    [SerializeField]
    private float damageRadius = 2f;

    [SerializeField]
    private float playerDamage = 50f;

    [SerializeField]
    private LayerMask wallLayers;

    [SerializeField]
    private LayerMask playerLayers;

    [Header("Effects")]
    [SerializeField]
    private AudioClip prepareSound;

    [SerializeField]
    private AudioClip chargeSound;

    [SerializeField]
    private AudioClip impactSound;

    [SerializeField]
    private GameObject chargeEffectPrefab;

    [SerializeField]
    private GameObject impactEffectPrefab;

    [Header("Animation")]
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private string prepareTriggerParam = "PrepareCharge";

    [SerializeField]
    private string chargeTriggerParam = "Charge";

    [SerializeField]
    private string impactTriggerParam = "ChargeImpact";

    // Internal state
    private NavMeshAgent _agent;
    private Vector3 _chargeDirection;
    private bool _isCharging = false;
    private bool _isPreparing = false;
    private bool _canCharge = true;
    private float _chargeTimer;
    private Transform _target;
    private Rigidbody _rigidbody;
    private Coroutine _chargeCoroutine;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public bool CanCharge()
    {
        return _canCharge && !_isCharging && !_isPreparing;
    }

    public void StartCharge(Transform target)
    {
        if (!CanCharge() || !IsServerInitialized)
            return;

        _target = target;

        // Stop the NavMeshAgent
        if (_agent != null)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }

        // Start the charge coroutine
        if (_chargeCoroutine != null)
            StopCoroutine(_chargeCoroutine);

        _chargeCoroutine = StartCoroutine(ChargeCoroutine());
    }

    public void CancelCharge()
    {
        if (_chargeCoroutine != null)
        {
            StopCoroutine(_chargeCoroutine);
            _chargeCoroutine = null;
        }

        ResetChargeState();
    }

    private IEnumerator ChargeCoroutine()
    {
        // Preparation phase
        _isPreparing = true;
        _canCharge = false;

        // Face the target
        if (_target != null)
        {
            Vector3 dirToTarget = (_target.position - transform.position).normalized;
            dirToTarget.y = 0;
            transform.forward = dirToTarget;
        }

        RpcPlayPrepareEffects();

        yield return new WaitForSeconds(prepareTime);

        // Start the actual charge
        _isPreparing = false;
        _isCharging = true;
        _chargeTimer = chargeDuration;

        // Set the charge direction toward the target
        if (_target != null)
        {
            Vector3 dirToTarget = (_target.position - transform.position).normalized;
            dirToTarget.y = 0;
            _chargeDirection = dirToTarget;
            transform.forward = _chargeDirection;
        }
        else
        {
            _chargeDirection = transform.forward;
        }

        // Trigger charge animation and sound
        RpcPlayChargeEffects();

        // Keep charging until time runs out or we hit something
        while (_chargeTimer > 0 && _isCharging)
        {
            if (
                Physics.Raycast(
                    transform.position + Vector3.up,
                    _chargeDirection,
                    out RaycastHit hit,
                    wallDetectionDistance,
                    wallLayers
                )
            )
            {
                // Hit a wall - stop charging and play impact effect
                RpcPlayImpactEffects();
                _isCharging = false;
                break;
            }

            // Check for players in damage radius
            Collider[] hitColliders = Physics.OverlapSphere(
                transform.position,
                damageRadius,
                playerLayers
            );
            foreach (var col in hitColliders)
            {
                // Damage player
                var player = col.GetComponent<PlayerHealth>();
                if (player != null)
                {
                    player.TakeDamage(playerDamage);
                }
            }

            // Move forward at charge speed
            transform.position += _chargeDirection * chargeSpeed * Time.deltaTime;

            _chargeTimer -= Time.deltaTime;
            yield return null;
        }

        _isCharging = false;

        yield return new WaitForSeconds(cooldownTime);

        ResetChargeState();
    }

    private void ResetChargeState()
    {
        _isCharging = false;
        _isPreparing = false;
        _canCharge = true;

        // Re-enable NavMeshAgent
        if (_agent != null)
        {
            _agent.isStopped = false;
        }
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        // Handle charging movement directly here if not using physics
        if (_isCharging && _rigidbody == null)
        {
            transform.position += _chargeDirection * chargeSpeed * Time.deltaTime;
        }
    }

    private void FixedUpdate()
    {
        // Use physics for movement if we have a rigidbody
        if (_isCharging && _rigidbody != null)
        {
            _rigidbody.linearVelocity = _chargeDirection * chargeSpeed;
        }
        else if (_rigidbody != null && !_isCharging)
        {
            _rigidbody.linearVelocity = Vector3.zero;
        }
    }

    [ObserversRpc]
    private void RpcPlayPrepareEffects()
    {
        if (animator != null)
            animator.SetTrigger(prepareTriggerParam);

        if (prepareSound != null)
            AudioManager.Instance.PlaySound("juggernaut_scream", transform.position);
    }

    [ObserversRpc]
    private void RpcPlayChargeEffects()
    {
        if (animator != null)
            animator.SetTrigger(chargeTriggerParam);

        if (chargeSound != null)
            AudioManager.Instance.PlaySoundAtPosition(chargeSound, transform.position);

        // Spawn charge effect
        if (chargeEffectPrefab != null)
            Instantiate(chargeEffectPrefab, transform.position, transform.rotation, transform);
    }

    [ObserversRpc]
    private void RpcPlayImpactEffects()
    {
        if (animator != null)
            animator.SetTrigger(impactTriggerParam);

        if (impactSound != null)
            AudioManager.Instance.PlaySoundAtPosition(impactSound, transform.position);

        // Spawn impact effect
        if (impactEffectPrefab != null)
            Instantiate(
                impactEffectPrefab,
                transform.position + transform.forward * wallDetectionDistance,
                transform.rotation
            );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, damageRadius);

        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * wallDetectionDistance);
    }
}
