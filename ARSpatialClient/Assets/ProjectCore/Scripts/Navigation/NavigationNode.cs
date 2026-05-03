using System.Collections.Generic;
using UnityEngine;

public class NavigationNode : MonoBehaviour
{
    public string NodeId      { get; private set; }
    public string DisplayName { get; private set; }
    public Vector3 Position   { get; private set; }

    // kept for legacy lookup compatibility
    public string LocationName => NodeId;

    private readonly List<NavigationNode> m_Connected = new List<NavigationNode>();
    public IReadOnlyList<NavigationNode> ConnectedNodes => m_Connected;

    public void Initialize(string id, string displayName, Vector3 position)
    {
        NodeId      = id.ToUpper();
        DisplayName = displayName;
        Position    = position;
        transform.position = position;

        Debug.Log($"[Node] {NodeId} | {DisplayName} | {Position}");
    }

    public void UpdatePosition(Vector3 worldPosition)
    {
        Position           = worldPosition;
        transform.position = worldPosition;
    }

    public void ConnectTo(NavigationNode other)
    {
        if (other == null || other == this) return;
        if (m_Connected.Contains(other)) return;

        m_Connected.Add(other);

        // bidirectional — only add back-link if not already there
        if (!other.m_Connected.Contains(this))
            other.m_Connected.Add(this);

        Debug.Log($"[Graph] {NodeId} <-> {other.NodeId}");
    }
}
