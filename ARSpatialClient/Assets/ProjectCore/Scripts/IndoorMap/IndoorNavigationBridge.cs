using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Connects the IndoorPathfinding grid system to the AR PathVisualizer.
/// 
/// How it works:
/// 1. User picks Floor + Room from dropdown → OnNavigate() called
/// 2. IndoorPathfinding finds path through the grid
/// 3. Grid node positions converted to world-space
/// 4. PathVisualizer spawns AR arrows along that path
/// 5. DirectionManager generates turn-by-turn text
///
/// Setup:
/// - Attach to any GameObject in scene
/// - Assign all references in Inspector
/// - GridOrigin = the transform placed at grid cell (0,0) in real world
/// </summary>
public class IndoorNavigationBridge : MonoBehaviour
{
    [Header("Indoor Map")]
    [SerializeField] private MapManager       mapManager;
    [SerializeField] private IndoorPathfinding indoorPathfinding;

    [Header("AR Output")]
    [SerializeField] private PathVisualizer   pathVisualizer;
    [SerializeField] private DirectionManager directionManager;

    [Header("Grid World Anchor")]
    [Tooltip("Place this transform at the real-world position of grid cell (0,0)")]
    [SerializeField] private Transform gridOrigin;
    [SerializeField] private float     cellSize = 1f;

// ── Public API called by ModernUIBuilder ─────────────────────────────────

    /// <summary>
    /// Navigate to a named node on a specific floor map.
    /// mapName = "Floor_0", "Floor_1" etc (must match saved map names)
    /// nodeName = the name you gave the node with Click+N in the editor
    /// </summary>
    public void NavigateTo(string mapName, string nodeName)
    {
        if (mapManager == null || indoorPathfinding == null)
        {
            Debug.LogError("[IndoorNavBridge] MapManager or IndoorPathfinding not assigned.");
            return;
        }

        // Load the correct floor map
        mapManager.LoadMap(mapName);

        Node[,] grid = mapManager.GetMap(mapName);
        if (grid == null)
        {
            Debug.LogError($"[IndoorNavBridge] Map '{mapName}' not found.");
            return;
        }

        // Find start node = QR scanned location (required)
        Node startNode = null;
        if (QRLocationManager.Instance != null && QRLocationManager.Instance.HasLocation)
        {
            startNode = FindNodeByName(grid, QRLocationManager.Instance.CurrentNodeId);
        }
        if (startNode == null)
        {
            Debug.LogError("[IndoorNavBridge] Scan QR code first to set your location.");
            return;
        }

        // Find target node by name
        Node targetNode = FindNodeByName(grid, nodeName);
        if (targetNode == null)
        {
            Debug.LogWarning($"[IndoorNavBridge] Node '{nodeName}' not found on map '{mapName}'.");
            return;
        }

        // Set globals for IndoorPathfinding
        IndoorPathfinding.globalStartNode  = startNode;
        IndoorPathfinding.globalTargetNode = targetNode;
        mapManager.gridManager.startNode   = startNode;
        mapManager.gridManager.targetNode  = targetNode;

        // Run pathfinding
        indoorPathfinding.HandlePathfinding();

        // Get the resulting path
        List<Node> path = null;
        if (mapManager.mapPaths.ContainsKey(mapName))
            path = mapManager.mapPaths[mapName];

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning("[IndoorNavBridge] No path found.");
            return;
        }

        // Convert grid path to world positions
        List<Vector3> worldPath = GridPathToWorld(path);

        // Draw AR arrows
        DrawARArrows(worldPath);

        Debug.Log($"[IndoorNavBridge] Path found: {path.Count} nodes → {nodeName}");
    }

    // ── Private ──────────────────────────────────────────────────────────────

private Node FindNodeByName(Node[,] grid, string name)
    {
        string lower = name.Trim().ToLower();
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                Node n = grid[x, y];
                if (n != null && !string.IsNullOrEmpty(n.nodeName))
                {
                    if (n.nodeName.Trim().ToLower() == lower)
                        return n;
                }
            }
        }
        return null;
    }

    private List<Vector3> GridPathToWorld(List<Node> path)
    {
        Vector3 origin = gridOrigin != null ? gridOrigin.position : Vector3.zero;
        List<Vector3> result = new List<Vector3>();

        foreach (Node n in path)
        {
            Vector3 worldPos = origin + new Vector3(
                n.x * cellSize + cellSize * 0.5f,
                0.05f,
                n.y * cellSize + cellSize * 0.5f
            );
            result.Add(worldPos);
        }

        return result;
    }

    private void DrawARArrows(List<Vector3> worldPath)
    {
        if (pathVisualizer == null)
        {
            Debug.LogError("[IndoorNavBridge] PathVisualizer not assigned.");
            return;
        }

        // Convert world positions to fake NavigationNodes for PathVisualizer
        // We create temporary NavigationNode objects just for visualization
        List<NavigationNode> navNodes = new List<NavigationNode>();

        foreach (Vector3 pos in worldPath)
        {
            GameObject go = new GameObject("TempNavNode");
            go.transform.position = pos;
            NavigationNode nn = go.AddComponent<NavigationNode>();
            nn.Initialize("TEMP", "temp", pos);
            navNodes.Add(nn);
        }

        // Connect them in sequence
        for (int i = 0; i < navNodes.Count - 1; i++)
            navNodes[i].ConnectTo(navNodes[i + 1]);

        // Draw arrows
        pathVisualizer.DrawPath(navNodes);

        // Clean up temp GameObjects after a frame
        StartCoroutine(CleanupAfterFrame(navNodes));
    }

    private System.Collections.IEnumerator CleanupAfterFrame(List<NavigationNode> nodes)
    {
        yield return null;
        foreach (var n in nodes)
            if (n != null) Destroy(n.gameObject);
    }
}
