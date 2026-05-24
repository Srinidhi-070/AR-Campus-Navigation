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
    private Sprite m_RoundedSprite;

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
        // Expand outwards to cover safe area gaps (notch/status bar)
        rt.offsetMin = new Vector2(-200, -200);
        rt.offsetMax = new Vector2(200, 200);
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
        // Container for floating top icons (no solid background)
        GameObject topBar = new GameObject("TopFloatingBar", typeof(RectTransform));
        topBar.transform.SetParent(NavigationChrome.transform, false);
        RectTransform rt = topBar.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 160);

        // Hamburger menu button with drop shadow
        MenuButton = CreateIconOnlyButton(topBar.transform, "MenuButton", "Icons/menu");
        RectTransform menuRT = MenuButton.GetComponent<RectTransform>();
        menuRT.anchorMin = new Vector2(0, 1);
        menuRT.anchorMax = new Vector2(0, 1);
        menuRT.pivot = new Vector2(0, 1);
        menuRT.anchoredPosition = new Vector2(36, -36);
        menuRT.sizeDelta = new Vector2(110, 110);
        
        // Add subtle shadow/border to MenuButton
        Outline menuOutline = MenuButton.gameObject.AddComponent<Outline>();
        menuOutline.effectColor = new Color(0, 0, 0, 0.4f);
        menuOutline.effectDistance = new Vector2(2, -2);

        // QR button 
        QRButton = CreateIconOnlyButton(topBar.transform, "QRButton", "Icons/qr");
        RectTransform qrRT = QRButton.GetComponent<RectTransform>();
        qrRT.anchorMin = new Vector2(1, 1);
        qrRT.anchorMax = new Vector2(1, 1);
        qrRT.pivot = new Vector2(1, 1);
        qrRT.anchoredPosition = new Vector2(-36, -36);
        qrRT.sizeDelta = new Vector2(110, 110);
        
        Outline qrOutline = QRButton.gameObject.AddComponent<Outline>();
        qrOutline.effectColor = new Color(0, 0, 0, 0.4f);
        qrOutline.effectDistance = new Vector2(2, -2);
    }

    private void BuildMenuPanel()
    {
        MenuPanel = CreatePanel("MenuPanel", NavigationChrome.transform, new Color(0.04f, 0.05f, 0.08f, 0.98f)); // Very dark navy/black background
        
        Outline panelOutline = MenuPanel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0, 0, 0, 0.4f);
        panelOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform rt = MenuPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(36, -176);
        rt.sizeDelta = new Vector2(660, 620);

        TextMeshProUGUI title = CreateLabel(MenuPanel.transform, "MenuTitle", "Navigate To", 44, TextAlignmentOptions.Left, new Vector2(36, -24), new Vector2(-24, -84));
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        TextMeshProUGUI bLabel = CreateLabel(MenuPanel.transform, "BuildingLabel", "Building", 28, TextAlignmentOptions.Left, new Vector2(36, -106), new Vector2(-24, -144));
        bLabel.color = new Color(0.8f, 0.82f, 0.85f, 1f); // Light gray
        BuildingDropdown = CreateDropdown(MenuPanel.transform, "BuildingDropdown", new Vector2(36, -156), new Vector2(-36, -236));

        TextMeshProUGUI fLabel = CreateLabel(MenuPanel.transform, "FloorLabel", "Floor", 28, TextAlignmentOptions.Left, new Vector2(36, -258), new Vector2(-24, -296));
        fLabel.color = new Color(0.8f, 0.82f, 0.85f, 1f);
        FloorDropdown = CreateDropdown(MenuPanel.transform, "FloorDropdown", new Vector2(36, -308), new Vector2(-36, -388));

        TextMeshProUGUI rLabel = CreateLabel(MenuPanel.transform, "RoomLabel", "Destination", 28, TextAlignmentOptions.Left, new Vector2(36, -410), new Vector2(-24, -448));
        rLabel.color = new Color(0.8f, 0.82f, 0.85f, 1f);
        RoomDropdown = CreateDropdown(MenuPanel.transform, "RoomDropdown", new Vector2(36, -460), new Vector2(-36, -540));

        NavigateButton = CreateButton(MenuPanel.transform, "NavigateButton", "CONTINUE");
        RectTransform navRT = NavigateButton.GetComponent<RectTransform>();
        navRT.anchorMin = new Vector2(0, 1);
        navRT.anchorMax = new Vector2(1, 1);
        navRT.offsetMin = new Vector2(36, -600);
        navRT.offsetMax = new Vector2(-36, -540);
        NavigateButton.GetComponent<Image>().color = new Color(0.25f, 0.35f, 1f, 1f); // Vibrant blue/indigo button
    }

    private void BuildBottomBar()
    {
        // Floating Status Drawer
        GameObject statusPill = CreatePanel("StatusDrawer", NavigationChrome.transform, new Color(0.04f, 0.05f, 0.08f, 0.98f));
        statusPill.GetComponent<Image>().sprite = GetRoundedSprite();
        statusPill.GetComponent<Image>().type = Image.Type.Sliced;
        RectTransform pillRT = statusPill.GetComponent<RectTransform>();
        pillRT.anchorMin = new Vector2(0f, 0f);
        pillRT.anchorMax = new Vector2(1f, 0f);
        pillRT.pivot = new Vector2(0.5f, 0f);
        pillRT.anchoredPosition = new Vector2(0, -24); // Shift down to hide bottom rounded corners
        pillRT.sizeDelta = new Vector2(0, 264); // 240px visible height + 24px shift
        
        Outline pillOutline = statusPill.AddComponent<Outline>();
        pillOutline.effectColor = new Color(0, 0, 0, 0.4f);
        pillOutline.effectDistance = new Vector2(2f, -2f);

        // Top drag handle indicator (purely visual)
        GameObject handle = CreatePanel("Handle", statusPill.transform, new Color(0.2f, 0.22f, 0.25f, 1f));
        handle.GetComponent<Image>().sprite = GetRoundedSprite();
        handle.GetComponent<Image>().type = Image.Type.Sliced;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = new Vector2(0.5f, 1f);
        handleRT.anchorMax = new Vector2(0.5f, 1f);
        handleRT.pivot = new Vector2(0.5f, 1f);
        handleRT.anchoredPosition = new Vector2(0, -20);
        handleRT.sizeDelta = new Vector2(120, 8);

        // Direction text (Large Blue)
        GameObject dirGO = new GameObject("DirectionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dirGO.transform.SetParent(statusPill.transform, false);
        RectTransform dirRT = dirGO.GetComponent<RectTransform>();
        dirRT.anchorMin = new Vector2(0, 1f);
        dirRT.anchorMax = new Vector2(1, 1f);
        dirRT.pivot = new Vector2(0.5f, 1f);
        dirRT.anchoredPosition = new Vector2(0, -60);
        dirRT.sizeDelta = new Vector2(-48, 80);
        DirectionText = dirGO.GetComponent<TextMeshProUGUI>();
        
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null) DirectionText.font = defaultFont;
        
        DirectionText.text = string.Empty;
        DirectionText.fontSize = 64;
        DirectionText.alignment = TextAlignmentOptions.Center;
        DirectionText.color = new Color(0.35f, 0.55f, 1f, 1f); // Vibrant blue to match image
        DirectionText.enableWordWrapping = true;
        DirectionText.overflowMode = TextOverflowModes.Ellipsis;
        DirectionText.fontStyle = FontStyles.Bold;

        // Floating Status Toast (above the drawer)
        GameObject statusToast = CreatePanel("StatusToast", NavigationChrome.transform, new Color(0.04f, 0.05f, 0.08f, 0.95f));
        statusToast.GetComponent<Image>().sprite = GetRoundedSprite();
        statusToast.GetComponent<Image>().type = Image.Type.Sliced;
        RectTransform toastRT = statusToast.GetComponent<RectTransform>();
        toastRT.anchorMin = new Vector2(0.5f, 0f);
        toastRT.anchorMax = new Vector2(0.5f, 0f);
        toastRT.pivot = new Vector2(0.5f, 0f);
        toastRT.anchoredPosition = new Vector2(0, 280); // 240px (drawer) + 40px gap
        toastRT.sizeDelta = new Vector2(800, 80);
        
        Outline toastOutline = statusToast.AddComponent<Outline>();
        toastOutline.effectColor = new Color(0, 0, 0, 0.4f);
        toastOutline.effectDistance = new Vector2(2f, -2f);

        // Status text inside the floating toast
        GameObject statusGO = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(statusToast.transform, false);
        RectTransform statusRT = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 0f);
        statusRT.anchorMax = new Vector2(1, 1f);
        statusRT.offsetMin = Vector2.zero;
        statusRT.offsetMax = Vector2.zero;
        StatusText = statusGO.GetComponent<TextMeshProUGUI>();
        
        if (defaultFont != null) StatusText.font = defaultFont;
        
        StatusText.text = "Loading campus map...";
        StatusText.fontSize = 28;
        StatusText.alignment = TextAlignmentOptions.Center;
        StatusText.color = Color.white;
        StatusText.enableWordWrapping = true;

        // Chat button integrated into the drawer
        ChatButton = CreateButton(statusPill.transform, "ChatButton", "ASK AI");
        ChatButton.GetComponent<Image>().color = new Color(0.25f, 0.35f, 1f, 1f); // Vibrant blue
        RectTransform chatRT = ChatButton.GetComponent<RectTransform>();
        chatRT.anchorMin = new Vector2(0.5f, 0f);
        chatRT.anchorMax = new Vector2(0.5f, 0f);
        chatRT.pivot = new Vector2(0.5f, 0f);
        chatRT.anchoredPosition = new Vector2(0, 60); 
        chatRT.sizeDelta = new Vector2(400, 90);

        // Retry button (hidden by default)
        RetryButton = CreateButton(statusPill.transform, "RetryButton", "RETRY");
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
        ChatPanel = CreatePanel("ChatPanel", NavigationChrome.transform, new Color(0.04f, 0.05f, 0.08f, 1f)); // Dark background
        RectTransform rt = ChatPanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Align perfectly with ChatCloseButton vertically (-24 to -108) and use MidlineLeft
        TextMeshProUGUI title = CreateLabel(ChatPanel.transform, "ChatTitle", "TRAILIX AI", 44, TextAlignmentOptions.MidlineLeft, new Vector2(120, -108), new Vector2(-140, -24));
        title.color = Color.white;
        title.fontStyle = FontStyles.Bold;

        ChatCloseButton = CreateButton(ChatPanel.transform, "ChatCloseButton", null, "Icons/close");
        RectTransform closeRT = ChatCloseButton.GetComponent<RectTransform>();
        closeRT.anchorMin = new Vector2(0, 1);
        closeRT.anchorMax = new Vector2(0, 1);
        closeRT.pivot = new Vector2(0, 1);
        closeRT.anchoredPosition = new Vector2(24, -24);
        closeRT.sizeDelta = new Vector2(84, 84);
        
        // Transparent background, white icon
        ChatCloseButton.GetComponent<Image>().color = new Color(0,0,0,0);

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

        // Distinct dark input area at bottom
        GameObject inputRow = CreatePanel("InputRow", ChatPanel.transform, new Color(0.12f, 0.14f, 0.18f, 1f));
        RectTransform inputRT = inputRow.GetComponent<RectTransform>();
        inputRT.anchorMin = new Vector2(0, 0);
        inputRT.anchorMax = new Vector2(1, 0);
        inputRT.offsetMin = new Vector2(36, 40);
        inputRT.offsetMax = new Vector2(-36, 160); // Much taller input area
        
        Image inputBg = inputRow.GetComponent<Image>();
        inputBg.sprite = GetRoundedSprite();
        inputBg.type = Image.Type.Sliced;

        Outline inputOutline = inputRow.AddComponent<Outline>();
        inputOutline.effectColor = new Color(0, 0, 0, 0.3f);
        inputOutline.effectDistance = new Vector2(2, -2);

        ChatInput = CreateInputField(inputRow.transform);
        RectTransform inputFieldRT = ChatInput.GetComponent<RectTransform>();
        inputFieldRT.anchorMin = new Vector2(0, 0);
        inputFieldRT.anchorMax = new Vector2(1, 1);
        inputFieldRT.offsetMin = new Vector2(24, 10);
        inputFieldRT.offsetMax = new Vector2(-140, -10);
        
        // Override default input field colors to match dark theme
        ChatInput.GetComponent<Image>().color = new Color(0,0,0,0);
        ChatInput.textComponent.color = Color.white;
        ((TextMeshProUGUI)ChatInput.placeholder).color = new Color(0.6f, 0.6f, 0.65f, 1f);
        ((TextMeshProUGUI)ChatInput.placeholder).text = "Ask anything";
        SendButton = CreateButton(inputRow.transform, "SendButton", null, "Icons/send");
        RectTransform sendRT = SendButton.GetComponent<RectTransform>();
        sendRT.anchorMin = new Vector2(1, 0.5f);
        sendRT.anchorMax = new Vector2(1, 0.5f);
        sendRT.pivot = new Vector2(1, 0.5f);
        sendRT.anchoredPosition = new Vector2(-24, 0);
        sendRT.sizeDelta = new Vector2(90, 90); // Slightly larger button
        
        SendButton.GetComponent<Image>().color = new Color(0.25f, 0.35f, 1f, 1f); // Vibrant blue
    }



    private GameObject CreatePanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = GetRoundedSprite();
        img.type = Image.Type.Sliced;
        return go;
    }

    public Sprite GetRoundedSprite()
    {
        if (m_RoundedSprite != null) return m_RoundedSprite;

        int size = 64;
        int cornerRadius = 24;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isCorner = false;
                int cx = -1, cy = -1;

                if (x < cornerRadius && y < cornerRadius) { cx = cornerRadius; cy = cornerRadius; isCorner = true; }
                else if (x >= size - cornerRadius && y < cornerRadius) { cx = size - 1 - cornerRadius; cy = cornerRadius; isCorner = true; }
                else if (x < cornerRadius && y >= size - cornerRadius) { cx = cornerRadius; cy = size - 1 - cornerRadius; isCorner = true; }
                else if (x >= size - cornerRadius && y >= size - cornerRadius) { cx = size - 1 - cornerRadius; cy = size - 1 - cornerRadius; isCorner = true; }

                if (isCorner)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    pixels[y * size + x] = dist <= cornerRadius ? Color.white : Color.clear;
                }
                else
                {
                    pixels[y * size + x] = Color.white;
                }
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        m_RoundedSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius));
        return m_RoundedSprite;
    }

    private Button CreateButton(Transform parent, string name, string label, string iconResourcePath = null)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.12f, 0.14f, 0.18f, 1f));
        Image background = go.GetComponent<Image>();
        background.sprite = GetRoundedSprite();
        background.type = Image.Type.Sliced;
        
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        Button button = go.AddComponent<Button>();
        button.interactable = true;
        button.targetGraphic = background;

        // Add hover effect
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
        button.colors = colors;

        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

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
                if (defaultFont != null) labelTMP.font = defaultFont;
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
        if (defaultFont != null) buttonTMP.font = defaultFont;
        buttonTMP.text = label;
        buttonTMP.fontSize = (label != null && label.Length == 1) ? 52 : 28;
        buttonTMP.alignment = TextAlignmentOptions.Center;
        buttonTMP.color = Color.white;
        buttonTMP.fontStyle = FontStyles.Bold;

        return button;
    }



    // Create button with ONLY icon (no text)
    private Button CreateIconOnlyButton(Transform parent, string name, string iconResourcePath)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.12f, 0.14f, 0.18f, 1f));
        Image background = go.GetComponent<Image>();
        background.sprite = GetRoundedSprite();
        background.type = Image.Type.Sliced;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        Button button = go.AddComponent<Button>();
        button.interactable = true;
        button.targetGraphic = background;

        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f, 1f);
        colors.selectedColor = Color.white;
        colors.colorMultiplier = 1f;
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
        
        TMP_FontAsset defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (defaultFont != null) tmp.font = defaultFont;
        
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = Color.white;
        tmp.enableWordWrapping = true;
        return tmp;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = CreatePanel(name, parent, new Color(0.12f, 0.14f, 0.18f, 0.8f)); // Dark translucent dropdown button
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.3f);
        outline.effectDistance = new Vector2(1, -1);

        TMP_Dropdown dropdown = go.AddComponent<TMP_Dropdown>();
        dropdown.interactable = true;
        
        // CRITICAL: Set target graphic to the background image
        Image bgImage = go.GetComponent<Image>();
        bgImage.sprite = GetRoundedSprite();
        bgImage.type = Image.Type.Sliced;
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
        label.fontStyle = FontStyles.Bold; // Make dropdown text bold

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

        // ── Dropdown popup template ──────────────────────────────────────────
        // When the user clicks the dropdown button, TMP_Dropdown clones
        // this template, fills it with option items, and shows it.
        
        GameObject template = new GameObject("Template", typeof(RectTransform), typeof(Image));
        template.transform.SetParent(go.transform, false);
        RectTransform templateRT = template.GetComponent<RectTransform>();
        
        // Position template BELOW the dropdown button, full width
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta = new Vector2(0, 300);
        
        // Template background
        Image templateBg = template.GetComponent<Image>();
        templateBg.sprite = GetRoundedSprite();
        templateBg.type = Image.Type.Sliced;
        templateBg.color = new Color(0.08f, 0.1f, 0.14f, 1f); // Dark popup
        template.SetActive(false);
        
        // Add visible border outline
        Outline templateOutline = template.AddComponent<Outline>();
        templateOutline.effectColor = new Color(0.25f, 0.35f, 1f, 0.5f); // Subtle blue outline
        templateOutline.effectDistance = new Vector2(1, -1);
        
        // Canvas override for sorting — ensures popup renders above everything
        Canvas templateCanvas = template.AddComponent<Canvas>();
        templateCanvas.overrideSorting = true;
        templateCanvas.sortingOrder = 30000;
        template.AddComponent<GraphicRaycaster>();

        // ScrollRect for scrollable item list
        ScrollRect scrollRect = template.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 30f;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image));
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.offsetMin = new Vector2(2, 2);
        viewportRT.offsetMax = new Vector2(-2, -2);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.sprite = GetRoundedSprite();
        viewportImage.type = Image.Type.Sliced;
        viewportImage.color = new Color(0.06f, 0.08f, 0.12f, 1f); // Dark bg
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = true;

        scrollRect.viewport = viewportRT;

        // Content container — grows with items via ContentSizeFitter
        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 80);
        
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 16;
        contentLayout.padding = new RectOffset(16, 16, 16, 16);
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childAlignment = TextAnchor.UpperLeft;
        
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        scrollRect.content = contentRT;

        // ── Item template (each dropdown option clones this) ─────────────
        GameObject item = new GameObject("Item", typeof(RectTransform), typeof(Image));
        item.transform.SetParent(content.transform, false);
        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.sizeDelta = new Vector2(0, 80);
        Image itemBg = item.GetComponent<Image>();
        itemBg.sprite = GetRoundedSprite();
        itemBg.type = Image.Type.Sliced;
        itemBg.color = new Color(0.12f, 0.14f, 0.18f, 1f); // Dark item
        
        LayoutElement itemLayout = item.AddComponent<LayoutElement>();
        itemLayout.minHeight = 80;
        itemLayout.preferredHeight = 80;
        
        Toggle toggle = item.AddComponent<Toggle>();
        toggle.targetGraphic = itemBg;
        
        // Selection highlight overlay
        GameObject checkmark = new GameObject("Item Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(item.transform, false);
        RectTransform checkRT = checkmark.GetComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = Vector2.zero;
        checkRT.offsetMax = Vector2.zero;
        Image checkImg = checkmark.GetComponent<Image>();
        checkImg.sprite = GetRoundedSprite();
        checkImg.type = Image.Type.Sliced;
        checkImg.color = new Color(0.25f, 0.35f, 1f, 1f); // Vibrant blue highlight
        checkImg.raycastTarget = false;
        toggle.graphic = checkImg;
        
        ColorBlock toggleColors = toggle.colors;
        toggleColors.normalColor = Color.white;
        toggleColors.highlightedColor = new Color(0.35f, 0.45f, 1f, 1f); // Light blue on hover
        toggleColors.pressedColor = new Color(0.2f, 0.3f, 0.9f, 1f);
        toggleColors.selectedColor = Color.white;
        toggleColors.colorMultiplier = 1f;
        toggle.colors = toggleColors;

        // Item label
        GameObject itemLabelGO = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        itemLabelGO.transform.SetParent(item.transform, false);
        RectTransform itemLabelRT = itemLabelGO.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(16, 4);
        itemLabelRT.offsetMax = new Vector2(-16, -4);
        TextMeshProUGUI itemLabel = itemLabelGO.GetComponent<TextMeshProUGUI>();
        itemLabel.fontSize = 32;
        itemLabel.color = Color.white; // White text
        itemLabel.alignment = TextAlignmentOptions.MidlineLeft;
        itemLabel.fontStyle = FontStyles.Bold;
        itemLabel.raycastTarget = false;

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

