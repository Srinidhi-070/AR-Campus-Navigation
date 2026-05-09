using UnityEngine;

/// <summary>
/// Central entry point. Runs before all other scripts via DefaultExecutionOrder.
/// Automatically installs the runtime system via CampusRuntimeInstaller.
/// </summary>
[DefaultExecutionOrder(-100)]
public class AppController : MonoBehaviour
{
    public static AppController Instance { get; private set; }

    [Header("Backend Configuration")]
    [SerializeField] private string m_BaseUrl = "http://192.168.1.4:8000";

    private LocationRegistry m_LocationRegistry;
    private QRLocationManager m_QRLocationManager;

    public LocationRegistry Locations => m_LocationRegistry;
    public QRLocationManager QRLocations => m_QRLocationManager;
    public string BaseUrl => m_BaseUrl;

    void Awake()
    {
        Debug.Log("[AppController] Awake called");
        
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[AppController] Duplicate instance detected, destroying");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        EnsureCoreComponents();
        EnsureRuntimeInstaller();
        
        Debug.Log("[AppController] Initialization complete");
    }

    private void EnsureCoreComponents()
    {
        m_LocationRegistry = GetComponent<LocationRegistry>();
        if (m_LocationRegistry == null)
            m_LocationRegistry = gameObject.AddComponent<LocationRegistry>();

        m_QRLocationManager = GetComponent<QRLocationManager>();
        if (m_QRLocationManager == null)
            m_QRLocationManager = gameObject.AddComponent<QRLocationManager>();
        
        Debug.Log("[AppController] Core components ensured");
    }

    private void EnsureRuntimeInstaller()
    {
        if (GetComponent<CampusRuntimeInstaller>() == null)
        {
            Debug.Log("[AppController] Adding CampusRuntimeInstaller");
            gameObject.AddComponent<CampusRuntimeInstaller>();
        }
        else
        {
            Debug.Log("[AppController] CampusRuntimeInstaller already present");
        }
    }

    public void SetBaseUrl(string url)
    {
        m_BaseUrl = url;
        Debug.Log($"[AppController] Base URL set to: {m_BaseUrl}");
    }
}
