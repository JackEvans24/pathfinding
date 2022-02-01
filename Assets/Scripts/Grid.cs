using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    public LayerMask UnwalkableLayers;
    public Vector2 GridWorldSize;
    public float NodeRadius;
    public List<Node> path;

    public bool displayGridGizmos;

    Node[,] grid;

    float nodeDiameter;
    int gridSizeX, gridSizeY;

    public int MaxSize { get => this.gridSizeX * this.gridSizeY; }

    private void Start()
    {
        this.nodeDiameter = this.NodeRadius * 2;
        this.gridSizeX = Mathf.RoundToInt(this.GridWorldSize.x / this.nodeDiameter);
        this.gridSizeY = Mathf.RoundToInt(this.GridWorldSize.y / this.nodeDiameter);

        this.CreateGrid();
    }

    private void CreateGrid()
    {
        this.grid = new Node[this.gridSizeX, this.gridSizeY];
        var bottomLeft = this.transform.position - (Vector3.right * this.GridWorldSize.x / 2) - (Vector3.forward * this.GridWorldSize.y / 2);

        for (int x = 0; x < this.gridSizeX; x++)
        {
            for (int y = 0; y < this.gridSizeY; y++)
            {
                var worldPoint = bottomLeft + (Vector3.right * (x * this.nodeDiameter + this.NodeRadius)) + (Vector3.forward * (y * this.nodeDiameter + this.NodeRadius));
                var walkable = !Physics.CheckSphere(worldPoint, 0.5f, this.UnwalkableLayers);

                this.grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        var percentX = Mathf.Clamp01((worldPosition.x + (this.GridWorldSize.x / 2)) / this.GridWorldSize.x);
        var percentY = Mathf.Clamp01((worldPosition.z + (this.GridWorldSize.y / 2)) / this.GridWorldSize.y);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return this.grid[x, y];
    }

    public IEnumerable<Node> GetNeighbouringNodes(Node referenceNode)
    {
        var neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = referenceNode.GridX + x;
                int checkY = referenceNode.GridY + y;

                if (checkX >= 0 && checkX < this.gridSizeX && checkY >= 0 && checkY < this.gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(this.GridWorldSize.x, 1f, this.GridWorldSize.y));

        if (grid == null)
            return;

        foreach (var node in grid)
        {
            if (path != null && path.Contains(node))
            {
                Gizmos.color = Color.black;
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * (this.nodeDiameter - 0.1f));
            }
            else if (this.displayGridGizmos)
            {
                Gizmos.color = node.Walkable ? Color.white : Color.red;
                Gizmos.DrawCube(node.WorldPosition, Vector3.one * (this.nodeDiameter - 0.1f));
            }
        }
    }
}
