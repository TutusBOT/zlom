using System.Collections.Generic;
using UnityEngine;

public class BSPNode
{
    public int x, y, width, height;
    public BSPNode left, right;
    
    public BSPNode(int x, int y, int width, int height)
    {
        this.x = x;
        this.y = y;
        this.width = width;
        this.height = height;
    }

    public void Split(int minSize)
    {
        if (width > height && width >= minSize * 2)
        {
            int splitX = Random.Range(x + minSize, x + width - minSize);
            left = new BSPNode(x, y, splitX - x, height);
            right = new BSPNode(splitX, y, x + width - splitX, height);
        }
        else if (height >= minSize * 2)
        {
            int splitY = Random.Range(y + minSize, y + height - minSize);
            left = new BSPNode(x, y, width, splitY - y);
            right = new BSPNode(x, splitY, width, y + height - splitY);
        }

        if (left != null && right != null)
        {
            left.Split(minSize);
            right.Split(minSize);
        }
    }

    public List<BSPNode> GetLeafNodes()
    {
        List<BSPNode> leaves = new List<BSPNode>();
        if (left == null && right == null)
        {
            leaves.Add(this);
        }
        else
        {
            if (left != null) leaves.AddRange(left.GetLeafNodes());
            if (right != null) leaves.AddRange(right.GetLeafNodes());
        }
        return leaves;
    }
}
