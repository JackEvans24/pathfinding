using System;
using UnityEngine;

public struct PathRequest
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
