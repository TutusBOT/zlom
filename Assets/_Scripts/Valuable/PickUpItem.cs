using System.Collections.Generic;
using FishNet.Component.Transforming;
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
    private Dictionary<int, GameObject> serverSpringAnchors = new Dictionary<int, GameObject>();

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

        // Add debug logging
        Debug.Log($"Trying to pick up {currentItem.name}");

        // Check if object has NetworkObject component
        NetworkObject netObj = currentItem.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError($"Cannot pick up {currentItem.name} - missing NetworkObject component!");
            return;
        }

        itemDistance = Vector3.Distance(playerHand.position, currentItem.transform.position);

        // Set flags first
        isHoldingItem = true;
        isApplyingForce = true;

        // Find the nearest attachment point on the object
        objectAttachPoint = hit.point;

        // Create SpringJoint locally
        springJoint = currentItem.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.anchor = currentItem.transform.InverseTransformPoint(objectAttachPoint);
        springJoint.connectedAnchor = playerHand.position;

        float massFactor = Mathf.Clamp(1 / currentRigidbody.mass, 0.1f, 1f);
        springJoint.spring = maxSpringForce * 0.4f * massFactor;
        springJoint.damper = 15f;
        springJoint.maxDistance = itemDistance * 1.1f;

        // Create server SpringJoint BEFORE notifying the Valuable
        if (IsOwner)
        {
            // Create server-side spring joint first
            CreateServerSpringJointRpc(
                netObj.ObjectId,
                currentItem.transform.InverseTransformPoint(objectAttachPoint),
                playerHand.position,
                springJoint.spring,
                springJoint.damper,
                springJoint.maxDistance
            );

            // Then apply physics properties
            ApplyPhysicsPropertiesServerRpc(netObj.ObjectId);

            // Then notify clients about movement frequency
            NotifyItemMovementRpc(netObj.ObjectId);
        }

        // Tell the valuable it's picked up LAST (to avoid state conflicts)
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

[ServerRpc]
private void CreateServerSpringJointRpc(
    int objectId,
    Vector3 localAnchor,
    Vector3 connectedAnchor,
    float spring,
    float damper,
    float maxDistance
)
{
    Debug.Log($"SERVER: Creating SpringJoint for object {objectId}");
    
    NetworkObject netObj;
    if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
    {
        // First clean up any existing springs and anchors
        CleanupServerSpringJoint(objectId, netObj.gameObject);
        
        // Create the server-side SpringJoint
        SpringJoint serverSpring = netObj.gameObject.AddComponent<SpringJoint>();
        serverSpring.autoConfigureConnectedAnchor = false;
        serverSpring.anchor = localAnchor;
        serverSpring.connectedAnchor = connectedAnchor;
        serverSpring.spring = spring;
        serverSpring.damper = damper;
        serverSpring.maxDistance = maxDistance;
        
        // Create and track the anchor object
        GameObject anchorObject = new GameObject($"SpringAnchor_{objectId}");
        anchorObject.transform.position = connectedAnchor;
        
        Rigidbody anchorRb = anchorObject.AddComponent<Rigidbody>();
        anchorRb.isKinematic = true; // Won't move by physics
        
        // Connect and track
        serverSpring.connectedBody = anchorRb;
        serverSpringAnchors[objectId] = anchorObject;
        
        Debug.Log($"SERVER: Successfully created SpringJoint with strength {spring}");
    }
    else
    {
        Debug.LogError($"SERVER: Failed to find NetworkObject with ID {objectId}");
    }
}

// Helper method to clean up existing springs and anchors
private void CleanupServerSpringJoint(int objectId, GameObject gameObj)
{
    // Clean up existing spring joint
    SpringJoint[] joints = gameObj.GetComponents<SpringJoint>();
    foreach (SpringJoint joint in joints)
    {
        Destroy(joint);
    }
    
    // Clean up existing anchor if any
    if (serverSpringAnchors.ContainsKey(objectId))
    {
        GameObject oldAnchor = serverSpringAnchors[objectId];
        if (oldAnchor != null)
        {
            Destroy(oldAnchor);
        }
        serverSpringAnchors.Remove(objectId);
    }
}

[ServerRpc]
private void UpdateServerSpringAnchorRpc(int objectId, Vector3 newAnchorPos) 
{
    // Simply update the anchor object's position directly
    if (serverSpringAnchors.TryGetValue(objectId, out GameObject anchorObject))
    {
        if (anchorObject != null)
        {
            anchorObject.transform.position = newAnchorPos;
        }
        else
        {
            Debug.LogError($"SERVER: Spring anchor object for {objectId} is null!");
            serverSpringAnchors.Remove(objectId); // Clean up dictionary
        }
    }
    else
    {
        Debug.LogError($"SERVER: No spring anchor found for object {objectId}");
    }
}

[ServerRpc]
private void DestroyServerSpringJointRpc(int objectId)
{
    NetworkObject netObj;
    if (FishNet.InstanceFinder.ServerManager.Objects.Spawned.TryGetValue(objectId, out netObj))
    {
        CleanupServerSpringJoint(objectId, netObj.gameObject);
    }
}

    private void HandleHeldItem()
    {
        if (currentRigidbody == null || springJoint == null || !isApplyingForce)
            return;

        // Update local joint for responsive feel
        springJoint.connectedAnchor = playerHand.position;

        // Update the server's spring joint anchor - this is key to prevent orbiting
        if (IsOwner && currentItem != null)
        {
            UpdateServerSpringAnchorRpc(
                currentItem.GetComponent<NetworkObject>().ObjectId,
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
            // Remove server's spring joint when dropping
            DestroyServerSpringJointRpc(currentItem.GetComponent<NetworkObject>().ObjectId);

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
