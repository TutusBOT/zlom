using UnityEngine;

public class FearfulAffliction : AbstractAffliction
{
    public override StressController.AfflictionType Type => StressController.AfflictionType.Fearful;
    public override string QuoteText => "No. No. No. I can't go there again.";

    public override void Update()
    {
        if (!IsOwner() || !isActive)
            return;
    }

    public override void OnAfflictionDeactivated()
    {
        base.OnAfflictionDeactivated();
    }

    public override GameObject CreateVisualIndicator(Transform parent)
    {
        // Create a visual indicator for other players to see
        // (e.g., particle effects of whispering shadows)
        return null; // Replace with actual implementation
    }
}
