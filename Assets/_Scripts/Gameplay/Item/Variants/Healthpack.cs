using FishNet.Object;
using UnityEngine;

public class Healthpack : Item
{
    [Header("Healing Settings")]
    [SerializeField]
    private float healAmount = 25f;

    [SerializeField]
    private ParticleSystem healEffect;

    [SerializeField]
    private string healSoundId = "heal";

    protected override void ExecuteItemAction()
    {
        if (player == null)
        {
            Debug.LogWarning("Healthpack used but no player reference found!");
            return;
        }

        PlayerHealth playerHealth = player.GetPlayerHealth();
        HealPlayerServerRpc(playerHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    private void HealPlayerServerRpc(PlayerHealth playerHealth)
    {
        if (playerHealth == null)
        {
            Debug.LogWarning("Healthpack used but no Health component found on player object!");
            return;
        }

        playerHealth.Heal(healAmount);

        ClientRpcOnHeal(true);

        if (destroyAfterUse)
        {
            DespawnItem();
        }
    }

    [ObserversRpc]
    private void ClientRpcOnHeal(bool wasHealed)
    {
        if (wasHealed)
        {
            // Play additional heal effects
            if (healEffect != null)
            {
                healEffect.Play();
            }

            // Play additional heal sound
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(healSoundId, transform.position);
            }
        }
        else
        {
            // Play "already at full health" feedback
            Debug.Log("Player is already at full health!");
        }
    }
}
