using UnityEngine;

public class Node : IHeapItem<Node>
{
    public bool Walkable;
    public Vector3 WorldPosition;
    public int GridX;
    public int GridY;
    public int MovementPenalty;

    public int gCost;
    public int hCost;
    public int fCost { get => this.gCost + this.hCost; }

    private int heapIndex;
    public int HeapIndex { get => this.heapIndex; set => this.heapIndex = value; }

    public Node Parent;

    public Node(bool walkable, Vector3 worldPosition, int gridX, int gridY, int movementPenalty)
    {
        this.Walkable = walkable;
        this.WorldPosition = worldPosition;
        this.GridX = gridX;
        this.GridY = gridY;
        this.MovementPenalty = movementPenalty;
    }

    // IComparable implementation
    public int CompareTo(Node other)
    {
        var result = this.fCost.CompareTo(other.fCost);
        if (result == 0)
            result = this.hCost.CompareTo(other.hCost);

        return -result;
    }
}
