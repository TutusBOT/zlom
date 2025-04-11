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

    // Private variables
    private GameObject heldObject;
    private Rigidbody heldRigidbody;
    private NetworkObject heldNetObj;
    private Camera playerCamera;
    private bool isHolding = false;
    private float pickupDistance;

    // Add this to store the original NetworkTransform settings
    private FishNet.Component.Transforming.NetworkTransform heldNetTransform;
    private bool originalKinematicState;

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
    private bool isObjectTooHeavyToLift = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!IsOwner)
            return;

        playerCamera = Camera.main;
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        // Raycast to find interactable objects
        if (!isHolding && Input.GetMouseButtonDown(0))
        {
            TryPickUp();
        }

        // Handle held object
        if (isHolding)
        {
            MoveHeldObject();

            // Drop on mouse release
            if (Input.GetMouseButtonUp(0))
            {
                DropObject();
            }
        }
    }

    void TryPickUp()
    {
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
        {
            GameObject target = hit.collider.gameObject;

            // Validate tags
            if (!target.CompareTag("Valuable"))
            {
                Debug.Log("This item cannot be picked up - not valuable.");
                return;
            }

            // Get required components
            pickupDistance = Vector3.Distance(playerCamera.transform.position, hit.point);
            Rigidbody rb = target.GetComponent<Rigidbody>();
            NetworkObject netObj = target.GetComponent<NetworkObject>();
            heldNetTransform =
                target.GetComponent<FishNet.Component.Transforming.NetworkTransform>();
            Valuable valuable = target.GetComponent<Valuable>();

            // Check if the object is too heavy to interact with at all
            if (rb != null && rb.mass > absoluteMaxMass)
            {
                Debug.Log($"Object is too heavy to move: {rb.mass}kg");
                return; // Can't interact at all
            }

            // Check if object is too heavy to fully lift (can only drag)
            isObjectTooHeavyToLift = (rb != null && rb.mass > maxLiftableMass);

            // Validate the object can be picked up
            if (rb != null && netObj != null && heldNetTransform != null)
            {
                Debug.Log($"Picking up {target.name}");

                // Store references
                heldObject = target;
                heldRigidbody = rb;
                heldNetObj = netObj;
                originalKinematicState = heldRigidbody.isKinematic;

                // Configure object physics differently based on weight
                if (!isObjectTooHeavyToLift)
                {
                    // Normal pickup - make kinematic for full control
                    SetObjectKinematicStateRpc(heldNetObj.ObjectId, true);
                    heldRigidbody.useGravity = false;
                    heldRigidbody.isKinematic = true;
                }
                else
                {
                    // Heavy object - keep physics active but reduce gravity
                    SetHeavyObjectPhysicsRpc(heldNetObj.ObjectId, true);
                    heldRigidbody.useGravity = true;
                    heldRigidbody.isKinematic = false;
                }

                // Configure for high-frequency updates
                heldNetTransform.SetInterval(1);
                heldRigidbody.interpolation = RigidbodyInterpolation.Interpolate;

                // Mark as holding
                isHolding = true;

                // Notify the valuable it's being picked up
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

    [ObserversRpc]
    private void SetHeavyObjectPhysicsRpc(int objectId, bool isBeingDragged)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Keep non-kinematic for heavy objects
                rb.isKinematic = false;
                rb.useGravity = true;

                // Adjust physics properties to make dragging possible but difficult
                if (isBeingDragged)
                {
                    // Increase drag to make movement difficult
                    rb.linearDamping = 10f;
                    rb.angularDamping = 10f;
                }
                else
                {
                    // Reset drag when released
                    rb.linearDamping = 0.05f;
                    rb.angularDamping = 0.05f;
                }

                Debug.Log(
                    $"Client: Set heavy object {netObj.name} drag properties for dragging: {isBeingDragged}"
                );
            }
        }
    }

    void MoveHeldObject()
    {
        if (heldObject == null || playerCamera == null || !isHolding)
            return;

        // Calculate target position in front of camera/player
        Vector3 targetPosition =
            playerCamera.transform.position + playerCamera.transform.forward * pickupDistance;

        if (!isObjectTooHeavyToLift)
        {
            // NORMAL OBJECT HANDLING - existing code with mass factor
            float massFactor = 1f;
            if (massInfluence > 0 && heldRigidbody != null)
            {
                // Calculate how "heavy" the object feels
                float massReference = 5f;
                massFactor = Mathf.Lerp(
                    1f,
                    minSpeedFactor,
                    Mathf.Clamp01(heldRigidbody.mass / massReference * massInfluence)
                );
            }

            // Direct position control for normal objects
            heldObject.transform.position = Vector3.Lerp(
                heldObject.transform.position,
                targetPosition,
                Time.deltaTime * followSpeed * massFactor
            );

            // Apply rotation for normal objects
            Quaternion targetRotation = Quaternion.Lerp(
                heldObject.transform.rotation,
                playerCamera.transform.rotation,
                Time.deltaTime * rotationSpeed * massFactor
            );
            heldObject.transform.rotation = targetRotation;
        }
        else
        {
            // HEAVY OBJECT HANDLING - apply limited force instead of direct movement
            Vector3 toTarget = targetPosition - heldObject.transform.position;

            // Only allow horizontal movement (dragging on ground) and limit upward pull
            toTarget.y = Mathf.Min(toTarget.y, 0.1f);

            // Apply very limited force
            Vector3 dragForce = toTarget.normalized * heavyDragFactor * heldRigidbody.mass;
            heldRigidbody.AddForce(dragForce, ForceMode.Force);

            // Limit velocity to prevent accumulation of momentum
            if (heldRigidbody.linearVelocity.magnitude > 2f)
            {
                heldRigidbody.linearVelocity = heldRigidbody.linearVelocity.normalized * 2f;
            }
        }

        // Update line renderer (for both normal and heavy objects)
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, holdPoint.position);
            lineRenderer.SetPosition(1, heldObject.transform.position);
        }
    }

    void DropObject()
    {
        if (heldObject == null)
            return;

        if (!isObjectTooHeavyToLift)
        {
            // Normal object - restore kinematic state
            SetObjectKinematicStateRpc(heldNetObj.ObjectId, false);

            // Restore physics state locally
            heldRigidbody.useGravity = true;
            heldRigidbody.isKinematic = originalKinematicState;
        }
        else
        {
            // Heavy object - reset drag properties
            SetHeavyObjectPhysicsRpc(heldNetObj.ObjectId, false);

            // Restore normal physics locally
            heldRigidbody.linearDamping = 0.05f;
            heldRigidbody.angularDamping = 0.05f;
        }

        // Apply small release velocity based on movement
        heldRigidbody.linearVelocity = playerCamera.transform.forward * 1f;

        // Notify the valuable it's been dropped
        Valuable valuable = heldObject.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuable.OnDropped();
        }

        // Reset state variable
        isObjectTooHeavyToLift = false;

        // Clear references
        heldObject = null;
        heldRigidbody = null;
        heldNetObj = null;
        heldNetTransform = null;
        isHolding = false;

        // Hide line renderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, Vector3.zero);
            lineRenderer.SetPosition(1, Vector3.zero);
        }
    }

    // **** NEW RPC TO SYNC KINEMATIC STATE ACROSS ALL CLIENTS ****
    [ObserversRpc]
    private void SetObjectKinematicStateRpc(int objectId, bool isKinematic)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = isKinematic;
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

    // Configure NetworkTransform for local override or normal operation
    private void ConfigureNetworkTransform(bool localControl)
    {
        if (heldNetTransform == null)
            return;

        if (localControl)
        {
            // Increase network update frequency for smooth replication
            heldNetTransform.SetInterval(1); // Highest frequency
        }
        else
        {
            // Reset to default update frequency
            heldNetTransform.SetInterval(20); // Default FishNet value
        }
    }

    [ObserversRpc]
    private void NotifyPickupRpc(int objectId)
    {
        if (IsOwner)
            return;

        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            // Visual effects for pickup could go here
            Debug.Log($"Client observed {gameObject.name} picking up object {objectId}");
        }
    }

    [ObserversRpc]
    private void NotifyDropRpc(int objectId)
    {
        if (IsOwner)
            return;

        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            // Visual effects for drop could go here
            Debug.Log($"Client observed {gameObject.name} dropping object {objectId}");
        }
    }
}
