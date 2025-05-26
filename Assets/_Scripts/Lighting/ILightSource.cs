using System;
using UnityEngine;

public interface ILightSource
{
    string name { get; }
}

public static class LightSourceEvents
{
    public static event Action<GameObject, ILightSource> OnObjectEnteredLight;
    public static event Action<GameObject, ILightSource> OnObjectExitedLight;

    public static void NotifyObjectEnteredLight(GameObject obj, ILightSource lightSource)
    {
        OnObjectEnteredLight?.Invoke(obj, lightSource);
    }

    public static void NotifyObjectExitedLight(GameObject obj, ILightSource lightSource)
    {
        OnObjectExitedLight?.Invoke(obj, lightSource);
    }
}
