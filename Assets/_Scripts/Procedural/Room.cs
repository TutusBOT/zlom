using UnityEngine;

public class Room
{
    public int x, y, width, height;

    public Room(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public Vector2Int GetCenter()
    {
        return new Vector2Int(x + width / 2, y + height / 2);
    }
}
