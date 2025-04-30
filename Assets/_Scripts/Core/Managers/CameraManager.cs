using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private static CameraManager _instance;
    public static CameraManager Instance => _instance;

    public Camera PlayerCamera { get; private set; }

    public event Action<Camera> OnPlayerCameraRegistered;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    public void RegisterPlayerCamera(Camera camera)
    {
        PlayerCamera = camera;
        OnPlayerCameraRegistered?.Invoke(camera);
    }
}
