using FishNet.Object;
using UnityEngine;

public class NetworkedObjectPickup : NetworkBehaviour
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
    private Vector3 initialPickupOffset;

    public override void OnStartClient()
    {
        base.OnStartClient();

        playerCamera = GetComponentInChildren<Camera>();
    }

    void Update()
    {
        if (!IsOwner)
            return;

        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
        {
            if (hit.collider != null && !isHoldingItem)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, hit.collider.transform.position);
            }

            if (Input.GetMouseButtonDown(0))
            {
                NetworkObject netObj = hit.collider.GetComponentInParent<NetworkObject>();
                if (netObj != null)
                {
                    initialPickupOffset =
                        Quaternion.Inverse(playerCamera.transform.rotation)
                        * (hit.point - playerCamera.transform.position);
                    RequestPickupServerRpc(netObj, hit.point);
                }
            }
        }

        if (isHoldingItem && springJoint != null)
        {
            playerHand.position =
                playerCamera.transform.position
                + playerCamera.transform.rotation * initialPickupOffset;
            UpdateAnchorServerRpc(playerHand.position);

            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentItem.transform.TransformPoint(springJoint.anchor));

            if (Input.GetMouseButtonUp(0))
            {
                RequestDropServerRpc();
            }
        }
        else
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    [ServerRpc]
    private void RequestPickupServerRpc(NetworkObject netObj, Vector3 hitPoint)
    {
        if (isHoldingItem)
            return;

        GameObject obj = netObj.gameObject;
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        itemDistance = Vector3.Distance(playerHand.position, obj.transform.position);
        isHoldingItem = true;
        currentItem = obj;
        currentRigidbody = rb;
        currentRigidbody.useGravity = true;

        objectAttachPoint = hitPoint;

        springJoint = obj.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.anchor = obj.transform.InverseTransformPoint(objectAttachPoint);
        springJoint.connectedAnchor = playerHand.position;

        float massFactor = Mathf.Clamp(1 / currentRigidbody.mass, 0.1f, 1f);
        springJoint.spring = maxSpringForce * massFactor;
        springJoint.damper = 10f;
        springJoint.maxDistance = itemDistance;

        currentRigidbody.linearDamping = 5f;
        currentRigidbody.angularDamping = 5f;

        RpcPickup(
            netObj,
            objectAttachPoint,
            playerHand.position,
            springJoint.spring,
            springJoint.damper,
            springJoint.maxDistance
        );
    }

    [ObserversRpc]
    private void RpcPickup(
        NetworkObject netObj,
        Vector3 attachPoint,
        Vector3 handPos,
        float spring,
        float damper,
        float maxDist
    )
    {
        if (IsServerInitialized)
            return;

        GameObject obj = netObj.gameObject;
        Rigidbody rb = obj.GetComponent<Rigidbody>();
        if (rb == null)
            return;

        isHoldingItem = true;
        currentItem = obj;
        currentRigidbody = rb;
        objectAttachPoint = attachPoint;

        currentRigidbody.isKinematic = true;
        currentRigidbody.linearDamping = 5f;
        currentRigidbody.angularDamping = 5f;

        springJoint = obj.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.anchor = obj.transform.InverseTransformPoint(objectAttachPoint);
        springJoint.connectedAnchor = handPos;
        springJoint.spring = spring;
        springJoint.damper = damper;
        springJoint.maxDistance = maxDist;
    }

    [ServerRpc]
    private void UpdateAnchorServerRpc(Vector3 handPos)
    {
        if (springJoint != null)
            springJoint.connectedAnchor = handPos;

        if (currentRigidbody != null && isHoldingItem)
        {
            Vector3 forceDirection = handPos - currentRigidbody.position;
            float maxForce = 50f;
            Vector3 clampedForce = Vector3.ClampMagnitude(forceDirection * moveForce, maxForce);
            currentRigidbody.AddForce(clampedForce, ForceMode.Acceleration);
        }

        RpcUpdateAnchor(handPos);
    }

    [ObserversRpc]
    private void RpcUpdateAnchor(Vector3 handPos)
    {
        if (IsServerInitialized)
            return;
        if (springJoint != null)
            springJoint.connectedAnchor = handPos;
    }

    [ServerRpc]
    private void RequestDropServerRpc()
    {
        if (!isHoldingItem)
            return;

        if (springJoint != null)
            Destroy(springJoint);

        if (currentRigidbody != null)
        {
            currentRigidbody.useGravity = true;
            currentRigidbody.linearDamping = 0f;
            currentRigidbody.angularDamping = 0.05f;
        }

        isHoldingItem = false;
        currentRigidbody = null;
        currentItem = null;

        RpcDrop();
    }

    [ObserversRpc]
    private void RpcDrop()
    {
        if (IsServerInitialized)
            return;

        if (currentRigidbody != null)
        {
            currentRigidbody.isKinematic = false;
            currentRigidbody.useGravity = true;
            currentRigidbody.linearDamping = 0f;
            currentRigidbody.angularDamping = 0.05f;
        }

        isHoldingItem = false;
        currentRigidbody = null;
        currentItem = null;
    }
}
