using FishNet.Object;
using UnityEngine;

public class SmoothPickUp : NetworkBehaviour
{
    public float pickupRange = 5f;
    public LayerMask interactableLayer;
    public LineRenderer lineRenderer;
    public Transform holdPoint;

    [Header("Movement Settings")]
    public float followSpeed = 12f;
    public float rotationSpeed = 5f;

    private GameObject _heldObject;
    private Rigidbody _heldRigidbody;
    private NetworkObject _heldNetObj;
    private Camera _playerCamera;
    private bool _isHolding = false;
    private float _pickupDistance;
    private FishNet.Component.Transforming.NetworkTransform _heldNetTransform;

    [Header("Mass Settings")]
    [Tooltip("How much object mass affects handling (0 = no effect, 1 = full effect)")]
    [Range(0, 1)]
    public float massInfluence = 0.8f;

    [Tooltip("Minimum movement speed multiplier for very heavy objects")]
    [Range(0.01f, 1f)]
    public float minSpeedFactor = 0f;

    [Tooltip("Maximum mass that can be fully lifted (in kg)")]
    public float maxLiftableMass = 10f;

    [Tooltip("Maximum mass that can be dragged at all (in kg)")]
    public float absoluteMaxMass = 50f;

    [Tooltip("Drag factor for objects too heavy to lift")]
    [Range(0.01f, 0.5f)]
    public float heavyDragFactor = 0.05f;
    private bool _isObjectTooHeavyToLift = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            return;

        _playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (!_isHolding && Input.GetMouseButtonDown(0))
        {
            TryPickUp();
        }

        if (_isHolding)
        {
            Move_HeldObject();

            if (Input.GetMouseButtonUp(0))
            {
                DropObject();
            }
        }
    }

    void TryPickUp()
    {
        Ray ray = _playerCamera.ScreenPointToRay(Input.mousePosition);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
        {
            GameObject target = hit.collider.gameObject;

            if (!target.CompareTag("Valuable"))
            {
                Debug.Log("This item cannot be picked up - not valuable.");
                return;
            }

            // Get required components
            _pickupDistance = Vector3.Distance(_playerCamera.transform.position, hit.point);
            Rigidbody rb = target.GetComponent<Rigidbody>();
            NetworkObject netObj = target.GetComponent<NetworkObject>();
            _heldNetTransform =
                target.GetComponent<FishNet.Component.Transforming.NetworkTransform>();
            Valuable valuable = target.GetComponent<Valuable>();

            if (rb != null && rb.mass > absoluteMaxMass)
            {
                Debug.Log($"Object is too heavy to move: {rb.mass}kg");
                return;
            }

            _isObjectTooHeavyToLift = rb != null && rb.mass > maxLiftableMass;

            if (rb != null && netObj != null && _heldNetTransform != null)
            {
                _heldObject = target;
                _heldRigidbody = rb;
                _heldNetObj = netObj;

                if (!_isObjectTooHeavyToLift)
                {
                    SetObjectKinematicStateServerRpc(_heldNetObj.ObjectId, true);
                    _heldRigidbody.useGravity = false;
                }
                else
                {
                    SetHeavyObjectPhysicsServerRpc(_heldNetObj.ObjectId, true);
                    _heldRigidbody.useGravity = true;
                }

                _heldNetTransform.SetInterval(1);
                _heldRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                _isHolding = true;

                if (valuable != null)
                {
                    valuable.OnPickedUp();
                }
            }
            else
            {
                Debug.LogError($"Cannot pick up {target.name} - missing required components.");
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void MoveObjectServerRpc(
        int objectId,
        Vector3 targetPosition,
        Quaternion targetRotation,
        bool isLightObject
    )
    {
        // Server-side implementation
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (isLightObject)
                {
                    // Light object - direct velocity control
                    Vector3 moveDirection = targetPosition - netObj.transform.position;
                    float distance = moveDirection.magnitude;

                    // Calculate velocity based on distance (server implementation)
                    Vector3 targetVelocity = moveDirection.normalized * distance * followSpeed;

                    // Apply velocity with smoothing
                    rb.linearVelocity = Vector3.Lerp(
                        rb.linearVelocity,
                        targetVelocity,
                        Time.deltaTime * 10f
                    );

                    // Prevent excessive speeds
                    if (rb.linearVelocity.magnitude > 10f)
                    {
                        rb.linearVelocity = rb.linearVelocity.normalized * 10f;
                    }

                    // Apply rotation via torque or direct rotation
                    netObj.transform.rotation = Quaternion.Slerp(
                        netObj.transform.rotation,
                        targetRotation,
                        Time.deltaTime * rotationSpeed
                    );
                }
                else
                {
                    // Heavy object - apply limited force
                    Vector3 toTarget = targetPosition - netObj.transform.position;

                    // Only allow horizontal movement (dragging on ground) and limit upward pull
                    toTarget.y = Mathf.Min(toTarget.y, 0.1f);

                    // Apply force
                    Vector3 dragForce = toTarget.normalized * heavyDragFactor * rb.mass;
                    rb.AddForce(dragForce, ForceMode.Force);

                    // Limit velocity
                    if (rb.linearVelocity.magnitude > 2f)
                    {
                        rb.linearVelocity = rb.linearVelocity.normalized * 2f;
                    }
                }
            }
        }
    }

    [ObserversRpc]
    private void SetHeavyObjectPhysicsRpc(int objectId, bool isBeingDragged)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;

                if (isBeingDragged)
                {
                    rb.linearDamping = 10f;
                    rb.angularDamping = 10f;
                }
                else
                {
                    rb.linearDamping = 0.05f;
                    rb.angularDamping = 0.05f;
                }

                Debug.Log(
                    $"Client: Set heavy object {netObj.name} drag properties for dragging: {isBeingDragged}"
                );
            }
        }
    }

    private Vector3 targetMovementPosition;
    private float lastUpdateTime = 0f;
    private float updateInterval = 0.05f;

    // df
    void Move_HeldObject()
    {
        if (_heldObject == null || _playerCamera == null || !_isHolding)
            return;

        // Calculate target position in front of camera/player
        targetMovementPosition =
            _playerCamera.transform.position + _playerCamera.transform.forward * _pickupDistance;
        Vector3 targetPosition =
            _playerCamera.transform.position + _playerCamera.transform.forward * _pickupDistance;

        if (!_isObjectTooHeavyToLift)
        {
            float massFactor = 1f;
            if (massInfluence > 0 && _heldRigidbody != null)
            {
                float massReference = 5f;
                massFactor = Mathf.Lerp(
                    1f,
                    minSpeedFactor,
                    Mathf.Clamp01(_heldRigidbody.mass / massReference * massInfluence)
                );
            }

            Vector3 moveDirection = targetPosition - _heldObject.transform.position;
            float distance = moveDirection.magnitude;

            // Calculate velocity based on distance
            // The further away, the faster it tries to catch up
            Vector3 targetVelocity = moveDirection.normalized * distance * followSpeed * massFactor;

            // Apply velocity with smoothing
            _heldRigidbody.linearVelocity = Vector3.Lerp(
                _heldRigidbody.linearVelocity,
                targetVelocity,
                Time.deltaTime * 10f
            );

            // Prevent excessive speeds
            if (_heldRigidbody.linearVelocity.magnitude > 10f)
            {
                _heldRigidbody.linearVelocity = _heldRigidbody.linearVelocity.normalized * 10f;
            }

            // Zero out angular velocity to prevent spinning
            _heldRigidbody.angularVelocity = Vector3.zero;

            // Apply rotation via torque or direct rotation depending on your needs
            Quaternion targetRotation = Quaternion.Slerp(
                _heldObject.transform.rotation,
                _playerCamera.transform.rotation,
                Time.deltaTime * rotationSpeed * massFactor
            );
            _heldObject.transform.rotation = targetRotation;
        }
        else
        {
            // HEAVY OBJECT HANDLING - apply limited force instead of direct movement
            Vector3 toTarget = targetPosition - _heldObject.transform.position;

            // Only allow horizontal movement (dragging on ground) and limit upward pull
            toTarget.y = Mathf.Min(toTarget.y, 0.1f);

            // Apply very limited force
            Vector3 dragForce = toTarget.normalized * heavyDragFactor * _heldRigidbody.mass;
            _heldRigidbody.AddForce(dragForce, ForceMode.Force);

            // Limit velocity to prevent accumulation of momentum
            if (_heldRigidbody.linearVelocity.magnitude > 2f)
            {
                _heldRigidbody.linearVelocity = _heldRigidbody.linearVelocity.normalized * 2f;
            }
        }

        if (Time.time > lastUpdateTime + updateInterval)
        {
            // Send intent to server
            MoveObjectServerRpc(
                _heldNetObj.ObjectId,
                targetMovementPosition,
                _playerCamera.transform.rotation,
                !_isObjectTooHeavyToLift
            );

            lastUpdateTime = Time.time;
        }

        // Update line renderer (for both normal and heavy objects)
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, holdPoint.position);
            lineRenderer.SetPosition(1, _heldObject.transform.position);
        }
    }

    void DropObject()
    {
        if (_heldObject == null)
            return;

        if (!_isObjectTooHeavyToLift)
        {
            SetObjectKinematicStateServerRpc(_heldNetObj.ObjectId, false);
            _heldRigidbody.useGravity = true;
        }
        else
        {
            SetHeavyObjectPhysicsServerRpc(_heldNetObj.ObjectId, false);
            _heldRigidbody.linearDamping = 0.05f;
            _heldRigidbody.angularDamping = 0.05f;
        }

        // Apply small release velocity based on movement
        _heldRigidbody.linearVelocity = _playerCamera.transform.forward * 1f;

        // Notify the valuable it's been dropped
        Valuable valuable = _heldObject.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuable.OnDropped();
        }

        _isObjectTooHeavyToLift = false;
        _isHolding = false;

        _heldObject = null;
        _heldRigidbody = null;
        _heldNetObj = null;
        _heldNetTransform = null;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetObjectKinematicStateServerRpc(int objectId, bool isKinematic)
    {
        // Server-side implementation
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Apply changes on server
                rb.useGravity = !isKinematic;
                if (isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                // Then broadcast to all clients
                SetObjectKinematicStateRpc(objectId, isKinematic);
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHeavyObjectPhysicsServerRpc(int objectId, bool isBeingDragged)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = true;
                if (isBeingDragged)
                {
                    rb.linearDamping = 10f;
                    rb.angularDamping = 10f;
                }
                else
                {
                    rb.linearDamping = 0.05f;
                    rb.angularDamping = 0.05f;
                }

                SetHeavyObjectPhysicsRpc(objectId, isBeingDragged);
            }
        }
    }

    [ObserversRpc]
    private void SetObjectKinematicStateRpc(int objectId, bool isKinematic)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = !isKinematic;

                if (isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                Debug.Log($"Client: Set object {netObj.name} kinematic to {isKinematic}");
            }
        }
    }
}
