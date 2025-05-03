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

    [Header("Movement")]
    public float moveSpeed = 3.5f;

    [Header("Sound Detection")]
    [SerializeField]
    private bool canHearSounds = true;

    [SerializeField]
    private float hearingRange = 10f;

    [SerializeField]
    private BehaviorGraphAgent behaviorGraph;

    public override void OnStartServer()
    {
        base.OnStartServer();
        _currentHealth.Value = maxHealth;
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

    protected virtual void Die()
    {
        Despawn();
    }

    protected void Despawn()
    {
        base.NetworkObject.Despawn();
    }

    private void Update()
    {
        if (IsServerInitialized)
        {
            ServerUpdate();
        }
    }

    protected virtual void ServerUpdate() { }
}
