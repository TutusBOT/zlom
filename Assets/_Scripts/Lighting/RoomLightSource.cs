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

    private void OnLightOff()
    {
        pointLight.enabled = false;
        lightCollider.enabled = false;
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
        Debug.Log("Light source triggered by: " + other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Light source exited by: " + other.name);
    }
}
