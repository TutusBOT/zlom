using UnityEngine;

/// <summary>
/// Detects when players are very close to the enemy (within a small radius)
/// </summary>
public class ProximityDetector : MonoBehaviour
{
    [SerializeField]
    private float proximityRadius = 3f;

    [SerializeField]
    private LayerMask playerLayer;

    [SerializeField]
    private bool showDebugVisuals = true;

    public GameObject DetectedTarget { get; private set; }

    /// <summary>
    /// Check for players within the proximity radius
    /// </summary>
    /// <returns>The closest detected player GameObject, or null if none found</returns>
    public GameObject CheckProximity()
    {
        Collider[] colliders = Physics.OverlapSphere(
            transform.position,
            proximityRadius,
            playerLayer
        );

        GameObject closestTarget = null;
        float closestDistance = float.MaxValue;

        foreach (Collider collider in colliders)
        {
            Player player = collider.gameObject.GetComponent<Player>();
            if (player == null || player.IsDead())
                continue;

            float distance = Vector3.Distance(transform.position, collider.transform.position);
            bool isPlayerDead = collider.gameObject.GetComponent<Player>().IsDead();

            if (distance < closestDistance && !isPlayerDead)
            {
                closestTarget = collider.gameObject;
                closestDistance = distance;
            }
        }

        DetectedTarget = closestTarget;
        return DetectedTarget;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugVisuals || !enabled)
            return;

        Gizmos.color = DetectedTarget ? Color.red : new Color(1f, 0f, 1f, 0.3f);
        ;
        Gizmos.DrawWireSphere(transform.position, proximityRadius);
    }
}
