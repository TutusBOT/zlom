using UnityEngine;

public class ValuableSpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    public ValuableSize allowedSize = ValuableSize.Small;
    public bool allowSmallerSizes = true;

    private void OnDrawGizmos()
    {
        Gizmos.color = allowedSize switch
        {
            ValuableSize.Small => Color.green,
            ValuableSize.Medium => Color.yellow,
            ValuableSize.Large => Color.red,
            _ => Color.white,
        };

        float size = allowedSize switch
        {
            ValuableSize.Small => 0.3f,
            ValuableSize.Medium => 0.5f,
            ValuableSize.Large => 0.8f,
            _ => 0.5f,
        };

        Gizmos.DrawWireSphere(transform.position, size);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.1f);
    }
}
