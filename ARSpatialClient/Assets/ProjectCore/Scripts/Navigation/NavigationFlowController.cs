using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NavigationFlowController : MonoBehaviour
{
    private CampusApiClient m_ApiClient;
    private LocationRegistry m_LocationRegistry;
    private QRLocationManager m_QRLocationManager;
    private PathVisualizer m_PathVisualizer;
    private CampusRuntimeUI m_UI;
    private CampusRuntimeValidator m_Validator;

    private readonly List<string> m_BuildingOptions = new List<string>();
    private readonly List<int> m_FloorOptions = new List<int>();
    private readonly List<LocationData> m_RoomOptions = new List<LocationData>();
    private bool m_UseLocalRouting;

    [System.Serializable]
    private class LocalNodesWrapper
    {
        public List<LocationData> nodes;
    }

    public void Configure(
        CampusApiClient apiClient,
        LocationRegistry locationRegistry,
        QRLocationManager qrLocationManager,
        PathVisualizer pathVisualizer,
        CampusRuntimeUI ui,
        CampusRuntimeValidator validator)
    {
        if (m_QRLocationManager != null)
            m_QRLocationManager.OnLocationChanged -= HandleQrLocationChanged;

        m_ApiClient = apiClient;
        m_LocationRegistry = locationRegistry;
        m_QRLocationManager = qrLocationManager;
        m_PathVisualizer = pathVisualizer;
        m_UI = ui;
        m_Validator = validator;

        if (m_QRLocationManager != null)
            m_QRLocationManager.OnLocationChanged += HandleQrLocationChanged;

        RefreshControls();
    }

    public void BeginLoad()
    {
        Debug.Log("[NavigationFlowController] BeginLoad called");

        if (m_LocationRegistry == null || m_UI == null)
        {
            Debug.LogError($"[NavigationFlowController] Missing dependencies: LocationRegistry={m_LocationRegistry != null}, UI={m_UI != null}");
            return;
        }

        m_UI.ShowStatus("Loading campus map...");
        RefreshControls();

        m_UseLocalRouting = false;
        if (m_ApiClient == null)
        {
            HandleLocationsError("Campus API client is not configured.");
            return;
        }

        Debug.Log($"[NavigationFlowController] Fetching from: {m_ApiClient.BaseUrl}");
        StartCoroutine(m_ApiClient.FetchLocations(HandleLocationsLoaded, HandleLocationsError));
    }


    public void HandleBuildingChanged(int index)
    {
        if (index < 0 || index >= m_BuildingOptions.Count)
            return;

        string building = m_BuildingOptions[index];
        m_FloorOptions.Clear();
        foreach (LocationData location in GetDestinationLocations())
        {
            if (location.building == building && !m_FloorOptions.Contains(location.floor))
                m_FloorOptions.Add(location.floor);
        }
        m_FloorOptions.Sort();

        m_UI.FloorDropdown.ClearOptions();
        List<string> floorLabels = m_FloorOptions.Count == 0
            ? new List<string> { "No Floors" }
            : m_FloorOptions.Select(floor => floor == 0 ? "Ground Floor" : $"Floor {floor}").ToList();
        m_UI.FloorDropdown.AddOptions(floorLabels);
        m_UI.FloorDropdown.RefreshShownValue();

        HandleFloorChanged(0);
    }

    public void HandleFloorChanged(int index)
    {
        if (m_BuildingOptions.Count == 0 || m_FloorOptions.Count == 0)
        {
            PopulateRoomOptions(null, int.MinValue);
            return;
        }

        if (index < 0 || index >= m_FloorOptions.Count)
            index = 0;

        string building = m_BuildingOptions[m_UI.BuildingDropdown.value];
        int floor = m_FloorOptions[index];
        PopulateRoomOptions(building, floor);
    }

    public void HandleNavigatePressed()
    {
        Debug.Log("[NavigationFlowController] HandleNavigatePressed called");
        
        if (m_RoomOptions.Count == 0)
        {
            Debug.Log("[NavigationFlowController] No room options available");
            m_UI.ShowStatus("No destination available.");
            RefreshControls();
            return;
        }

        int roomIndex = Mathf.Clamp(m_UI.RoomDropdown.value, 0, m_RoomOptions.Count - 1);
        Debug.Log($"[NavigationFlowController] Selected room: {m_RoomOptions[roomIndex].id} ({m_RoomOptions[roomIndex].displayName})");
        NavigateToDestination(m_RoomOptions[roomIndex].id, m_RoomOptions[roomIndex].displayName);
    }

    public void NavigateToDestination(string destinationId, string displayName = null)
    {
        if (m_Validator == null)
        {
            m_UI.ShowStatus("Navigation validator is not configured.");
            return;
        }

        if (!m_Validator.CanRequestPath(
                m_LocationRegistry,
                m_QRLocationManager,
                destinationId,
                out LocationData destination,
                out string validationMessage))
        {
            Debug.LogWarning($"[NavigationFlowController] Validation failed: {validationMessage}");
            m_UI.ShowStatus(validationMessage);
            RefreshControls();
            return;
        }

        if (destination.id.ToUpper() == m_QRLocationManager.CurrentNodeId)
        {
            m_PathVisualizer.ClearPath();
            m_UI.ShowStatus("You are already at the destination.");
            m_UI.ShowDirections(new List<string> { "Destination Reached" });
            RefreshControls();
            return;
        }

        string label = string.IsNullOrEmpty(displayName) ? destination.displayName : displayName;
        m_UI.ShowStatus($"Calculating path to {label}...");

        string startNodeId = m_QRLocationManager.CurrentNodeId;
        string destinationNodeId = destination.id;

        if (m_UseLocalRouting || m_ApiClient == null)
        {
            if (!TryNavigateLocally(startNodeId, destinationNodeId, out string localError))
            {
                m_PathVisualizer.ClearPath();
                m_UI.ShowDirections(new List<string>());
                m_UI.ShowStatus($"No path found. {localError}");
                RefreshControls();
            }
            else
            {
                m_UI.ShowStatus("Navigation active (offline map).");
            }

            return;
        }

        StartCoroutine(m_ApiClient.RequestPath(
            startNodeId,
            destinationNodeId,
            response =>
            {
                if (!IsValidPathResponse(response) && TryNavigateLocally(startNodeId, destinationNodeId, out _))
                {
                    m_UI.ShowStatus("Navigation active (offline map).");
                    return;
                }

                HandlePathResponse(response);
            },
            error =>
            {
                if (TryNavigateLocally(startNodeId, destinationNodeId, out string localError))
                {
                    Debug.LogWarning($"[NavigationFlowController] Backend path error, using local route: {error}");
                    m_UI.ShowStatus("Navigation active (offline map).");
                    return;
                }

                m_PathVisualizer.ClearPath();
                m_UI.ShowDirections(new List<string>());
                m_UI.ShowStatus($"No path found. {error} {localError}");
                RefreshControls();
            }));
    }

    private void HandleLocationsLoaded(List<LocationData> locations)
    {
        Debug.Log($"[NavigationFlowController] HandleLocationsLoaded called with {locations?.Count ?? 0} locations");

        m_UseLocalRouting = false;
        m_LocationRegistry.SetLocations(locations);

        // Debug: Log all loaded locations
        if (locations != null)
        {
            foreach (var loc in locations)
            {
                Debug.Log($"[NavigationFlowController] Loaded: {loc.id} | {loc.displayName} | Type: {loc.type} | Building: {loc.building} | Floor: {loc.floor}");
            }
        }
        
        PopulateBuildingOptions();
        
        // Debug: Log building options
        Debug.Log($"[NavigationFlowController] Building options count: {m_BuildingOptions.Count}");
        foreach (var building in m_BuildingOptions)
        {
            Debug.Log($"[NavigationFlowController] Building option: {building}");
        }
        
        RefreshControls();

        if (locations == null || locations.Count == 0)
        {
            m_UI.ShowStatus("No floor map exported yet.");
            return;
        }

        m_UI.ShowStatus("Scan QR code to begin.");
    }

    private void HandleLocationsError(string error)
    {
        if (TryLoadLocalLocations(out List<LocationData> localLocations, out string localError))
        {
            m_UseLocalRouting = true;
            m_LocationRegistry.SetLocations(localLocations);
            PopulateBuildingOptions();
            RefreshControls();
            m_UI.ShowStatus("Offline map loaded. Scan QR code to begin.");
            Debug.LogWarning($"[NavigationFlowController] Backend error, loaded local map instead: {error}");
            return;
        }

        m_UseLocalRouting = false;
        m_LocationRegistry.Clear();
        PopulateBuildingOptions();

        m_UI.ShowStatus("Campus map unavailable. Start the backend or export nodes.json.");

        if (m_UI != null)
        {
            m_UI.QRButton.interactable = true;
            m_UI.MenuButton.interactable = true;
        }

        Debug.LogWarning($"[NavigationFlowController] Backend error: {error}. Local map error: {localError}");
    }

    private void HandlePathResponse(CampusApiClient.PathResponsePayload response)
    {
        if (response == null || response.path == null || response.path.Count < 2)
        {
            m_PathVisualizer.ClearPath();
            m_UI.ShowDirections(new List<string>());
            m_UI.ShowStatus("No valid path was returned by the backend.");
            RefreshControls();
            return;
        }

        List<Vector3> worldPath = new List<Vector3>();
        foreach (CampusApiClient.PathPointPayload point in response.path)
            worldPath.Add(new Vector3(point.x, point.y, point.z));

        // --- CRITICAL AR ALIGNMENT (Position + Rotation) ---
        // The backend returns coordinates in the map's absolute space.
        // We must align both the POSITION and ROTATION of the path to the physical world.
        if (Camera.main != null && worldPath.Count > 0)
        {
            Vector3 camPos = Camera.main.transform.position;
            Vector3 mapStart = worldPath[0];
            
            // Calculate rotation offset: how much did the user turn compared to the map's forward direction?
            float mapStartRotY = response.path[0].rotation_y;
            float scanCamRotY = m_QRLocationManager != null ? m_QRLocationManager.ScanCameraRotationY : Camera.main.transform.eulerAngles.y;
            float rotationDiff = scanCamRotY - mapStartRotY;
            Quaternion rotationOffset = Quaternion.Euler(0, rotationDiff, 0);
            
            Debug.Log($"[NavigationFlowController] Aligning AR Path. MapRotY={mapStartRotY}, CamRotY={scanCamRotY}, Offset={rotationDiff}");
            Debug.Log($"[NavigationFlowController] Camera Pos at Path Start: {camPos}");

            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 point = worldPath[i];
                
                // 1. Center around start node
                Vector3 localPoint = point - mapStart;
                
                // 2. Rotate to match camera's heading at scan time
                localPoint = rotationOffset * localPoint;
                
                // 3. Move to physical camera position (X/Z), and place on floor (Y)
                // We use camPos.y - 1.2f as a heuristic for the floor height
                Vector3 finalPos = new Vector3(
                    camPos.x + localPoint.x,
                    (camPos.y - 1.2f) + (point.y - mapStart.y),
                    camPos.z + localPoint.z
                );

                worldPath[i] = finalPos;
            }

            if (worldPath.Count > 1)
            {
                Debug.Log($"[NavigationFlowController] First Point: {worldPath[0]}, Second Point: {worldPath[1]}");
                Debug.Log($"[NavigationFlowController] Distance to first point: {Vector3.Distance(camPos, worldPath[0])}");
            }
        }

        m_PathVisualizer.ClearPath();
        m_PathVisualizer.DrawPath(worldPath);
        m_UI.ShowDirections(response.directions ?? new List<string>());
        m_UI.ShowStatus("Navigation active.");
        RefreshControls();
    }

    private bool IsValidPathResponse(CampusApiClient.PathResponsePayload response)
    {
        return response != null && response.path != null && response.path.Count >= 2;
    }

    private bool TryLoadLocalLocations(out List<LocationData> locations, out string error)
    {
        locations = null;
        error = string.Empty;

        TextAsset asset = Resources.Load<TextAsset>("nodes");
        if (asset == null)
        {
            error = "Resources/nodes.json was not found.";
            return false;
        }

        LocalNodesWrapper wrapper = JsonUtility.FromJson<LocalNodesWrapper>(asset.text);
        if (wrapper == null || wrapper.nodes == null || wrapper.nodes.Count == 0)
        {
            error = "Resources/nodes.json does not contain any nodes.";
            return false;
        }

        foreach (LocationData location in wrapper.nodes)
        {
            if (location == null)
                continue;

            location.id = NormalizeNodeId(location.id);
            if (location.neighbors == null)
                continue;

            for (int i = 0; i < location.neighbors.Length; i++)
                location.neighbors[i] = NormalizeNodeId(location.neighbors[i]);
        }

        locations = wrapper.nodes.Where(location => location != null && !string.IsNullOrEmpty(location.id)).ToList();
        if (locations.Count == 0)
        {
            error = "Resources/nodes.json only contains invalid nodes.";
            return false;
        }

        return true;
    }

    private bool TryNavigateLocally(string startNodeId, string destinationNodeId, out string error)
    {
        error = string.Empty;

        if (!TryBuildLocalPath(startNodeId, destinationNodeId, out CampusApiClient.PathResponsePayload response, out error))
            return false;

        HandlePathResponse(response);
        return true;
    }

    private bool TryBuildLocalPath(
        string startNodeId,
        string destinationNodeId,
        out CampusApiClient.PathResponsePayload response,
        out string error)
    {
        response = null;
        error = string.Empty;

        string startKey = NormalizeNodeId(startNodeId);
        string destinationKey = NormalizeNodeId(destinationNodeId);

        if (string.IsNullOrEmpty(startKey) || string.IsNullOrEmpty(destinationKey))
        {
            error = "Start or destination node is missing.";
            return false;
        }

        Dictionary<string, LocationData> locations = new Dictionary<string, LocationData>();
        foreach (LocationData location in m_LocationRegistry.GetAllLocations())
        {
            if (location == null || string.IsNullOrEmpty(location.id))
                continue;

            string key = NormalizeNodeId(location.id);
            if (!locations.ContainsKey(key))
                locations.Add(key, location);
        }

        if (!locations.ContainsKey(startKey))
        {
            error = $"Current QR node '{startKey}' is not in the local map.";
            return false;
        }

        if (!locations.ContainsKey(destinationKey))
        {
            error = $"Destination '{destinationKey}' is not in the local map.";
            return false;
        }

        Dictionary<string, float> costs = new Dictionary<string, float>();
        Dictionary<string, string> previous = new Dictionary<string, string>();
        HashSet<string> visited = new HashSet<string>();
        List<string> open = new List<string> { startKey };
        costs[startKey] = 0f;

        while (open.Count > 0)
        {
            string current = GetLowestCostNode(open, costs);
            open.Remove(current);

            if (!visited.Add(current))
                continue;

            if (current == destinationKey)
                break;

            LocationData currentLocation = locations[current];
            if (currentLocation.neighbors == null)
                continue;

            foreach (string rawNeighborId in currentLocation.neighbors)
            {
                string neighborId = NormalizeNodeId(rawNeighborId);
                if (string.IsNullOrEmpty(neighborId) || !locations.ContainsKey(neighborId) || visited.Contains(neighborId))
                    continue;

                float newCost = costs[current] + GetDistance(currentLocation, locations[neighborId]);
                if (!costs.ContainsKey(neighborId) || newCost < costs[neighborId])
                {
                    costs[neighborId] = newCost;
                    previous[neighborId] = current;
                    if (!open.Contains(neighborId))
                        open.Add(neighborId);
                }
            }
        }

        if (!costs.ContainsKey(destinationKey))
        {
            error = $"No local route from '{startKey}' to '{destinationKey}'.";
            return false;
        }

        List<string> orderedIds = new List<string>();
        string cursor = destinationKey;
        orderedIds.Add(cursor);

        while (cursor != startKey)
        {
            if (!previous.TryGetValue(cursor, out cursor))
            {
                error = $"No local route from '{startKey}' to '{destinationKey}'.";
                return false;
            }

            orderedIds.Add(cursor);
        }

        orderedIds.Reverse();

        List<LocationData> pathLocations = orderedIds.Select(id => locations[id]).ToList();
        response = new CampusApiClient.PathResponsePayload
        {
            path = pathLocations.Select(location => new CampusApiClient.PathPointPayload
            {
                id = location.id,
                x = location.x,
                y = location.y,
                z = location.z,
                rotation_y = location.rotation_y,
                building = location.building,
                floor = location.floor
            }).ToList(),
            directions = BuildLocalDirections(pathLocations)
        };

        return true;
    }

    private string GetLowestCostNode(List<string> open, Dictionary<string, float> costs)
    {
        string best = open[0];
        float bestCost = costs.ContainsKey(best) ? costs[best] : float.MaxValue;

        for (int i = 1; i < open.Count; i++)
        {
            string candidate = open[i];
            float candidateCost = costs.ContainsKey(candidate) ? costs[candidate] : float.MaxValue;
            if (candidateCost < bestCost)
            {
                best = candidate;
                bestCost = candidateCost;
            }
        }

        return best;
    }

    private float GetDistance(LocationData a, LocationData b)
    {
        Vector3 aPosition = new Vector3(a.x, a.y, a.z);
        Vector3 bPosition = new Vector3(b.x, b.y, b.z);
        return Vector3.Distance(aPosition, bPosition);
    }

    private List<string> BuildLocalDirections(List<LocationData> path)
    {
        List<string> directions = new List<string>();
        if (path == null || path.Count == 0)
            return directions;

        directions.Add($"Start at {GetLocationName(path[0])}.");

        for (int i = 1; i < path.Count; i++)
        {
            LocationData previous = path[i - 1];
            LocationData current = path[i];

            if (current.floor != previous.floor)
            {
                directions.Add($"Change to {GetFloorLabel(current.floor)} via {GetLocationName(current)}.");
                continue;
            }

            if (i == path.Count - 1)
            {
                directions.Add($"Arrive at {GetLocationName(current)}.");
                continue;
            }

            directions.Add($"Continue toward {GetLocationName(current)}.");
        }

        return directions;
    }

    private string GetLocationName(LocationData location)
    {
        if (location == null)
            return "the next point";

        if (!string.IsNullOrWhiteSpace(location.displayName))
            return location.displayName;

        return string.IsNullOrWhiteSpace(location.id) ? "the next point" : location.id;
    }

    private string GetFloorLabel(int floor)
    {
        return floor == 0 ? "Ground Floor" : $"Floor {floor}";
    }

    private string NormalizeNodeId(string id)
    {
        return string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim().ToUpperInvariant();
    }

    private IEnumerable<LocationData> GetDestinationLocations()
    {
        var destinations = m_LocationRegistry.GetAllLocations().Where(location =>
            location != null &&
            !string.IsNullOrEmpty(location.id) &&
            location.type != "corridor" &&
            location.type != "entrance" &&
            location.type != "staircase" &&
            location.type != "lift" &&
            location.type != "wall");
        
        Debug.Log($"[NavigationFlowController] GetDestinationLocations found {destinations.Count()} destinations");
        return destinations;
    }

    private void PopulateBuildingOptions()
    {
        m_BuildingOptions.Clear();
        foreach (LocationData location in GetDestinationLocations())
        {
            if (!m_BuildingOptions.Contains(location.building))
                m_BuildingOptions.Add(location.building);
        }

        m_BuildingOptions.Sort();
        m_UI.BuildingDropdown.ClearOptions();
        m_UI.BuildingDropdown.AddOptions(m_BuildingOptions.Count == 0 ? new List<string> { "No Buildings" } : m_BuildingOptions);
        m_UI.BuildingDropdown.value = 0;
        m_UI.BuildingDropdown.RefreshShownValue();

        HandleBuildingChanged(0);
    }

    private void PopulateRoomOptions(string building, int floor)
    {
        m_RoomOptions.Clear();
        foreach (LocationData location in GetDestinationLocations())
        {
            if (location.building == building && location.floor == floor)
                m_RoomOptions.Add(location);
        }

        m_UI.RoomDropdown.ClearOptions();
        List<string> options = m_RoomOptions.Count == 0
            ? new List<string> { "No Destinations" }
            : m_RoomOptions.Select(location => location.displayName).ToList();
        m_UI.RoomDropdown.AddOptions(options);
        m_UI.RoomDropdown.value = 0;
        m_UI.RoomDropdown.RefreshShownValue();
        RefreshControls();
    }

    private void HandleQrLocationChanged(string nodeId)
    {
        RefreshControls();

        if (m_UI != null && !string.IsNullOrEmpty(nodeId))
        {
            LocationData loc = m_LocationRegistry != null ? m_LocationRegistry.GetLocation(nodeId) : null;
            string name = loc != null ? loc.displayName : nodeId;
            m_UI.ShowStatus($"Location: {name}");
        }
    }

    private void RefreshControls()
    {
        if (m_UI == null)
            return;

        bool graphReady = m_LocationRegistry != null && m_LocationRegistry.IsLoaded && m_LocationRegistry.Count > 0;
        bool qrReady = m_QRLocationManager != null && m_QRLocationManager.HasLocation;
        bool hasDestinations = graphReady && m_RoomOptions.Count > 0;

        m_UI.BuildingDropdown.interactable = graphReady && m_BuildingOptions.Count > 0;
        m_UI.FloorDropdown.interactable = graphReady && m_FloorOptions.Count > 0;
        m_UI.RoomDropdown.interactable = graphReady && hasDestinations;
        m_UI.NavigateButton.interactable = graphReady && qrReady && hasDestinations;
        m_UI.ChatButton.interactable = graphReady;
    }

    private void OnDestroy()
    {
        if (m_QRLocationManager != null)
            m_QRLocationManager.OnLocationChanged -= HandleQrLocationChanged;
    }
}
