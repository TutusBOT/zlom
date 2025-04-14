using UnityEngine;

public interface IAffliction
{
    StressController.AfflictionType Type { get; }
    string QuoteText { get; }

    void Initialize(StressController controller, Player player);
    void OnAfflictionActivated();
    void OnAfflictionDeactivated();

    void Update();

    GameObject CreateVisualIndicator(Transform parent);
}
