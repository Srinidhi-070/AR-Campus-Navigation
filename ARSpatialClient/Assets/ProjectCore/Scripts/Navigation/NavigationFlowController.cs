using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class NavigationFlowController : MonoBehaviour
{
    private CampusApiClient m_ApiClient;
    private LocationRegistry m_LocationRegistry;
    private QRLocationManager m_QRLocationManager;
    private PathVisualizer m_PathVisualizer;
    private CampusRuntimeUI m_UI;
    private CampusRuntimeValidator m_Validator;

    // Scale factor: 1.0 means 1 grid unit = 1 meter.
    private float m_MetersPerGridUnit = 1.0f;

    private List<Vector3> m_ActiveWorldPath;
    private List<PathVisualizer.FloorTransition> m_ActiveTransitions;
    private float m_PathUpdateTimer = 0f;

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
        m_ActiveWorldPath = null;
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
            m_ActiveWorldPath = null;
            m_PathVisualizer.ClearPath();
            m_UI.ShowStatus("You are already at the destination.");
            m_UI.ShowDirections(new List<string> { "Destination Reached" });
            RefreshControls();
            return;
        }

        m_ActiveWorldPath = null;
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
        m_LocationRegistry.Clear();
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
        Debug.LogWarning($"[NavigationFlowController] HandleLocationsError called. backendError={error}");

        if (TryLoadLocalLocations(out List<LocationData> localLocations, out string localError))
        {
            int count = localLocations != null ? localLocations.Count : 0;
            Debug.LogWarning($"[NavigationFlowController] Local fallback loaded nodes: {count}");

            m_UseLocalRouting = true;
            m_LocationRegistry.Clear();
            m_LocationRegistry.SetLocations(localLocations);

            Debug.LogWarning($"[NavigationFlowController] LocationRegistry after SetLocations: IsLoaded={m_LocationRegistry.IsLoaded}, Count={m_LocationRegistry.Count}");

            PopulateBuildingOptions();
            RefreshControls();
            m_UI.ShowStatus("Offline map loaded. Scan QR code to begin.");
            Debug.LogWarning($"[NavigationFlowController] Backend error, loaded local map instead: {error} (localError={localError})");
            return;
        }

        Debug.LogWarning($"[NavigationFlowController] Local fallback FAILED. localError={localError}");

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
        m_ActiveWorldPath = null;

        if (response == null || response.path == null || response.path.Count < 2)
        {
            m_PathVisualizer.ClearPath();
            m_UI.ShowDirections(new List<string>());
            m_UI.ShowStatus("No valid path was returned by the backend.");
            RefreshControls();
            return;
        }

        // ── Detect floor transitions BEFORE coordinate transformation ──
        // We need the original floor/building data from the response.
        List<PathVisualizer.FloorTransition> transitions = new List<PathVisualizer.FloorTransition>();
        for (int i = 0; i < response.path.Count - 1; i++)
        {
            CampusApiClient.PathPointPayload current = response.path[i];
            CampusApiClient.PathPointPayload next = response.path[i + 1];

            if (current.floor != next.floor)
            {
                // Determine transition type using the node 'type' field from the backend.
                // Fallback: check if node ID contains "LIFT".
                string currentType = (current.type ?? "").ToUpper();
                string nextType = (next.type ?? "").ToUpper();
                string currentId = (current.id ?? "").ToUpper();
                string nextId = (next.id ?? "").ToUpper();

                bool isLift = currentType.Contains("LIFT") || nextType.Contains("LIFT") ||
                              currentId.Contains("LIFT") || nextId.Contains("LIFT");

                transitions.Add(new PathVisualizer.FloorTransition
                {
                    segmentStartIndex = i,
                    type = isLift
                        ? PathVisualizer.TransitionType.Lift
                        : PathVisualizer.TransitionType.Staircase,
                    fromFloor = current.floor,
                    toFloor = next.floor,
                    goingUp = next.floor > current.floor
                });

                Debug.Log($"[NavigationFlowController] Floor transition at segment {i}: " +
                          $"{(isLift ? "Lift" : "Stairs")} Floor {current.floor} → {next.floor}" +
                          $" (type: {current.type} → {next.type})");
            }
        }

        List<Vector3> worldPath = new List<Vector3>();
        foreach (CampusApiClient.PathPointPayload point in response.path)
            worldPath.Add(new Vector3(point.x, point.y, point.z));

        // --- AR ALIGNMENT (stable, no hardcoded vertical offsets, no rotation_y dependency) ---
        // Backend points are in map space. We map them into world space by:
        // 1) picking a stable world anchor position from AR plane raycast near screen center
        // 2) aligning heading using the first segment direction (map) vs current camera forward projected to XZ
        if (worldPath.Count > 0)
        {
            // 1) Get a stable world anchor point via AR plane raycast
            Vector3 worldAnchorPos;
            bool gotAnchor = TryGetWorldAnchorFromRaycast(out worldAnchorPos);

            if (!gotAnchor)
            {
                // Fallback: use camera position, estimate floor ~1.5m below phone
                Transform fallbackCam = Camera.main != null ? Camera.main.transform : null;
                if (fallbackCam != null)
                {
                    worldAnchorPos = fallbackCam.position + Vector3.down * 1.5f;
                    Debug.LogWarning("[NavigationFlowController] No AR plane detected. Using camera position as anchor.");
                }
                else
                {
                    Debug.LogWarning("[NavigationFlowController] No camera or AR plane available. Rendering path unaligned.");
                    m_PathVisualizer.ClearPath();
                    m_PathVisualizer.DrawPath(worldPath, transitions);
                    m_UI.ShowDirections(response.directions ?? new List<string>());
                    m_UI.ShowStatus("Navigation active (unaligned).");
                    RefreshControls();
                    return;
                }
            }

            // 2) Compute map heading from first segment
            Vector3 mapStart = worldPath[0];
            Vector3 mapNext = worldPath.Count > 1 ? worldPath[1] : worldPath[0];

            Vector3 mapDir = new Vector3(mapNext.x - mapStart.x, 0f, mapNext.z - mapStart.z);
            if (mapDir.sqrMagnitude < 0.0001f)
                mapDir = Vector3.forward;

            mapDir.Normalize();

            // 3) Compute camera heading (project forward onto XZ)
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            Vector3 camForward = cam != null ? cam.forward : Vector3.forward;
            Vector3 camDir = new Vector3(camForward.x, 0f, camForward.z);
            if (camDir.sqrMagnitude < 0.0001f)
                camDir = Vector3.forward;
            camDir.Normalize();

            float mapYaw = Mathf.Atan2(mapDir.x, mapDir.z) * Mathf.Rad2Deg;
            float camYaw = Mathf.Atan2(camDir.x, camDir.z) * Mathf.Rad2Deg;
            float yawOffset = camYaw - mapYaw;
            Quaternion rotationOffset = Quaternion.Euler(0f, yawOffset, 0f);

            Debug.Log($"[NavigationFlowController] AnchorPos={worldAnchorPos} mapDirYaw={mapYaw:F1} camDirYaw={camYaw:F1} yawOffset={yawOffset:F1}");

            // 4) Transform all points:
            // translate so mapStart lands at worldAnchorPos, then rotate around Y around origin (using translated vectors)
            List<Vector3> transformed = new List<Vector3>(worldPath.Count);
            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 p = worldPath[i];
                Vector3 v = p - mapStart;        // local vector in map space relative to start
                v *= m_MetersPerGridUnit;        // scale grid units to real-world meters
                v = rotationOffset * v;          // rotate heading
                Vector3 outPos = new Vector3(
                    worldAnchorPos.x + v.x,
                    worldAnchorPos.y + (p.y - mapStart.y) * m_MetersPerGridUnit, // keep scaled vertical delta
                    worldAnchorPos.z + v.z
                );
                transformed.Add(outPos);
            }

            worldPath = transformed;

            if (worldPath.Count > 1)
            {
                Debug.Log($"[NavigationFlowController] First Point: {worldPath[0]}, Second Point: {worldPath[1]}");
                Debug.Log($"[NavigationFlowController] Distance anchor->first: {Vector3.Distance(worldAnchorPos, worldPath[0])}");
            }
        }

        m_ActiveWorldPath = worldPath; // Store for live dynamic updating
        m_ActiveTransitions = transitions; // Store transitions so they survive path trimming

        m_PathVisualizer.ClearPath();
        m_PathVisualizer.DrawPath(worldPath, transitions);
        m_UI.ShowDirections(response.directions ?? new List<string>());
        m_UI.ShowStatus("Navigation active.");
        RefreshControls();
    }

    void Update()
    {
        if (m_ActiveWorldPath == null || m_ActiveWorldPath.Count < 2)
            return;

        m_PathUpdateTimer += Time.deltaTime;
        if (m_PathUpdateTimer < 0.25f) // Update route 4x per second for smoother tracking
            return;
        m_PathUpdateTimer = 0f;

        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null) return;

        Vector3 userPos = cam.position;
        int closestIndex = 0;
        float minDistance = float.MaxValue;

        // Look ahead up to 20 waypoints to see if user has reached them
        int searchLimit = Mathf.Min(m_ActiveWorldPath.Count, 20);
        for (int i = 0; i < searchLimit; i++)
        {
            // Measure distance in 3D space so vertical stairs aren't skipped prematurely
            float dist = Vector3.Distance(userPos, m_ActiveWorldPath[i]);
                
            if (dist < minDistance)
            {
                minDistance = dist;
                closestIndex = i;
            }
        }

        // If the user has walked past previous waypoints and is within 1.5m of a new one, trim the path
        if (closestIndex > 0 && minDistance < 1.5f)
        {
            m_ActiveWorldPath.RemoveRange(0, closestIndex);
            
            // Shift transition indices
            if (m_ActiveTransitions != null)
            {
                for (int i = m_ActiveTransitions.Count - 1; i >= 0; i--)
                {
                    var t = m_ActiveTransitions[i];
                    t.segmentStartIndex -= closestIndex;
                    if (t.segmentStartIndex < 0)
                        m_ActiveTransitions.RemoveAt(i);
                    else
                        m_ActiveTransitions[i] = t;
                }
            }

            if (m_ActiveWorldPath.Count < 2)
            {
                m_PathVisualizer.ClearPath();
                m_UI.ShowStatus("Destination Reached!");
                m_ActiveWorldPath = null;
                m_ActiveTransitions = null;
            }
            else
            {
                m_PathVisualizer.DrawPath(m_ActiveWorldPath, m_ActiveTransitions);
            }
        }
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
                type = location.type,
                building = location.building,
                floor = location.floor
            }).ToList(),
            directions = BuildLocalDirections(pathLocations)
        };

        return true;
    }

    private bool TryGetWorldAnchorFromRaycast(out Vector3 worldAnchorPos)
    {
        worldAnchorPos = Vector3.zero;

        // Find raycast manager (created by ARFoundationBootstrap / XROrigin)
        ARRaycastManager raycastManager = FindObjectOfType<ARRaycastManager>();
        if (raycastManager == null)
            return false;

        // Raycast from screen center for stability
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        var hits = new List<ARRaycastHit>();
        if (!raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
            return false;

        if (hits.Count == 0)
            return false;

        worldAnchorPos = hits[0].pose.position;
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
        // Snapshot for logging (avoid multiple enumeration side-effects)
        var all = m_LocationRegistry != null ? m_LocationRegistry.GetAllLocations() : null;

        if (all == null)
        {
            Debug.LogWarning("[NavigationFlowController] GetDestinationLocations: LocationRegistry is null.");
            yield break;
        }

        // Show all navigable locations. Only filter out corridor waypoints and wall markers
        // which are structural, not destinations. Entrances, stairs, lifts ARE valid destinations.
        var destinations = all.Where(location =>
            location != null &&
            !string.IsNullOrEmpty(location.id) &&
            location.type != "corridor" &&
            location.type != "wall"
        ).ToList();

        Debug.Log($"[NavigationFlowController] GetDestinationLocations found {destinations.Count} destinations (from loaded {destinations.Count + all.Count() - (destinations.Count)})");

        // Helpful: log room nodes specifically (id/building/floor/type)
        int roomCount = destinations.Count(l => l.type == "room");
        int entranceCount = destinations.Count(l => l.type == "entrance");
        int staircaseCount = destinations.Count(l => l.type == "staircase");
        int liftCount = destinations.Count(l => l.type == "lift");
        int corridorCount = destinations.Count(l => l.type == "corridor");
        int wallCount = destinations.Count(l => l.type == "wall");

        Debug.Log(
            $"[NavigationFlowController] Destination type breakdown: room={roomCount}, entrance={entranceCount}, staircase={staircaseCount}, lift={liftCount}, corridor={corridorCount}, wall={wallCount}"
        );

        foreach (var d in destinations)
            yield return d;
    }

    private void PopulateBuildingOptions()
    {
        m_BuildingOptions.Clear();

        var dest = GetDestinationLocations().ToList();
        Debug.Log($"[NavigationFlowController] PopulateBuildingOptions: destinations={dest.Count}");

        foreach (LocationData location in dest)
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

        var dest = GetDestinationLocations().ToList();
        Debug.Log($"[NavigationFlowController] PopulateRoomOptions: building='{building}', floor={floor}, destinations={dest.Count}");

        foreach (LocationData location in dest)
        {
            if (location.building == building && location.floor == floor)
                m_RoomOptions.Add(location);
        }

        Debug.Log($"[NavigationFlowController] Room matches count={m_RoomOptions.Count}");
        foreach (var r in m_RoomOptions)
            Debug.Log($"[NavigationFlowController] Room match: id={r.id}, displayName={r.displayName}, type={r.type}, building={r.building}, floor={r.floor}");

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
