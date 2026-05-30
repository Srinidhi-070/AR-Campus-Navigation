using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// CampusRuntimeUI — Premium Glassmorphism + Material You redesign.
///
/// Visual language:
///   • Frosted-glass panels  → semi-transparent dark surfaces with a subtle
///     1-px bright border and soft shadow layer underneath.
///   • Floating circular FABs (top-left / top-right) hug the safe-area
///     corners and never obstruct the AR viewport.
///   • Pill-shaped "ASK AI" FAB with a purple→cyan gradient accent.
///   • Bottom status strip is a narrow glassmorphism pill docked to the
///     very bottom edge.
///   • All text uses TMPro with carefully chosen opacities so it stays
///     legible over any real-world camera background.
///
/// Public API is identical to the previous version so that
/// CampusRuntimeInstaller and NavigationFlowController compile unchanged.
/// </summary>
public class CampusRuntimeUI : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Design tokens  (tweak here to retheme the whole app)
    // ─────────────────────────────────────────────────────────────────────────

    // Glass surface: very dark, ~72 % opaque
    static readonly Color k_Glass         = new Color(0.06f, 0.07f, 0.10f, 0.72f);
    // Slightly lighter glass for panels/drawers
    static readonly Color k_GlassLight    = new Color(0.09f, 0.10f, 0.15f, 0.82f);
    // Glass border: bright cool-white tint, low opacity
    static readonly Color k_Border        = new Color(0.70f, 0.80f, 1.00f, 0.18f);
    // Shadow layer rendered behind glass panels (pure black, low opacity)
    static readonly Color k_Shadow        = new Color(0.00f, 0.00f, 0.00f, 0.45f);
    // Accent A: vivid purple (gradient start)
    static readonly Color k_AccentA       = new Color(0.42f, 0.18f, 0.92f, 1.00f);
    // Accent B: bright cyan (gradient end / icon tint)
    static readonly Color k_AccentB       = new Color(0.00f, 0.72f, 0.90f, 1.00f);
    // Muted accent for secondary buttons
    static readonly Color k_AccentMuted   = new Color(0.15f, 0.50f, 0.70f, 0.85f);
    // Text primary
    static readonly Color k_TextPrimary   = new Color(0.96f, 0.97f, 1.00f, 1.00f);
    // Text secondary / placeholder
    static readonly Color k_TextSecondary = new Color(0.65f, 0.72f, 0.88f, 0.75f);
    // Scrim for modal overlay
    static readonly Color k_Scrim         = new Color(0.00f, 0.00f, 0.00f, 0.55f);

    // Spacing / sizing constants (logical pixels at 1080×1920 reference)
    const float k_Margin     = 28f;   // safe-area inner margin
    const float k_FabSize    = 116f;  // circular FAB diameter
    const float k_CornerFAB  = 58f;   // FAB corner radius (full circle = diameter/2)
    const float k_CornerCard = 28f;   // panel / card corner radius
    const float k_CornerPill = 48f;   // pill-shaped element corner radius

    // ─────────────────────────────────────────────────────────────────────────
    //  Public properties (same surface as original — DO NOT RENAME)
    // ─────────────────────────────────────────────────────────────────────────

    public Canvas         RootCanvas      { get; private set; }
    public GameObject     NavigationChrome{ get; private set; }
    public GameObject     MenuPanel       { get; private set; }
    public GameObject     ChatPanel       { get; private set; }

    public Button         MenuButton      { get; private set; }
    public Button         QRButton        { get; private set; }
    public Button         ChatButton      { get; private set; }
    public Button         NavigateButton  { get; private set; }
    public Button         SendButton      { get; private set; }
    public Button         ChatCloseButton { get; private set; }
    public Button         RetryButton     { get; private set; }

    public TMP_Dropdown   BuildingDropdown{ get; private set; }
    public TMP_Dropdown   FloorDropdown   { get; private set; }
    public TMP_Dropdown   RoomDropdown    { get; private set; }
    public TMP_InputField ChatInput       { get; private set; }
    public Transform      ChatContent     { get; private set; }
    public ScrollRect     ChatScrollRect  { get; private set; }

    public TextMeshProUGUI StatusText     { get; private set; }
    public TextMeshProUGUI DirectionText  { get; private set; }

    // ─────────────────────────────────────────────────────────────────────────
    //  Private state
    // ─────────────────────────────────────────────────────────────────────────

    private readonly Dictionary<string, Sprite> m_IconCache = new();
    private GameObject m_MenuOverlay;
    private bool       m_Built;

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public void EnsureBuilt()
    {
        if (m_Built) return;
        EnsureEventSystem();
        BuildCanvas();
        m_Built = true;
        Debug.Log("[CampusRuntimeUI] Premium UI build complete.");
    }

    public void SetNavigationChromeVisible(bool visible) =>
        NavigationChrome?.SetActive(visible);

    public void SetMenuVisible(bool visible)
    {
        MenuPanel?.SetActive(visible);
        m_MenuOverlay?.SetActive(visible);
    }

    public void SetChatVisible(bool visible)
    {
        ChatPanel?.SetActive(visible);
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

        if (RetryButton != null)
        {
            bool showRetry = message != null &&
                (message.Contains("offline")  ||
                 message.Contains("timeout")  ||
                 message.Contains("failed")   ||
                 message.Contains("Cannot connect"));

            RetryButton.gameObject.SetActive(showRetry);
            if (ChatButton != null)
                ChatButton.gameObject.SetActive(!showRetry);
        }
    }

    public void ShowDirections(IList<string> directions)
    {
        if (DirectionText == null) return;
        DirectionText.text = directions == null
            ? string.Empty
            : string.Join("\n", directions);
    }

    /// <summary>Extra property consumed by CampusRuntimeInstaller.</summary>
    public Button MenuOverlayButton =>
        m_MenuOverlay == null ? null : m_MenuOverlay.GetComponent<Button>();

    // ─────────────────────────────────────────────────────────────────────────
    //  Bootstrap
    // ─────────────────────────────────────────────────────────────────────────

    private void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>(true) != null) return;
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private void BuildCanvas()
    {
        // ── Root canvas ──────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("CampusCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        RootCanvas = canvasGO.GetComponent<Canvas>();
        RootCanvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        RootCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode       = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;
        scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        // ── Safe-area chrome container ────────────────────────────────────────
        NavigationChrome = new GameObject("NavigationChrome", typeof(RectTransform));
        NavigationChrome.transform.SetParent(canvasGO.transform, false);
        ApplySafeArea(NavigationChrome.GetComponent<RectTransform>());

        // ── Build layers ─────────────────────────────────────────────────────
        BuildMenuOverlay();
        BuildMenuPanel();
        BuildTopFABs();
        BuildBottomHub();
        BuildChatPanel();

        SetMenuVisible(false);
        SetChatVisible(false);
    }

    private void ApplySafeArea(RectTransform rt)
    {
        Rect   sa      = Screen.safeArea;
        float  sw      = Mathf.Max(1, Screen.width);
        float  sh      = Mathf.Max(1, Screen.height);
        Vector2 aMin   = new Vector2(sa.xMin / sw, sa.yMin / sh);
        Vector2 aMax   = new Vector2(sa.xMax / sw, sa.yMax / sh);
        rt.anchorMin   = aMin;
        rt.anchorMax   = aMax;
        rt.offsetMin   = Vector2.zero;
        rt.offsetMax   = Vector2.zero;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Menu overlay (invisible tap-to-dismiss scrim)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildMenuOverlay()
    {
        m_MenuOverlay = new GameObject("MenuOverlay", typeof(RectTransform), typeof(Image));
        m_MenuOverlay.transform.SetParent(NavigationChrome.transform, false);
        RectTransform rt = m_MenuOverlay.GetComponent<RectTransform>();
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;

        m_MenuOverlay.GetComponent<Image>().color = new Color(0, 0, 0, 0.01f);
        Button btn = m_MenuOverlay.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        m_MenuOverlay.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Top FABs  (Hamburger — top-left / QR — top-right)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildTopFABs()
    {
        // ── Hamburger FAB ────────────────────────────────────────────────────
        MenuButton = BuildCircularFAB(
            NavigationChrome.transform, "MenuButton",
            corner: TextAnchor.UpperLeft,
            offset: new Vector2(k_Margin, -k_Margin));
        BuildHamburgerIcon(MenuButton.transform);

        // ── QR FAB ───────────────────────────────────────────────────────────
        QRButton = BuildCircularFAB(
            NavigationChrome.transform, "QRButton",
            corner: TextAnchor.UpperRight,
            offset: new Vector2(-k_Margin, -k_Margin));
        BuildQRIcon(QRButton.transform);
    }

    /// <summary>
    /// Creates a circular frosted-glass FAB.
    /// Corner: UpperLeft / UpperRight only (extend as needed).
    /// </summary>
    private Button BuildCircularFAB(Transform parent, string name,
                                    TextAnchor corner, Vector2 offset)
    {
        // Shadow layer (rendered first, slightly larger, pure black)
        GameObject shadow = MakeRect(name + "_Shadow", parent);
        Image shadowImg   = shadow.AddComponent<Image>();
        shadowImg.color   = k_Shadow;
        shadowImg.raycastTarget = false;
        RectTransform shadowRT  = shadow.GetComponent<RectTransform>();
        PinCorner(shadowRT, corner, offset + new Vector2(
            corner == TextAnchor.UpperLeft ? 4f : -4f, -4f),
            new Vector2(k_FabSize + 8f, k_FabSize + 8f));

        // Glass surface
        GameObject go     = MakeRect(name, parent);
        Image img         = go.AddComponent<Image>();
        img.color         = k_Glass;
        Button btn        = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb       = btn.colors;
        cb.normalColor      = k_Glass;
        cb.highlightedColor = k_GlassLight;
        cb.pressedColor     = new Color(0.04f, 0.05f, 0.09f, 0.90f);
        cb.colorMultiplier  = 1f;
        btn.colors          = cb;

        RectTransform rt    = go.GetComponent<RectTransform>();
        PinCorner(rt, corner, offset, new Vector2(k_FabSize, k_FabSize));

        // Thin bright border (Outline component — lightweight glass rim)
        Outline border      = go.AddComponent<Outline>();
        border.effectColor  = k_Border;
        border.effectDistance = new Vector2(1.5f, -1.5f);

        return btn;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Menu panel (slide-out drawer, top-left anchored)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildMenuPanel()
    {
        // Drop-shadow layer
        GameObject shadow   = MakeGlassPanel("MenuPanel_Shadow", NavigationChrome.transform, k_Shadow);
        RectTransform shadowRT = shadow.GetComponent<RectTransform>();
        shadowRT.anchorMin  = new Vector2(0, 1);
        shadowRT.anchorMax  = new Vector2(0, 1);
        shadowRT.pivot      = new Vector2(0, 1);
        shadowRT.anchoredPosition = new Vector2(k_Margin + 6f, -(k_Margin + k_FabSize + 20f) - 6f);
        shadowRT.sizeDelta  = new Vector2(660, 660);

        // Glass drawer
        MenuPanel           = MakeGlassPanel("MenuPanel", NavigationChrome.transform, k_GlassLight);
        Outline border      = MenuPanel.AddComponent<Outline>();
        border.effectColor  = k_Border;
        border.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform rt    = MenuPanel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(k_Margin, -(k_Margin + k_FabSize + 20f));
        rt.sizeDelta        = new Vector2(660, 660);

        // Title
        TextMeshProUGUI title = MakeLabel(MenuPanel.transform, "MenuTitle",
            "Navigate To", 40, TextAlignmentOptions.Left, FontStyles.Bold, k_TextPrimary);
        AnchorStretchTop(title.GetComponent<RectTransform>(), 28, 28, 28, 88);

        // ── Building ──────────────────────────────────────────────────────────
        MakeFieldLabel(MenuPanel.transform, "Building", offsetFromTop: 100);
        BuildingDropdown = MakeDropdown(MenuPanel.transform, "BuildingDropdown",
            new Vector2(28, -148), new Vector2(-28, -228));

        // ── Floor ─────────────────────────────────────────────────────────────
        MakeFieldLabel(MenuPanel.transform, "Floor", offsetFromTop: 248);
        FloorDropdown = MakeDropdown(MenuPanel.transform, "FloorDropdown",
            new Vector2(28, -296), new Vector2(-28, -376));

        // ── Destination ───────────────────────────────────────────────────────
        MakeFieldLabel(MenuPanel.transform, "Destination", offsetFromTop: 396);
        RoomDropdown = MakeDropdown(MenuPanel.transform, "RoomDropdown",
            new Vector2(28, -444), new Vector2(-28, -524));

        // ── Navigate button ───────────────────────────────────────────────────
        NavigateButton = MakeAccentButton(MenuPanel.transform, "NavigateButton", "NAVIGATE");
        RectTransform navRT = NavigateButton.GetComponent<RectTransform>();
        navRT.anchorMin     = new Vector2(0, 1);
        navRT.anchorMax     = new Vector2(1, 1);
        navRT.pivot         = new Vector2(0.5f, 1f);
        navRT.offsetMin     = new Vector2(28, -628);
        navRT.offsetMax     = new Vector2(-28, -540);
    }

    private void MakeFieldLabel(Transform parent, string labelText, float offsetFromTop)
    {
        TextMeshProUGUI lbl = MakeLabel(parent, labelText + "Label",
            labelText, 26, TextAlignmentOptions.Left, FontStyles.Normal, k_TextSecondary);
        AnchorStretchTop(lbl.GetComponent<RectTransform>(),
            28, 28, offsetFromTop, offsetFromTop + 36);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Bottom hub  (pill ASK-AI + status strip)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildBottomHub()
    {
        // ── ASK AI pill FAB ──────────────────────────────────────────────────
        // Positioned just above the status strip, centred horizontally.
        // Shadow
        GameObject chatShadow   = MakeRect("ChatButton_Shadow", NavigationChrome.transform);
        Image chatShadowImg     = chatShadow.AddComponent<Image>();
        chatShadowImg.color     = k_Shadow;
        chatShadowImg.raycastTarget = false;
        RectTransform chatShadowRT  = chatShadow.GetComponent<RectTransform>();
        chatShadowRT.anchorMin  = new Vector2(0.5f, 0);
        chatShadowRT.anchorMax  = new Vector2(0.5f, 0);
        chatShadowRT.pivot      = new Vector2(0.5f, 0);
        chatShadowRT.anchoredPosition = new Vector2(4, 160f);
        chatShadowRT.sizeDelta  = new Vector2(364, 84);

        // Pill button
        GameObject chatGO       = MakeRect("ChatButton", NavigationChrome.transform);
        Image chatImg           = chatGO.AddComponent<Image>();
        // Gradient-look: single vivid accent color (true gradient needs shader;
        // this gives the vivid Material You "dynamic color" feel)
        chatImg.color           = k_AccentA;
        ChatButton              = chatGO.AddComponent<Button>();
        ChatButton.targetGraphic = chatImg;

        ColorBlock chatCB       = ChatButton.colors;
        chatCB.normalColor      = k_AccentA;
        chatCB.highlightedColor = new Color(0.55f, 0.28f, 0.98f, 1f);
        chatCB.pressedColor     = new Color(0.30f, 0.10f, 0.72f, 1f);
        chatCB.colorMultiplier  = 1f;
        ChatButton.colors       = chatCB;

        RectTransform chatRT    = chatGO.GetComponent<RectTransform>();
        chatRT.anchorMin        = new Vector2(0.5f, 0);
        chatRT.anchorMax        = new Vector2(0.5f, 0);
        chatRT.pivot            = new Vector2(0.5f, 0);
        chatRT.anchoredPosition = new Vector2(0, 158f);
        chatRT.sizeDelta        = new Vector2(360, 80);

        // Border shimmer around the pill
        Outline chatBorder      = chatGO.AddComponent<Outline>();
        chatBorder.effectColor  = new Color(0.70f, 0.50f, 1.00f, 0.50f);
        chatBorder.effectDistance = new Vector2(1.5f, -1.5f);

        // "✦ ASK AI" label inside pill
        TextMeshProUGUI chatLbl = MakeLabel(chatGO.transform, "Label",
            "✦  ASK AI", 30, TextAlignmentOptions.Center, FontStyles.Bold, k_TextPrimary);
        StretchFill(chatLbl.GetComponent<RectTransform>());

        // ── Status strip ─────────────────────────────────────────────────────
        // A narrow glass pill docked to the very bottom edge.
        GameObject statusShadow = MakeRect("StatusStrip_Shadow", NavigationChrome.transform);
        statusShadow.AddComponent<Image>().color = k_Shadow;
        statusShadow.GetComponent<Image>().raycastTarget = false;
        RectTransform ssShadowRT = statusShadow.GetComponent<RectTransform>();
        ssShadowRT.anchorMin = new Vector2(0.5f, 0);
        ssShadowRT.anchorMax = new Vector2(0.5f, 0);
        ssShadowRT.pivot     = new Vector2(0.5f, 0);
        ssShadowRT.anchoredPosition = new Vector2(4, k_Margin - 4);
        ssShadowRT.sizeDelta = new Vector2(1036, 144);

        GameObject statusStrip  = MakeGlassPanel("StatusStrip", NavigationChrome.transform, k_Glass);
        Outline ssBorder        = statusStrip.AddComponent<Outline>();
        ssBorder.effectColor    = k_Border;
        ssBorder.effectDistance = new Vector2(1.5f, -1.5f);

        RectTransform ssRT      = statusStrip.GetComponent<RectTransform>();
        ssRT.anchorMin          = new Vector2(0.5f, 0);
        ssRT.anchorMax          = new Vector2(0.5f, 0);
        ssRT.pivot              = new Vector2(0.5f, 0);
        ssRT.anchoredPosition   = new Vector2(0, k_Margin);
        ssRT.sizeDelta          = new Vector2(1032, 140);

        // Direction text (top half of strip)
        GameObject dirGO        = new GameObject("DirectionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        dirGO.transform.SetParent(statusStrip.transform, false);
        RectTransform dirRT     = dirGO.GetComponent<RectTransform>();
        dirRT.anchorMin         = new Vector2(0, 0.5f);
        dirRT.anchorMax         = new Vector2(1, 1);
        dirRT.offsetMin         = new Vector2(32, 4);
        dirRT.offsetMax         = new Vector2(-32, -4);
        DirectionText           = dirGO.GetComponent<TextMeshProUGUI>();
        DirectionText.text      = string.Empty;
        DirectionText.fontSize  = 22;
        DirectionText.color     = k_AccentB;
        DirectionText.alignment = TextAlignmentOptions.Center;
        DirectionText.fontStyle = FontStyles.Bold;
        DirectionText.enableWordWrapping = true;
        DirectionText.overflowMode = TextOverflowModes.Ellipsis;

        // Status text (bottom half of strip)
        GameObject statusGO     = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        statusGO.transform.SetParent(statusStrip.transform, false);
        RectTransform statusRT  = statusGO.GetComponent<RectTransform>();
        statusRT.anchorMin      = new Vector2(0, 0);
        statusRT.anchorMax      = new Vector2(1, 0.5f);
        statusRT.offsetMin      = new Vector2(32, 6);
        statusRT.offsetMax      = new Vector2(-32, -4);
        StatusText              = statusGO.GetComponent<TextMeshProUGUI>();
        StatusText.text         = "Loading campus map…";
        StatusText.fontSize     = 22;
        StatusText.color        = k_TextSecondary;
        StatusText.alignment    = TextAlignmentOptions.Center;
        StatusText.enableWordWrapping = true;

        // Retry button (hidden by default, shown when offline)
        RetryButton = MakeAccentButton(statusStrip.transform, "RetryButton", "RETRY");
        RectTransform retryRT   = RetryButton.GetComponent<RectTransform>();
        retryRT.anchorMin       = new Vector2(0.5f, 0.5f);
        retryRT.anchorMax       = new Vector2(0.5f, 0.5f);
        retryRT.pivot           = new Vector2(0.5f, 0.5f);
        retryRT.anchoredPosition = Vector2.zero;
        retryRT.sizeDelta       = new Vector2(280, 64);
        RetryButton.gameObject.SetActive(false);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chat panel (full-screen modal with glass header)
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildChatPanel()
    {
        // Full-screen scrim + glass panel
        ChatPanel               = MakeGlassPanel("ChatPanel", NavigationChrome.transform,
                                      new Color(0.04f, 0.05f, 0.08f, 0.96f));
        RectTransform rt        = ChatPanel.GetComponent<RectTransform>();
        rt.anchorMin            = Vector2.zero;
        rt.anchorMax            = Vector2.one;
        rt.offsetMin            = Vector2.zero;
        rt.offsetMax            = Vector2.zero;

        // ── Header bar ───────────────────────────────────────────────────────
        GameObject header       = MakeGlassPanel("Header", ChatPanel.transform, k_GlassLight);
        Outline hBorder         = header.AddComponent<Outline>();
        hBorder.effectColor     = k_Border;
        hBorder.effectDistance  = new Vector2(0, -1.5f); // bottom border only feel
        RectTransform headerRT  = header.GetComponent<RectTransform>();
        headerRT.anchorMin      = new Vector2(0, 1);
        headerRT.anchorMax      = new Vector2(1, 1);
        headerRT.pivot          = new Vector2(0.5f, 1);
        headerRT.anchoredPosition = Vector2.zero;
        headerRT.sizeDelta      = new Vector2(0, 110);

        // Title
        TextMeshProUGUI titleTMP = MakeLabel(header.transform, "ChatTitle",
            "AI Navigation Assistant", 36, TextAlignmentOptions.Left,
            FontStyles.Bold, k_TextPrimary);
        RectTransform titleRT   = titleTMP.GetComponent<RectTransform>();
        titleRT.anchorMin       = new Vector2(0, 0);
        titleRT.anchorMax       = new Vector2(1, 1);
        titleRT.offsetMin       = new Vector2(28, 0);
        titleRT.offsetMax       = new Vector2(-120, 0);

        // Close button (glass circle with × glyph)
        ChatCloseButton         = BuildCircularFAB(header.transform, "CloseBtn",
                                      TextAnchor.UpperRight,
                                      new Vector2(-14f, -14f));
        // Override FAB size to smaller
        RectTransform closeRT   = ChatCloseButton.GetComponent<RectTransform>();
        closeRT.sizeDelta       = new Vector2(80, 80);
        closeRT.anchoredPosition = new Vector2(-14, -14);

        TextMeshProUGUI closeLbl = MakeLabel(ChatCloseButton.transform, "Icon",
            "✕", 36, TextAlignmentOptions.Center, FontStyles.Bold, k_TextPrimary);
        StretchFill(closeLbl.GetComponent<RectTransform>());

        // ── Message scroll area ───────────────────────────────────────────────
        GameObject scrollGO     = MakeRect("ChatScroll", ChatPanel.transform);
        scrollGO.AddComponent<Image>().color = Color.clear;
        RectTransform scrollRT  = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin      = new Vector2(0, 0);
        scrollRT.anchorMax      = new Vector2(1, 1);
        scrollRT.offsetMin      = new Vector2(0, 120);
        scrollRT.offsetMax      = new Vector2(0, -110);

        ChatScrollRect          = scrollGO.AddComponent<ScrollRect>();
        ChatScrollRect.horizontal = false;

        GameObject viewport     = MakeRect("Viewport", scrollGO.transform);
        Image vpImg             = viewport.AddComponent<Image>();
        vpImg.color             = Color.white; // needs alpha > 0 for Mask stencil
        RectTransform vpRT      = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin          = Vector2.zero;
        vpRT.anchorMax          = Vector2.one;
        vpRT.offsetMin          = Vector2.zero;
        vpRT.offsetMax          = Vector2.zero;
        Mask vpMask             = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic  = false;
        ChatScrollRect.viewport = vpRT;

        GameObject content      = MakeRect("Content", viewport.transform);
        content.AddComponent<Image>().color = Color.clear;
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin     = new Vector2(0, 1);
        contentRT.anchorMax     = new Vector2(1, 1);
        contentRT.pivot         = new Vector2(0.5f, 1);
        contentRT.sizeDelta     = Vector2.zero;

        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment       = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight   = true;
        vlg.childControlWidth    = true;
        vlg.spacing              = 10;
        vlg.padding              = new RectOffset(20, 20, 16, 16);

        ContentSizeFitter csf   = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit         = ContentSizeFitter.FitMode.PreferredSize;
        ChatScrollRect.content  = contentRT;
        ChatContent             = content.transform;

        // ── Input row (glass strip at very bottom) ────────────────────────────
        GameObject inputRow     = MakeGlassPanel("InputRow", ChatPanel.transform, k_GlassLight);
        Outline inBorder        = inputRow.AddComponent<Outline>();
        inBorder.effectColor    = k_Border;
        inBorder.effectDistance = new Vector2(0, 1.5f); // top border
        RectTransform inputRT   = inputRow.GetComponent<RectTransform>();
        inputRT.anchorMin       = new Vector2(0, 0);
        inputRT.anchorMax       = new Vector2(1, 0);
        inputRT.pivot           = new Vector2(0.5f, 0);
        inputRT.anchoredPosition = Vector2.zero;
        inputRT.sizeDelta       = new Vector2(0, 110);

        ChatInput               = MakeChatInputField(inputRow.transform);
        RectTransform cifRT     = ChatInput.GetComponent<RectTransform>();
        cifRT.anchorMin         = new Vector2(0, 0);
        cifRT.anchorMax         = new Vector2(1, 1);
        cifRT.offsetMin         = new Vector2(20, 14);
        cifRT.offsetMax         = new Vector2(-180, -14);

        SendButton              = MakeAccentButton(inputRow.transform, "SendButton", "SEND");
        RectTransform sendRT    = SendButton.GetComponent<RectTransform>();
        sendRT.anchorMin        = new Vector2(1, 0.5f);
        sendRT.anchorMax        = new Vector2(1, 0.5f);
        sendRT.pivot            = new Vector2(1, 0.5f);
        sendRT.anchoredPosition = new Vector2(-14, 0);
        sendRT.sizeDelta        = new Vector2(152, 68);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Factory helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Bare RectTransform child (no Image).</summary>
    private GameObject MakeRect(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    /// <summary>RectTransform child with an Image (glass colour).</summary>
    private GameObject MakeGlassPanel(string name, Transform parent, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    /// <summary>
    /// Accent-coloured button (purple, pill or rect shape).
    /// Uses k_AccentA as default; override colors after calling if needed.
    /// </summary>
    private Button MakeAccentButton(Transform parent, string name, string label)
    {
        GameObject go   = MakeGlassPanel(name, parent, k_AccentA);
        Image img       = go.GetComponent<Image>();
        Button btn      = go.AddComponent<Button>();
        btn.targetGraphic = img;

        ColorBlock cb       = btn.colors;
        cb.normalColor      = k_AccentA;
        cb.highlightedColor = new Color(0.55f, 0.28f, 0.98f, 1f);
        cb.pressedColor     = new Color(0.30f, 0.10f, 0.72f, 1f);
        cb.colorMultiplier  = 1f;
        btn.colors          = cb;

        TextMeshProUGUI lbl = MakeLabel(go.transform, "Label",
            label, 28, TextAlignmentOptions.Center, FontStyles.Bold, k_TextPrimary);
        StretchFill(lbl.GetComponent<RectTransform>());
        return btn;
    }

    private TextMeshProUGUI MakeLabel(
        Transform parent, string name,
        string text, float fontSize,
        TextAlignmentOptions alignment,
        FontStyles style, Color color)
    {
        GameObject go   = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text            = text;
        tmp.fontSize        = fontSize;
        tmp.alignment       = alignment;
        tmp.fontStyle       = style;
        tmp.color           = color;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget   = false;
        return tmp;
    }

    private TMP_InputField MakeChatInputField(Transform parent)
    {
        GameObject go       = MakeGlassPanel("ChatInput", parent,
                                  new Color(0.10f, 0.12f, 0.18f, 1f));
        TMP_InputField field = go.AddComponent<TMP_InputField>();
        field.lineType      = TMP_InputField.LineType.SingleLine;
        field.inputType     = TMP_InputField.InputType.Standard;
        field.keyboardType  = TouchScreenKeyboardType.Default;
        field.characterValidation = TMP_InputField.CharacterValidation.None;
        field.characterLimit = 200;

        // Text Area (masked)
        GameObject ta       = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        ta.transform.SetParent(go.transform, false);
        RectTransform taRT  = ta.GetComponent<RectTransform>();
        taRT.anchorMin      = Vector2.zero;
        taRT.anchorMax      = Vector2.one;
        taRT.offsetMin      = new Vector2(14, 8);
        taRT.offsetMax      = new Vector2(-14, -8);

        // Placeholder
        GameObject phGO     = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        phGO.transform.SetParent(ta.transform, false);
        StretchFill(phGO.GetComponent<RectTransform>());
        TextMeshProUGUI ph  = phGO.GetComponent<TextMeshProUGUI>();
        ph.text             = "Ask where you want to go…";
        ph.fontSize         = 26;
        ph.color            = k_TextSecondary;
        ph.alignment        = TextAlignmentOptions.MidlineLeft;

        // Input text
        GameObject txtGO    = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGO.transform.SetParent(ta.transform, false);
        StretchFill(txtGO.GetComponent<RectTransform>());
        TextMeshProUGUI txt = txtGO.GetComponent<TextMeshProUGUI>();
        txt.fontSize        = 26;
        txt.color           = k_TextPrimary;
        txt.alignment       = TextAlignmentOptions.MidlineLeft;

        field.textViewport  = taRT;
        field.textComponent = txt;
        field.placeholder   = ph;
        return field;
    }

    private TMP_Dropdown MakeDropdown(Transform parent, string name,
                                      Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go       = MakeGlassPanel(name, parent,
                                  new Color(0.10f, 0.12f, 0.18f, 1f));
        RectTransform rt    = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(1, 1);
        rt.pivot            = new Vector2(0.5f, 1f);
        rt.offsetMin        = offsetMin;
        rt.offsetMax        = offsetMax;

        // Add border
        Outline outline     = go.AddComponent<Outline>();
        outline.effectColor = k_Border;
        outline.effectDistance = new Vector2(1f, -1f);

        TMP_Dropdown dd     = go.AddComponent<TMP_Dropdown>();
        Image bgImg         = go.GetComponent<Image>();
        dd.targetGraphic    = bgImg;
        dd.interactable     = true;

        // Caption label
        GameObject lblGO    = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lblGO.transform.SetParent(go.transform, false);
        RectTransform lblRT = lblGO.GetComponent<RectTransform>();
        lblRT.anchorMin     = Vector2.zero;
        lblRT.anchorMax     = Vector2.one;
        lblRT.offsetMin     = new Vector2(16, 8);
        lblRT.offsetMax     = new Vector2(-48, -8);
        TextMeshProUGUI lbl = lblGO.GetComponent<TextMeshProUGUI>();
        lbl.fontSize        = 28;
        lbl.color           = k_TextPrimary;
        lbl.alignment       = TextAlignmentOptions.MidlineLeft;
        lbl.raycastTarget   = false;

        // Chevron
        GameObject arrGO    = new GameObject("Arrow", typeof(RectTransform), typeof(TextMeshProUGUI));
        arrGO.transform.SetParent(go.transform, false);
        RectTransform arrRT = arrGO.GetComponent<RectTransform>();
        arrRT.anchorMin     = new Vector2(1, 0.5f);
        arrRT.anchorMax     = new Vector2(1, 0.5f);
        arrRT.pivot         = new Vector2(1, 0.5f);
        arrRT.anchoredPosition = new Vector2(-16, 0);
        arrRT.sizeDelta     = new Vector2(28, 28);
        TextMeshProUGUI arr = arrGO.GetComponent<TextMeshProUGUI>();
        arr.text            = "⌄";
        arr.fontSize        = 28;
        arr.color           = k_AccentB;
        arr.alignment       = TextAlignmentOptions.Center;
        arr.raycastTarget   = false;

        // ── Popup template ─────────────────────────────────────────────────────
        GameObject template     = MakeGlassPanel("Template", go.transform,
                                      new Color(0.08f, 0.09f, 0.14f, 0.97f));
        RectTransform templateRT = template.GetComponent<RectTransform>();
        templateRT.anchorMin    = new Vector2(0, 0);
        templateRT.anchorMax    = new Vector2(1, 0);
        templateRT.pivot        = new Vector2(0.5f, 1f);
        templateRT.anchoredPosition = new Vector2(0, 2);
        templateRT.sizeDelta    = new Vector2(0, 320);
        template.SetActive(false);

        Outline tplBorder       = template.AddComponent<Outline>();
        tplBorder.effectColor   = k_Border;
        tplBorder.effectDistance = new Vector2(1.5f, -1.5f);

        Canvas tplCanvas        = template.AddComponent<Canvas>();
        tplCanvas.overrideSorting = true;
        tplCanvas.sortingOrder  = 30000;
        template.AddComponent<GraphicRaycaster>();

        ScrollRect sr           = template.AddComponent<ScrollRect>();
        sr.horizontal           = false;
        sr.vertical             = true;
        sr.scrollSensitivity    = 30f;
        sr.movementType         = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject vp           = MakeGlassPanel("Viewport", template.transform,
                                      new Color(0.08f, 0.09f, 0.14f, 1f));
        RectTransform vpRT      = vp.GetComponent<RectTransform>();
        vpRT.anchorMin          = Vector2.zero;
        vpRT.anchorMax          = Vector2.one;
        vpRT.offsetMin          = new Vector2(2, 2);
        vpRT.offsetMax          = new Vector2(-2, -2);
        vp.AddComponent<Mask>().showMaskGraphic = true;
        sr.viewport             = vpRT;

        // Content
        GameObject cont         = new GameObject("Content", typeof(RectTransform));
        cont.transform.SetParent(vp.transform, false);
        RectTransform contRT    = cont.GetComponent<RectTransform>();
        contRT.anchorMin        = new Vector2(0, 1);
        contRT.anchorMax        = new Vector2(1, 1);
        contRT.pivot            = new Vector2(0.5f, 1f);
        contRT.sizeDelta        = new Vector2(0, 80);
        VerticalLayoutGroup cvlg = cont.AddComponent<VerticalLayoutGroup>();
        cvlg.spacing            = 2;
        cvlg.padding            = new RectOffset(4, 4, 4, 4);
        cvlg.childForceExpandWidth = true;
        cvlg.childForceExpandHeight = false;
        cvlg.childControlWidth  = true;
        cvlg.childControlHeight = true;
        ContentSizeFitter ccsf  = cont.AddComponent<ContentSizeFitter>();
        ccsf.verticalFit        = ContentSizeFitter.FitMode.PreferredSize;
        sr.content              = contRT;

        // Item template
        GameObject item         = MakeGlassPanel("Item", cont.transform,
                                      new Color(0.14f, 0.16f, 0.24f, 1f));
        RectTransform itemRT    = item.GetComponent<RectTransform>();
        itemRT.sizeDelta        = new Vector2(0, 80);
        Image itemBg            = item.GetComponent<Image>();

        LayoutElement itemLE    = item.AddComponent<LayoutElement>();
        itemLE.minHeight        = 80;
        itemLE.preferredHeight  = 80;

        Toggle toggle           = item.AddComponent<Toggle>();
        toggle.targetGraphic    = itemBg;

        // Selection highlight
        GameObject checkmark    = MakeGlassPanel("Item Checkmark", item.transform,
                                      new Color(0.00f, 0.72f, 0.90f, 0.28f));
        StretchFill(checkmark.GetComponent<RectTransform>());
        Image checkImg          = checkmark.GetComponent<Image>();
        checkImg.raycastTarget  = false;
        toggle.graphic          = checkImg;

        ColorBlock tCB          = toggle.colors;
        tCB.normalColor         = Color.white;
        tCB.highlightedColor    = new Color(0.80f, 0.95f, 1.00f, 1f);
        tCB.pressedColor        = new Color(0.60f, 0.88f, 0.96f, 1f);
        tCB.selectedColor       = new Color(0.70f, 0.92f, 0.98f, 1f);
        tCB.colorMultiplier     = 1f;
        toggle.colors           = tCB;

        // Item label
        GameObject ilGO         = new GameObject("Item Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        ilGO.transform.SetParent(item.transform, false);
        RectTransform ilRT      = ilGO.GetComponent<RectTransform>();
        ilRT.anchorMin          = Vector2.zero;
        ilRT.anchorMax          = Vector2.one;
        ilRT.offsetMin          = new Vector2(16, 4);
        ilRT.offsetMax          = new Vector2(-16, -4);
        TextMeshProUGUI il      = ilGO.GetComponent<TextMeshProUGUI>();
        il.fontSize             = 30;
        il.color                = k_TextPrimary;
        il.alignment            = TextAlignmentOptions.MidlineLeft;
        il.raycastTarget        = false;

        dd.captionText          = lbl;
        dd.template             = templateRT;
        dd.itemText             = il;

        return dd;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Icon builders (programmatic, no texture assets required)
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Three white horizontal bars centred inside a parent.</summary>
    private void BuildHamburgerIcon(Transform parent)
    {
        for (int i = 0; i < 3; i++)
        {
            GameObject line = MakeGlassPanel($"HamLine{i}", parent, k_TextPrimary);
            line.GetComponent<Image>().raycastTarget = false;
            RectTransform rt = line.GetComponent<RectTransform>();
            rt.anchorMin     = new Vector2(0.22f, 0.5f);
            rt.anchorMax     = new Vector2(0.78f, 0.5f);
            rt.pivot         = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, (i - 1) * 22f);
            rt.sizeDelta     = new Vector2(0, 7f);
        }
    }

    /// <summary>Minimalist QR-code icon: outer frame + 3 corner squares.</summary>
    private void BuildQRIcon(Transform parent)
    {
        // Outer frame
        MakeIconRect(parent, "QRFrame", k_TextPrimary,
            new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), 0f, 0f, false);

        // Three corner finder squares
        float[] xs = { 0.24f, 0.56f, 0.24f };
        float[] ys = { 0.56f, 0.56f, 0.24f };
        for (int i = 0; i < 3; i++)
        {
            MakeIconRect(parent, $"QRCorner{i}", k_TextPrimary,
                new Vector2(xs[i], ys[i]),
                new Vector2(xs[i] + 0.20f, ys[i] + 0.20f),
                0f, 0f, false);
        }

        // A few dots to hint at QR data
        MakeIconRect(parent, "QRDot0", k_AccentB,
            new Vector2(0.56f, 0.24f), new Vector2(0.68f, 0.36f), 0, 0, false);
        MakeIconRect(parent, "QRDot1", k_AccentB,
            new Vector2(0.58f, 0.42f), new Vector2(0.70f, 0.50f), 0, 0, false);
    }

    /// <summary>Helper: add a small rect inside a parent using anchor fractions.</summary>
    private void MakeIconRect(Transform parent, string name, Color color,
                               Vector2 anchorMin, Vector2 anchorMax,
                               float offsetX, float offsetY, bool raycast)
    {
        GameObject go   = MakeGlassPanel(name, parent, color);
        Image img       = go.GetComponent<Image>();
        img.raycastTarget = raycast;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin    = anchorMin;
        rt.anchorMax    = anchorMax;
        rt.offsetMin    = new Vector2(offsetX, offsetY);
        rt.offsetMax    = new Vector2(-offsetX, -offsetY);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Layout helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Pin a rect to a screen corner.</summary>
    private void PinCorner(RectTransform rt, TextAnchor corner,
                            Vector2 anchoredPos, Vector2 size)
    {
        switch (corner)
        {
            case TextAnchor.UpperLeft:
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(0, 1);
                rt.pivot     = new Vector2(0, 1);
                break;
            case TextAnchor.UpperRight:
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot     = new Vector2(1, 1);
                break;
            case TextAnchor.LowerLeft:
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(0, 0);
                rt.pivot     = new Vector2(0, 0);
                break;
            case TextAnchor.LowerRight:
                rt.anchorMin = new Vector2(1, 0);
                rt.anchorMax = new Vector2(1, 0);
                rt.pivot     = new Vector2(1, 0);
                break;
        }
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = size;
    }

    /// <summary>Stretch to fill parent (full anchor).</summary>
    private void StretchFill(RectTransform rt)
    {
        rt.anchorMin  = Vector2.zero;
        rt.anchorMax  = Vector2.one;
        rt.offsetMin  = Vector2.zero;
        rt.offsetMax  = Vector2.zero;
    }

    /// <summary>
    /// Anchor to top-stretch: anchors to top edge, stretches horizontally.
    /// offsetFromTop / offsetToTop are measured downward from the top.
    /// </summary>
    private void AnchorStretchTop(RectTransform rt,
        float leftInset, float rightInset,
        float offsetFromTop, float offsetToTop)
    {
        rt.anchorMin  = new Vector2(0, 1);
        rt.anchorMax  = new Vector2(1, 1);
        rt.pivot      = new Vector2(0.5f, 1f);
        rt.offsetMin  = new Vector2(leftInset,  -offsetToTop);
        rt.offsetMax  = new Vector2(-rightInset, -offsetFromTop);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  (Retained for StretchBottom, SetAnchoredRect if referenced externally)
    // ─────────────────────────────────────────────────────────────────────────

    private void StretchBottom(RectTransform rt, float left, float right, float height)
    {
        rt.anchorMin  = new Vector2(0, 0);
        rt.anchorMax  = new Vector2(1, 0);
        rt.pivot      = new Vector2(0.5f, 0f);
        rt.offsetMin  = new Vector2(left, 0);
        rt.offsetMax  = new Vector2(-right, height);
    }

    // Icon loading (kept for any callers that still use it)
    private Sprite LoadIcon(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath)) return null;
        if (m_IconCache.TryGetValue(resourcePath, out Sprite cached)) return cached;
        Texture2D tex = Resources.Load<Texture2D>(resourcePath);
        if (tex == null) { m_IconCache[resourcePath] = null; return null; }
        Sprite s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                                 new Vector2(0.5f, 0.5f), 100f);
        m_IconCache[resourcePath] = s;
        return s;
    }
}
