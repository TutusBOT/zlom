using System;
using UnityEngine;

public class RoomLightSource : MonoBehaviour
{
    [SerializeField]
    Light pointLight;

    [SerializeField]
    Collider lightCollider;

    public enum LightState
    {
        Off,
        On,
        Destroyed,
    };

    private LightState _lightState = LightState.Off;

    public static event Action<GameObject, RoomLightSource> OnObjectEnteredLight;
    public static event Action<GameObject, RoomLightSource> OnObjectExitedLight;

    private void Start()
    {
        if (pointLight == null)
        {
            Debug.LogError("Point light is not assigned.");
            return;
        }

        if (lightCollider == null)
        {
            Debug.LogError("Light collider is not assigned.");
            return;
        }

        SetLightState(LightState.Off);
    }

    public void SetLightState(LightState state)
    {
        if (_lightState == LightState.Destroyed)
            return;

        _lightState = state;

        switch (_lightState)
        {
            case LightState.Off:
                OnLightOff();
                break;
            case LightState.On:
                OnLightOn();
                break;
            case LightState.Destroyed:
                OnLightDestroyed();
                break;
        }
    }

    private void NotifyObjectsInLight()
    {
        // Find all colliders within the light's collider
        Collider[] collidersInLight = Physics.OverlapBox(
            lightCollider.bounds.center,
            lightCollider.bounds.extents,
            transform.rotation
        );

        foreach (Collider col in collidersInLight)
        {
            if (col.gameObject == gameObject)
                continue; // Skip self

            // Get the proper target (check for LightDetector in hierarchy)
            GameObject targetObject = GetTargetGameObject(col.gameObject);

            // Notify that this object is no longer in light
            OnObjectExitedLight?.Invoke(targetObject, this);
        }
    }

    private void OnLightOff()
    {
        pointLight.enabled = false;
        lightCollider.enabled = false;
        NotifyObjectsInLight();
    }

    private void OnLightOn()
    {
        pointLight.enabled = true;
        lightCollider.enabled = true;
        // Play light on animation
    }

    private void OnLightDestroyed()
    {
        // Play destroyed animation

        pointLight.enabled = false;
        lightCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        GameObject targetObject = GetTargetGameObject(other.gameObject);

        Debug.Log("Light source triggered by: " + targetObject.name);
        OnObjectEnteredLight?.Invoke(targetObject, this);
    }

    private void OnTriggerExit(Collider other)
    {
        GameObject targetObject = GetTargetGameObject(other.gameObject);

        Debug.Log("Light source exited by: " + targetObject.name);
        OnObjectExitedLight?.Invoke(targetObject, this);
    }

    private GameObject GetTargetGameObject(GameObject original)
    {
        // First check if this object has a LightDetector
        if (original.GetComponent<LightDetector>() != null)
            return original;

        // Check if parent has a LightDetector
        if (
            original.transform.parent != null
            && original.transform.parent.GetComponent<LightDetector>() != null
        )
            return original.transform.parent.gameObject;

        // If not, check up the hierarchy for any LightDetector
        Transform current = original.transform.parent;
        while (current != null)
        {
            if (current.GetComponent<LightDetector>() != null)
                return current.gameObject;
            current = current.parent;
        }

        // If no LightDetector found in hierarchy, return the original object
        return original;
    }
}
