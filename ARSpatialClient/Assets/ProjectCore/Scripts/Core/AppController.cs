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
    [SerializeField] private string m_BaseUrl = "http://127.0.0.1:8000";

    // Optional override so you can quickly switch backend URLs after deploying
    // (you can set this from code later, or via PlayerSettings in a debug workflow).
    private const string BaseUrlPlayerPrefsKey = "backend_base_url";

    private LocationRegistry m_LocationRegistry;
    private QRLocationManager m_QRLocationManager;

    public LocationRegistry Locations => m_LocationRegistry;
    public QRLocationManager QRLocations => m_QRLocationManager;
    public string BaseUrl => m_BaseUrl;

    void Awake()
    {
        Debug.Log("[AppController] Awake called");

        // If previously set, override inspector default.
        if (PlayerPrefs.HasKey(BaseUrlPlayerPrefsKey))
        {
            string savedUrl = PlayerPrefs.GetString(BaseUrlPlayerPrefsKey);
            if (!string.IsNullOrWhiteSpace(savedUrl))
            {
                m_BaseUrl = savedUrl.Trim();
                Debug.Log($"[AppController] Base URL overridden from PlayerPrefs: {m_BaseUrl}");
            }
        }
        
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
        PlayerPrefs.SetString(BaseUrlPlayerPrefsKey, m_BaseUrl);
        PlayerPrefs.Save();
        Debug.Log($"[AppController] Base URL set to: {m_BaseUrl}");
    }
}
