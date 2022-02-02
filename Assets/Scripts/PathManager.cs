using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Pathfinding))]
public class PathManager : MonoBehaviour
{
    Queue<PathRequest> requestQueue = new Queue<PathRequest>();
    PathRequest currentRequest;

    private static PathManager instance;

    private Pathfinding pathfinding;
    private bool isProcessingPath;

    private void Awake()
    {
        if (instance != null)
            Destroy(this.gameObject);
        else
            instance = this;

        this.pathfinding = GetComponent<Pathfinding>();
    }

    /// <summary>
    /// Enqueues the path request, and tries to start the next path
    /// </summary>
    /// <param name="pathStart">World space position of the start of the path</param>
    /// <param name="pathEnd">World space position of the end of the path</param>
    /// <param name="callback">The function to be executed when the path is found</param>
    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
    {
        var newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.requestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    /// <summary>
    /// If not currently processing a path, dequeue the next path and start processing
    /// </summary>
    private void TryProcessNext()
    {
        if (this.isProcessingPath || this.requestQueue.Count <= 0)
            return;

        this.isProcessingPath = true;
        this.currentRequest = this.requestQueue.Dequeue();
        this.pathfinding.StartFindPath(this.currentRequest.PathStart, this.currentRequest.PathEnd);
    }

    /// <summary>
    /// Call when the current path has finished being processed
    /// </summary>
    /// <param name="path">The waypoints of the path</param>
    /// <param name="success">Whether finding the path was successful</param>
    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        this.currentRequest.Callback(path, success);
        this.isProcessingPath = false;
        this.TryProcessNext();
    }
}
