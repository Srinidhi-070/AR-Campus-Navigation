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
        qrScanner.WireUI(ui.RootCanvas.transform, modeManager);
        chatManager.Configure(apiClient, navigationFlow, ui.ChatInput, ui.ChatContent, ui.StatusText, ui.ChatScrollRect);
        navigationFlow.Configure(apiClient, locationRegistry, qrLocationManager, pathVisualizer, ui, runtimeValidator);

        BindUI(ui, modeManager, navigationFlow, chatManager, qrScanner);
        m_UI = ui;
        navigationFlow.BeginLoad();
    }
    
    private void BindUI(
        CampusRuntimeUI ui,
        ModeManager modeManager,
        NavigationFlowController navigationFlow,
        ChatManager chatManager,
        QRScanner qrScanner)
    {
        // These two should ALWAYS be interactable
        ui.MenuButton.interactable = true;
        ui.QRButton.interactable = true;

        ui.MenuButton.onClick.RemoveAllListeners();
        ui.MenuButton.onClick.AddListener(modeManager.ToggleMenu);

        ui.QRButton.onClick.RemoveAllListeners();
        ui.QRButton.onClick.AddListener(qrScanner.OpenScanner);

        ui.ChatButton.onClick.RemoveAllListeners();
        ui.ChatButton.onClick.AddListener(modeManager.ToggleChat);

        ui.NavigateButton.onClick.RemoveAllListeners();
        ui.NavigateButton.onClick.AddListener(navigationFlow.HandleNavigatePressed);

        ui.SendButton.onClick.RemoveAllListeners();
        ui.SendButton.onClick.AddListener(() => chatManager.SendChatMessage(ui.ChatInput.text));

        // Enter key also sends chat message
        ui.ChatInput.onSubmit.RemoveAllListeners();
        ui.ChatInput.onSubmit.AddListener(text => chatManager.SendChatMessage(text));

        ui.ChatCloseButton.onClick.RemoveAllListeners();
        ui.ChatCloseButton.onClick.AddListener(modeManager.CloseChat);

        ui.RetryButton.onClick.RemoveAllListeners();
        ui.RetryButton.onClick.AddListener(() =>
        {
            ui.ShowStatus("Retrying connection...");
            navigationFlow.BeginLoad();
        });

        // Tapping the dim overlay behind the menu closes it
        if (ui.MenuOverlayButton != null)
        {
            ui.MenuOverlayButton.onClick.RemoveAllListeners();
            ui.MenuOverlayButton.onClick.AddListener(modeManager.ToggleMenu);
        }

        ui.BuildingDropdown.onValueChanged.RemoveAllListeners();
        ui.BuildingDropdown.onValueChanged.AddListener(navigationFlow.HandleBuildingChanged);

        ui.FloorDropdown.onValueChanged.RemoveAllListeners();
        ui.FloorDropdown.onValueChanged.AddListener(navigationFlow.HandleFloorChanged);

        Debug.Log("[CampusRuntimeInstaller] All UI bound");
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
