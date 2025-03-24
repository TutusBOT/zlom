using System.Collections.Generic;
using UnityEngine;

public static class AStar
{
    public static List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();

        Vector2Int current = start;
        while (current != end)
        {
            if (current.x < end.x) current.x++;
            else if (current.x > end.x) current.x--;

            if (current.y < end.y) current.y++;
            else if (current.y > end.y) current.y--;

            path.Add(new Vector2Int(current.x, current.y));
        }
        
        return path;
    }
}
