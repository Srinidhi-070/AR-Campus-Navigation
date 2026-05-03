using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds the full navigation graph at startup from nodes.json via LocationRegistry.
/// Nodes are laid out in a circle for Editor testing.
/// On Android, call SetNodePosition() to anchor nodes to real AR positions.
/// </summary>
public class NavigationManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject nodePrefab;

    [Header("Settings")]
    [SerializeField] private Material lineMaterial;

    private readonly Dictionary<string, NavigationNode> m_NodeMap  = new Dictionary<string, NavigationNode>();
    private readonly List<GameObject>                   m_LineObjs = new List<GameObject>();

    void Start()
    {
        if (AppController.Instance == null)
        {
            Debug.LogError("[NavigationManager] AppController.Instance is null.");
            return;
        }

        if (AppController.Instance.Locations == null)
        {
            Debug.LogError("[NavigationManager] LocationRegistry is null.");
            return;
        }

        var locations = new System.Collections.Generic.List<LocationData>(
            AppController.Instance.Locations.GetAllLocations());

        if (locations.Count == 0)
        {
            Debug.LogWarning("[NavigationManager] nodes.json is empty. Draw a floor map and Export first.");
            return;
        }

        BuildGraph();
        // Graph is hidden by default — only shown when navigation is active
        SetGraphVisible(false);
    }

    // ── Public API ───────────────────────────────────────────────────────────

    public bool IsGraphBuilt => m_NodeMap.Count > 0;

    public NavigationNode FindNodeById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        m_NodeMap.TryGetValue(id.ToUpper(), out NavigationNode node);
        return node;
    }

    public List<NavigationNode> GetAllNodes() => new List<NavigationNode>(m_NodeMap.Values);

    public void SetNodePosition(string id, Vector3 worldPosition)
    {
        NavigationNode node = FindNodeById(id);
        if (node == null) return;
        node.UpdatePosition(worldPosition);
    }

    public void ClearAllNodes()
    {
        foreach (var node in m_NodeMap.Values)
            if (node != null) Destroy(node.gameObject);
        m_NodeMap.Clear();

        foreach (var line in m_LineObjs)
            if (line != null) Destroy(line);
        m_LineObjs.Clear();
    }

    // Called by NavigationManager itself — hide all node spheres and lines
    public void SetGraphVisible(bool visible)
    {
        foreach (var node in m_NodeMap.Values)
            if (node != null) node.gameObject.SetActive(visible);
        foreach (var line in m_LineObjs)
            if (line != null) line.SetActive(visible);
    }

    public void RebuildGraph()
    {
        ClearAllNodes();
        BuildGraph();
    }

    // ── Private ──────────────────────────────────────────────────────────────

    private void BuildGraph()
    {
        var allLocations = new List<LocationData>(AppController.Instance.Locations.GetAllLocations());

        // Pass 1: create all node GameObjects
        for (int i = 0; i < allLocations.Count; i++)
        {
            LocationData loc = allLocations[i];
            Vector3 pos = new Vector3(loc.x, loc.y, loc.z);
            SpawnNode(loc.id, loc.displayName, pos);
        }

        // Pass 2: wire neighbor connections
        foreach (LocationData loc in allLocations)
        {
            if (loc.neighbors == null) continue;

            NavigationNode a = FindNodeById(loc.id);
            if (a == null) continue;

            foreach (string neighborId in loc.neighbors)
            {
                NavigationNode b = FindNodeById(neighborId);
                if (b == null) continue;

                a.ConnectTo(b);
                SpawnConnectionLine(a.transform, b.transform);
            }
        }

        Debug.Log($"[NavigationManager] Graph built: {m_NodeMap.Count} nodes.");
    }

    private NavigationNode SpawnNode(string id, string displayName, Vector3 position)
    {
        if (nodePrefab == null)
        {
            Debug.LogError("[NavigationManager] nodePrefab not assigned in Inspector.");
            return null;
        }

        GameObject obj = Instantiate(nodePrefab, position, Quaternion.identity);
        obj.name = "Node_" + id;

        NavigationNode node = obj.GetComponent<NavigationNode>();
        if (node == null)
        {
            Debug.LogError("[NavigationManager] NavigationNode component missing on nodePrefab.");
            Destroy(obj);
            return null;
        }

        node.Initialize(id, displayName, position);
        m_NodeMap[id.ToUpper()] = node;
        return node;
    }

    private void SpawnConnectionLine(Transform a, Transform b)
    {
        GameObject obj = new GameObject($"Line_{a.name}_{b.name}");
        m_LineObjs.Add(obj);

        LineRenderer lr  = obj.AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.startWidth    = 0.05f;
        lr.endWidth      = 0.05f;
        lr.useWorldSpace = true;

        if (lineMaterial != null)
            lr.material = lineMaterial;

        NodeConnectionVisualizer vis = obj.AddComponent<NodeConnectionVisualizer>();
        vis.Initialize(a, b);
    }

}
