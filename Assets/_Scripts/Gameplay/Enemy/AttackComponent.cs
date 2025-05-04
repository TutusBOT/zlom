using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class AttackComponent : NetworkBehaviour
{
    [Header("Attack Settings")]
    [Tooltip("Damage amount to deal")]
    public float damage = 10f;

    [Tooltip("Cooldown time between attacks (seconds)")]
    public float cooldownDuration = 2f;

    [Tooltip("Range at which attack can hit target")]
    public float attackRange = 2f;

    [Header("Animation")]
    [Tooltip("Animation parameter name for attack")]
    public string attackAnimParam = "IsAttacking";

    [Tooltip("Name of the attack state in the animator")]
    public string attackStateName = "Attack";

    private Animator _animator;
    private float _attackTimer;
    private bool _hasAttacked;
    private GameObject _currentTarget;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _attackTimer = 0f;
        _hasAttacked = false;
    }

    private void Update()
    {
        // Update cooldown timer
        if (_attackTimer > 0)
            _attackTimer -= Time.deltaTime;
    }

    /// <summary>
    /// Attempt to attack a specific target
    /// </summary>
    /// <param name="target">The GameObject to attack</param>
    /// <returns>True if attack was initiated, false otherwise</returns>
    public bool TryAttack(GameObject target)
    {
        if (!IsServerInitialized)
            return false;

        if (target == null)
            return false;

        _currentTarget = target;

        if (_attackTimer > 0)
            return false;

        float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        if (distanceToTarget > attackRange)
            return false;

        StartAttack();
        return true;
    }

    /// <summary>
    /// Start the attack sequence
    /// </summary>
    private void StartAttack()
    {
        if (_animator != null)
            _animator.SetBool(attackAnimParam, true);

        if (_currentTarget != null)
        {
            PlayerHealth playerHealth = _currentTarget.GetComponent<PlayerHealth>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                playerHealth.TakeDamage(damage, gameObject);
            }
        }

        _attackTimer = cooldownDuration;
        _hasAttacked = true;
    }

    /// <summary>
    /// Stop the attack animation
    /// </summary>
    public void StopAttack()
    {
        if (_animator != null)
            _animator.SetBool(attackAnimParam, false);
    }

    /// <summary>
    /// Check if attack animation is complete
    /// </summary>
    public bool IsAttackAnimationComplete()
    {
        if (_animator == null)
            return true;

        AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
        return !stateInfo.IsName(attackStateName) || stateInfo.normalizedTime >= 0.9f;
    }

    /// <summary>
    /// Get the current cooldown time
    /// </summary>
    public float GetCooldownTime()
    {
        return _attackTimer;
    }

    /// <summary>
    /// Check if the attack is on cooldown
    /// </summary>
    public bool IsOnCooldown()
    {
        return _attackTimer > 0;
    }

    /// <summary>
    /// Has an attack been executed and is still in progress
    /// </summary>
    public bool HasAttacked()
    {
        return _hasAttacked;
    }

    /// <summary>
    /// Reset attack state
    /// </summary>
    public void ResetAttackState()
    {
        _hasAttacked = false;
    }
}
