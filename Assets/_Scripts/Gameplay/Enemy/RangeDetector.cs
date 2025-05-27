using UnityEngine;

public class RangeDetector : MonoBehaviour
{
    [SerializeField]
    protected float detectionRadius = 20f;

    [SerializeField]
    protected float detectionAngle = 90f;

    [SerializeField]
    protected LayerMask detectionMask;

    [SerializeField]
    protected bool showDebugVisuals = true;
    public GameObject DetectedTarget { get; set; }

    public virtual GameObject UpdateDetector()
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
            Vector3 directionToTarget = collider.transform.position - transform.position;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            if (angle > detectionAngle / 2)
                continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position);
            bool isPlayerDead = collider.gameObject.GetComponent<Player>().IsDead();

            if (distance < detectionRadius && angle < closestAngle && !isPlayerDead)
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
        if (!showDebugVisuals || this.enabled == false)
            return;

        Gizmos.color = DetectedTarget ? Color.green : Color.yellow;
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
