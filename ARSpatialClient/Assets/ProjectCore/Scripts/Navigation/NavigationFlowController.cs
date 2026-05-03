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

        if (AppController.Instance?.UI != null)
            AppController.Instance.UI.SetTextTargets(ui.DirectionText, ui.StatusText);

        RefreshControls();
    }

    public void BeginLoad()
    {
        if (m_ApiClient == null || m_LocationRegistry == null || m_UI == null)
            return;

        m_UI.ShowStatus("Loading campus map...");
        RefreshControls();
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
        if (m_RoomOptions.Count == 0)
        {
            m_UI.ShowStatus("No destination available.");
            RefreshControls();
            return;
        }

        int roomIndex = Mathf.Clamp(m_UI.RoomDropdown.value, 0, m_RoomOptions.Count - 1);
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
        m_LocationRegistry.SetLocations(locations);
        PopulateBuildingOptions();
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
        RefreshControls();
        m_UI.ShowStatus($"Could not load campus data. {error}");
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

        m_PathVisualizer.ClearPath();
        m_PathVisualizer.DrawPath(worldPath);
        m_UI.ShowDirections(response.directions ?? new List<string>());
        m_UI.ShowStatus("Navigation active.");
        RefreshControls();
    }

    private IEnumerable<LocationData> GetDestinationLocations()
    {
        return m_LocationRegistry.GetAllLocations().Where(location =>
            location != null &&
            !string.IsNullOrEmpty(location.id) &&
            location.type != "corridor" &&
            location.type != "entrance" &&
            location.type != "staircase" &&
            location.type != "lift" &&
            location.type != "wall");
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
        RefreshControls();
    }

    private void HandleQrLocationChanged(string nodeId)
    {
        RefreshControls();
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
