using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Connection;

public class ObjectPickup : NetworkBehaviour
{
    public Camera playerCamera;
    public float pickupRange = 5f;
    public LayerMask interactableLayer;
    public LineRenderer lineRenderer;
    public Transform playerHand;

    private NetworkObject currentNetObj;
    private bool isHolding = false;
    private float itemDistance;

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


    private void Update()
    {
        if (!IsOwner)
            return;

        Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, interactableLayer))
        {
            if (!isHolding)
            {
                lineRenderer.SetPosition(0, transform.position);
                lineRenderer.SetPosition(1, hit.collider.transform.position);
            }

            if (Input.GetMouseButtonDown(0) && hit.collider.CompareTag("Valuable"))
            {
                NetworkObject netObj = hit.collider.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    itemDistance = Vector3.Distance(playerHand.position, hit.collider.transform.position);
                    RequestGrabServer(netObj, itemDistance);
                }
            }
        }

        if (isHolding && currentNetObj != null)
        {
            lineRenderer.SetPosition(0, playerHand.position);
            lineRenderer.SetPosition(1, currentNetObj.transform.position);

            Vector3 targetPos = playerCamera.ScreenToWorldPoint(
                new Vector3(Input.mousePosition.x, Input.mousePosition.y, itemDistance)
            );

            SendGrabPositionServer(currentNetObj, targetPos);

            if (Input.GetMouseButtonUp(0))
            {
                ReleaseGrabServer(currentNetObj);
                isHolding = false;
                currentNetObj = null;
            }
        }
        else
        {
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, transform.position);
        }
    }

    [ServerRpc]
    private void RequestGrabServer(NetworkObject target, float distance)
    {
        Valuable2 val = target.GetComponent<Valuable2>();
        if (val != null)
        {
            val.AddGrabber(Owner, distance);
            TargetConfirmGrab(Owner, target);
        }
    }

    [TargetRpc]
    private void TargetConfirmGrab(NetworkConnection conn, NetworkObject target)
    {
        isHolding = true;
        currentNetObj = target;
    }

    [ServerRpc]
    private void SendGrabPositionServer(NetworkObject target, Vector3 targetPosition)
    {
        Valuable2 val = target.GetComponent<Valuable2>();
        if (val != null)
        {
            val.UpdateGrabPosition(Owner, targetPosition);
        }
    }

    [ServerRpc]
    private void ReleaseGrabServer(NetworkObject target)
    {
        Valuable2 val = target.GetComponent<Valuable2>();
        if (val != null)
        {
            val.RemoveGrabber(Owner);
        }
    }

    public void ForceDrop()
    {
        if (isHolding && currentNetObj != null)
        {
            ReleaseGrabServer(currentNetObj);
            isHolding = false;
            currentNetObj = null;
        }
    }
}
