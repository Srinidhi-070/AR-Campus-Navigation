using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class NodeConnectionVisualizer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private Transform pointA;
    private Transform pointB;

    public void Initialize(Transform from, Transform to)
    {
        pointA = from;
        pointB = to;

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.startWidth    = 0.01f;
        lineRenderer.endWidth      = 0.01f;
    }

    void Update()
    {
        if (pointA == null || pointB == null) return;

        lineRenderer.SetPosition(0, pointA.position);
        lineRenderer.SetPosition(1, pointB.position);
    }
}
