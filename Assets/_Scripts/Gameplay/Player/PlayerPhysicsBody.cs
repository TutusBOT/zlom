using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlayerPhysicsBody : NetworkBehaviour
{
    [SerializeField]
    private float pushForce = 1.5f;

    [SerializeField]
    private float massLimit = 150f;

    [SerializeField]
    private float pushCooldown = 0.2f;

    private CharacterController _characterController;
    private Dictionary<Rigidbody, float> _lastPushTimePerObject =
        new Dictionary<Rigidbody, float>();

    public override void OnStartClient()
    {
        base.OnStartClient();

        if (!IsOwner)
            return;

        _characterController = GetComponent<CharacterController>();
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!IsOwner)
            return;

        Rigidbody hitRigidbody = hit.collider.attachedRigidbody;

        if (hitRigidbody == null || hitRigidbody.isKinematic)
            return;

        // Don't push objects below us
        if (hit.moveDirection.y < -0.3f)
            return;

        // Check if object is too heavy
        if (hitRigidbody.mass > massLimit)
            return;

        if (!_lastPushTimePerObject.TryGetValue(hitRigidbody, out float lastPush))
            lastPush = -10f; // Default to a time in the past

        if (Time.time - lastPush < pushCooldown)
            return; // Still in cooldown for this object

        // Record push time
        _lastPushTimePerObject[hitRigidbody] = Time.time;

        // Calculate push direction (horizontal only)
        Vector3 pushDir = hit.moveDirection;
        pushDir.y = 0;
        pushDir.Normalize(); // Important: normalize to get consistent force

        // Apply force at center of mass rather than hit point
        float speedFactor = Mathf.Clamp01(_characterController.velocity.magnitude / 5f);
        float force = pushForce * speedFactor * (1f - hitRigidbody.mass / massLimit);

        hitRigidbody.AddForce(pushDir * force, ForceMode.Impulse);
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        // Remove any destroyed objects from our dictionary
        List<Rigidbody> keysToRemove = new List<Rigidbody>();
        foreach (var key in _lastPushTimePerObject.Keys)
        {
            if (key == null)
                keysToRemove.Add(key);
        }

        foreach (var key in keysToRemove)
            _lastPushTimePerObject.Remove(key);
    }
}
