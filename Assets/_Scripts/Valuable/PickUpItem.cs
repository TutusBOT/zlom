using UnityEngine;

public class ObjectPickup : MonoBehaviour
{
    public Camera playerCamera;
    public float pickupRange = 5f;
    public LayerMask interactableLayer;
    public LineRenderer lineRenderer;
    public float moveForce = 10f;
    public float maxSpringForce = 20f;
    public Transform playerHand; // A point where the object should be held (e.g., empty GameObject in front of player)

    private GameObject currentItem;
    private Rigidbody currentRigidbody;
    private SpringJoint springJoint;
    private bool isHoldingItem = false;
    private float itemDistance;
    private Vector3 objectAttachPoint; // Dynamic attachment point on object

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
        // Check if this is our currently held item
        if (isHoldingItem && currentItem == brokenItem)
        {
            // Clean up references
            isHoldingItem = false;
            if (springJoint != null)
                Destroy(springJoint);

            currentRigidbody = null;
            currentItem = null;

            // Reset line renderer
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    void Update()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
        {
            if (hit.collider != null && !isHoldingItem)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, hit.collider.transform.position);
            }

            if (Input.GetMouseButtonDown(0)) // Pick up object
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

                itemDistance = Vector3.Distance(
                    playerHand.position,
                    currentItem.transform.position
                );
                isHoldingItem = true;
                currentRigidbody.useGravity = true;

                // Find the nearest attachment point on the object
                objectAttachPoint = hit.point; // Use the point where the player clicked

                // Create a SpringJoint for smooth movement
                springJoint = currentItem.AddComponent<SpringJoint>();
                springJoint.autoConfigureConnectedAnchor = false;

                // Dynamic attachment points
                springJoint.anchor = currentItem.transform.InverseTransformPoint(objectAttachPoint); // Object attachment point
                springJoint.connectedAnchor = playerHand.position; // Player attachment point

                float massFactor = Mathf.Clamp(1 / currentRigidbody.mass, 0.1f, 1f);
                springJoint.spring = maxSpringForce * massFactor;
                springJoint.damper = 10f;
                springJoint.maxDistance = itemDistance;

                // Increase drag to slow movement
                currentRigidbody.linearDamping = 5f;
                currentRigidbody.angularDamping = 5f;
            }
        }

        if (isHoldingItem)
        {
            // Dynamically update attachment point on player
            springJoint.connectedAnchor = playerHand.position;

            Vector3 mouseWorldPos = playerCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, itemDistance)
            );

            Vector3 forceDirection = (mouseWorldPos - currentRigidbody.position);

            float massAdjustedForce = moveForce;
            currentRigidbody.AddForce(forceDirection * massAdjustedForce, ForceMode.Force);

            if (springJoint != null)
            {
                // Heavier objects need stiffer springs to avoid excessive stretching
                float massFactor = Mathf.Clamp(currentRigidbody.mass, 0.1f, 10f);
                springJoint.spring = maxSpringForce * massFactor;

                // Increase damper for heavier objects to reduce oscillation
                springJoint.damper = 10f * massFactor;
            }

            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentItem.transform.TransformPoint(springJoint.anchor));

            if (Input.GetMouseButtonUp(0)) // Drop object
            {
                isHoldingItem = false;
                Destroy(springJoint);
                currentRigidbody.useGravity = true;

                // Reset drag & stop movement
                currentRigidbody.linearDamping = 0f;
                currentRigidbody.angularDamping = 0.05f;

                currentRigidbody = null;
                currentItem = null;

                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, transform.position);
            }
        }
        else
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }
}
