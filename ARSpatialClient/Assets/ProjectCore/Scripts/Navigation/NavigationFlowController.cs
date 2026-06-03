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
    private GPSLocationService m_GPSService;

    // Scale factor: 1.0 means 1 grid unit = 1 meter.
    private float m_MetersPerGridUnit = 1.0f;

    private List<Vector3> m_ActiveWorldPath;
    private List<PathVisualizer.FloorTransition> m_ActiveTransitions;
    [SerializeField] private float m_PathHeightOffset = -1.5f;
    [SerializeField] private float m_MapCompassOffset = 0f; // Offset to align Grid North with True North

    private CampusApiClient.PathResponsePayload m_LastRawPathResponse;

    // ── Dynamic Navigation State ──────────────────────────────────────────────
    private string m_ActiveDestinationId;      // Stored for off-path recalculation
    private string m_ActiveDestinationName;
    private float m_OffPathTimer = 0f;
    private const float OFF_PATH_DISTANCE = 12f;  // meters
    private const float OFF_PATH_TIMEOUT = 3f;   // seconds before recalculating
    private const float RECALC_COOLDOWN = 5f;    // prevent spamming recalculations
    private float m_PathUpdateTimer = 0f;
    private float m_LastRecalcTime = -999f;

    [Header("UI Feedback")]

    // ── Floor Transition State ──
    private bool m_IsPendingTransition = false;
    private string m_PendingFloorNextNodeId = null;
    private int m_PendingFloorNextFloor = 0;
    private string m_PendingTransitionType = "";
    
    private int m_CurrentPathStartIndex = 0;
    private int m_NextPathStartIndex = 0;

    // ── Floor Transition Anchor State ──
    private Vector3 m_FloorTransitionARPos;
    private bool m_HasFloorTransitionAnchor = false;

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
        {
            m_QRLocationManager.OnLocationChanged -= HandleQrLocationChanged;
        }

        m_ApiClient = apiClient;
        m_LocationRegistry = locationRegistry;
        m_QRLocationManager = qrLocationManager;
        m_PathVisualizer = pathVisualizer;
        m_UI = ui;
        m_Validator = validator;

        if (m_QRLocationManager != null)
        {
            m_QRLocationManager.OnLocationChanged += HandleQrLocationChanged;
        }

        if (m_UI != null && m_UI.FloorTransitionButton != null)
        {
            m_UI.FloorTransitionButton.onClick.RemoveAllListeners();
            m_UI.FloorTransitionButton.onClick.AddListener(HandleFloorTransitionResume);
        }

        // Subscribe to GPS building detection for auto-selecting building dropdown
        if (m_GPSService != null)
            m_GPSService.OnBuildingDetected -= HandleGPSBuildingDetected;
        m_GPSService = AppController.Instance != null ? AppController.Instance.GPS : null;
        if (m_GPSService != null)
            m_GPSService.OnBuildingDetected += HandleGPSBuildingDetected;

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
            m_UI.UpdateNavigationGuidance("📍", "Destination Reached");
            RefreshControls();
            return;
        }

        // Store destination for potential off-path recalculation
        m_ActiveDestinationId = destination.id;
        m_ActiveDestinationName = string.IsNullOrEmpty(displayName) ? destination.displayName : displayName;
        m_OffPathTimer = 0f;

        m_ActiveWorldPath = null;
        m_CurrentPathStartIndex = 0;
        m_NextPathStartIndex = 0;
        m_HasFloorTransitionAnchor = false;
        
        string label = m_ActiveDestinationName;
        m_UI.ShowStatus($"Calculating path to {label}...");

        // Find nearest node to user's actual position (they may have walked from the QR code)
        string startNodeId = FindNearestGraphNode();
        if (string.IsNullOrEmpty(startNodeId))
            startNodeId = m_QRLocationManager.CurrentNodeId; // Fallback to QR node
        string destinationNodeId = destination.id;

        if (m_ApiClient == null)
        {
            if (!TryNavigateLocally(startNodeId, destinationNodeId, out string localError))
            {
                m_PathVisualizer.ClearPath();
                m_UI.UpdateNavigationGuidance("", "");
                m_UI.ShowStatus($"No path found. {localError}");
                RefreshControls();
            }
            else
            {
                m_UI.ShowStatus("Navigation active (offline map).");
                UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
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
                    UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
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
                    UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
                    return;
                }

                m_PathVisualizer.ClearPath();
                m_UI.UpdateNavigationGuidance("", "");
                m_UI.ShowStatus($"No path found. {error} {localError}");
                RefreshControls();
            }));
    }

    private void HandleLocationsLoaded(List<LocationData> locations)
    {
        int count = locations?.Count ?? 0;
        Debug.Log($"[NavigationFlowController] HandleLocationsLoaded: {count} locations");

        m_UseLocalRouting = false;
        m_LocationRegistry.Clear();
        m_LocationRegistry.SetLocations(locations);
        
        PopulateBuildingOptions();
        Debug.Log($"[NavigationFlowController] Building options: {m_BuildingOptions.Count}");
        
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
        m_LastRawPathResponse = response;

        if (response == null || response.path == null || response.path.Count < 2)
        {
            m_PathVisualizer.ClearPath();
            m_UI.UpdateNavigationGuidance("", "");
            m_UI.ShowStatus("No valid path was returned by the backend.");
            RefreshControls();
            return;
        }
        RebuildPathVisuals();
    }

    private void RebuildPathVisuals()
    {
        if (m_LastRawPathResponse == null || m_LastRawPathResponse.path == null || m_LastRawPathResponse.path.Count < 2)
            return;

        m_ActiveWorldPath = null;
        
        // Deep copy the path so we don't truncate the original on floor transitions
        CampusApiClient.PathResponsePayload response = new CampusApiClient.PathResponsePayload
        {
            path = new List<CampusApiClient.PathPointPayload>(m_LastRawPathResponse.path),
            directions = m_LastRawPathResponse.directions
        };

        // ── Detect floor transitions BEFORE coordinate transformation ──
        // We need the original floor/building data from the response.
        List<PathVisualizer.FloorTransition> transitions = new List<PathVisualizer.FloorTransition>();
        
        m_IsPendingTransition = false;
        if (m_UI != null && m_UI.FloorTransitionButton != null)
            m_UI.FloorTransitionButton.gameObject.SetActive(false);

        int endIndex = response.path.Count - 1;

        for (int i = m_CurrentPathStartIndex; i < response.path.Count - 1; i++)
        {
            CampusApiClient.PathPointPayload current = response.path[i];
            CampusApiClient.PathPointPayload next = response.path[i + 1];

            if (current.floor != next.floor)
            {
                // Determine transition type using the node 'type' field from the backend.
                string currentType = (current.type ?? "").ToUpper();
                string nextType = (next.type ?? "").ToUpper();
                string currentId = (current.id ?? "").ToUpper();
                string nextId = (next.id ?? "").ToUpper();

                bool isLift = currentType.Contains("LIFT") || nextType.Contains("LIFT") ||
                              currentId.Contains("LIFT") || nextId.Contains("LIFT");

                Debug.Log($"[NavigationFlowController] Floor transition at segment {i}: " +
                          $"{(isLift ? "Lift" : "Stairs")} Floor {current.floor} → {next.floor}");
                          
                m_IsPendingTransition = true;
                m_PendingFloorNextNodeId = next.id;
                m_PendingFloorNextFloor = next.floor;
                m_PendingTransitionType = isLift ? "Lift" : "Stairs";
                m_NextPathStartIndex = i + 1; // Mark the index where the next floor starts
                
                transitions.Add(new PathVisualizer.FloorTransition
                {
                    segmentStartIndex = i - m_CurrentPathStartIndex,
                    fromFloor = current.floor,
                    toFloor = next.floor,
                    goingUp = next.floor > current.floor,
                    type = isLift ? PathVisualizer.TransitionType.Lift : PathVisualizer.TransitionType.Staircase
                });
                
                endIndex = i; // Stop rendering at the stairs
                break;
            }
        }

        // Sub-slice the path so we only render up to the transition
        if (m_CurrentPathStartIndex < response.path.Count)
        {
            int count = (endIndex - m_CurrentPathStartIndex) + 1;
            response.path = response.path.GetRange(m_CurrentPathStartIndex, count);
        }

        List<Vector3> worldPath = new List<Vector3>();
        foreach (CampusApiClient.PathPointPayload point in response.path)
            worldPath.Add(new Vector3(point.x, point.y, point.z));

        // --- AR ALIGNMENT using Walk-to-Calibrate yaw offset ---
        // Backend points are in map space. We map them into world space by:
        // 1) picking a stable world anchor position from AR plane raycast near screen center
        // 2) using the calibrated yaw offset (computed from walk-to-calibrate)
        if (worldPath.Count > 0)
        {
            // 1) Get a stable world anchor point
            Vector3 worldAnchorPos = Vector3.zero;
            Vector3 mapAnchorPos = worldPath[0]; // Fallback to start of path
            
            if (m_HasFloorTransitionAnchor && m_CurrentPathStartIndex > 0 && m_CurrentPathStartIndex < m_LastRawPathResponse.path.Count)
            {
                // Lock the AR map anchor to the physical location where the user clicked "Resume"
                worldAnchorPos = m_FloorTransitionARPos;
                var startNode = m_LastRawPathResponse.path[m_CurrentPathStartIndex];
                mapAnchorPos = new Vector3(startNode.x, startNode.y, startNode.z);
            }
            else if (m_QRLocationManager != null && m_QRLocationManager.HasLocation)
            {
                // Lock the AR map anchor to the exact physical location of the QR scan!
                worldAnchorPos = m_QRLocationManager.ScanCameraPosition;
                
                // Get the grid map coordinates of the scanned node
                LocationData startNode = m_LocationRegistry.GetLocation(m_QRLocationManager.CurrentNodeId);
                if (startNode != null)
                {
                    mapAnchorPos = new Vector3(startNode.x, startNode.y, startNode.z);
                }
                
                // Project the camera's scan height down to the physical AR floor
                if (TryGetWorldAnchorFromRaycast(out Vector3 floorAnchor))
                    worldAnchorPos.y = floorAnchor.y;
                else
                    worldAnchorPos.y -= 1.5f; // Fallback to 1.5m below camera if no planes
            }
            else
            {
                // Uncalibrated fallback
                bool gotAnchor = TryGetWorldAnchorFromRaycast(out worldAnchorPos);
                if (!gotAnchor)
                {
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
                        m_UI.UpdateNavigationGuidance("↑", "Calculating...");
                        m_UI.ShowStatus("Navigation active (unaligned).");
                        RefreshControls();
                        return;
                    }
                }
            }

            // 2) Use fixed QR orientation math
            // The physical poster's orientation in the map grid is the QR node's rotation_y
            // When looking at the poster, the user is facing the opposite direction (poster + 180).
            // Thus, the camera's Y rotation (ScanCameraRotationY) in the AR session corresponds to (poster + 180) in the map.
            float nodeRotY = 0f;
            if (m_QRLocationManager != null && m_QRLocationManager.HasLocation)
            {
                LocationData qrNode = m_LocationRegistry.GetLocation(m_QRLocationManager.CurrentNodeId);
                if (qrNode != null)
                {
                    nodeRotY = qrNode.rotation_y;
                }
            }
            else if (response.path.Count > 0)
            {
                nodeRotY = response.path[0].rotation_y;
            }
            float camYaw = m_QRLocationManager != null ? m_QRLocationManager.ScanCameraRotationY : 0f;
            float yawOffset = camYaw - (nodeRotY + 180f) + m_MapCompassOffset;

            Quaternion rotationOffset = Quaternion.Euler(0f, yawOffset, 0f);

            // 3) Transform all points:
            // translate so mapAnchorPos lands at worldAnchorPos, then rotate around Y
            List<Vector3> transformed = new List<Vector3>(worldPath.Count);
            for (int i = 0; i < worldPath.Count; i++)
            {
                Vector3 p = worldPath[i];
                Vector3 v = p - mapAnchorPos;    // local vector relative to static map anchor
                v *= m_MetersPerGridUnit;        // scale grid units to real-world meters
                v = rotationOffset * v;          // rotate heading
                Vector3 outPos = new Vector3(
                    worldAnchorPos.x + v.x,
                    worldAnchorPos.y + (p.y - mapAnchorPos.y) * m_MetersPerGridUnit, // keep scaled vertical delta
                    worldAnchorPos.z + v.z
                );
                transformed.Add(outPos);
            }
            
            // 4) Dynamic User Snapping & Path Trimming
            // If the user has already walked past the first node, simply prepending their position
            // causes the path to zig-zag backwards. Instead, we find the closest segment on the path,
            // trim everything behind them, and start exactly from their feet.
            Transform cam = Camera.main != null ? Camera.main.transform : null;
            if (cam != null && transformed.Count > 1)
            {
                Vector3 userPos = cam.position;
                // We do NOT project userPos.y to transformed[0].y anymore, because the path
                // goes up and down multiple floors. We use true 3D distance!
                
                int closestSegment = 0;
                float closestDist = float.MaxValue;

                for (int i = 0; i < transformed.Count - 1; i++)
                {
                    Vector3 start = transformed[i];
                    Vector3 end = transformed[i + 1];
                    Vector3 closestPt = ClosestPointOnLineSegment(userPos, start, end);
                    float dist = Vector3.Distance(userPos, closestPt);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        closestSegment = i;
                    }
                }

                // If they are reasonably close to the path (e.g. within 8 meters), snap and trim!
                if (closestDist < 8.0f)
                {
                    List<Vector3> trimmed = new List<Vector3>();
                    trimmed.Add(userPos); // Start exactly at the user
                    
                    // Add the rest of the path from the END of the closest segment onwards
                    for (int i = closestSegment + 1; i < transformed.Count; i++)
                    {
                        // Prevent adding nodes that are too close to the user to avoid sharp kinks
                        if (Vector3.Distance(userPos, transformed[i]) > 0.5f)
                        {
                            trimmed.Add(transformed[i]);
                        }
                    }
                    
                    // If we accidentally trimmed everything, keep at least the destination
                    if (trimmed.Count == 1)
                        trimmed.Add(transformed[transformed.Count - 1]);
                        
                    transformed = trimmed;
                }
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
        
        m_UI.UpdateNavigationGuidance("↑", "Calculating..."); // Initial state, will be overwritten in Update()
        
        m_UI.ShowStatus("Navigation active.");
        UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
        RefreshControls();
    }

    private Vector3 ClosestPointOnLineSegment(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float sqrMag = ab.sqrMagnitude;
        if (sqrMag < 0.0001f) return a;
        
        float t = Vector3.Dot(p - a, ab) / sqrMag;
        t = Mathf.Clamp01(t);
        return a + t * ab;
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

        // ── Off-path detection & recalculation ──
        if (minDistance > OFF_PATH_DISTANCE)
        {
            m_OffPathTimer += Time.deltaTime;
            if (m_OffPathTimer >= OFF_PATH_TIMEOUT && (Time.time - m_LastRecalcTime) > RECALC_COOLDOWN)
            {
                Debug.Log($"[NavigationFlowController] User is {minDistance:F1}m off-path. Recalculating...");
                UpdateGuidance("Recalculating route...", new Color(1f, 0.85f, 0.2f, 1f), true);
                TryRecalculateFromNearestNode();
                m_OffPathTimer = 0f;
                return;
            }
        }
        else
        {
            m_OffPathTimer = 0f;
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
                
                if (m_IsPendingTransition)
                {
                    m_UI.UpdateNavigationGuidance("⬆", $"Take {m_PendingTransitionType}");
                    UpdateGuidance($"Take {m_PendingTransitionType} to Floor {m_PendingFloorNextFloor}", new Color(0.2f, 0.6f, 1f, 1f));
                    m_UI.ShowStatus($"Arrived at {m_PendingTransitionType}.");
                    
                    if (m_UI.FloorTransitionText != null)
                        m_UI.FloorTransitionText.text = $"RESUME ON FLOOR {m_PendingFloorNextFloor}";
                    if (m_UI.FloorTransitionButton != null)
                        m_UI.FloorTransitionButton.gameObject.SetActive(true);
                }
                else
                {
                    m_UI.ShowStatus("Destination Reached!");
                    m_UI.UpdateNavigationGuidance("📍", "Arrived");
                    UpdateGuidance("You've arrived!", new Color(0.2f, 0.9f, 0.4f, 1f));
                    m_ActiveDestinationId = null;
                }
                
                m_ActiveWorldPath = null;
                m_ActiveTransitions = null;
            }
            else
            {
                m_PathVisualizer.DrawPath(m_ActiveWorldPath, m_ActiveTransitions);
            }
        }
        
        // ── Dynamic Turn-by-Turn Guidance ──
        if (m_ActiveWorldPath != null && m_ActiveWorldPath.Count >= 2)
        {
            (NavManeuver maneuver, float dist) = GetNextManeuver(userPos, m_ActiveWorldPath);
            UpdateManeuverUI(maneuver, dist);
        }
    }

    private void UpdateManeuverUI(NavManeuver maneuver, float distance)
    {
        string icon = "";
        string text = "";
        string distText = distance < 2f ? "now" : $"in {Mathf.RoundToInt(distance)}m";

        switch (maneuver)
        {
            case NavManeuver.Straight:
                icon = "↑";
                text = "Continue straight";
                // Don't show distance for straight unless it's the very end
                if (m_ActiveWorldPath != null && m_ActiveWorldPath.Count <= 2)
                    text = $"Destination {distText}";
                break;
            case NavManeuver.SlightLeft:
                icon = "←";
                text = $"Slight left {distText}";
                break;
            case NavManeuver.TurnLeft:
                icon = "←";
                text = $"Turn left {distText}";
                break;
            case NavManeuver.SlightRight:
                icon = "→";
                text = $"Slight right {distText}";
                break;
            case NavManeuver.TurnRight:
                icon = "→";
                text = $"Turn right {distText}";
                break;
            case NavManeuver.Arrived:
                icon = "📍";
                text = "Arrived";
                break;
        }

        m_UI.UpdateNavigationGuidance(icon, text);
    }

    private (NavManeuver, float) GetNextManeuver(Vector3 userPos, List<Vector3> path)
    {
        if (path.Count == 0) return (NavManeuver.Arrived, 0f);
        if (path.Count == 1) return (NavManeuver.Arrived, Vector3.Distance(userPos, path[0]));

        float totalDist = Vector3.Distance(userPos, path[0]);
        Vector3 currentDir = (path[0] - userPos).normalized;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector3 seg = path[i + 1] - path[i];
            // Ignore very short segments
            if (seg.sqrMagnitude < 0.04f) continue;
            
            Vector3 nextDir = seg.normalized;
            float angle = Vector3.SignedAngle(currentDir, nextDir, Vector3.up);
            
            if (Mathf.Abs(angle) > 20f)
            {
                NavManeuver maneuver = NavManeuver.Straight;
                if (angle >= 60f) maneuver = NavManeuver.TurnRight;
                else if (angle > 20f) maneuver = NavManeuver.SlightRight;
                else if (angle <= -60f) maneuver = NavManeuver.TurnLeft;
                else if (angle < -20f) maneuver = NavManeuver.SlightLeft;

                return (maneuver, totalDist);
            }
            
            totalDist += seg.magnitude;
            currentDir = nextDir;
        }

        return (NavManeuver.Straight, totalDist);
    }

    private void UpdateGuidance(string text, Color color, bool pulse = false, float progress = -1f)
    {
        if (m_UI == null) return;
        m_UI.ShowGuidance(text, color, pulse, progress);
    }

    // ── Turn-by-Turn Enums ───────────────────────────────────────────────────
    private enum NavManeuver
    {
        Straight,
        SlightRight,
        TurnRight,
        SlightLeft,
        TurnLeft,
        Arrived
    }

    private bool m_IsSimulating = false;

    private void HandleFloorTransitionResume()
    {
        if (!m_IsPendingTransition || string.IsNullOrEmpty(m_PendingFloorNextNodeId) || m_QRLocationManager == null)
            return;

        if (m_UI != null && m_UI.FloorTransitionButton != null)
            m_UI.FloorTransitionButton.gameObject.SetActive(false);
            
        m_IsPendingTransition = false;
        
        // Fast-forward the path index to the next floor and redraw.
        // We do NOT recalculate from the backend so that we preserve the original AR anchor!
        m_CurrentPathStartIndex = m_NextPathStartIndex;
        
        // Safely update the floor for snapping logic without breaking the AR anchor
        m_QRLocationManager.UpdateFloor(m_PendingFloorNextFloor);
        
        RebuildPathVisuals();
    }

    private void TryRecalculateFromNearestNode()
    {
        if (string.IsNullOrEmpty(m_ActiveDestinationId))
            return;

        m_LastRecalcTime = Time.time;

        // Find the nearest graph node to the user's actual AR position
        string startNodeId = FindNearestGraphNode();
        if (string.IsNullOrEmpty(startNodeId))
            startNodeId = m_QRLocationManager.CurrentNodeId; // Fallback to QR node
        
        string destId = m_ActiveDestinationId;

        Debug.Log($"[NavigationFlowController] Recalculating from nearest node: {startNodeId} → {destId}");

        if (m_ApiClient == null)
        {
            if (TryNavigateLocally(startNodeId, destId, out string localError))
            {
                m_UI.ShowStatus("Route recalculated.");
                UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
            }
            else
            {
                Debug.LogWarning($"[NavigationFlowController] Recalculation failed: {localError}");
                UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
            }
            return;
        }

        StartCoroutine(m_ApiClient.RequestPath(
            startNodeId,
            destId,
            response =>
            {
                HandlePathResponse(response);
                m_UI.ShowStatus("Route recalculated.");
                UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
            },
            error =>
            {
                if (TryNavigateLocally(startNodeId, destId, out _))
                {
                    m_UI.ShowStatus("Route recalculated (offline).");
                    UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
                    return;
                }
                Debug.LogWarning($"[NavigationFlowController] Recalculation failed: {error}");
                UpdateGuidance("Follow the arrows to your destination", new Color(1f, 0.6f, 0.2f, 1f));
            }));
    }

    /// <summary>
    /// Finds the graph node nearest to the user's current AR position by performing
    /// an inverse transformation from AR world space back to map space.
    /// This enables accurate path re-routing even when the user has walked away from the QR node.
    /// </summary>
    private string FindNearestGraphNode()
    {
        if (m_QRLocationManager == null || !m_QRLocationManager.HasLocation)
            return null;
        if (m_LocationRegistry == null || !m_LocationRegistry.IsLoaded)
            return null;
        
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        if (cam == null) return null;

        // Get the user's AR position
        Vector3 userARPos = cam.position;

        // Get the anchor points (same logic as HandlePathResponse)
        Vector3 worldAnchorPos = m_QRLocationManager.ScanCameraPosition;
        LocationData qrNode = m_LocationRegistry.GetLocation(m_QRLocationManager.CurrentNodeId);
        if (qrNode == null) return null;

        Vector3 mapAnchorPos = new Vector3(qrNode.x, qrNode.y, qrNode.z);

        // Get calibrated yaw offset
        float nodeRotY = qrNode.rotation_y;
        float camYaw = m_QRLocationManager != null ? m_QRLocationManager.ScanCameraRotationY : 0f;
        float yawOffset = camYaw - (nodeRotY + 180f) + m_MapCompassOffset;

        // Inverse transform: AR world → Map space
        // Forward: mapPoint → (p - mapAnchor) * scale → rotate(yaw) → + worldAnchor = arPoint
        // Inverse: arPoint → (arPoint - worldAnchor) → rotate(-yaw) → / scale → + mapAnchor = mapPoint
        Quaternion inverseRotation = Quaternion.Euler(0f, -yawOffset, 0f);
        Vector3 arDelta = userARPos - worldAnchorPos;
        arDelta.y = 0f; // Project to XZ plane
        Vector3 mapDelta = inverseRotation * arDelta;
        
        if (m_MetersPerGridUnit > 0.001f)
            mapDelta /= m_MetersPerGridUnit;

        Vector3 userMapPos = mapAnchorPos + mapDelta;

        // Find the closest node on the same floor
        int userFloor = m_QRLocationManager.CurrentFloor;
        string bestNodeId = null;
        float bestDist = float.MaxValue;

        foreach (LocationData loc in m_LocationRegistry.GetAllLocations())
        {
            if (loc == null || string.IsNullOrEmpty(loc.id)) continue;
            if (loc.floor != userFloor) continue;

            float dx = loc.x - userMapPos.x;
            float dz = loc.z - userMapPos.z;
            float dist = dx * dx + dz * dz; // sqrMagnitude for speed

            if (dist < bestDist)
            {
                bestDist = dist;
                bestNodeId = loc.id;
            }
        }

        if (bestNodeId != null)
            Debug.Log($"[NavigationFlowController] Nearest node: {bestNodeId} (dist={Mathf.Sqrt(bestDist):F1} grid units from user)");

        return bestNodeId;
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

        ARRaycastManager raycastManager = FindObjectOfType<ARRaycastManager>();
        if (raycastManager == null)
            return false;

        var hits = new List<ARRaycastHit>();
        Transform cam = Camera.main != null ? Camera.main.transform : null;

        // Strategy 1: Raycast straight down from the camera.
        // This guarantees we hit the floor beneath the user, not a wall in the distance.
        if (cam != null)
        {
            Ray downRay = new Ray(cam.position, Vector3.down);
            if (raycastManager.Raycast(downRay, hits, TrackableType.PlaneWithinPolygon) && hits.Count > 0)
            {
                worldAnchorPos = hits[0].pose.position;
                return true;
            }
        }

        // Strategy 2: Raycast from screen center (fallback if no floor directly below)
        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        if (raycastManager.Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in hits)
            {
                // Filter out vertical wall planes by ensuring the hit is significantly below eye level
                if (cam == null || hit.pose.position.y < cam.position.y - 0.5f)
                {
                    worldAnchorPos = hit.pose.position;
                    return true;
                }
            }
        }

        return false;
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
        var all = m_LocationRegistry != null ? m_LocationRegistry.GetAllLocations() : null;
        if (all == null) yield break;

        var destinations = all.Where(location =>
            location != null &&
            !string.IsNullOrEmpty(location.id) &&
            location.type != "corridor" &&
            location.type != "wall" &&
            !string.IsNullOrWhiteSpace(location.displayName)
        ).ToList();

        foreach (var dest in destinations)
        {
            yield return dest;
        }
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
        // A physical QR scan acts as a hard override for calibration.
        // We no longer rely on a floating floor transition anchor if they scan a physical code.
        m_HasFloorTransitionAnchor = false;

        RefreshControls();

        if (m_UI != null && !string.IsNullOrEmpty(nodeId))
        {
            LocationData loc = m_LocationRegistry != null ? m_LocationRegistry.GetLocation(nodeId) : null;
            string name = loc != null ? loc.displayName : nodeId;
            m_UI.ShowStatus($"Location: {name}");
        }
    }

    /// <summary>
    /// Called by GPSLocationService when the user is detected near a known building.
    /// Automatically pre-selects the building in the dropdown for convenience.
    /// </summary>
    private void HandleGPSBuildingDetected(string buildingName)
    {
        if (m_UI == null || m_BuildingOptions.Count == 0) return;

        // Find the matching building in our dropdown options (case-insensitive, trimmed)
        string trimmedName = buildingName.Trim();
        for (int i = 0; i < m_BuildingOptions.Count; i++)
        {
            if (string.Equals(m_BuildingOptions[i].Trim(), trimmedName, System.StringComparison.OrdinalIgnoreCase))
            {
                if (m_UI.BuildingDropdown.value != i)
                {
                    Debug.Log($"[NavigationFlowController] GPS auto-selected building: {buildingName}");
                    m_UI.BuildingDropdown.value = i;
                    HandleBuildingChanged(i);
                    m_UI.ShowStatus($"GPS detected: {trimmedName}");
                }
                return;
            }
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
        if (m_GPSService != null)
            m_GPSService.OnBuildingDetected -= HandleGPSBuildingDetected;
    }
}
