using System;
using FishNet.Object;
using UnityEngine;

public class Item : NetworkBehaviour, IPickable
{
    [Header("Item Properties")]
    [SerializeField]
    protected string itemName = "Item";

    [SerializeField]
    protected string description = "An interactive item";

    [SerializeField]
    protected Sprite itemIcon;

    [SerializeField]
    protected float interactionCooldown = 0.5f;

    [SerializeField]
    protected bool destroyAfterUse = false;

    [Header("Feedback")]
    [SerializeField]
    protected AudioClip useSound;

    [SerializeField]
    protected GameObject useEffectPrefab;

    // State
    protected Player player;
    protected bool isBeingHeld = true;
    protected bool isOnCooldown = false;
    protected float cooldownTimer = 0f;

    // Events
    public event Action<Item> OnUse;

    // Properties
    public string ItemName => itemName;
    public string Description => description;
    public Sprite Icon => itemIcon;
    public bool IsBeingHeld
    {
        get => isBeingHeld;
        set => isBeingHeld = value;
    }

    private void Update()
    {
        if (!IsServerInitialized)
            return;

        if (isOnCooldown)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownTimer <= 0)
            {
                isOnCooldown = false;
            }
        }

        if (!isOnCooldown && isBeingHeld && Input.GetKeyDown(KeyCode.E))
        {
            UseItem();
        }
    }

    public virtual void UseItem()
    {
        if (isOnCooldown || !isBeingHeld)
            return;

        isOnCooldown = true;
        cooldownTimer = interactionCooldown;

        ExecuteItemAction();
        PlayItemFeedback();

        OnUse?.Invoke(this);
    }

    public void OnPickedUp(Player player)
    {
        this.player = player;
        isBeingHeld = true;
    }

    public void OnDropped()
    {
        isBeingHeld = false;
    }

    public bool CanBePickedUp()
    {
        return true;
    }

    public GameObject GetGameObject()
    {
        return gameObject;
    }

    protected virtual void ExecuteItemAction()
    {
        Debug.Log($"Item '{itemName}' was used!");
        return;
    }

    protected void DespawnItem()
    {
        IPickable.RaiseItemDestroyed(gameObject);
        Despawn();
    }

    protected virtual void PlayItemFeedback()
    {
        if (useSound != null)
        {
            AudioSource.PlayClipAtPoint(useSound, transform.position);
        }

        if (useEffectPrefab != null)
        {
            GameObject effect = Instantiate(
                useEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            Destroy(effect, 2.0f);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ServerUseItem()
    {
        UseItem();
        ClientRpcOnItemUsed();
    }

    [ObserversRpc]
    private void ClientRpcOnItemUsed()
    {
        PlayItemFeedback();
    }
}
