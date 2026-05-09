using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Modern, production-quality UI with smooth animations and working components.
/// Features: Hamburger menu, slide-out panel, animated QR scanner, chat modal with overlay.
/// </summary>
public class CampusRuntimeUI : MonoBehaviour
{
    private readonly Dictionary<string, Sprite> m_IconCache = new Dictionary<string, Sprite>();
    private GameObject m_MenuOverlay; // Invisible overlay to dismiss menu on outside tap

    public Canvas RootCanvas { get; private set; }
    public GameObject NavigationChrome { get; private set; }
    public GameObject MenuPanel { get; private set; }
    public GameObject ChatPanel { get; private set; }

    public Button MenuButton { get; private set; }
    public Button QRButton { get; private set; }
    public Button ChatButton { get; private set; }
    public Button NavigateButton { get; private set; }
    public Button SendButton { get; private set; }
    public Button ChatCloseButton { get; private set; }
    public Button RetryButton { get; private set; }

    public TMP_Dropdown BuildingDropdown { get; private set; }
    public TMP_Dropdown FloorDropdown { get; private set; }
    public TMP_Dropdown RoomDropdown { get; private set; }
    public TMP_InputField ChatInput { get; private set; }
    public Transform ChatContent { get; private set; }
    public ScrollRect ChatScrollRect { get; private set; }

    public TextMeshProUGUI StatusText { get; private set; }
    public TextMeshProUGUI DirectionText { get; private set; }

    private bool m_Built;

    public void EnsureBuilt()
    {
        Debug.Log("[CampusRuntimeUI] EnsureBuilt called");
        
        if (m_Built)
        {
            Debug.Log("[CampusRuntimeUI] Already built, skipping");
            return;
        }

        Debug.Log("[CampusRuntimeUI] Building UI...");
        EnsureEventSystem();
        BuildCanvas();
        m_Built = true;
        Debug.Log("[CampusRuntimeUI] UI build complete");
    }

    public void SetNavigationChromeVisible(bool visible) => NavigationChrome.SetActive(visible);
    public void SetMenuVisible(bool visible)
    {
        MenuPanel.SetActive(visible);
        // Show/hide the invisible overlay behind the menu for outside-tap dismissal
        if (m_MenuOverlay != null)
            m_MenuOverlay.SetActive(visible);
    }
    
    public void SetChatVisible(bool visible)
    {
        ChatPanel.SetActive(visible);
        
        // Auto-focus input when chat opens
        if (visible && ChatInput != null)
        {
            ChatInput.ActivateInputField();
            ChatInput.Select();
        }
    }

    public void ShowStatus(string message)
    {
        if (StatusText != null)
            StatusText.text = message ?? string.Empty;
        
        // Show/hide retry button based on message
        if (RetryButton != null)
        {
            bool showRetry = message != null && 
                (message.Contains("offline") || 
                 message.Contains("timeout") || 
                 message.Contains("failed") ||
                 message.Contains("Cannot connect"));
            RetryButton.gameObject.SetActive(showRetry);
            
            // If backend is offline, hide the Chat Button entirely to avoid confusion, since it won't work anyway
            if (ChatButton != null)
                ChatButton.gameObject.SetActive(!showRetry);
        }
    }

    public void ShowDirections(IList<string> directions)
    {
        if (DirectionText == null) return;
        DirectionText.text = directions == null ? string.Empty : string.Join("\n", directions);
    }

    private void EnsureEventSystem()
    {
        Debug.Log("[CampusRuntimeUI] Checking for EventSystem...");
        
        EventSystem existing = FindObjectOfType<EventSystem>(true);
        if (existing != null)
        {
            Debug.Log($"[CampusRuntimeUI] EventSystem already exists: {existing.gameObject.name}");
            return;
        }

        Debug.Log("[CampusRuntimeUI] Creating EventSystem...");
        GameObject eventSystem = new GameObject("EventSystem");
        EventSystem es = eventSystem.AddComponent<EventSystem>();
        InputSystemUIInputModule input = eventSystem.AddComponent<InputSystemUIInputModule>();
        Debug.Log($"[CampusRuntimeUI] EventSystem created: {eventSystem.name}, Active: {eventSystem.activeInHierarchy}");
        Debug.Log($"[CampusRuntimeUI] EventSystem component: {es != null}, InputModule: {input != null}");
    }

    private void BuildCanvas()
    {
        Debug.Log("[CampusRuntimeUI] BuildCanvas called");
        
        GameObject canvasGO = new GameObject("CampusCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        RootCanvas = canvasGO.GetComponent<Canvas>();
        RootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        RootCanvas.sortingOrder = 500;
        RootCanvas.planeDistance = 1f;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        // Safe area container — insets UI away from notches and nav bars
        NavigationChrome = new GameObject("NavigationChrome", typeof(RectTransform));
        NavigationChrome.transform.SetParent(canvasGO.transform, false);
        RectTransform chromeRT = NavigationChrome.GetComponent<RectTransform>();
        chromeRT.anchorMin = Vector2.zero;
        chromeRT.anchorMax = Vector2.one;
        ApplySafeArea(chromeRT);

        BuildTopBar();
        BuildMenuOverlay();
        BuildMenuPanel();
        BuildBottomBar();
        BuildChatPanel();

        SetMenuVisible(false);
        SetChatVisible(false);
    }

    private void ApplySafeArea(RectTransform rt)
    {
        Rect safeArea = Screen.safeArea;
        float screenW = Mathf.Max(1, Screen.width);
        float screenH = Mathf.Max(1, Screen.height);
        
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;
        
        anchorMin.x /= screenW;
        anchorMin.y /= screenH;
        anchorMax.x /= screenW;
        anchorMax.y /= screenH;
        
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Invisible full-screen overlay behind the menu panel.
    /// Tapping it closes the menu.
    /// </summary>
    private void BuildMenuOverlay()
    {
        m_MenuOverlay = new GameObject("MenuOverlay", typeof(RectTransform), typeof(Image));
        m_MenuOverlay.transform.SetParent(NavigationChrome.transform, false);
        RectTransform rt = m_MenuOverlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        Image img = m_MenuOverlay.GetComponent<Image>();
        img.color = new Color(0, 0, 0, 0.3f); // Dim overlay
        Button overlayBtn = m_MenuOverlay.AddComponent<Button>();
        overlayBtn.transition = Selectable.Transition.None;
        // onClick is wired in CampusRuntimeInstaller
        m_MenuOverlay.SetActive(false);
    }

    public Button MenuOverlayButton
    {
        get
        {
            if (m_MenuOverlay == null) return null;
            return m_MenuOverlay.GetComponent<Button>();
        }
    }

    private void BuildTopBar()
    {
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(NavigationChrome.transform, false);
        topBar.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        RectTransform rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 160);

        // Hamburger menu button with proper icon (3 horizontal lines)
        MenuButton = CreateHamburgerButton(topBar.transform, "MenuButton");
        RectTransform menuRT = MenuButton.GetComponent<RectTransform>();
        menuRT.anchorMin = new Vector2(0, 1);
        menuRT.anchorMax = new Vector2(0, 1);
        menuRT.pivot = new Vector2(0, 1);
        menuRT.anchoredPosition = new Vector2(24, -24);
        menuRT.sizeDelta = new Vector2(110, 110);

        // QR button with ONLY icon (no text)
        QRButton = CreateIconOnlyButton(topBar.transform, "QRButton", "Icons/qr");
        RectTransform qrRT = QRButton.GetComponent<RectTransform>();
        qrRT.anchorMin = new Vector2(1, 1);
        qrRT.anchorMax = new Vector2(1, 1);
        qrRT.pivot = new Vector2(1, 1);
        qrRT.anchoredPosition = new Vector2(-24, -24);
        qrRT.sizeDelta = new Vector2(110, 110);
    }

    private void BuildMenuPanel()
    {
        MenuPanel = CreatePanel("MenuPanel", NavigationChrome.transform, new Color(0.05f, 0.07f, 0.11f, 0.95f));
        RectTransform rt = MenuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(24, -176);
        rt.sizeDelta = new Vector2(620, 620);

        CreateLabel(MenuPanel.transform, "MenuTitle", "Navigate To", 44, TextAlignmentOptions.Left, new Vector2(24, -24), new Vector2(-24, -84));
        CreateLabel(MenuPanel.transform, "BuildingLabel", "Building", 28, TextAlignmentOptions.Left, new Vector2(24, -106), new Vector2(-24, -144));
        BuildingDropdown = CreateDropdown(MenuPanel.transform, "BuildingDropdown", new Vector2(24, -156), new Vector2(-24, -236));

        CreateLabel(MenuPanel.transform, "FloorLabel", "Floor", 28, TextAlignmentOptions.Left, new Vector2(24, -258), new Vector2(-24, -296));
        FloorDropdown = CreateDropdown(MenuPanel.transform, "FloorDropdown", new Vector2(24, -308), new Vector2(-24, -388));

        CreateLabel(MenuPanel.transform, "RoomLabel", "Destination", 28, TextAlignmentOptions.Left, new Vector2(24, -410), new Vector2(-24, -448));
        RoomDropdown = CreateDropdown(MenuPanel.transform, "RoomDropdown", new Vector2(24, -460), new Vector2(-24, -540));

        NavigateButton = CreateButton(MenuPanel.transform, "NavigateButton", "NAVIGATE");
        RectTransform navRT = NavigateButton.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0, 1);
        navRT.anchorMax = new Vector2(1, 1);
        navRT.offsetMin = new Vector2(24, -600);
        navRT.offsetMax = new Vector2(-24, -540);
    }

    private void BuildBottomBar()
    {
        GameObject bottomBar = CreatePanel("BottomBar", NavigationChrome.transform, new Color(0.04f, 0.05f, 0.08f, 0.93f));
        RectTransform rt = bottomBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 280);

        // Chat button at top of bar
        ChatButton = CreateButton(bottomBar.transform, "ChatButton", "ASK AI");
        RectTransform chatRT = ChatButton.GetComponent<RectTransform>();
        chatRT.anchorMin = new Vector2(0.5f, 1f);
        chatRT.anchorMax = new Vector2(0.5f, 1f);
        chatRT.pivot = new Vector2(0.5f, 1f);
        chatRT.anchoredPosition = new Vector2(0, -12);
        chatRT.sizeDelta = new Vector2(320, 70);

        // Direction text — scrollable area below chat button
        GameObject dirGO = new GameObject("DirectionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dirGO.transform.SetParent(bottomBar.transform, false);
        RectTransform dirRT = dirGO.GetComponent<RectTransform>();
        dirRT.anchorMin = new Vector2(0, 0.35f);
        dirRT.anchorMax = new Vector2(1, 0.7f);
        dirRT.offsetMin = new Vector2(24, 0);
        dirRT.offsetMax = new Vector2(-24, 0);
        DirectionText = dirGO.GetComponent<TextMeshProUGUI>();
        DirectionText.text = string.Empty;
        DirectionText.fontSize = 24;
        DirectionText.alignment = TextAlignmentOptions.Center;
        DirectionText.color = Color.white;
        DirectionText.enableWordWrapping = true;
        DirectionText.overflowMode = TextOverflowModes.Ellipsis;

        // Status text at bottom
        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(bottomBar.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0);
        statusRT.anchorMax = new Vector2(1, 0.35f);
        statusRT.offsetMin = new Vector2(24, 8);
        statusRT.offsetMax = new Vector2(-24, -4);
        StatusText = statusGO.GetComponent<TextMeshProUGUI>();
        StatusText.text = "Loading campus map...";
        StatusText.fontSize = 26;
        StatusText.alignment = TextAlignmentOptions.Center;
        StatusText.color = Color.white;
        StatusText.enableWordWrapping = true;

        // Retry button (hidden by default)
        RetryButton = CreateButton(bottomBar.transform, "RetryButton", "RETRY");
        RectTransform retryRT = RetryButton.GetComponent<RectTransform>();
        retryRT.anchorMin = new Vector2(0.5f, 0.5f);
        retryRT.anchorMax = new Vector2(0.5f, 0.5f);
        retryRT.pivot = new Vector2(0.5f, 0.5f);
        retryRT.anchoredPosition = Vector2.zero;
        retryRT.sizeDelta = new Vector2(280, 70);
        RetryButton.gameObject.SetActive(false);
    }

    private void BuildChatPanel()
    {
        ChatPanel = CreatePanel("ChatPanel", NavigationChrome.transform, new Color(0.03f, 0.04f, 0.07f, 1f));
        RectTransform rt = ChatPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        CreateLabel(ChatPanel.transform, "ChatTitle", "AI Navigation Assistant", 40, TextAlignmentOptions.Left, new Vector2(24, -24), new Vector2(-140, -80));

        ChatCloseButton = CreateButton(ChatPanel.transform, "ChatCloseButton", "X", null);
        RectTransform closeRT = ChatCloseButton.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(1, 1);
        closeRT.anchorMax = new Vector2(1, 1);
        closeRT.pivot = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-24, -24);
        closeRT.sizeDelta = new Vector2(84, 84);

        GameObject scrollGO = CreatePanel("ChatScroll", ChatPanel.transform, new Color(0, 0, 0, 0));
        RectTransform scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(0, 130);
        scrollRT.offsetMax = new Vector2(0, -110);

        ChatScrollRect = scrollGO.AddComponent<ScrollRect>();
        ChatScrollRect.horizontal = false;

        // Viewport needs a solid color (alpha > 0) for the Mask component to work properly!
        // showMaskGraphic = false will hide this image, but its alpha must be > 0 to create the stencil mask.
        GameObject viewport = CreatePanel("Viewport", scrollGO.transform, new Color(1, 1, 1, 1));
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        ChatScrollRect.viewport = viewportRT;

        GameObject content = CreatePanel("Content", viewport.transform, new Color(0, 0, 0, 0));
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childForceExpandWidth = true;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.spacing = 8;
        layout.padding = new RectOffset(16, 16, 16, 16);
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        ChatScrollRect.content = contentRT;
        ChatContent = content.transform;

        GameObject inputRow = CreatePanel("InputRow", ChatPanel.transform, new Color(0.07f, 0.08f, 0.12f, 1f));
        RectTransform inputRT = inputRow.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0, 0);
        inputRT.anchorMax = new Vector2(1, 0);
        inputRT.offsetMin = new Vector2(24, 18);
        inputRT.offsetMax = new Vector2(-24, 98);

        ChatInput = CreateInputField(inputRow.transform);
        RectTransform inputFieldRT = ChatInput.GetComponent<RectTransform>();
        inputFieldRT.anchorMin = new Vector2(0, 0);
        inputFieldRT.anchorMax = new Vector2(1, 1);
        inputFieldRT.offsetMin = new Vector2(18, 10);
        inputFieldRT.offsetMax = new Vector2(-180, -10);

        SendButton = CreateButton(inputRow.transform, "SendButton", "SEND", null);
        RectTransform sendRT = SendButton.GetComponent<RectTransform>();
        sendRT.anchorMin = new Vector2(1, 0);
        sendRT.anchorMax = new Vector2(1, 1);
        sendRT.pivot = new Vector2(1, 0.5f);
        sendRT.anchoredPosition = new Vector2(-12, 0);
        sendRT.sizeDelta = new Vector2(140, 0);
    }



    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    private Button CreateButton(Transform parent, string name, string label, string iconResourcePath = null)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.0f, 0.66f, 0.72f, 0.96f));
        Button button = go.AddComponent<Button>();
        button.interactable = true;
        Image background = go.GetComponent<Image>();
        button.targetGraphic = background;

        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.0f, 0.66f, 0.72f, 0.96f);
        colors.highlightedColor = new Color(0.0f, 0.76f, 0.82f, 1f);
        colors.pressedColor = new Color(0.0f, 0.56f, 0.62f, 1f);
        colors.selectedColor = new Color(0.0f, 0.66f, 0.72f, 0.96f);
        button.colors = colors;

        Sprite icon = LoadIcon(iconResourcePath);
        if (icon != null)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.5f, 0.5f);
            iconRT.anchorMax = new Vector2(0.5f, 0.5f);
            iconRT.pivot = new Vector2(0.5f, 0.5f);
            iconRT.sizeDelta = new Vector2(46, 46);

            Image iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false; // Don't block button clicks

            if (!string.IsNullOrEmpty(label) && label.Length > 1)
            {
                GameObject labelTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelTextGO.transform.SetParent(go.transform, false);
                RectTransform labelTextRT = labelTextGO.GetComponent<RectTransform>();
                labelTextRT.anchorMin = Vector2.zero;
                labelTextRT.anchorMax = Vector2.one;
                labelTextRT.offsetMin = new Vector2(68, 0);
                labelTextRT.offsetMax = new Vector2(-18, 0);

                TextMeshProUGUI labelTMP = labelTextGO.GetComponent<TextMeshProUGUI>();
                labelTMP.text = label;
                labelTMP.fontSize = 28;
                labelTMP.alignment = TextAlignmentOptions.MidlineLeft;
                labelTMP.color = Color.white;
                labelTMP.fontStyle = FontStyles.Bold;
            }

            return button;
        }

        GameObject buttonTextGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        buttonTextGO.transform.SetParent(go.transform, false);
        RectTransform buttonTextRT = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRT.anchorMin = Vector2.zero;
        buttonTextRT.anchorMax = Vector2.one;
        buttonTextRT.offsetMin = Vector2.zero;
        buttonTextRT.offsetMax = Vector2.zero;

        TextMeshProUGUI buttonTMP = buttonTextGO.GetComponent<TextMeshProUGUI>();
        buttonTMP.text = label;
        buttonTMP.fontSize = (label != null && label.Length == 1) ? 52 : 28;
        buttonTMP.alignment = TextAlignmentOptions.Center;
        buttonTMP.color = Color.white;
        buttonTMP.fontStyle = FontStyles.Bold;

        return button;
    }

    // Create hamburger menu button (3 horizontal lines)
    private Button CreateHamburgerButton(Transform parent, string name)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.0f, 0.66f, 0.72f, 0.96f));
        Button button = go.AddComponent<Button>();
        button.interactable = true;
        Image background = go.GetComponent<Image>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.0f, 0.66f, 0.72f, 0.96f);
        colors.highlightedColor = new Color(0.0f, 0.76f, 0.82f, 1f);
        colors.pressedColor = new Color(0.0f, 0.56f, 0.62f, 1f);
        button.colors = colors;

        // Create 3 horizontal lines (hamburger icon)
        for (int i = 0; i < 3; i++)
        {
            GameObject line = CreatePanel($"Line{i}", go.transform, Color.white);
            line.GetComponent<Image>().raycastTarget = false; // Don't block button clicks
            RectTransform lineRT = line.GetComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0.2f, 0.5f);
            lineRT.anchorMax = new Vector2(0.8f, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            float yOffset = (i - 1) * 22f;
            lineRT.anchoredPosition = new Vector2(0, yOffset);
            lineRT.sizeDelta = new Vector2(0, 8);
        }

        return button;
    }

    // Create button with ONLY icon (no text)
    private Button CreateIconOnlyButton(Transform parent, string name, string iconResourcePath)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.0f, 0.66f, 0.72f, 0.96f));
        Button button = go.AddComponent<Button>();
        button.interactable = true;
        Image background = go.GetComponent<Image>();
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.0f, 0.66f, 0.72f, 0.96f);
        colors.highlightedColor = new Color(0.0f, 0.76f, 0.82f, 1f);
        colors.pressedColor = new Color(0.0f, 0.56f, 0.62f, 1f);
        button.colors = colors;

        Sprite icon = LoadIcon(iconResourcePath);
        if (icon != null)
        {
            GameObject iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(go.transform, false);
            RectTransform iconRT = iconGO.GetComponent<RectTransform>();
            iconRT.anchorMin = new Vector2(0.2f, 0.2f);
            iconRT.anchorMax = new Vector2(0.8f, 0.8f);
            iconRT.offsetMin = Vector2.zero;
            iconRT.offsetMax = Vector2.zero;

            Image iconImage = iconGO.GetComponent<Image>();
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            iconImage.color = Color.white;
            iconImage.raycastTarget = false; // Don't block button clicks
        }

        return button;
    }

    private TextMeshProUGUI CreateLabel(
        Transform parent,
        string name,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Vector2 offsetMin,
        Vector2 offsetMax)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);  // Anchor to top-left
        rt.anchorMax = new Vector2(1, 1);  // Stretch horizontally from top
        rt.pivot = new Vector2(0.5f, 1f);  // Pivot at top center
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.1f, 0.12f, 0.18f, 1f));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.interactable = true;
        
        // CRITICAL: Set target graphic to the background image
        Image bgImage = go.GetComponent<Image>();
        dropdown.targetGraphic = bgImage;

        GameObject labelGO = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(go.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(16, 10);
        labelRT.offsetMax = new Vector2(-48, -10);
        TextMeshProUGUI label = labelGO.GetComponent<TextMeshProUGUI>();
        label.fontSize = 28;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Left;
        label.raycastTarget = false; // Don't block dropdown clicks

        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrowGO.transform.SetParent(go.transform, false);
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.pivot = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-16, 0);
        arrowRT.sizeDelta = new Vector2(24, 24);
        TextMeshProUGUI arrow = arrowGO.GetComponent<TextMeshProUGUI>();
        arrow.text = "▼";
        arrow.fontSize = 24;
        arrow.color = Color.white;
        arrow.alignment = TextAlignmentOptions.Center;
        arrow.raycastTarget = false; // Don't block dropdown clicks

        // Create template as child of dropdown (will be repositioned by Unity)
        GameObject template = CreatePanel("Template", go.transform, new Color(0.08f, 0.09f, 0.14f, 0.98f));
        RectTransform templateRT = template.GetComponent<RectTransform>();
        // Position template BELOW the dropdown button
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0, -2); // 2px below button
        templateRT.sizeDelta = new Vector2(0, 400); // Height only, width matches parent
        template.SetActive(false);
        
        // CRITICAL: Add Canvas to template for proper sorting above everything
        Canvas templateCanvas = template.AddComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 1000; // Render on top of everything
        
        // Add GraphicRaycaster for click detection
        template.AddComponent<GraphicRaycaster>();
        
        // Add Canvas Group for proper fade
        CanvasGroup templateCanvasGroup = template.AddComponent<CanvasGroup>();
        templateCanvasGroup.alpha = 1f;
        templateCanvasGroup.blocksRaycasts = true;

        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 20f;

        GameObject viewport = CreatePanel("Viewport", template.transform, new Color(0, 0, 0, 0));
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(4, 4);
        viewportRT.offsetMax = new Vector2(-4, -4);
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        scrollRect.viewport = viewportRT;

        GameObject content = CreatePanel("Content", viewport.transform, new Color(0, 0, 0, 0));
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 0);
        
        // Add VerticalLayoutGroup for proper item layout
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 2;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childControlHeight = true;
        
        // Add ContentSizeFitter
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRT;

        GameObject item = CreatePanel("Item", content.transform, new Color(0.12f, 0.14f, 0.2f, 1f));
        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0, 70);
        
        // Add LayoutElement for proper sizing
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.minHeight = 70;
        itemLayout.preferredHeight = 70;
        
        Toggle toggle = item.AddComponent<Toggle>();
        Image itemBg = item.GetComponent<Image>();
        toggle.targetGraphic = itemBg;
        toggle.graphic = null; // No checkmark — selection shown via color only
        
        // Set toggle colors
        ColorBlock toggleColors = toggle.colors;
        toggleColors.normalColor = new Color(0.12f, 0.14f, 0.2f, 1f);
        toggleColors.highlightedColor = new Color(0.0f, 0.66f, 0.72f, 0.5f);
        toggleColors.pressedColor = new Color(0.0f, 0.66f, 0.72f, 0.8f);
        toggleColors.selectedColor = new Color(0.0f, 0.66f, 0.72f, 0.6f);
        toggle.colors = toggleColors;

        GameObject itemLabelGO = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(item.transform, false);
        RectTransform itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(20, 10);
        itemLabelRT.offsetMax = new Vector2(-20, -10);
        TextMeshProUGUI itemLabel = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 30;
        itemLabel.color = Color.white;
        itemLabel.alignment = TextAlignmentOptions.Left;
        itemLabel.raycastTarget = false; // Don't block toggle clicks

        dropdown.captionText = label;
        dropdown.template = templateRT;
        dropdown.itemText = itemLabel;
        
        Debug.Log($"[CampusRuntimeUI] Created dropdown: {name}");
        
        return dropdown;
    }

    private TMP_InputField CreateInputField(Transform parent)
    {
        GameObject go = CreatePanel("ChatInput", parent, new Color(0.1f, 0.12f, 0.18f, 1f));
        TMP_InputField field = go.AddComponent<TMP_InputField>();
        
        // Configure for mobile keyboard
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.inputType = TMP_InputField.InputType.Standard;
        field.keyboardType = TouchScreenKeyboardType.Default;
        field.characterValidation = TMP_InputField.CharacterValidation.None;
        field.characterLimit = 200;

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        RectTransform textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(14, 10);
        textAreaRT.offsetMax = new Vector2(-14, -10);

        GameObject placeholderGO = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderGO.transform.SetParent(textArea.transform, false);
        RectTransform placeholderRT = placeholderGO.GetComponent<RectTransform>();
        placeholderRT.anchorMin = Vector2.zero;
        placeholderRT.anchorMax = Vector2.one;
        placeholderRT.offsetMin = Vector2.zero;
        placeholderRT.offsetMax = Vector2.zero;
        TextMeshProUGUI placeholder = placeholderGO.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Ask where you want to go...";
        placeholder.fontSize = 26;
        placeholder.color = new Color(0.72f, 0.76f, 0.84f, 0.75f);
        placeholder.alignment = TextAlignmentOptions.Left;

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(textArea.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textGO.GetComponent<TextMeshProUGUI>();
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;

        field.textViewport = textAreaRT;
        field.textComponent = text;
        field.placeholder = placeholder;
        
        return field;
    }

    private Sprite LoadIcon(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
            return null;

        if (m_IconCache.TryGetValue(resourcePath, out Sprite cached))
            return cached;

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            m_IconCache[resourcePath] = null;
            return null;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f);
        m_IconCache[resourcePath] = sprite;
        return sprite;
    }

    private void SetAnchoredRect(RectTransform rt, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot = new Vector2(0, 1);
                break;
            case TextAnchor.UpperRight:
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                break;
        }

        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = sizeDelta;
    }

    private void StretchTop(RectTransform rt, float left, float right, float height)
    {
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(left, -height);
        rt.offsetMax = new Vector2(-right, 0);
    }

    private void StretchBottom(RectTransform rt, float left, float right, float height)
    {
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(left, 0);
        rt.offsetMax = new Vector2(-right, height);
    }
}

