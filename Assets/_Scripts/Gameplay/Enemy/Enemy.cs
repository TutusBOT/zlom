using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class Enemy : NetworkBehaviour
{
    [Header("Enemy Settings")]
    [SerializeField]
    private float maxHealth = 100f;

    private readonly SyncVar<float> _currentHealth = new();

    [Header("Movement")]
    public float moveSpeed = 3.5f;

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
