using System.Collections.Generic;
using UnityEngine;

public class Room
{
    public int xOrigin,
        zOrigin;
    public int width,
        length;
    public List<Room> connections;
    public List<ConnectionPoint> connectionPoints;
    public RoomController roomController;

    public Room(int x, int z, int w, int l)
    {
        xOrigin = x;
        zOrigin = z;
        width = w;
        length = l;
        connections = new List<Room>();
        connectionPoints = new List<ConnectionPoint>();
    }
}

public struct ConnectionPoint
{
    public Room connectedRoom;
    public Vector2Int localPosition;
    public Direction direction;
}
