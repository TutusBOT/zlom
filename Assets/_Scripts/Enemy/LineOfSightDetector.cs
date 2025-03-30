using UnityEngine;

public class LineOfSightDetector : MonoBehaviour
{
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private float detectionRange = 10.0f;
    [SerializeField] private float detectionHeight = 3f;

    [SerializeField] private bool showDebugVisuals = true;

    public GameObject PerformDetection(GameObject potentialTarget)
    {
        RaycastHit hit;
        Vector3 direction = potentialTarget.transform.position - (transform.position + Vector3.up * detectionHeight); // Uwzględniamy wysokość detektora

        // Zmieniamy promień, aby uwzględniał tylko kierunek i zasięg detekcji
        if (Physics.Raycast(transform.position + Vector3.up * detectionHeight, direction.normalized, out hit, detectionRange, playerLayerMask))
        {
            // Sprawdzamy, czy trafiony obiekt to nasz potencjalny cel
            if (hit.collider.gameObject == potentialTarget)
            {
                if (showDebugVisuals && this.enabled)
                {
                    Debug.DrawLine(transform.position + Vector3.up * detectionHeight, potentialTarget.transform.position, Color.green);
                }
                return hit.collider.gameObject;
            }
        }
        return null;
    }

    private void OnDrawGizmos()
    {
        if (showDebugVisuals)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + Vector3.up * detectionHeight, 0.3f);
        }
    }
}
