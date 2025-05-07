using System.Collections.Generic;
using UnityEngine;

public class WaypointManager : MonoBehaviour
{
    public static WaypointManager Instance { get; private set; }

    private List<Waypoint> allWaypoints = new List<Waypoint>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        RefreshWaypointsList();
    }

    public void RefreshWaypointsList()
    {
        allWaypoints.Clear();
        Waypoint[] waypoints = FindObjectsByType<Waypoint>(FindObjectsSortMode.None);
        allWaypoints.AddRange(waypoints);
    }

    public Waypoint GetNearestWaypoint(Vector3 position)
    {
        Waypoint nearest = null;
        float nearestDistance = float.MaxValue;

        foreach (var waypoint in allWaypoints)
        {
            float dist = Vector3.Distance(position, waypoint.transform.position);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    public Waypoint GetNearestWaypointInRange(Vector3 position, float maxRange)
    {
        Waypoint nearest = null;
        float nearestDistance = maxRange;

        foreach (var waypoint in allWaypoints)
        {
            float dist = Vector3.Distance(position, waypoint.transform.position);
            if (dist < nearestDistance)
            {
                nearestDistance = dist;
                nearest = waypoint;
            }
        }

        return nearest;
    }

    public Waypoint GetRandomWaypoint()
    {
        if (allWaypoints.Count == 0)
            return null;

        int randomIndex = Random.Range(0, allWaypoints.Count);
        return allWaypoints[randomIndex];
    }

    public Waypoint GetRandomWaypointInRange(Vector3 position, float maxRange)
    {
        List<Waypoint> inRange = new List<Waypoint>();

        foreach (var waypoint in allWaypoints)
        {
            float dist = Vector3.Distance(position, waypoint.transform.position);
            if (dist <= maxRange)
            {
                inRange.Add(waypoint);
            }
        }

        if (inRange.Count == 0)
            return null;

        int randomIndex = Random.Range(0, inRange.Count);
        return inRange[randomIndex];
    }

    public Waypoint GetOneOfNearestWaypoints(Vector3 position, int count = 3)
    {
        if (allWaypoints.Count == 0)
            return null;

        // Create a sorted list of waypoints by distance
        List<(Waypoint waypoint, float distance)> sortedWaypoints = new List<(Waypoint, float)>();

        foreach (var waypoint in allWaypoints)
        {
            float dist = Vector3.Distance(position, waypoint.transform.position);
            sortedWaypoints.Add((waypoint, dist));
        }

        sortedWaypoints.Sort((a, b) => a.distance.CompareTo(b.distance));

        // Pick a random waypoint from the N nearest
        int selectCount = Mathf.Min(count, sortedWaypoints.Count);
        int randomIndex = Random.Range(0, selectCount);

        return sortedWaypoints[randomIndex].waypoint;
    }
}
