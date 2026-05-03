using UnityEngine;

/// <summary>
/// Central entry point. Runs before all other scripts via DefaultExecutionOrder.
/// Owns references to every manager subsystem.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AppController : MonoBehaviour
{
    public static AppController Instance { get; private set; }

    [Header("Runtime Mode")]
    [SerializeField] private bool m_UseProductionRuntime = true;

    [Header("Navigation")]
    [SerializeField] private NavigationManager  m_NavigationManager;
    [SerializeField] private PathfindingManager m_PathfindingManager;
    [SerializeField] private PathVisualizer     m_PathVisualizer;
    [SerializeField] private DirectionManager   m_DirectionManager;

    [Header("AI")]
    [SerializeField] private AIManager          m_AIManager;

    [Header("UI")]
    [SerializeField] private UIManager          m_UIManager;

    [Header("Data")]
    [SerializeField] private LocationRegistry   m_LocationRegistry;

    [Header("Indoor Map (optional)")]
    [SerializeField] private MapManager         m_MapManager;
    [SerializeField] private GridManager        m_GridManager;

    [Header("QR System")]
    [SerializeField] private QRLocationManager  m_QRLocationManager;

    public NavigationManager Navigation => m_NavigationManager != null ? m_NavigationManager : (m_NavigationManager = FindObjectOfType<NavigationManager>());
    public PathfindingManager Pathfinding => m_PathfindingManager != null ? m_PathfindingManager : (m_PathfindingManager = FindObjectOfType<PathfindingManager>());
    public PathVisualizer Visualizer => m_PathVisualizer != null ? m_PathVisualizer : (m_PathVisualizer = FindObjectOfType<PathVisualizer>());
    public DirectionManager Directions => m_DirectionManager != null ? m_DirectionManager : (m_DirectionManager = FindObjectOfType<DirectionManager>());
    public AIManager AI => m_AIManager != null ? m_AIManager : (m_AIManager = FindObjectOfType<AIManager>());
    public UIManager UI => m_UIManager != null ? m_UIManager : (m_UIManager = FindObjectOfType<UIManager>());
    public LocationRegistry Locations => m_LocationRegistry != null ? m_LocationRegistry : (m_LocationRegistry = FindObjectOfType<LocationRegistry>());
    public MapManager IndoorMap => m_MapManager != null ? m_MapManager : (m_MapManager = FindObjectOfType<MapManager>());
    public GridManager Grid => m_GridManager != null ? m_GridManager : (m_GridManager = FindObjectOfType<GridManager>());
    public QRLocationManager QRLocations => m_QRLocationManager != null ? m_QRLocationManager : (m_QRLocationManager = FindObjectOfType<QRLocationManager>());

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureCoreReferences();

        if (GetComponent<CampusRuntimeInstaller>() == null)
            gameObject.AddComponent<CampusRuntimeInstaller>();

        if (!m_UseProductionRuntime)
            ValidateLegacyReferences();
    }

    private void EnsureCoreReferences()
    {
        if (m_UIManager == null)
            m_UIManager = FindObjectOfType<UIManager>();
        if (m_UIManager == null)
            m_UIManager = gameObject.AddComponent<UIManager>();

        if (m_LocationRegistry == null)
            m_LocationRegistry = FindObjectOfType<LocationRegistry>();
        if (m_LocationRegistry == null)
            m_LocationRegistry = gameObject.AddComponent<LocationRegistry>();

        if (m_QRLocationManager == null)
            m_QRLocationManager = FindObjectOfType<QRLocationManager>();
        if (m_QRLocationManager == null)
            m_QRLocationManager = gameObject.AddComponent<QRLocationManager>();
    }

    private void ValidateLegacyReferences()
    {
        if (m_NavigationManager  == null) Debug.LogError("[AppController] NavigationManager not assigned.");
        if (m_PathfindingManager == null) Debug.LogError("[AppController] PathfindingManager not assigned.");
        if (m_PathVisualizer     == null) Debug.LogError("[AppController] PathVisualizer not assigned.");
        if (m_DirectionManager   == null) Debug.LogError("[AppController] DirectionManager not assigned.");
        if (m_AIManager          == null) Debug.LogError("[AppController] AIManager not assigned.");
        if (m_UIManager          == null) Debug.LogError("[AppController] UIManager not assigned.");
        if (m_LocationRegistry   == null) Debug.LogError("[AppController] LocationRegistry not assigned.");
        if (m_MapManager         == null) Debug.LogWarning("[AppController] MapManager not assigned (optional).");
        if (m_GridManager        == null) Debug.LogWarning("[AppController] GridManager not assigned (optional).");
    }
}
