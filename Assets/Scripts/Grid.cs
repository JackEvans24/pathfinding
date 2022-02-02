using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [Header("Grid Attributes")]
    [SerializeField] private LayerMask unwalkableLayers;
    [SerializeField] private Vector2 gridWorldSize = Vector2.one * 100f;
    [SerializeField] private float nodeRadius = 0.5f;
    [SerializeField] private TerrainType[] walkableRegions;
    [SerializeField] private int obstacleProximityPenalty = 10;

    [HideInInspector] public int MaxSize { get => this.gridSizeX * this.gridSizeY; }

    [Header("Region Detection")]
    [SerializeField] private float RayHeight = 50f;
    [SerializeField] private float RayDistance = 100f;
    [SerializeField] private int BlurSize = 3;

    [Header("Gizmos")]
    [SerializeField] private bool displayGridGizmos;

    // Grid member variables
    Node[,] grid;

    // Walkable layers member variables
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();
    int penaltyMin = int.MaxValue, penaltyMax = int.MinValue;

    // Computed variables
    float nodeDiameter { get => this.nodeRadius * 2f; }
    int gridSizeX { get => Mathf.RoundToInt(this.gridWorldSize.x / this.nodeDiameter); }
    int gridSizeY { get => Mathf.RoundToInt(this.gridWorldSize.y / this.nodeDiameter); }

    private void Awake()
    {
        this.ResetWalkableMask();
        this.CreateGrid();
        this.BlurPenaltyMap(this.BlurSize);
    }

    /// <summary>
    /// Set the value of the walkable mask, and map the walkable region penalties to their layer index
    /// </summary>
    private void ResetWalkableMask()
    {
        this.walkableMask = 0;
        this.walkableRegionsDictionary.Clear();

        foreach (var region in this.walkableRegions)
        {
            this.walkableMask.value |= region.TerrainMask.value;
            this.walkableRegionsDictionary.Add(Mathf.RoundToInt(Mathf.Log(region.TerrainMask.value, 2f)), region.TerrainPenalty);
        }
    }

    /// <summary>
    /// Create a node at each point on the grid
    /// </summary>
    private void CreateGrid()
    {
        this.grid = new Node[this.gridSizeX, this.gridSizeY];
        var bottomLeft = this.transform.position - (Vector3.right * this.gridWorldSize.x / 2) - (Vector3.forward * this.gridWorldSize.y / 2);

        for (int x = 0; x < this.gridSizeX; x++)
        for (int y = 0; y < this.gridSizeY; y++)
            this.grid[x, y] = this.CreateNodeAtPoint(bottomLeft, x, y);
    }

    /// <summary>
    /// Calculates the position, walkable value, and movement penalty for a node given its position
    /// </summary>
    /// <param name="bottomLeft">The bottom-left point of the grid</param>
    /// <param name="x">x coordinate</param>
    /// <param name="y">y coordinate</param>
    /// <returns>New fully initialised Node instantiation</returns>
    private Node CreateNodeAtPoint(Vector3 bottomLeft, int x, int y)
    {
        var worldPoint = this.GetWorldPoint(bottomLeft, x, y);
        var walkable = !Physics.CheckSphere(worldPoint, this.nodeRadius - 0.05f, this.unwalkableLayers);

        var movementPenalty = this.GetMovementPenalty(worldPoint, walkable);

        return new Node(walkable, worldPoint, x, y, movementPenalty);
    }

    /// <summary>
    /// Gets the world point of a coordinate on the grid
    /// </summary>
    /// <param name="bottomLeft">The bottom-left point of the grid</param>
    /// <param name="x">x coordinate</param>
    /// <param name="y">y coordinate</param>
    /// <returns>World space Vector3</returns>
    private Vector3 GetWorldPoint(Vector3 bottomLeft, int x, int y) =>
        bottomLeft + (Vector3.right * (x * this.nodeDiameter + this.nodeRadius)) + (Vector3.forward * (y * this.nodeDiameter + this.nodeRadius));

    /// <summary>
    /// Calculates the movement penalty of a node
    /// </summary>
    /// <param name="worldPoint">The world point of the node</param>
    /// <param name="walkable">The walkable value of the node</param>
    /// <returns>A movement penalty score for the node</returns>
    private int GetMovementPenalty(Vector3 worldPoint, bool walkable)
    {
        var movementPenalty = 0;

        // Update the movement penalty if a downwards raycast finds a walkable layer
        if (Physics.Raycast(worldPoint + Vector3.up * this.RayHeight, Vector3.down, out var hit, this.RayDistance, this.walkableMask))
            this.walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);

        // If not walkable, add the obstruction penalty
        if (!walkable)
            movementPenalty += this.obstacleProximityPenalty;

        return movementPenalty;
    }

    /// <summary>
    /// Applies blurring to the penalty map by taking an average of surrounding nodes' penalty scores
    /// </summary>
    /// <param name="kernelRadius">Nodes between the central node and the edge of the blur area.
    /// Kernel radius 1 gives a 3x3 blur area (central node plus one node either side)</param>
    private void BlurPenaltyMap(int kernelRadius)
    {
        int kernelSize = (kernelRadius * 2) + 1;

        var penaltiesHorizontalPass = new int[this.gridSizeX, this.gridSizeY];
        var penaltiesVerticalPass = new int[this.gridSizeX, this.gridSizeY];

        // For each row...
        for (int y = 0; y < this.gridSizeY; y++)
        {
            // Start by finding the sum of cells horizontally adjacent to cell with x=0
            // Cells past the edge of the map get the same score as those on the edge
            for (int x = -kernelRadius; x <= kernelRadius; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelRadius);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].MovementPenalty;
            }

            // Then for every other cell, remove the score of the cell no longer in the kernel,
            // then add the score which has just entered the kernel
            for (int x = 1; x < this.gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelRadius - 1, 0, this.gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelRadius, 0, this.gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - this.grid[removeIndex, y].MovementPenalty + this.grid[addIndex, y].MovementPenalty;
            }
        }

        // Repeat the process for each column, using the horizontal pass values
        // and divide by kernel area before assigning back to the node
        for (int x = 0; x < this.gridSizeX; x++)
        {
            for (int y = -kernelRadius; y <= kernelRadius; y++)
            {
                int sampleY = Mathf.Clamp(x, 0, kernelRadius);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            this.AssignBlurredPenalty(x, 0, penaltiesVerticalPass, kernelSize);

            for (int y = 1; y < this.gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelRadius - 1, 0, this.gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelRadius, 0, this.gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                
                this.AssignBlurredPenalty(x, y, penaltiesVerticalPass, kernelSize);
            }
        }
    }

    /// <summary>
    /// Calculate blurred penalty and assign to relevant node
    /// </summary>
    /// <param name="x">x coordinate</param>
    /// <param name="y">y coordinate</param>
    /// <param name="penaltyMap">The map of penalty totals</param>
    /// <param name="kernelSize">The size of kernel used to get penalty totals</param>
    private void AssignBlurredPenalty(int x, int y, int[,] penaltyMap, int kernelSize)
    {
        var blurredPenalty = Mathf.RoundToInt((float)penaltyMap[x, y] / (kernelSize * kernelSize));
        grid[x, y].MovementPenalty = blurredPenalty;

        // Set penalty min and max for gizmos
        if (blurredPenalty > this.penaltyMax)
            this.penaltyMax = blurredPenalty;
        if (blurredPenalty < this.penaltyMin)
            this.penaltyMin = blurredPenalty;
    }

    /// <summary>
    /// Get the closest node to a world position
    /// </summary>
    /// <param name="worldPosition">3D world space coordinate</param>
    /// <returns>Closest node to the <paramref name="worldPosition"/> input</returns>
    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        var percentX = Mathf.Clamp01((worldPosition.x + (this.gridWorldSize.x / 2)) / this.gridWorldSize.x);
        var percentY = Mathf.Clamp01((worldPosition.z + (this.gridWorldSize.y / 2)) / this.gridWorldSize.y);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);

        return this.grid[x, y];
    }

    /// <summary>
    /// Return all nodes between -1 and 1 x or y away from a given <paramref name="referenceNode"/>.
    /// Does not include the <paramref name="referenceNode"/>.
    /// </summary>
    /// <param name="referenceNode">The node for which to return neighbours</param>
    /// <returns>A collection of nodes surrounding the <paramref name="referenceNode"/></returns>
    public IEnumerable<Node> GetNeighbouringNodes(Node referenceNode)
    {
        var neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        for (int y = -1; y <= 1; y++)
        {
            if (x == 0 && y == 0)
                continue;

            int checkX = referenceNode.GridX + x;
            int checkY = referenceNode.GridY + y;

            if (checkX >= 0 && checkX < this.gridSizeX && checkY >= 0 && checkY < this.gridSizeY)
                neighbours.Add(grid[checkX, checkY]);
        }

        return neighbours;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(this.transform.position, new Vector3(this.gridWorldSize.x, 1f, this.gridWorldSize.y));

        if (this.grid == null || !this.displayGridGizmos)
            return;

        foreach (var node in this.grid)
        {
            if (node.Walkable)
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(this.penaltyMin, this.penaltyMax, node.MovementPenalty));
            else
                Gizmos.color = Color.red;

            Gizmos.DrawCube(node.WorldPosition, Vector3.one * this.nodeDiameter);
        }
    }
}
