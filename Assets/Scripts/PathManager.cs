using System;
using System.Collections;
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

    public static void RequestPath(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
    {
        var newRequest = new PathRequest(pathStart, pathEnd, callback);
        instance.requestQueue.Enqueue(newRequest);
        instance.TryProcessNext();
    }

    private void TryProcessNext()
    {
        if (this.isProcessingPath || this.requestQueue.Count <= 0)
            return;

        this.isProcessingPath = true;
        this.currentRequest = this.requestQueue.Dequeue();
        this.pathfinding.StartFindPath(this.currentRequest.PathStart, this.currentRequest.PathEnd);
    }

    public void FinishedProcessingPath(Vector3[] path, bool success)
    {
        this.currentRequest.Callback(path, success);
        this.isProcessingPath = false;
        this.TryProcessNext();
    }

    struct PathRequest
    {
        public Vector3 PathStart;
        public Vector3 PathEnd;
        public Action<Vector3[], bool> Callback;

        public PathRequest(Vector3 pathStart, Vector3 pathEnd, Action<Vector3[], bool> callback)
        {
            this.PathStart = pathStart;
            this.PathEnd = pathEnd;
            this.Callback = callback;
        }
    } 
}
