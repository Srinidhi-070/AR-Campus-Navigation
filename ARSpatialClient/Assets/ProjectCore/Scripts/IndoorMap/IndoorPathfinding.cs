using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class IndoorPathfinding : MonoBehaviour
{
    static int connectionCounter = 1;
    public static Node globalStartNode;
    public static Node globalTargetNode;
    Vector2Int connectionStartPos;
    string connectionStartMap = "";
    Node selectedNode;
    public string nodeLabel = "";
    public GridManager gridManager;
    public Node startNode;
    public Node targetNode;
    int lockedConnectionID = -1;

    bool IsSameNode(Node a, Node b)
    {
        if (a == null || b == null) return false;
        return a.x == b.x && a.y == b.y && a.mapName == b.mapName;
    }

    public List<Node> path = new List<Node>();

    void Update()
    {
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            SelectNode();

            if (Keyboard.current.sKey.isPressed && selectedNode != null)
            {
                selectedNode.mapName = MapManager.instance.currentMapName;
                globalStartNode = selectedNode;
                startNode = selectedNode;
                gridManager.startNode = startNode;
            }
            else if (Keyboard.current.tKey.isPressed && selectedNode != null)
            {
                selectedNode.mapName = MapManager.instance.currentMapName;
                globalTargetNode = selectedNode;
                targetNode = selectedNode;
                gridManager.targetNode = targetNode;
                if (startNode != null) HandlePathfinding();
            }
            else if (Keyboard.current.lKey.isPressed && selectedNode != null)
            {
                selectedNode.nodeType = NodeType.LiftEntry;
                selectedNode.isWalkable = true;
            }
            else if (Keyboard.current.eKey.isPressed && selectedNode != null)
            {
                selectedNode.nodeType = NodeType.StairEntry;
                selectedNode.isWalkable = true;
            }
            else if (Keyboard.current.rKey.isPressed && selectedNode != null)
            {
                selectedNode.nodeType = NodeType.RoomDoor;
                selectedNode.isWalkable = true;
            }
            else if (Keyboard.current.cKey.isPressed && selectedNode != null)
            {
                ConnectNodePrompt();
            }
            else if (Keyboard.current.kKey.isPressed && selectedNode != null)
            {
                HandleConnectionNode();
            }
            else if (Keyboard.current.nKey.isPressed && selectedNode != null)
            {
                // Node naming now handled by FloorMapEditor in Edit mode
                Debug.Log($"[IndoorPathfinding] Select node at ({selectedNode.x}, {selectedNode.y}) and use FloorMapEditor to name it.");
            }
        }

        if (Mouse.current.rightButton.wasPressedThisFrame)
            ToggleObstacle();
    }

    void SelectNode()
    {
        if (gridManager == null || gridManager.grid == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            int x = Mathf.FloorToInt(hit.point.x);
            int y = Mathf.FloorToInt(hit.point.z);

            if (x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
            {
                Node node = gridManager.grid[x, y];
                if (node != null)
                {
                    selectedNode = node;
                    gridManager.selectedNode = selectedNode;
                }
            }
        }
    }

    void ToggleObstacle()
    {
        if (gridManager == null || gridManager.grid == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            int x = Mathf.FloorToInt(hit.point.x);
            int y = Mathf.FloorToInt(hit.point.z);

            if (x >= 0 && x < gridManager.width && y >= 0 && y < gridManager.height)
            {
                Node node = gridManager.grid[x, y];
                if (node != null)
                {
                    node.isWalkable = !node.isWalkable;
                    node.nodeType = node.isWalkable ? NodeType.Normal : NodeType.Obstacle;
                    gridManager.UpdateWalls();
                }
            }
        }
    }

    public void ResetNodes(Node[,] grid)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                grid[x, y].gCost = 0;
                grid[x, y].hCost = 0;
                grid[x, y].parent = null;
            }
    }

    void FindPath(Node startNode, Node targetNode)
    {
        ResetNodes(gridManager.grid);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        openSet.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
                if (openSet[i].fCost < current.fCost ||
                    openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost)
                    current = openSet[i];

            openSet.Remove(current);
            closedSet.Add(current);

            if (IsSameNode(current, targetNode))
            {
                RetracePath(startNode, targetNode);
                gridManager.path = path;
                MapManager.instance.mapPaths[startNode.mapName] = new List<Node>(path);
                Debug.Log("PATH FOUND");
                return;
            }

            foreach (Node neighbour in GetNeighbours(current))
            {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour)) continue;

                int newCost = current.gCost + GetDistance(current, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }
        Debug.Log("NO PATH FOUND");
    }

    List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();
        int x = node.x, y = node.y;
        if (x > 0) neighbours.Add(gridManager.grid[x - 1, y]);
        if (x < gridManager.width - 1) neighbours.Add(gridManager.grid[x + 1, y]);
        if (y > 0) neighbours.Add(gridManager.grid[x, y - 1]);
        if (y < gridManager.height - 1) neighbours.Add(gridManager.grid[x, y + 1]);
        return neighbours;
    }

    List<Node> FindPathExternal(Node startNode, Node targetNode)
    {
        ResetNodes(gridManager.grid);

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
                if (openSet[i].fCost < current.fCost ||
                    openSet[i].fCost == current.fCost && openSet[i].hCost < current.hCost)
                    current = openSet[i];

            openSet.Remove(current);
            closedSet.Add(current);

            if (IsSameNode(current, targetNode))
            {
                List<Node> result = new List<Node>();
                Node temp = targetNode;
                while (temp != null)
                {
                    result.Add(temp);
                    if (IsSameNode(temp, startNode)) break;
                    temp = temp.parent;
                }
                result.Reverse();
                if (result.Count > 0 && result[0] != startNode)
                    result.Insert(0, startNode);
                return result;
            }

            foreach (Node neighbour in GetNeighbours(current))
            {
                if (!neighbour.isWalkable || closedSet.Contains(neighbour)) continue;
                int newCost = current.gCost + GetDistance(current, neighbour);
                if (newCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = current;
                    if (!openSet.Contains(neighbour)) openSet.Add(neighbour);
                }
            }
        }
        return null;
    }

    int GetDistance(Node a, Node b) =>
        Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);

    void RetracePath(Node startNode, Node endNode)
    {
        path = new List<Node>();
        Node current = endNode;
        while (!IsSameNode(current, startNode))
        {
            path.Add(current);
            if (current.parent == null)
            {
                Debug.LogError("RetracePath: broken chain.");
                path.Clear();
                return;
            }
            current = current.parent;
        }
        path.Reverse();
    }

    void OnDrawGizmos()
    {
        if (MapManager.instance == null) return;

        if (startNode != null && startNode.mapName == MapManager.instance.currentMapName)
        { Gizmos.color = Color.blue; Gizmos.DrawCube(new Vector3(startNode.x + 0.5f, 0.5f, startNode.y + 0.5f), Vector3.one * 0.6f); }

        if (targetNode != null && targetNode.mapName == MapManager.instance.currentMapName)
        { Gizmos.color = Color.red; Gizmos.DrawCube(new Vector3(targetNode.x + 0.5f, 0.5f, targetNode.y + 0.5f), Vector3.one * 0.6f); }

        if (path != null)
        {
            Gizmos.color = Color.green;
            foreach (Node n in path)
                Gizmos.DrawCube(new Vector3(n.x + 0.5f, 0.5f, n.y + 0.5f), Vector3.one * 0.5f);
        }
    }

    void ConnectNodePrompt()
    {
        if (selectedNode == null) return;
        string map = MapManager.instance.currentMapName;
        selectedNode.connectedMap = map;
        selectedNode.connectedNode = new Vector2Int(selectedNode.x, selectedNode.y);
    }

    void HandleConnectionNode()
    {
        if (selectedNode == null) return;

        string currentMap = MapManager.instance.currentMapName;
        string mapAName = connectionStartMap;

        int floorA = MapManager.instance.mapToFloor.ContainsKey(mapAName) ? MapManager.instance.mapToFloor[mapAName] : -1;
        int floorB = MapManager.instance.mapToFloor.ContainsKey(currentMap) ? MapManager.instance.mapToFloor[currentMap] : -1;
        string buildingA = MapManager.instance.mapToBuilding.ContainsKey(mapAName) ? MapManager.instance.mapToBuilding[mapAName] : "";
        string buildingB = MapManager.instance.mapToBuilding.ContainsKey(currentMap) ? MapManager.instance.mapToBuilding[currentMap] : "";

        if (selectedNode.nodeType == NodeType.LiftEntry)
        {
            if (buildingA != "" && buildingA != buildingB) { Debug.LogError("Lift: same building only!"); connectionStartMap = ""; return; }

            if (string.IsNullOrEmpty(connectionStartMap))
            {
                selectedNode.connectionID = connectionCounter++;
                connectionStartMap = currentMap;
                connectionStartPos = new Vector2Int(selectedNode.x, selectedNode.y);
                return;
            }

            Node[,] mapA = MapManager.instance.GetMap(connectionStartMap);
            if (mapA == null) return;
            selectedNode.connectionID = mapA[connectionStartPos.x, connectionStartPos.y].connectionID;
            return;
        }

        if (!string.IsNullOrEmpty(connectionStartMap) && floorA != floorB) { Debug.LogError("Same floor only!"); connectionStartMap = ""; return; }
        if (selectedNode.nodeType == NodeType.StairEntry && buildingA != buildingB) { Debug.LogError("Stairs: same building only!"); connectionStartMap = ""; return; }

        if (string.IsNullOrEmpty(connectionStartMap))
        {
            connectionStartPos = new Vector2Int(selectedNode.x, selectedNode.y);
            connectionStartMap = currentMap;
        }
        else
        {
            Node[,] mapA = MapManager.instance.GetMap(connectionStartMap);
            Node[,] mapB = MapManager.instance.GetMap(currentMap);
            if (mapA == null || mapB == null) { Debug.LogError("Map null."); return; }

            Node nodeA = mapA[connectionStartPos.x, connectionStartPos.y];
            Node nodeB = mapB[selectedNode.x, selectedNode.y];
            int id = connectionCounter++;

            nodeA.connectedMap = currentMap; nodeA.connectedNode = new Vector2Int(nodeB.x, nodeB.y); nodeA.connectionID = id;
            nodeB.connectedMap = connectionStartMap; nodeB.connectedNode = new Vector2Int(nodeA.x, nodeA.y); nodeB.connectionID = id;

            connectionStartPos = new Vector2Int(-1, -1);
            connectionStartMap = "";
        }
    }

    Node FindNearestConnection(Node start)
    {
        Node nearest = null;
        int shortest = int.MaxValue;
        Node[,] map = MapManager.instance.GetMap(start.mapName);
        if (map == null) return null;

        for (int x = 0; x < map.GetLength(0); x++)
            for (int y = 0; y < map.GetLength(1); y++)
            {
                Node node = map[x, y];
                if (lockedConnectionID > 0) { if (node.connectionID != lockedConnectionID) continue; }
                else { if (string.IsNullOrEmpty(node.connectedMap) && node.connectionID <= 0) continue; }
                int dist = GetDistance(start, node);
                if (dist < shortest) { shortest = dist; nearest = node; }
            }
        return nearest;
    }

    public void HandlePathfinding()
    {
        if (globalStartNode == null || globalTargetNode == null) return;
        lockedConnectionID = -1;

        if (globalStartNode.mapName == globalTargetNode.mapName)
        { FindPath(globalStartNode, globalTargetNode); return; }

        Node currentStart = globalStartNode;
        HashSet<string> visited = new HashSet<string>();

        while (currentStart.mapName != globalTargetNode.mapName)
        {
            if (visited.Contains(currentStart.mapName)) { Debug.LogError("Circular map connection."); return; }
            visited.Add(currentStart.mapName);

            Node connection = FindNearestConnection(currentStart);
            if (connection == null) { Debug.Log("No connection found."); return; }

            FindPath(currentStart, connection);
            if (connection.connectionID > 0 && lockedConnectionID == -1) lockedConnectionID = connection.connectionID;
            if (string.IsNullOrEmpty(connection.connectedMap)) { Debug.Log("No target map."); return; }

            Node[,] nextMap = MapManager.instance.GetMap(connection.connectedMap);
            if (nextMap == null) { Debug.Log("Next map null."); return; }

            Vector2Int pos = connection.connectedNode;
            if (pos.x < 0 || pos.y < 0 || pos.x >= nextMap.GetLength(0) || pos.y >= nextMap.GetLength(1)) { Debug.Log("Invalid pos."); return; }

            currentStart = nextMap[pos.x, pos.y];
        }

        List<Node> finalPath = FindPathExternal(currentStart, globalTargetNode);
        if (finalPath == null || finalPath.Count == 0) { Debug.Log("Final path failed."); return; }
        MapManager.instance.mapPaths[globalTargetNode.mapName] = finalPath;
    }
}
