using UnityEngine;

public class LineOfSightDetector : MonoBehaviour
{
    [SerializeField]
    private float detectionRadius = 20f;

    [SerializeField]
    private float detectionAngle = 90f;

    [SerializeField]
    private LayerMask detectionMask;

    [SerializeField]
    private bool showDebugVisuals = true;

    public GameObject DetectedTarget { get; private set; }

    public GameObject PerformDetection()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            detectionRadius,
            detectionMask
        );

        GameObject closestTarget = null;
        float closestAngle = detectionAngle;

        foreach (Collider collider in colliders)
        {
            Vector3 directionToTarget = (
                collider.transform.position - transform.position
            ).normalized;

            float angle = Vector3.Angle(transform.forward, directionToTarget);

            bool angleCheck = angle <= detectionAngle / 2;
            bool raycastHit = Physics.Raycast(
                transform.position,
                directionToTarget,
                out RaycastHit hit,
                detectionRadius,
                detectionMask
            );

            bool hitValidation = raycastHit && hit.collider.gameObject == collider.gameObject;
            bool closestCheck = angle < closestAngle;

            if (angleCheck && hitValidation && closestCheck)
            {
                closestTarget = collider.gameObject;
                closestAngle = angle;
            }
        }
        DetectedTarget = closestTarget;
        return DetectedTarget;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || !enabled)
            return;

        Gizmos.color = DetectedTarget ? Color.green : Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = new Color(1, 0, 0, 0.3f);
        Vector3 leftBoundary =
            Quaternion.Euler(0, -detectionAngle / 2, 0) * transform.forward * detectionRadius;
        Vector3 rightBoundary =
            Quaternion.Euler(0, detectionAngle / 2, 0) * transform.forward * detectionRadius;

        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
    }
}
