using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    [SerializeField]
    private List<Waypoint> connectedWaypoints = new List<Waypoint>();

    [SerializeField]
    private bool showGizmos = true;

    [SerializeField]
    private Color gizmoColor = Color.blue;

    [SerializeField]
    private float gizmoSize = 0.25f;

    public List<Waypoint> ConnectedWaypoints => connectedWaypoints;

    public Waypoint GetNextWaypoint(Waypoint previousWaypoint = null)
    {
        if (connectedWaypoints.Count == 0)
            return null;

        if (connectedWaypoints.Count == 1)
            return connectedWaypoints[0];

        List<Waypoint> possibleWaypoints = new List<Waypoint>(connectedWaypoints);

        if (previousWaypoint != null && possibleWaypoints.Count > 1)
            possibleWaypoints.Remove(previousWaypoint);

        int randomIndex = Random.Range(0, possibleWaypoints.Count);
        return possibleWaypoints[randomIndex];
    }

    public void AddConnection(Waypoint waypoint)
    {
        if (waypoint != this && !connectedWaypoints.Contains(waypoint))
            connectedWaypoints.Add(waypoint);
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmoColor;
        Gizmos.DrawSphere(transform.position, gizmoSize);

        if (connectedWaypoints == null)
            return;

        foreach (var waypoint in connectedWaypoints)
        {
            if (waypoint != null)
            {
                Gizmos.color = Color.Lerp(gizmoColor, Color.white, 0.5f);
                Gizmos.DrawLine(transform.position, waypoint.transform.position);

                Vector3 direction = waypoint.transform.position - transform.position;
                Vector3 arrowPos = transform.position + direction * 0.8f;
                Gizmos.DrawSphere(arrowPos, gizmoSize * 0.5f);
            }
        }
    }
}
