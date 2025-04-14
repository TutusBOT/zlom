using FishNet.Object;
using UnityEngine;

public class Player : NetworkBehaviour
{
    [SerializeField]
    private StressController stressController;

    [SerializeField]
    private VoiceChatManager voiceChatManager;

    [SerializeField]
    private FlashlightController flashlightController;

    [SerializeField]
    private PauseManager pauseManager;

    [SerializeField]
    private CrosshairController crosshairController;

    private void Awake()
    {
        if (stressController == null)
            Debug.LogError("StressController is not assigned in the inspector.");

        if (voiceChatManager == null)
            Debug.LogError("VoiceChatManager is not assigned in the inspector.");

        if (flashlightController == null)
            Debug.LogError("FlashlightController is not assigned in the inspector.");

        if (pauseManager == null)
            Debug.LogError("PauseManager is not assigned in the inspector.");

        if (crosshairController == null)
            Debug.LogError("CrosshairController is not assigned in the inspector.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // Register with player manager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RegisterPlayer(this);
        }
    }

    private void OnDestroy()
    {
        // Unregister from player manager
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(this);
        }
    }

    // Public getters for controllers
    public StressController GetStressController() => stressController;

    public VoiceChatManager GetVoiceChatManager() => voiceChatManager;

    // Utility methods for common player actions
    public bool IsIsolated(float distance)
    {
        return !PlayerManager.Instance.IsAnyPlayerInRange(transform.position, distance, this);
    }
}
