using FishNet.Object;
using UnityEngine;

public class PickUpItem : NetworkBehaviour
{
    private Camera playerCamera;
    public float pickupRange = 5f;
    public LayerMask interactableLayer;
    public LineRenderer lineRenderer;
    public float moveForce = 10f;
    public float maxSpringForce = 20f;
    public Transform playerHand;

    private GameObject currentItem;
    private Rigidbody currentRigidbody;
    private SpringJoint springJoint;
    private bool isHoldingItem = false;
    private float itemDistance;
    private Vector3 objectAttachPoint;
    private bool isApplyingForce = false;

    public override void OnStartClient()
    {
        base.OnStartClient();
        enabled = IsOwner;

        if (IsOwner)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                Debug.LogError("PickUpItem: Could not find main camera!");

                playerCamera = GetComponentInChildren<Camera>();

                if (playerCamera == null)
                    Debug.LogError("PickUpItem: No camera found at all!");
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

        itemDistance = Vector3.Distance(playerHand.position, currentItem.transform.position);

        // Tell the valuable it's picked up (this will notify the server via RPC)
        Valuable valuable = currentItem.GetComponent<Valuable>();
        if (valuable != null)
        {
            valuable.OnPickedUp();
        }

        isHoldingItem = true;
        isApplyingForce = true;

        // Find the nearest attachment point on the object
        objectAttachPoint = hit.point;

        // Create SpringJoint locally - for responsiveness & feel only
        springJoint = currentItem.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.anchor = currentItem.transform.InverseTransformPoint(objectAttachPoint);
        springJoint.connectedAnchor = playerHand.position;

        float massFactor = Mathf.Clamp(1 / currentRigidbody.mass, 0.1f, 1f);

        // Adjusted for better feel
        springJoint.spring = maxSpringForce * 0.4f * massFactor;
        springJoint.damper = 15f;
        springJoint.maxDistance = itemDistance * 1.1f;
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
        if (currentRigidbody == null || springJoint == null || !isApplyingForce)
            return;

        // Update local joint for responsive feel
        springJoint.connectedAnchor = playerHand.position;

        // Calculate target position
        Vector3 targetPos =
            playerCamera.transform.position + playerCamera.transform.forward * itemDistance;

        Vector3 forceDirection = targetPos - currentRigidbody.position;

        // SIMPLIFIED: Just use a constant force value without all the distance checks
        float forceMagnitude = moveForce * 0.7f;

        // Apply force directly - no stabilizing forces
        ApplyForceServerRpc(
            currentItem.GetComponent<NetworkObject>().ObjectId,
            forceDirection.normalized,
            forceMagnitude
        );

        // Update line renderer
        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentItem.transform.position);
        }
    }

    [ServerRpc]
    private void ApplyForceServerRpc(int objectId, Vector3 direction, float force)
    {
        NetworkObject netObj;
        if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
        {
            Rigidbody rb = netObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                float serverForceFactor = 0.15f;
                rb.AddForce(direction * (force * serverForceFactor), ForceMode.Force);

                rb.linearVelocity *= 0.97f;
                rb.angularVelocity *= 0.97f;
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

        // Stop applying force when dropped
        isApplyingForce = false;

        CleanupHeldItem();

        // Reset physics properties on drop
        if (currentRigidbody != null)
        {
            ResetPhysicsPropertiesServerRpc(currentItem.GetComponent<NetworkObject>().ObjectId);

            if (isForced && IsOwner)
            {
                ApplyImpulseServerRpc(
                    currentItem.GetComponent<NetworkObject>().ObjectId,
                    Vector3.down * 2f + Random.insideUnitSphere * 2f
                );
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

    private void CleanupHeldItem()
    {
        isHoldingItem = false;

        if (springJoint != null)
        {
            Destroy(springJoint);
            springJoint = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }
}
