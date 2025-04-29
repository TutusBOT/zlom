using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private RoomController targetRoom;

    [Header("Visual Feedback")]
    [SerializeField]
    private GameObject onVisual;

    [SerializeField]
    private GameObject offVisual;

    [SerializeField]
    private GameObject disabledVisual;

    [SerializeField]
    private bool debug;

    private void Start()
    {
        if (targetRoom == null)
        {
            throw new System.Exception(
                "LightSwitch: Target room is not assigned. Please assign a target room."
            );
        }

        UpdateVisuals();
    }

    // Called by player interaction system
    public void Interact()
    {
        if (targetRoom == null || RoomLightingManager.Instance == null)
        {
            Debug.LogWarning(
                $"Cannot interact - targetRoom: {targetRoom != null}, RoomLightingManager: {RoomLightingManager.Instance != null}"
            );
            return;
        }

        if (debug)
            Debug.Log(
                $"Client attempting to toggle light switch for room {targetRoom.name}, current state: {targetRoom.IsPowered}"
            );

        // Toggle power
        RoomLightingManager.Instance.SetRoomPowerServerRpc(
            targetRoom.transform.position,
            !targetRoom.IsPowered
        );

        UpdateVisuals();
    }

    // Update visual state
    public void UpdateVisuals()
    {
        if (targetRoom == null)
            return;

        bool isPowered = targetRoom.IsPowered;
        bool canBePowered = targetRoom.CanBePowered;

        if (onVisual != null)
            onVisual.SetActive(isPowered && canBePowered);

        if (offVisual != null)
            offVisual.SetActive(!isPowered && canBePowered);

        if (disabledVisual != null)
            disabledVisual.SetActive(!canBePowered);
    }

    public void SetTargetRoom(RoomController room)
    {
        targetRoom = room;
        UpdateVisuals();
    }
}
