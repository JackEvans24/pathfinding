using System.Collections.Generic;
using UnityEngine;

public class Grid : MonoBehaviour
{
    [Header("Grid Attributes")]
    public LayerMask UnwalkableLayers;
    public Vector2 GridWorldSize;
    public float NodeRadius;
    public TerrainType[] WalkableRegions;
    public int obstacleProximityPenalty = 10;

    [Header("Region Detection")]
    public float RayHeight = 50f;
    public float RayDistance = 100f;
    public int BlurSize = 1;

    [Header("Gizmos")]
    public bool displayGridGizmos;

    Node[,] grid;
    LayerMask walkableMask;
    Dictionary<int, int> walkableRegionsDictionary = new Dictionary<int, int>();

    float nodeDiameter;
    int gridSizeX, gridSizeY;
    int penaltyMin = int.MaxValue, penaltyMax = int.MinValue;

    public int MaxSize { get => this.gridSizeX * this.gridSizeY; }

    private void Awake()
    {
        this.nodeDiameter = this.NodeRadius * 2;
        this.gridSizeX = Mathf.RoundToInt(this.GridWorldSize.x / this.nodeDiameter);
        this.gridSizeY = Mathf.RoundToInt(this.GridWorldSize.y / this.nodeDiameter);

        this.SetWalkableMask();

        this.CreateGrid();
    }

    private void SetWalkableMask()
    {
        this.walkableMask = 0;
        this.walkableRegionsDictionary.Clear();

        foreach (var region in this.WalkableRegions)
        {
            this.walkableMask.value |= region.TerrainMask.value;
            this.walkableRegionsDictionary.Add(Mathf.RoundToInt(Mathf.Log(region.TerrainMask.value, 2f)), region.TerrainPenalty);
        }
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
                
                var movementPenalty = 0;

                var ray = new Ray(worldPoint + Vector3.up * this.RayHeight, Vector3.down);
                if (Physics.Raycast(ray, out var hit, this.RayDistance, this.walkableMask))
                {
                    this.walkableRegionsDictionary.TryGetValue(hit.collider.gameObject.layer, out movementPenalty);
                }

                if (!walkable)
                    movementPenalty += this.obstacleProximityPenalty;

                this.grid[x, y] = new Node(walkable, worldPoint, x, y, movementPenalty);
            }
        }

        this.BlurPenaltyMap(this.BlurSize);
    }

    private void BlurPenaltyMap(int kernelRadius)
    {
        int kernelSize = (kernelRadius * 2) + 1;

        var penaltiesHorizontalPass = new int[this.gridSizeX, this.gridSizeY];
        var penaltiesVerticalPass = new int[this.gridSizeX, this.gridSizeY];

        for (int y = 0; y < this.gridSizeY; y++)
        {
            for (int x = -kernelRadius; x <= kernelRadius; x++)
            {
                int sampleX = Mathf.Clamp(x, 0, kernelRadius);
                penaltiesHorizontalPass[0, y] += grid[sampleX, y].MovementPenalty;
            }

            for (int x = 1; x < this.gridSizeX; x++)
            {
                int removeIndex = Mathf.Clamp(x - kernelRadius - 1, 0, this.gridSizeX);
                int addIndex = Mathf.Clamp(x + kernelRadius, 0, this.gridSizeX - 1);

                penaltiesHorizontalPass[x, y] = penaltiesHorizontalPass[x - 1, y] - this.grid[removeIndex, y].MovementPenalty + this.grid[addIndex, y].MovementPenalty;
            }
        }

        for (int x = 0; x < this.gridSizeX; x++)
        {
            for (int y = -kernelRadius; y <= kernelRadius; y++)
            {
                int sampleY = Mathf.Clamp(x, 0, kernelRadius);
                penaltiesVerticalPass[x, 0] += penaltiesHorizontalPass[x, sampleY];
            }

            int blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, 0] / (kernelSize * kernelSize));
            grid[x, 0].MovementPenalty = blurredPenalty;

            for (int y = 1; y < this.gridSizeY; y++)
            {
                int removeIndex = Mathf.Clamp(y - kernelRadius - 1, 0, this.gridSizeY);
                int addIndex = Mathf.Clamp(y + kernelRadius, 0, this.gridSizeY - 1);

                penaltiesVerticalPass[x, y] = penaltiesVerticalPass[x, y - 1] - penaltiesHorizontalPass[x, removeIndex] + penaltiesHorizontalPass[x, addIndex];
                
                blurredPenalty = Mathf.RoundToInt((float)penaltiesVerticalPass[x, y] / (kernelSize * kernelSize));
                grid[x, y].MovementPenalty = blurredPenalty;

                if (blurredPenalty > this.penaltyMax)
                    this.penaltyMax = blurredPenalty;
                if (blurredPenalty < this.penaltyMin)
                    this.penaltyMin = blurredPenalty;
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

        if (grid == null || !this.displayGridGizmos)
            return;

        foreach (var node in grid)
        {
            if (node.Walkable)
                Gizmos.color = Color.Lerp(Color.white, Color.black, Mathf.InverseLerp(this.penaltyMin, this.penaltyMax, node.MovementPenalty));
            else
                Gizmos.color = Color.red;

            Gizmos.DrawCube(node.WorldPosition, Vector3.one * this.nodeDiameter);
        }
    }
}
