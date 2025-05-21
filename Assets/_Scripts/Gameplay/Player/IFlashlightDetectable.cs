using UnityEngine;

public interface IFlashlightDetectable
{
    void OnFlashlightHit(
        FlashlightController flashlight,
        Vector3 hitPoint,
        Vector3 hitNormal,
        float intensityFactor
    );
}
