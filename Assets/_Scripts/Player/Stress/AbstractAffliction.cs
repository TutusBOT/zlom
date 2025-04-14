using UnityEngine;

public abstract class AbstractAffliction : IAffliction
{
    protected StressController stressController;
    protected Player player;
    protected bool isActive = false;

    // Implement interface properties
    public abstract StressController.AfflictionType Type { get; }
    public abstract string QuoteText { get; }

    // Constructor
    public AbstractAffliction() { }

    // Initialize with references
    public virtual void Initialize(StressController controller, Player player)
    {
        this.stressController = controller;
        this.player = player;
    }

    // Called when affliction is first activated
    public virtual void OnAfflictionActivated()
    {
        isActive = true;
        Debug.Log($"Affliction {Type} activated");
    }

    // Called when affliction is deactivated
    public virtual void OnAfflictionDeactivated()
    {
        isActive = false;
        Debug.Log($"Affliction {Type} deactivated");
    }

    // Update called by the StressController
    public virtual void Update()
    {
        // Base implementation does nothing
    }

    // Create visual indicator on player
    public virtual GameObject CreateVisualIndicator(Transform parent)
    {
        return null;
    }

    // Helper methods
    protected VoiceChatManager GetVoiceChat()
    {
        return player?.GetVoiceChatManager();
    }

    protected bool IsOwner()
    {
        return stressController?.IsOwner ?? false;
    }
}
