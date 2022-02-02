using System.Collections;
using UnityEngine;

public class Unit : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float speed = 5f;

    Vector3[] path;
    int targetIndex;

    void Start()
    {
        PathManager.RequestPath(this.transform.position, this.target.position, this.OnPathFound);
    }

    /// <summary>
    /// Callback for <see cref="PathManager.RequestPath"/>
    /// </summary>
    /// <param name="waypoints">World space coordinates along the returned path</param>
    /// <param name="success">Whether path resolution was successful</param>
    private void OnPathFound(Vector3[] waypoints, bool success)
    {
        if (!success)
            return;

        this.path = waypoints;

        StopCoroutine("FollowPath");
        StartCoroutine("FollowPath");
    }

    /// <summary>
    /// Coroutine which moves this object's <see cref="Transform"/> towards the next waypoint in the path
    /// </summary>
    private IEnumerator FollowPath()
    {
        if (path == null || path.Length == 0)
            yield break;

        this.targetIndex = 0;
        var currentWaypoint = this.path[this.targetIndex];

        while (true)
        {
            if (this.transform.position == currentWaypoint)
            {
                this.targetIndex++;
                if (this.targetIndex >= this.path.Length)
                {
                    this.path = null;
                    yield break;
                }

                currentWaypoint = this.path[this.targetIndex];
            }

            transform.position = Vector3.MoveTowards(this.transform.position, currentWaypoint, this.speed * Time.deltaTime);
            yield return null;
        }
    }

    private void OnDrawGizmos()
    {
        if (this.path == null)
            return;

        for (int i = this.targetIndex; i < path.Length; i++)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawCube(this.path[i], Vector3.one * 0.1f);

            if (i == this.targetIndex)
                Gizmos.DrawLine(this.transform.position, this.path[i]);
            else
                Gizmos.DrawLine(this.path[i - 1], this.path[i]);
        }
    }
}
