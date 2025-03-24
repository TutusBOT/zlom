using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    public int gridWidth = 20, gridHeight = 20;
    public GameObject roomPrefab, hallwayPrefab;
    
    private int[,] grid;  // 0 = empty, 1 = room, 2 = hallway
    private List<Room> rooms = new List<Room>();

    void Start()
    {
        grid = new int[gridWidth, gridHeight];
        GenerateLevel();
    }

    void GenerateLevel()
    {
        // Step 1: Generate Rooms
        for (int i = 0; i < 5; i++)  // Create 5 rooms (adjust as needed)
        {
            int w = Random.Range(3, 6);
            int h = Random.Range(3, 6);
            int x = Random.Range(1, gridWidth - w - 1);
            int y = Random.Range(1, gridHeight - h - 1);

            Room newRoom = new Room(x, y, w, h);
            if (!IsOverlapping(newRoom))
            {
                rooms.Add(newRoom);
                MarkRoomOnGrid(newRoom);
                Instantiate(roomPrefab, new Vector3(x, 0, y), Quaternion.identity);
            }
        }

        // Step 2: Connect Rooms with Hallways
        for (int i = 0; i < rooms.Count - 1; i++)
        {
            ConnectRooms(rooms[i], rooms[i + 1]);
        }
    }

    bool IsOverlapping(Room room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                if (grid[x, y] != 0) return true;  // If occupied, return true
            }
        }
        return false;
    }

    void MarkRoomOnGrid(Room room)
    {
        for (int x = room.x; x < room.x + room.width; x++)
        {
            for (int y = room.y; y < room.y + room.height; y++)
            {
                grid[x, y] = 1;  // Mark as occupied by a room
            }
        }
    }

    void ConnectRooms(Room roomA, Room roomB)
    {
        Vector2Int start = roomA.GetCenter();
        Vector2Int end = roomB.GetCenter();
        
        List<Vector2Int> path = AStar.FindPath(start, end);

        foreach (Vector2Int tile in path)
        {
            if (grid[tile.x, tile.y] == 0)  // Only place hallway if the space is empty
            {
                grid[tile.x, tile.y] = 2;
                Instantiate(hallwayPrefab, new Vector3(tile.x, 0, tile.y), Quaternion.identity);
            }
        }
    }
}
