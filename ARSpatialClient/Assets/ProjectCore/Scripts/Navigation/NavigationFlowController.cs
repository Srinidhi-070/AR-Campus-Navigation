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
        
        if (m_ApiClient == null || m_LocationRegistry == null || m_UI == null)
        {
            Debug.LogError($"[NavigationFlowController] Missing dependencies: ApiClient={m_ApiClient != null}, LocationRegistry={m_LocationRegistry != null}, UI={m_UI != null}");
            return;
        }

        m_UI.ShowStatus("Loading campus map...");
        RefreshControls();
        
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

        StartCoroutine(m_ApiClient.RequestPath(
            m_QRLocationManager.CurrentNodeId,
            destination.id,
            HandlePathResponse,
            error =>
            {
                m_PathVisualizer.ClearPath();
                m_UI.ShowDirections(new List<string>());
                m_UI.ShowStatus($"No path found. {error}");
                RefreshControls();
            }));
    }

    private void HandleLocationsLoaded(List<LocationData> locations)
    {
        Debug.Log($"[NavigationFlowController] HandleLocationsLoaded called with {locations?.Count ?? 0} locations");
        
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
        m_LocationRegistry.Clear();
        PopulateBuildingOptions();
        
        // Show user-friendly error message
        m_UI.ShowStatus("Backend offline. Use QR to navigate.");
        
        // Enable basic functionality even when backend is down
        if (m_UI != null)
        {
            m_UI.QRButton.interactable = true;  // Always allow QR scanning
            m_UI.MenuButton.interactable = true; // Always allow menu access
        }
        
        Debug.LogWarning($"[NavigationFlowController] Backend error: {error}");
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

        // --- CRITICAL AR ALIGNMENT ---
        // The backend returns coordinates in the map's absolute space (e.g. 10, 0, 5)
        // but the AR Camera could be anywhere in Unity's world space depending on where the app launched.
        // Since the user just scanned the QR code, we assume they are physically at worldPath[0].
        // We shift the entire path so the start node aligns perfectly with the user's feet!
        if (Camera.main != null && worldPath.Count > 0)
        {
            Vector3 camPos = Camera.main.transform.position;
            Vector3 mapStart = worldPath[0];
            
            // Offset X and Z exactly to the camera. Offset Y to place arrows slightly below eye level (on the floor).
            Vector3 offset = new Vector3(camPos.x - mapStart.x, (camPos.y - 1.2f) - mapStart.y, camPos.z - mapStart.z);

            for (int i = 0; i < worldPath.Count; i++)
            {
                worldPath[i] += offset;
            }
        }

        m_PathVisualizer.ClearPath();
        m_PathVisualizer.DrawPath(worldPath);
        m_UI.ShowDirections(response.directions ?? new List<string>());
        m_UI.ShowStatus("Navigation active.");
        RefreshControls();
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
            m_UI.ShowStatus($"📍 Location: {name}");
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
