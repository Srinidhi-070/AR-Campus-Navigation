using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;


public class MapManager : MonoBehaviour
{
    public static MapManager instance;
    public GridManager gridManager;
    public string currentMapName = "Floor1";
    Dictionary<string, Node[,]> maps = new Dictionary<string, Node[,]>();
    public Dictionary<string, List<Node>> mapPaths = new Dictionary<string, List<Node>>();
    public Dictionary<string, string> mapToBuilding = new Dictionary<string, string>();
    public Dictionary<string, int> mapToFloor = new Dictionary<string, int>();
    public Dictionary<string, List<string>> buildings = new Dictionary<string, List<string>>();

    void Awake()
    {
        instance = this;

        LoadBuildingsFromFile();
        LoadAllMapsFromFiles();
    }

    public void SaveCurrentMap()
    {
        if (gridManager == null || gridManager.grid == null)
        {
            Debug.LogError("Cannot save: GridManager or grid is null");
            return;
        }

        if (string.IsNullOrEmpty(currentMapName))
        {
            Debug.LogError("Cannot save: currentMapName is empty");
            return;
        }

        Node[,] newGrid = new Node[gridManager.width, gridManager.height];

        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                Node original = gridManager.grid[x, y];
                
                if (original == null)
                {
                    Debug.LogError($"Null node at ({x}, {y}) - cannot save");
                    return;
                }

                Node copy = new Node(original.x, original.y);
                copy.mapName = currentMapName;

                copy.isWalkable = original.isWalkable;
                copy.nodeType = original.nodeType;
                copy.connectedMap = original.connectedMap;
                copy.connectedNode = original.connectedNode;
                copy.connectionID = original.connectionID;
                copy.nodeName = original.nodeName;

                newGrid[x, y] = copy;
            }
        }

        maps[currentMapName] = newGrid;
        SaveMapToFile(currentMapName);

        Debug.Log("Map saved: " + currentMapName);
    }

    public void LoadMap(string mapName)
    {
        if (!maps.ContainsKey(mapName))
        {
            Debug.LogError("Map not found: " + mapName);
            return;
        }

        Node[,] map = maps[mapName];

        if (map == null)
        {
            Debug.LogError("Map grid is null: " + mapName);
            return;
        }

        gridManager.ClearWalls();

        //DIRECT REFERENCE (NO COPY)
        gridManager.grid = map;
        gridManager.width = map.GetLength(0);
        gridManager.height = map.GetLength(1);
        
        // Only update walls if wallPrefab is assigned
        if (gridManager.wallPrefab != null)
            gridManager.UpdateWalls();

        currentMapName = mapName;

        gridManager.selectedNode = null;

        // Only access IndoorPathfinding if in Play Mode
        if (Application.isPlaying)
        {
            var pathfinding = FindObjectOfType<IndoorPathfinding>();
            if (pathfinding != null)
            {
                //Restore start node ONLY if it belongs to this map
                if (IndoorPathfinding.globalStartNode != null &&
                    IndoorPathfinding.globalStartNode.mapName == mapName)
                {
                    gridManager.startNode = IndoorPathfinding.globalStartNode;
                    pathfinding.startNode = IndoorPathfinding.globalStartNode;
                }
                else
                {
                    gridManager.startNode = null;
                }

                //Restore target node ONLY if it belongs to this map
                if (IndoorPathfinding.globalTargetNode != null &&
                    IndoorPathfinding.globalTargetNode.mapName == mapName)
                {
                    gridManager.targetNode = IndoorPathfinding.globalTargetNode;
                    pathfinding.targetNode = IndoorPathfinding.globalTargetNode;
                }
                else
                {
                    gridManager.targetNode = null;
                }
                
                // load stored path if exists
                if (mapPaths.ContainsKey(mapName))
                {
                    gridManager.path = mapPaths[mapName];
                }
                else
                {
                    gridManager.path = new List<Node>();
                }

                // also clear Pathfinding runtime copy
                pathfinding.path = gridManager.path;
                pathfinding.ResetNodes(gridManager.grid);
            }
        }
        else
        {
            // In Edit Mode, just clear these
            gridManager.startNode = null;
            gridManager.targetNode = null;
            gridManager.path = new List<Node>();
        }

        Debug.Log("Map loaded: " + mapName);
    }

    public void CreateNewMap(string mapName)
    {
        if (string.IsNullOrEmpty(mapName))
            return;

        currentMapName = mapName;
        mapToFloor[mapName] = 0;
        RegisterMapToBuilding(mapName, "Default");

        gridManager.ResetGrid();
        gridManager.startNode = null;
        gridManager.targetNode = null;
        gridManager.selectedNode = null;
        gridManager.path = null;

        // Store the fresh empty grid so it can be saved
        SaveCurrentMap();
        SaveBuildingsToFile();

        Debug.Log("New map created: " + mapName);
    }

    public void CopyCurrentMap(string newMapName)
    {
        if (string.IsNullOrEmpty(newMapName))
            return;

        if (gridManager == null || gridManager.grid == null)
        {
            Debug.LogError("Cannot copy: no active map loaded.");
            return;
        }

        string building = mapToBuilding.ContainsKey(currentMapName) ? mapToBuilding[currentMapName] : "Default";
        int floor = mapToFloor.ContainsKey(currentMapName) ? mapToFloor[currentMapName] : 0;

        // Change active name
        currentMapName = newMapName;
        mapToFloor[newMapName] = floor;
        RegisterMapToBuilding(newMapName, building);

        // SaveCurrentMap creates a deep copy of the grid, sets the mapName properly, and saves it
        SaveCurrentMap();
        SaveBuildingsToFile();

        // Reload the new map into the active grid so we stop pointing at the old map's memory
        LoadMap(newMapName);

        // Clear out old unique node names and cross-floor connections on the new map
        for (int x = 0; x < gridManager.width; x++)
        {
            for (int y = 0; y < gridManager.height; y++)
            {
                if (gridManager.grid[x, y] != null)
                {
                    gridManager.grid[x, y].nodeName = "";
                    gridManager.grid[x, y].connectedMap = "";
                    gridManager.grid[x, y].connectedNode = Vector2Int.zero;
                }
            }
        }
        
        // Save the cleaned-up copy
        SaveCurrentMap();

        Debug.Log("Map duplicated to: " + newMapName);
    }

    public void DeleteMap(string mapName)
    {
        // Always clear any currently spawned wall visuals.
        // This fixes cases where a deleted map wasn't the active one,
        // so gridManager.ResetGrid() never ran.
        if (gridManager != null)
            gridManager.ClearWalls();

        if (maps.ContainsKey(mapName))
            maps.Remove(mapName);

        // Remove from building index
        if (mapToBuilding.ContainsKey(mapName))
        {
            string building = mapToBuilding[mapName];
            if (buildings.ContainsKey(building))
                buildings[building].Remove(mapName);
            mapToBuilding.Remove(mapName);
        }

        mapToFloor.Remove(mapName);
        mapPaths.Remove(mapName);

        // Delete the actual file from Resources/Maps
        string path = GetMapPath(mapName);
        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Deleted map file: {path}");
        }

        SaveBuildingsToFile();

        if (currentMapName == mapName)
        {
            gridManager.ResetGrid();
            currentMapName = "";
            gridManager.startNode = null;
            gridManager.targetNode = null;
            gridManager.path = null;
        }

        Debug.Log("Map deleted: " + mapName);
    }

    public List<string> GetAllMaps()
    {
        List<string> mapList = new List<string>();

        foreach (var key in maps.Keys)
        {
            if (!string.IsNullOrEmpty(key))
                mapList.Add(key);
        }

        mapList.Sort();
        return mapList;
    }

public Node[,] GetMap(string mapName)
    {
        if (maps.ContainsKey(mapName))
            return maps[mapName];

        return null;
    }

    SerializableMapData ConvertToSerializable(string mapName, Node[,] grid)
    {
        SerializableMapData data = new SerializableMapData();
        data.mapName = mapName;
        data.width = grid.GetLength(0);
        data.height = grid.GetLength(1);
        data.buildingName = mapToBuilding.ContainsKey(mapName) ? mapToBuilding[mapName] : "Default";
        data.floorNumber = mapToFloor.ContainsKey(mapName) ? mapToFloor[mapName] : 0;

        for (int x = 0; x < data.width; x++)
        {
            for (int y = 0; y < data.height; y++)
            {
                Node n = grid[x, y];

                SerializableNode sn = new SerializableNode();
                sn.x = n.x;
                sn.y = n.y;
                sn.isWalkable = n.isWalkable;
                sn.nodeType = (int)n.nodeType;
                sn.connectedMap = n.connectedMap;
                sn.connectedNode = n.connectedNode;
                sn.connectionID = n.connectionID;
                sn.nodeName = n.nodeName;

                data.nodes.Add(sn);
            }
        }

        return data;
    }


    Node[,] ConvertToGrid(SerializableMapData data)
    {
        Node[,] grid = new Node[data.width, data.height];

        foreach (var sn in data.nodes)
        {
            Node n = new Node(sn.x, sn.y);
            n.mapName = data.mapName;

            n.isWalkable = sn.isWalkable;
            n.nodeType = (NodeType)sn.nodeType;
            n.connectedMap = sn.connectedMap;
            n.connectedNode = sn.connectedNode;
            n.connectionID = sn.connectionID;
            n.nodeName = sn.nodeName;

            grid[sn.x, sn.y] = n;
        }

        return grid;
    }


    string GetMapPath(string mapName)
    {
        string dir = Path.Combine(Application.dataPath, "ProjectCore", "Resources", "Maps");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, mapName + ".json");
    }

    public void SaveMapToFile(string mapName)
    {
        Node[,] grid = maps[mapName];

        SerializableMapData data = ConvertToSerializable(mapName, grid);

        string json = JsonUtility.ToJson(data, true);

        string savePath = GetMapPath(mapName);
        string saveDir = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(saveDir))
            Directory.CreateDirectory(saveDir);

        File.WriteAllText(savePath, json);

        Debug.Log("Map saved to file: " + savePath);
    }


    public void LoadMapFromFile(string mapName)
    {
        string path = GetMapPath(mapName);

        if (!File.Exists(path))
        {
            Debug.Log("File not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);
        SerializableMapData data = JsonUtility.FromJson<SerializableMapData>(json);

        if (data == null || string.IsNullOrEmpty(data.mapName))
        {
            Debug.LogError("Invalid map file: " + path);
            return;
        }

        maps[mapName] = ConvertToGrid(data);

        string building = string.IsNullOrEmpty(data.buildingName) ? "Default" : data.buildingName;
        mapToBuilding[mapName] = building;
        mapToFloor[mapName] = data.floorNumber;
        RegisterMapToBuilding(mapName, building);

        LoadMap(mapName);
    }


    public void LoadAllMapsFromDisk() => LoadAllMapsFromFiles();

    void LoadAllMapsFromFiles()
    {
        string dir = Path.Combine(Application.dataPath, "ProjectCore", "Resources", "Maps");

        if (!Directory.Exists(dir))
            return;

        string[] files = Directory.GetFiles(dir, "*.json");

        foreach (string file in files)
        {
            if (Path.GetFileName(file) == "buildings.json")
                continue;

            string json = File.ReadAllText(file);
            SerializableMapData data = JsonUtility.FromJson<SerializableMapData>(json);

            if (data == null || string.IsNullOrEmpty(data.mapName))
            {
                Debug.LogError("Invalid map file skipped: " + file);
                continue;
            }

            maps[data.mapName] = ConvertToGrid(data);

            string building = string.IsNullOrEmpty(data.buildingName) ? "Default" : data.buildingName;
            mapToBuilding[data.mapName] = building;
            mapToFloor[data.mapName] = data.floorNumber;

            RegisterMapToBuilding(data.mapName, building);

            Debug.Log($"Loaded Map: {data.mapName}, Building: {building}, Floor: {data.floorNumber}");
        }

        Debug.Log("Maps loaded from disk: " + maps.Count);
    }

    public void RegisterMapToBuilding(string mapName, string buildingName)
    {
        if (string.IsNullOrEmpty(mapName))
            return;

        if (string.IsNullOrEmpty(buildingName))
            buildingName = "Default";

        if (!buildings.ContainsKey(buildingName))
        {
            buildings[buildingName] = new List<string>();
        }

        if (!buildings[buildingName].Contains(mapName))
        {
            buildings[buildingName].Add(mapName);
        }

        mapToBuilding[mapName] = buildingName;
    }

    public void MoveMapToBuilding(string mapName, string newBuilding)
    {
        // Remove from old building
        foreach (var kvp in buildings)
        {
            if (kvp.Value.Contains(mapName))
            {
                kvp.Value.Remove(mapName);
                break;
            }
        }

        // Add to new building
        RegisterMapToBuilding(mapName, newBuilding);
        SaveBuildingsToFile();
    }

    string GetBuildingPath()
    {
#if UNITY_EDITOR
        string dir = Path.Combine(Application.dataPath, "ProjectCore", "Resources", "Maps");
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return Path.Combine(dir, "buildings.json");
#else
        // On device, use Resources folder (read-only)
        return string.Empty;
#endif
    }

    public void SaveBuildingsToFile()
    {
#if UNITY_EDITOR
        List<SerializableBuildingData> dataList = new List<SerializableBuildingData>();

        foreach (var kvp in buildings)
        {
            SerializableBuildingData data = new SerializableBuildingData();
            data.buildingName = kvp.Key;
            data.maps = kvp.Value;

            dataList.Add(data);
        }

        string json = JsonUtility.ToJson(new Wrapper { list = dataList }, true);
        File.WriteAllText(GetBuildingPath(), json);

        Debug.Log("Buildings saved");
#endif
    }

    [Serializable]
    class Wrapper
    {
        public List<SerializableBuildingData> list;
    }

    public void LoadBuildingsFromFile()
    {
#if UNITY_EDITOR
        string path = GetBuildingPath();

        if (!File.Exists(path))
            return;

        string json = File.ReadAllText(path);

        Wrapper wrapper = JsonUtility.FromJson<Wrapper>(json);

        if (wrapper == null || wrapper.list == null)
        {
            Debug.LogError("Failed to parse buildings.json");
            return;
        }

        buildings.Clear();

        foreach (var data in wrapper.list)
        {
            buildings[data.buildingName] = data.maps;

            foreach (var map in data.maps)
            {
                mapToBuilding[map] = data.buildingName;
            }
        }

        Debug.Log("Buildings loaded");
#else
        // On device, skip file loading - MapManager is editor-only
        Debug.Log("[MapManager] Skipping file operations on device");
#endif
    }
}
