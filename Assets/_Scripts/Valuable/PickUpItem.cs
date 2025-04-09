using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class PickUpItem : NetworkBehaviour // Changed to match filename and inherit from NetworkBehaviour
{
    public Camera playerCamera;
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

    public override void OnStartClient()
    {
        base.OnStartClient();
        enabled = IsOwner;
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

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
                return;
        }

        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

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
        currentRigidbody.useGravity = true;

        // Find the nearest attachment point on the object
        objectAttachPoint = hit.point;

        // Create a SpringJoint for smooth movement
        springJoint = currentItem.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;

        // Dynamic attachment points
        springJoint.anchor = currentItem.transform.InverseTransformPoint(objectAttachPoint);
        springJoint.connectedAnchor = playerHand.position;

        float massFactor = Mathf.Clamp(1 / currentRigidbody.mass, 0.1f, 1f);
        springJoint.spring = maxSpringForce * massFactor;
        springJoint.damper = 10f;
        springJoint.maxDistance = itemDistance;

        float massDampingFactor = Mathf.Lerp(3f, 15f, Mathf.Clamp01(currentRigidbody.mass / 10f));
        currentRigidbody.linearDamping = massDampingFactor;
        currentRigidbody.angularDamping = massDampingFactor;
    }

    private void HandleHeldItem()
    {
        if (currentRigidbody == null || springJoint == null)
            return;

        // Dynamically update attachment point on player
        springJoint.connectedAnchor = playerHand.position;

        Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(
            new Vector3(Input.mousePosition.x, Input.mousePosition.y, itemDistance)
        );

        Vector3 forceDirection = mouseWorldPos - currentRigidbody.position;

        float massAdjustedForce = moveForce;
        currentRigidbody.AddForce(forceDirection * massAdjustedForce, ForceMode.Force);

        // Heavier objects need stiffer springs
        float massFactor = Mathf.Clamp(currentRigidbody.mass, 0.1f, 10f);
        springJoint.spring = maxSpringForce * massFactor;
        springJoint.damper = 10f * massFactor;

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentItem.transform.TransformPoint(springJoint.anchor));
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

        if (currentRigidbody != null)
        {
            currentRigidbody.useGravity = true;

            currentRigidbody.linearDamping = 0f;
            currentRigidbody.angularDamping = 0.05f;

            if (isForced)
            {
                currentRigidbody.AddForce(
                    Vector3.down * 2f + Random.insideUnitSphere * 2f,
                    ForceMode.Impulse
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
