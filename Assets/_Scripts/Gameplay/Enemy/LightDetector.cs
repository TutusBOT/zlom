using UnityEngine;

public class LightDetector : MonoBehaviour
{
    public bool IsInLight { get; private set; }
    public ILightSource CurrentLightSource { get; private set; }

    [SerializeField]
    private float memoryDuration = 0.5f;
    private float lastTimeInLight = -999f;

    public bool WasRecentlyInLight => (Time.time - lastTimeInLight) < memoryDuration;

    private void OnEnable()
    {
        // Subscribe to the universal light source events
        LightSourceEvents.OnObjectEnteredLight += HandleEnteredLight;
        LightSourceEvents.OnObjectExitedLight += HandleExitedLight;
    }

    private void OnDisable()
    {
        // Unsubscribe from the universal light source events
        LightSourceEvents.OnObjectEnteredLight -= HandleEnteredLight;
        LightSourceEvents.OnObjectExitedLight -= HandleExitedLight;
    }

    private void HandleEnteredLight(GameObject obj, ILightSource lightSource)
    {
        Debug.Log(
            $"LightDetector: HandleEnteredLight called for {obj.name} with light source {lightSource.name}"
        );

        if (obj != gameObject)
            return;

        IsInLight = true;
        CurrentLightSource = lightSource;
        lastTimeInLight = Time.time;
    }

    private void HandleExitedLight(GameObject obj, ILightSource lightSource)
    {
        if (obj != gameObject)
            return;

        if (lightSource != CurrentLightSource)
            return;

        IsInLight = false;
        CurrentLightSource = null;
    }
}
