using System;
using UnityEngine;

public class LightDetector : MonoBehaviour
{
    public bool IsInLight { get; private set; }
    public RoomLightSource CurrentLightSource { get; private set; }

    // Memory feature (optional)
    [SerializeField]
    private float memoryDuration = 0.5f;
    private float lastTimeInLight = -999f;

    public bool WasRecentlyInLight => (Time.time - lastTimeInLight) < memoryDuration;

    private void OnEnable()
    {
        // Subscribe to light events
        RoomLightSource.OnObjectEnteredLight += HandleEnteredLight;
        RoomLightSource.OnObjectExitedLight += HandleExitedLight;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent memory leaks
        RoomLightSource.OnObjectEnteredLight -= HandleEnteredLight;
        RoomLightSource.OnObjectExitedLight -= HandleExitedLight;
    }

    private void HandleEnteredLight(GameObject obj, RoomLightSource lightSource)
    {
        // Only process events for this GameObject
        if (obj != gameObject)
            return;

        IsInLight = true;
        CurrentLightSource = lightSource;
        lastTimeInLight = Time.time;
    }

    private void HandleExitedLight(GameObject obj, RoomLightSource lightSource)
    {
        // Only process events for this GameObject
        if (obj != gameObject)
            return;

        // Only process if this is our current light
        if (lightSource != CurrentLightSource)
            return;

        IsInLight = false;
        CurrentLightSource = null;
    }
}
