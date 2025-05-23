using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.Behavior;
using UnityEngine;

public class Enemy : NetworkBehaviour, IHear
{
    [Header("Enemy Settings")]
    [SerializeField]
    private float maxHealth = 100f;

    private readonly SyncVar<float> _currentHealth = new();
    protected readonly SyncVar<bool> isAlive = new(true);
    public float respawnTime = 30f;

    [SerializeField]
    protected AudioClip deathSound;

    [Header("Movement")]
    [Tooltip("Regular patrol/walking speed")]
    [SerializeField]
    private float moveSpeed = 3f;

    [Tooltip("Chase/running speed")]
    [SerializeField]
    private float runSpeed = 5f;
    public List<GameObject> waypoints = new List<GameObject>();

    [Header("Sound Detection")]
    [SerializeField]
    private bool canHearSounds = true;

    [SerializeField]
    private float hearingRange = 10f;

    [SerializeField]
    protected BehaviorGraphAgent behaviorGraph;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentHealth.Value = maxHealth;

        behaviorGraph.SetVariableValue("Waypoints", waypoints);

        ChaseComponent chaseComp = GetComponent<ChaseComponent>();
        if (chaseComp != null)
            chaseComp.Initialize(moveSpeed, runSpeed);

        PatrolComponent patrolComp = GetComponent<PatrolComponent>();
        if (patrolComp != null)
            patrolComp.Initialize(moveSpeed);
    }

    public virtual void TakeDamage(float amount)
    {
        _currentHealth.Value -= amount;
        if (_currentHealth.Value <= 0)
        {
            Die();
        }
    }

    public void RespondToSound(Sound sound)
    {
        if (!canHearSounds)
            return;

        if (Vector3.Distance(transform.position, sound.pos) > hearingRange)
            return;

        behaviorGraph.SetVariableValue("HasHeardSound", true);
        behaviorGraph.SetVariableValue("SoundPosition", sound.pos);
    }

    [ServerRpc(RequireOwnership = false)]
    public virtual void Die()
    {
        if (!isAlive.Value || !IsServerInitialized)
            return;

        isAlive.Value = false;

        ResetBehaviorGraph();
        RpcDie();
        HandleDeathEvents();
        EnemiesManager.Instance.RegisterEnemyDeath(this);
    }

    [ObserversRpc]
    protected virtual void RpcDie()
    {
        SetComponentsEnabled(false);
        PlayDeathEffects();
    }

    protected virtual void PlayDeathEffects()
    {
        if (deathSound != null)
        {
            AudioManager.Instance.PlaySoundAtPosition(deathSound, transform.position);
        }
    }

    protected virtual void HandleDeathEvents()
    {
        // Base implementation empty - override in specific enemy types
    }

    protected virtual void ResetBehaviorGraph()
    {
        behaviorGraph.SetVariableValue("HasHeardSound", false);
        behaviorGraph.SetVariableValue("SoundPosition", Vector3.zero);
        behaviorGraph.SetVariableValue("ShouldPerformAction", false);
    }

    public virtual void Respawn()
    {
        isAlive.Value = true;
        _currentHealth.Value = maxHealth;
        SetComponentsEnabled(true);
    }

    [ObserversRpc]
    private void SetComponentsEnabled(bool enabled)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = enabled;
        }

        var aiComponents = GetComponents<MonoBehaviour>()
            .Where(x => x.GetType().Name.Contains("AI") || x.GetType().Name.Contains("Controller"));

        foreach (var comp in aiComponents)
            comp.enabled = enabled;

        // Handle colliders
        foreach (var collider in GetComponentsInChildren<Collider>())
            collider.enabled = enabled;

        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
            agent.enabled = enabled;

        var behaviorGraph = GetComponent<BehaviorGraphAgent>();
        if (behaviorGraph != null)
            behaviorGraph.enabled = enabled;
    }

    protected virtual void Update()
    {
        if (IsServerInitialized)
        {
            ServerUpdate();
        }
    }

    protected virtual void ServerUpdate() { }
}
