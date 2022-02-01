using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

[RequireComponent(typeof(Grid))]
public class Pathfinding : MonoBehaviour
{
    public Transform seeker;
    public Transform target;

    private Grid grid;

    private void Awake()
    {
        this.grid = GetComponent<Grid>();
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
            this.FindPath(this.seeker.position, this.target.position);
    }

    private void FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var startNode = this.grid.NodeFromWorldPoint(startPosition);
        var targetNode = this.grid.NodeFromWorldPoint(targetPosition);

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

                this.RetracePath(startNode, targetNode);
                return;
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
    }

    private void RetracePath(Node startNode, Node endNode)
    {
        var path = new List<Node>();
        var currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.Parent;
        }
        path.Reverse();

        this.grid.path = path;
    }

    private int GetDistance(Node nodeA, Node nodeB)
    {
        var xDistance = Mathf.Abs(nodeA.GridX - nodeB.GridX);
        var yDistance = Mathf.Abs(nodeA.GridY - nodeB.GridY);

        return (14 * Mathf.Min(xDistance, yDistance)) + (10 * Mathf.Abs(xDistance - yDistance));
    }
}
