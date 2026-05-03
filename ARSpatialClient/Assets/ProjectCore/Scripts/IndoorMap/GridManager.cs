using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GridManager : MonoBehaviour
{
    public GameObject wallPrefab;
    Dictionary<Vector2Int, GameObject> wallObjects = new Dictionary<Vector2Int, GameObject>();
    public int width = 60;
    public int height = 60;
    public float cellSize = 1f;

    public Node[,] grid;
    public Node startNode;
    public Node targetNode;
    public List<Node> path;
    public Node selectedNode;
    private int currentWidth;
    private int currentHeight;

    void Awake()
    {
        // Don't call UpdateWalls here — wallPrefab may not be assigned yet.
        // MapManager.LoadMap() calls UpdateWalls after assignment.
        ClearWalls();
        currentWidth  = width;
        currentHeight = height;
        CreateGrid();
    }

    public void UpdateWalls()
    {
        if (grid == null) return;
        if (wallPrefab == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];
                Vector2Int pos = new Vector2Int(x, y);

                if (node.nodeType == NodeType.Obstacle)
                {
                    if (!wallObjects.ContainsKey(pos))
                    {
                        GameObject wall = Instantiate(
                            wallPrefab,
                            new Vector3(x + 0.5f, 1f, y + 0.5f),
                            Quaternion.identity
                        );

                        wallObjects[pos] = wall;
                    }
                }
                else
                {
                    if (wallObjects.ContainsKey(pos))
                    {
                        #if UNITY_EDITOR
                        if (!Application.isPlaying)
                            DestroyImmediate(wallObjects[pos]);
                        else
                        #endif
                            Destroy(wallObjects[pos]);
                        wallObjects.Remove(pos);
                    }
                }
            }
        }
    }

    public void ResetGrid()
    {
        ClearWalls();
        currentWidth = width;
        currentHeight = height;
        CreateGrid();
        UpdateWalls();
    }

    void OnDrawGizmos()
    {
        if (grid == null || currentWidth != width || currentHeight != height)
        {
            CreateGrid();
            currentWidth = width;
            currentHeight = height;
        }

        if (grid == null) return;

        string currentMapName = MapManager.instance != null ? MapManager.instance.currentMapName : string.Empty;

        // 🔹 FUNCTION FOR CONNECTION COLOR
        Color GetColorFromID(int id)
        {
            Random.InitState(id);
            return new Color(Random.value, Random.value, Random.value);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = grid[x, y];

                if (node == null) continue;

                // 🔹 BASE TILE COLOR
                if (node.nodeType == NodeType.Obstacle)
                    Gizmos.color = Color.black;

                else if (node.nodeType == NodeType.LiftEntry)
                    Gizmos.color = new Color(0.29f, 0f, 0.51f);

                else if (node.nodeType == NodeType.StairEntry)
                    Gizmos.color = new Color(1f, 0.1f, 0.1f);

                else if (node.nodeType == NodeType.RoomDoor)
                    Gizmos.color = Color.cyan;

                else
                    Gizmos.color = Color.gray;

                Gizmos.DrawCube(
                    new Vector3(x + 0.5f, 0.002f, y + 0.5f),
                    new Vector3(1f, 0.001f, 1f)
                );

                Gizmos.color = Color.black;
                Gizmos.DrawWireCube(
                    new Vector3(x + 0.5f, 0.021f, y + 0.5f),
                    new Vector3(1f, 0.001f, 1f)
                );

                // 🔹 CONNECTION NODE (SMALL COLORED)
                if (!string.IsNullOrEmpty(node.connectedMap))
                {
                    Gizmos.color = GetColorFromID(node.connectionID);

                    Gizmos.DrawCube(
                        new Vector3(x + 0.5f, 0.6f, y + 0.5f),
                        new Vector3(0.35f, 0.1f, 0.35f)
                    );
                }

                // 🔹 NODE NAME DISPLAY
    #if UNITY_EDITOR
                if (!string.IsNullOrEmpty(node.nodeName))
                {
                    UnityEditor.Handles.Label(
                        new Vector3(x + 0.5f, 0.8f, y + 0.5f),
                        node.nodeName
                    );
                }
    #endif
            }
        }

        // 🔹 SELECTED NODE
        if (selectedNode != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(
                new Vector3(selectedNode.x + 0.5f, 0.5f, selectedNode.y + 0.5f),
                Vector3.one * 0.7f
            );
        }

        // 🔹 START NODE
        if (startNode != null && startNode.mapName == currentMapName)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawCube(
                new Vector3(startNode.x + 0.5f, 0.5f, startNode.y + 0.5f),
                Vector3.one * 0.6f
            );
        }

        // 🔹 TARGET NODE
        if (targetNode != null && targetNode.mapName == currentMapName)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawCube(
                new Vector3(targetNode.x + 0.5f, 0.5f, targetNode.y + 0.5f),
                Vector3.one * 0.6f
            );
        }

        // 🔹 PATH (FILTERED BY MAP)
        if (path != null)
        {
            Gizmos.color = Color.green;

            foreach (Node node in path)
            {
                if (node == null) continue;

                // 🔥 STRICT FILTER
                if (node.mapName != currentMapName)
                    continue;

                Gizmos.DrawCube(
                    new Vector3(node.x + 0.5f, 0.5f, node.y + 0.5f),
                    Vector3.one * 0.5f
                );
            }
        }
    }

    void CreateGrid()
    {
        if (width <= 0 || height <= 0) return;
        grid = new Node[width, height];

        string currentMapName = MapManager.instance != null ? MapManager.instance.currentMapName : string.Empty;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Node node = new Node(x, y);
                node.mapName = currentMapName;
                grid[x, y] = node;
            }
        }
    }

    public Node GetNode(int x, int y)
    {
        return grid[x, y];
    }

    public void ClearWalls()
    {
        foreach (var wall in wallObjects.Values)
        {
            #if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(wall);
            else
            #endif
                Destroy(wall);
        }

        wallObjects.Clear();
    }
}