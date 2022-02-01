using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Grid), typeof(PathManager))]
public class Pathfinding : MonoBehaviour
{
    private Grid grid;
    private PathManager manager;

    private void Awake()
    {
        this.grid = GetComponent<Grid>();
        this.manager = GetComponent<PathManager>();
    }

    public void StartFindPath(Vector3 pathStart, Vector3 pathEnd)
    {
        StartCoroutine(FindPath(pathStart, pathEnd));
    }

    private IEnumerator FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var waypoints = new Vector3[0];
        var success = false;

        var startNode = this.grid.NodeFromWorldPoint(startPosition);
        var targetNode = this.grid.NodeFromWorldPoint(targetPosition);

        if (!startNode.Walkable || !targetNode.Walkable)
        {
            this.manager.FinishedProcessingPath(waypoints, success);
            yield break;
        }

        var openSet = new Heap<Node>(this.grid.MaxSize);
        openSet.Add(startNode);
        var closedSet = new HashSet<Node>();

        while (openSet.Count > 0)
        {
            var currentNode = openSet.Pop();
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                stopwatch.Stop();
                UnityEngine.Debug.Log($"Path found in {stopwatch.ElapsedMilliseconds}ms");

                success = true;
                break;
            }

            foreach (var neighbour in this.grid.GetNeighbouringNodes(currentNode))
            {
                if (!neighbour.Walkable || closedSet.Contains(neighbour))
                    continue;

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.Parent = currentNode;

                    if (!openSet.Contains(neighbour))
                        openSet.Add(neighbour);
                    else
                        openSet.UpdateItem(neighbour);
                }
            }
        }

        yield return null;

        if (success)
            waypoints = this.RetracePath(startNode, targetNode);

        this.manager.FinishedProcessingPath(waypoints, success);
    }

    private Vector3[] RetracePath(Node startNode, Node endNode)
    {
        var path = new List<Node>();
        var currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }

        var waypoints = this.SimplifyPath(path);
        Array.Reverse(waypoints);

        return waypoints;
    }

    private Vector3[] SimplifyPath(List<Node> pathNodes)
    {
        var waypoints = new List<Vector3>();
        Vector2 lastDirection = Vector2.zero;

        for (int i = 1; i < pathNodes.Count; i++)
        {
            Vector2 newDirection = new Vector2(
                pathNodes[i - 1].GridX - pathNodes[i].GridX,
                pathNodes[i - 1].GridY - pathNodes[i].GridY
            );

            if (newDirection == lastDirection)
                continue;

            waypoints.Add(pathNodes[i].WorldPosition);
            lastDirection = newDirection;
        }

        return waypoints.ToArray();
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        var xDistance = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        var yDistance = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        return (14 * Mathf.Min(xDistance, yDistance)) + (10 * Mathf.Abs(xDistance - yDistance));
    }
}
