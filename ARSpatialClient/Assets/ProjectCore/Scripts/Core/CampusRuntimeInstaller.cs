using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CampusRuntimeInstaller : MonoBehaviour
{
    private bool m_Initialized;
    private CampusRuntimeUI m_UI;

    void Awake()
    {
        Debug.Log("[CampusRuntimeInstaller] Awake called");
        
        if (m_Initialized)
        {
            Debug.Log("[CampusRuntimeInstaller] Already initialized, skipping");
            return;
        }

        m_Initialized = true;
        DisableLegacySceneUI();
        DisableLegacyRuntimeComponents();
        InstallRuntime();
        
        Debug.Log("[CampusRuntimeInstaller] Installation complete");
    }

    private void DisableLegacySceneUI()
    {
        // Disable ALL existing canvases first
        foreach (Canvas canvas in FindObjectsOfType<Canvas>(true))
        {
            if (canvas != null)
            {
                Debug.Log($"[CampusRuntimeInstaller] Disabling canvas: {canvas.gameObject.name}");
                canvas.gameObject.SetActive(false);
            }
        }

        // Disable legacy UI GameObjects
        string[] legacyNames =
        {
            "Canvas",
            "UIBuilder",
            "DebugMenu",
            "PromptWindow",
            "Greeting Prompt",
            "Options Modal",
            "Coaching UI",
            "Object Menu",
            "Button (Triangle)",
            "Button (Cube Debug)",
            "Button (Pyramid)",
            "Button (Cube)",
            "Button (Cylinder)",
            "Button (Arch)",
            "Button (Torus)",
            "Hints Button",
            "Options Button",
            "Create Button",
            "Delete Button",
            "Remove Objects Button"
        };

        foreach (string legacyName in legacyNames)
        {
            GameObject legacy = GameObject.Find(legacyName);
            if (legacy != null)
            {
                Debug.Log($"[CampusRuntimeInstaller] Disabling legacy UI: {legacyName}");
                legacy.SetActive(false);
            }
        }
    }

    private void DisableLegacyRuntimeComponents()
    {
        DisableIfPresent<AIManager>();
        DisableIfPresent<IndoorNavigationBridge>();
        DisableIfPresent<IndoorPathfinding>();
        DisableIfPresent<ModernUIBuilder>();
        DisableIfPresent<NavigationManager>();
        DisableIfPresent<PathfindingManager>();
    }

    private void InstallRuntime()
    {
        // Add AR Foundation components first
        ARFoundationBootstrap arBootstrap = GetOrCreateComponent<ARFoundationBootstrap>(gameObject);
        
        CampusRuntimeUI ui = GetComponent<CampusRuntimeUI>();
        if (ui == null)
            ui = gameObject.AddComponent<CampusRuntimeUI>();
        ui.EnsureBuilt();

        CampusApiClient apiClient = GetOrCreateComponent<CampusApiClient>(gameObject);
        ModeManager modeManager = GetOrCreateComponent<ModeManager>(gameObject);
        CampusRuntimeValidator runtimeValidator = GetOrCreateComponent<CampusRuntimeValidator>(gameObject);
        NavigationFlowController navigationFlow = GetOrCreateComponent<NavigationFlowController>(gameObject);

        LocationRegistry locationRegistry = AppController.Instance != null
            ? AppController.Instance.Locations
            : FindObjectOfType<LocationRegistry>();
        if (locationRegistry == null)
            locationRegistry = gameObject.AddComponent<LocationRegistry>();

        QRLocationManager qrLocationManager = FindObjectOfType<QRLocationManager>();
        if (qrLocationManager == null)
            qrLocationManager = gameObject.AddComponent<QRLocationManager>();

        QRScanner qrScanner = FindObjectOfType<QRScanner>();
        if (qrScanner == null)
            qrScanner = gameObject.AddComponent<QRScanner>();

        ChatManager chatManager = FindObjectOfType<ChatManager>();
        if (chatManager == null)
            chatManager = gameObject.AddComponent<ChatManager>();

        PathVisualizer pathVisualizer = FindObjectOfType<PathVisualizer>();
        if (pathVisualizer == null)
        {
            GameObject pathRoot = new GameObject("PathVisualizer");
            pathRoot.transform.SetParent(transform, false);
            pathVisualizer = pathRoot.AddComponent<PathVisualizer>();
        }

        modeManager.Initialize(ui, pathVisualizer);
        qrScanner.WireUI(ui.ScannerPanel, ui.ScannerStatusText, ui.ScannerLocationText, ui.ScannerPreview, modeManager);
        chatManager.Configure(apiClient, navigationFlow, ui.ChatInput, ui.ChatContent, ui.StatusText, ui.ChatScrollRect);
        navigationFlow.Configure(apiClient, locationRegistry, qrLocationManager, pathVisualizer, ui, runtimeValidator);

        BindUI(ui, modeManager, navigationFlow, chatManager, qrScanner);
        
        // Store UI reference and start continuous enabler
        m_UI = ui;
        StartCoroutine(ContinuouslyEnableButtons());
        
        navigationFlow.BeginLoad();
    }
    
    private System.Collections.IEnumerator ContinuouslyEnableButtons()
    {
        // Enable buttons every frame for 5 seconds to override any disabling
        for (int i = 0; i < 300; i++)
        {
            yield return null;
            if (m_UI != null)
            {
                m_UI.MenuButton.interactable = true;
                m_UI.QRButton.interactable = true;
                m_UI.ChatButton.interactable = true;
                m_UI.NavigateButton.interactable = true;
                m_UI.SendButton.interactable = true;
                m_UI.ScannerCloseButton.interactable = true;
                m_UI.ChatCloseButton.interactable = true;
                m_UI.BuildingDropdown.interactable = true;
                m_UI.FloorDropdown.interactable = true;
                m_UI.RoomDropdown.interactable = true;
            }
        }
        Debug.Log("[CampusRuntimeInstaller] Stopped continuous button enabling");
    }
    
    private System.Collections.IEnumerator ForceEnableButtonsDelayed(CampusRuntimeUI ui)
    {
        yield return null;
        ui.MenuButton.interactable = true;
        ui.QRButton.interactable = true;
        ui.ChatButton.interactable = true;
        ui.NavigateButton.interactable = true;
        ui.SendButton.interactable = true;
        ui.ScannerCloseButton.interactable = true;
        ui.ChatCloseButton.interactable = true;
        ui.BuildingDropdown.interactable = true;
        ui.FloorDropdown.interactable = true;
        ui.RoomDropdown.interactable = true;
        Debug.Log("[CampusRuntimeInstaller] All buttons force-enabled after delay");
    }

    private void BindUI(
        CampusRuntimeUI ui,
        ModeManager modeManager,
        NavigationFlowController navigationFlow,
        ChatManager chatManager,
        QRScanner qrScanner)
    {
        Debug.Log("[CampusRuntimeInstaller] Binding UI buttons...");
        
        // Force enable all buttons
        ui.MenuButton.interactable = true;
        ui.QRButton.interactable = true;
        ui.ChatButton.interactable = true;
        ui.NavigateButton.interactable = true;
        ui.SendButton.interactable = true;
        ui.ScannerCloseButton.interactable = true;
        ui.ChatCloseButton.interactable = true;
        Debug.Log("[CampusRuntimeInstaller] All buttons set to interactable");
        
        ui.MenuButton.onClick.RemoveAllListeners();
        ui.MenuButton.onClick.AddListener(modeManager.ToggleMenu);
        Debug.Log("[CampusRuntimeInstaller] Menu button bound");

        ui.QRButton.onClick.RemoveAllListeners();
        ui.QRButton.onClick.AddListener(qrScanner.OpenScanner);
        Debug.Log("[CampusRuntimeInstaller] QR button bound");

        ui.ChatButton.onClick.RemoveAllListeners();
        ui.ChatButton.onClick.AddListener(modeManager.ToggleChat);
        Debug.Log("[CampusRuntimeInstaller] Chat button bound");

        ui.NavigateButton.onClick.RemoveAllListeners();
        ui.NavigateButton.onClick.AddListener(navigationFlow.HandleNavigatePressed);

        ui.SendButton.onClick.RemoveAllListeners();
        ui.SendButton.onClick.AddListener(() => chatManager.SendChatMessage(ui.ChatInput.text));

        ui.ScannerCloseButton.onClick.RemoveAllListeners();
        ui.ScannerCloseButton.onClick.AddListener(qrScanner.CloseScanner);

        ui.ChatCloseButton.onClick.RemoveAllListeners();
        ui.ChatCloseButton.onClick.AddListener(modeManager.CloseChat);

        ui.BuildingDropdown.onValueChanged.RemoveAllListeners();
        ui.BuildingDropdown.onValueChanged.AddListener(navigationFlow.HandleBuildingChanged);

        ui.FloorDropdown.onValueChanged.RemoveAllListeners();
        ui.FloorDropdown.onValueChanged.AddListener(navigationFlow.HandleFloorChanged);
        
        Debug.Log("[CampusRuntimeInstaller] All UI buttons bound successfully");
    }

    private T GetOrCreateComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        if (component == null)
            component = target.AddComponent<T>();
        return component;
    }

    private void DisableIfPresent<T>() where T : Behaviour
    {
        T component = FindObjectOfType<T>();
        if (component != null)
            component.enabled = false;
    }
}
