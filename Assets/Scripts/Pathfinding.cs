using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Grid), typeof(PathManager))]
public class Pathfinding : MonoBehaviour
{
    [Header("Diagnostics")]
    [SerializeField] private bool showPathfindingTime;

    private Grid grid;
    private PathManager manager;
    private Stopwatch stopwatch;

    private void Awake()
    {
        this.grid = GetComponent<Grid>();
        this.manager = GetComponent<PathManager>();

        this.stopwatch = new Stopwatch();
    }

    /// <summary>
    /// Start the coroutine to find the path between two points
    /// </summary>
    /// <param name="pathStart">Starting world space point</param>
    /// <param name="pathEnd">Ending world space point</param>
    public void StartFindPath(Vector3 pathStart, Vector3 pathEnd) =>
        StartCoroutine(this.FindPath(pathStart, pathEnd));

    /// <summary>
    /// Find the most efficient path between two points
    /// </summary>
    /// <param name="startPosition">Starting world space point</param>
    /// <param name="targetPosition">Ending world space point</param>
    /// <returns></returns>
    private IEnumerator FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        if (this.showPathfindingTime)
            this.stopwatch.Restart();

        // Create result values
        var waypoints = new Vector3[0];
        var success = false;

        // Find closest nodes to start and end points
        var startNode = this.grid.NodeFromWorldPoint(startPosition);
        var targetNode = this.grid.NodeFromWorldPoint(targetPosition);

        // If either node is not walkable, no path can be found
        if (!startNode.Walkable || !targetNode.Walkable)
        {
            this.manager.FinishedProcessingPath(waypoints, success);
            yield break;
        }

        // Add the first node to the collection of nodes to check
        var nodesToCheck = new Heap<Node>(this.grid.MaxSize);
        nodesToCheck.Add(startNode);
        var checkedNodes = new HashSet<Node>();

        // Start pathfinding algorithm
        while (nodesToCheck.Count > 0)
        {
            // Get the next node to check
            var currentNode = nodesToCheck.Pop();
            checkedNodes.Add(currentNode);

            // If the current node is the end of the path, stop the loop
            if (currentNode == targetNode)
            {
                this.stopwatch.Stop();
                if (this.showPathfindingTime)
                    UnityEngine.Debug.Log($"Path found in {this.stopwatch.ElapsedMilliseconds}ms");

                success = true;
                break;
            }

            // For each neighbour of the current node
            foreach (var neighbour in this.grid.GetNeighbouringNodes(currentNode))
            {
                // If already checked or not walkable, continue
                if (!neighbour.Walkable || checkedNodes.Contains(neighbour))
                    continue;

                // If already in the collection of nodes to check, continue
                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.MovementPenalty;
                if (newMovementCostToNeighbour >= neighbour.gCost && nodesToCheck.Contains(neighbour))
                    continue;

                // We now have a walkable neighbour node which needs to be updated in the collection of nodes to check
                // Update node cost values
                neighbour.gCost = newMovementCostToNeighbour;
                neighbour.hCost = GetDistance(neighbour, targetNode);
                neighbour.Parent = currentNode;

                // Add or update node in collection
                if (!nodesToCheck.Contains(neighbour))
                    nodesToCheck.Add(neighbour);
                else
                    nodesToCheck.UpdateItem(neighbour);
            }
        }

        yield return null;

        if (success)
            waypoints = this.RetracePath(startNode, targetNode);

        // Notify PathManager that the pathfinding is complete
        this.manager.FinishedProcessingPath(waypoints, success);
    }

    /// <summary>
    /// Get waypoints along the found path
    /// </summary>
    /// <param name="startNode">The node at the start of the path</param>
    /// <param name="endNode">The node at the end of the path</param>
    /// <returns>An array of world-space coordinates along the found path</returns>
    private Vector3[] RetracePath(Node startNode, Node endNode)
    {
        var path = new List<Node>();
        var currentNode = endNode;

        // Add all nodes to array
        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        // Remove nodes between points where path direction changes
        var waypoints = this.SimplifyPath(path);
        // Reverse array so that it starts with the start node
        Array.Reverse(waypoints);

        return waypoints;
    }

    /// <summary>
    /// Remove nodes between points where path direction changes
    /// </summary>
    /// <param name="pathNodes">A collection of all nodes in the path</param>
    /// <returns>An array of world-space coordinates relating to path waypoints</returns>
    private Vector3[] SimplifyPath(List<Node> pathNodes)
    {
        var waypoints = new List<Vector3>();
        Vector2 lastDirection = Vector2.zero;

        // For each node in the path
        for (int i = 1; i < pathNodes.Count; i++)
        {
            // Calculate direction from last node
            Vector2 newDirection = new Vector2(
                pathNodes[i - 1].GridX - pathNodes[i].GridX,
                pathNodes[i - 1].GridY - pathNodes[i].GridY
            );

            // If the direction is unchanged, do not add to new list
            if (newDirection == lastDirection)
                continue;

            // Add node and update last direction
            waypoints.Add(pathNodes[i].WorldPosition);
            lastDirection = newDirection;
        }

        return waypoints.ToArray();
    }

    /// <summary>
    /// Return the distance between 2 nodes
    /// </summary>
    /// <param name="nodeA">First node</param>
    /// <param name="nodeB">Second node</param>
    /// <returns>An int signifying the minimum distance cost between the two nodes</returns>
    private int GetDistance(Node nodeA, Node nodeB)
    {
        var xDistance = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        var yDistance = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        return (14 * Mathf.Min(xDistance, yDistance)) + (10 * Mathf.Abs(xDistance - yDistance));
    }
}
