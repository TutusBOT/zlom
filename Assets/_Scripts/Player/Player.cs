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

    [SerializeField]
    private PlayerController playerController;

    [SerializeField]
    private PlayerChatDisplay playerChatDisplay;

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

        if (playerController == null)
            Debug.LogError("PlayerController is not assigned in the inspector.");

        if (playerChatDisplay == null)
            Debug.LogError("PlayerChatDisplay is not assigned in the inspector.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.RegisterPlayer(this);
        }
    }

    private void OnDestroy()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.UnregisterPlayer(this);
        }
    }

    public StressController GetStressController() => stressController;

    public VoiceChatManager GetVoiceChatManager() => voiceChatManager;

    public FlashlightController GetFlashlightController() => flashlightController;

    public PauseManager GetPauseManager() => pauseManager;

    public CrosshairController GetCrosshairController() => crosshairController;

    public PlayerController GetPlayerController() => playerController;

    public PlayerChatDisplay GetPlayerChatDisplay() => playerChatDisplay;

    public bool IsIsolated(float distance)
    {
        return !PlayerManager.Instance.IsAnyPlayerInRange(transform.position, distance, this);
    }

    public void ToggleControls(bool enable)
    {
        flashlightController.enabled = enable;
        playerController.ToggleControls(enable);
        voiceChatManager.enabled = enable;
    }
}
