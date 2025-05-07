using System;
using UnityEngine;

public class LightDetector : MonoBehaviour
{
    public bool IsInLight { get; private set; }
    public RoomLightSource CurrentLightSource { get; private set; }

    [SerializeField]
    private float memoryDuration = 0.5f;
    private float lastTimeInLight = -999f;

    public bool WasRecentlyInLight => (Time.time - lastTimeInLight) < memoryDuration;

    private void OnEnable()
    {
        RoomLightSource.OnObjectEnteredLight += HandleEnteredLight;
        RoomLightSource.OnObjectExitedLight += HandleExitedLight;
    }

    private void OnDisable()
    {
        RoomLightSource.OnObjectEnteredLight -= HandleEnteredLight;
        RoomLightSource.OnObjectExitedLight -= HandleExitedLight;
    }

    private void HandleEnteredLight(GameObject obj, RoomLightSource lightSource)
    {
        if (obj != gameObject)
            return;

        IsInLight = true;
        CurrentLightSource = lightSource;
        lastTimeInLight = Time.time;
    }

    private void HandleExitedLight(GameObject obj, RoomLightSource lightSource)
    {
        if (obj != gameObject)
            return;

        if (lightSource != CurrentLightSource)
            return;

        IsInLight = false;
        CurrentLightSource = null;
    }
}
