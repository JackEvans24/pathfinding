using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node
{
    public bool Walkable;
    public Vector3 WorldPosition;
    public int GridX;
    public int GridY;

    public int gCost;
    public int hCost;
    public int fCost { get => this.gCost + this.hCost; }

    public Node Parent;

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY)
    {
        this.Walkable = walkable;
        this.WorldPosition = worldPosition;
        this.GridX = gridX;
        this.GridY = gridY;
    }
}
