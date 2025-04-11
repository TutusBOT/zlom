using FishNet.Component.Transforming;
using FishNet.Object;
using UnityEngine;

public class ForceBasedPickUp : NetworkBehaviour
{
    private Camera playerCamera;
    public float pickupRange = 5f;
    public LayerMask interactableLayer;
    public LineRenderer lineRenderer;
    public Transform playerHand;

    // Force-based parameters (tunable)
    [Header("Force Parameters")]
    [Tooltip("Base force applied to move object toward target")]
    public float springForce = 20f;

    [Tooltip("How strongly to counter existing velocity (prevents oscillation)")]
    public float dampingCoefficient = 5f;

    [Tooltip("Multiplier for mass - how strongly heavy objects resist movement")]
    public float massInfluence = 1f;

    [Tooltip("Maximum velocity magnitude allowed")]
    public float maxVelocity = 10f;

    [Tooltip("How quickly force diminishes with distance")]
    [Range(0f, 1f)]
    public float distanceInfluence = 0.1f;

    private GameObject currentItem;
    private Rigidbody currentRigidbody;
    private bool isHoldingItem = false;
    private float itemDistance;
    private Vector3 objectAttachPoint;

    public override void OnStartClient()
    {
        base.OnStartClient();
        enabled = IsOwner;

        if (IsOwner)
        {
            // First try to find main camera
            playerCamera = Camera.main;

            // If no main camera found, try to find any camera in the scene
            if (playerCamera == null)
            {
                Debug.LogWarning(
                    "ForceBasedPickUp: Could not find main camera, searching for alternatives..."
                );

                // Try to find camera in children
                playerCamera = GetComponentInChildren<Camera>();

                // Try to find any camera in the scene
                if (playerCamera == null)
                {
                    Camera[] allCameras = FindObjectsOfType<Camera>();
                    if (allCameras.Length > 0)
                    {
                        playerCamera = allCameras[0];
                        Debug.LogWarning(
                            $"ForceBasedPickUp: Using fallback camera: {playerCamera.name}"
                        );
                    }
                    else
                    {
                        Debug.LogError("ForceBasedPickUp: No cameras found in the scene!");
                    }
                }
            }
        }
    }

    void OnEnable()
    {
        Valuable.OnItemBroke += HandleItemBreak;
    }

    void OnDisable()
    {
        Valuable.OnItemBroke -= HandleItemBreak;
    }

    private void HandleItemBreak(GameObject brokenItem)
    {
        if (isHoldingItem && currentItem == brokenItem)
        {
            CleanupHeldItem();
        }
    }

    void Update()
    {
        if (!IsOwner)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * pickupRange, Color.green);

        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
        {
            if (hit.collider != null && !isHoldingItem)
            {
                if (lineRenderer != null)
                {
                    lineRenderer.SetPosition(0, playerHand.position);
                    lineRenderer.SetPosition(1, hit.collider.transform.position);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPickupObject(hit);
            }
        }

        // Handle held item
        if (isHoldingItem)
        {
            HandleHeldItem();

            // Drop on mouse release
            if (Input.GetMouseButtonUp(0))
            {
                DropObject(false);
            }
        }
        else if (lineRenderer != null)
        {
            // Hide line when not holding or targeting
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    private void TryPickupObject(RaycastHit hit)
    {
        currentItem = hit.collider.gameObject;

        if (!currentItem.CompareTag("Valuable"))
        {
            Debug.Log("This item cannot be picked up - not valuable.");
            return;
        }

        currentRigidbody = currentItem.GetComponent<Rigidbody>();
        if (currentRigidbody == null)
            return;

        Debug.Log($"Trying to pick up {currentItem.name} using force-based approach");

        // Check if object has NetworkObject component
        NetworkObject netObj = currentItem.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"Cannot pick up {currentItem.name} - missing NetworkObject component!");
            return;
        }

        itemDistance = Vector3.Distance(playerHand.position, currentItem.transform.position);
        objectAttachPoint = hit.point;
        isHoldingItem = true;

        // Apply physics properties on server
        if (IsOwner)
    {
        // Disable gravity when picking up
        SetGravityStateServerRpc(netObj.ObjectId, false);
        
        ApplyPhysicsPropertiesServerRpc(netObj.ObjectId);
        NotifyItemMovementRpc(netObj.ObjectId);
    }

        // Tell the valuable it's picked up
        Valuable valuable = currentItem.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuable.OnPickedUp();
        }
    }

    [ObserversRpc]
    private void NotifyItemMovementRpc(int objectId)
    {
        // Only run on non-owner clients
        if (IsOwner)
            return;

        // Try to find the object
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ClientManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            // Force network transform to update more frequently when being moved
            NetworkTransform netTransform = netObj.GetComponent<NetworkTransform>();
            if (netTransform != null)
            {
                netTransform.SetInterval(1); // Highest frequency
            }
        }
    }

    [ServerRpc]
    private void ApplyPhysicsPropertiesServerRpc(int objectId)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float massDampingFactor = Mathf.Lerp(2f, 10f, Mathf.Clamp01(rb.mass / 10f));
                rb.linearDamping = massDampingFactor;
                rb.angularDamping = massDampingFactor;
            }
        }
    }

    private void HandleHeldItem()
    {
        if (currentRigidbody == null || !isHoldingItem)
            return;

        // Calculate target position - where we want the object to be
        Vector3 targetPosition =
            playerCamera.transform.position + playerCamera.transform.forward * itemDistance;

        // Apply force-based movement on the server
        if (IsOwner && currentItem != null)
        {
            ApplyForceToTargetServerRpc(
                currentItem.GetComponent<NetworkObject>().ObjectId,
                targetPosition,
                playerHand.position
            );
        }

        // Update line renderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentItem.transform.position);
        }
    }

    [ServerRpc]
    private void ApplyForceToTargetServerRpc(
        int objectId,
        Vector3 targetPosition,
        Vector3 handPosition
    )
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Calculate spring-like force
                Vector3 toTarget = targetPosition - rb.position;
                float distance = toTarget.magnitude;

                // Spring force is stronger at greater distances (like a real spring)
                float forceMagnitude =
                    springForce * Mathf.Clamp01(distance * distanceInfluence + 0.1f);
                Vector3 springForceVec = toTarget.normalized * forceMagnitude;

                // Calculate mass influence - heavier objects are harder to move
                float massEffect = Mathf.Clamp(1f / (rb.mass * massInfluence), 0.1f, 1f);
                springForceVec *= massEffect;

                // Add damping force to prevent oscillation
                Vector3 dampingForce = -rb.linearVelocity * dampingCoefficient * rb.mass;

                // Apply combined forces
                rb.AddForce(springForceVec + dampingForce, ForceMode.Force);

                // Prevent excessive velocities
                if (rb.linearVelocity.magnitude > maxVelocity)
                {
                    rb.linearVelocity = rb.linearVelocity.normalized * maxVelocity;
                }

                // Always zero out angular velocity for stability
                rb.angularVelocity = Vector3.zero;

                // Point attachment point toward hand - optional visual effect
                if (distance > 0.1f)
                {
                    // Calculate direction from object to hand
                    Vector3 toHand = handPosition - rb.position;

                    // Only rotate if we have a significant direction
                    if (toHand.magnitude > 0.1f)
                    {
                        // Apply a small corrective torque to align object with hand
                        Vector3 desiredUp = toHand.normalized;
                        Vector3 currentUp = rb.transform.up;

                        // Apply a gentle corrective torque
                        Vector3 torqueDirection = Vector3.Cross(currentUp, desiredUp);
                        rb.AddTorque(torqueDirection * 0.5f, ForceMode.Force);
                    }
                }
            }
        }
    }

    public void ForceDrop()
    {
        DropObject(true);
    }

    private void DropObject(bool isForced = false)
    {
        if (currentItem == null)
            return;

        CleanupHeldItem();

        // Reset physics properties on drop
        if (currentRigidbody != null)
        {
            NetworkObject netObj = currentItem.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                SetGravityStateServerRpc(netObj.ObjectId, true);
                ResetPhysicsPropertiesServerRpc(netObj.ObjectId);

                if (isForced && IsOwner)
                {
                    ApplyImpulseServerRpc(
                        netObj.ObjectId,
                        Vector3.down * 2f + Random.insideUnitSphere * 2f
                    );
                }
            }
        }

        Valuable valuable = currentItem.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuable.OnDropped();
        }

        currentRigidbody = null;
        currentItem = null;
    }

    [ServerRpc]
    private void ResetPhysicsPropertiesServerRpc(int objectId)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearDamping = 0.05f;
                rb.angularDamping = 0.05f;
            }
        }
    }

    [ServerRpc]
    private void ApplyImpulseServerRpc(int objectId, Vector3 impulse)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(impulse, ForceMode.Impulse);
            }
        }
    }

    [ServerRpc]
private void SetGravityStateServerRpc(int objectId, bool useGravity)
{
    NetworkObject netObj;
    if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
    {
        Rigidbody rb = netObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.useGravity = useGravity;
            
            // When disabling gravity, also zero out velocity for stability
            if (!useGravity)
            {
                rb.linearVelocity = Vector3.zero;
            }
        }
    }
}

    private void CleanupHeldItem()
    {
        isHoldingItem = false;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }
}
