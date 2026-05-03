using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModernUIBuilder : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private AIManager              aiManager;
    [SerializeField] private IndoorNavigationBridge indoorBridge;
    [SerializeField] private ChatManager            chatManager;
    [SerializeField] private QRScanner              qrScanner;

    [HideInInspector] public TextMeshProUGUI directionText;
    [HideInInspector] public TextMeshProUGUI statusText;

    private GameObject   m_ChatPanel;
    private bool         m_ChatOpen = false;
    private TMP_Dropdown m_FloorDropdown;
    private TMP_Dropdown m_EntranceDropdown;
    private TMP_Dropdown m_RoomDropdown;
    private GameObject   m_MenuPanel;
    private GameObject   m_MenuButton;
    private bool         m_MenuOpen = false;

    private Dictionary<int, List<LocationData>> m_FloorMap;
    private List<int>          m_FloorKeys;
    private List<LocationData> m_EntranceList;

    void Awake()
    {
        BuildDataMaps();
        BuildUI();
    }

    void Start() => WireUIManager();

    // ─────────────────────────────────────────────────────────────────────────
    // DATA
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildDataMaps()
    {
        m_FloorMap     = new Dictionary<int, List<LocationData>>();
        m_FloorKeys    = new List<int>();
        m_EntranceList = new List<LocationData>();

        if (AppController.Instance?.Locations == null) return;

        foreach (LocationData loc in AppController.Instance.Locations.GetAllLocations())
        {
            // Floor map — only non-corridor/staircase rooms as destinations
            if (loc.type != "staircase")
            {
                if (!m_FloorMap.ContainsKey(loc.floor))
                    m_FloorMap[loc.floor] = new List<LocationData>();
                m_FloorMap[loc.floor].Add(loc);
            }

            if (loc.type == "entrance" || loc.type == "corridor")
                m_EntranceList.Add(loc);
        }

        m_FloorKeys = new List<int>(m_FloorMap.Keys);
        m_FloorKeys.Sort();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // BUILD UI
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("NavUICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight  = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        Transform root = canvasGO.transform;

        // QR Scan button (top-right) — pill shape with teal accent
        GameObject qrBtn = new GameObject("QRBtn");
        qrBtn.transform.SetParent(root, false);
        RectTransform qrBtnRT = qrBtn.AddComponent<RectTransform>();
        qrBtnRT.anchorMin        = new Vector2(1, 1);
        qrBtnRT.anchorMax        = new Vector2(1, 1);
        qrBtnRT.pivot            = new Vector2(1, 1);
        qrBtnRT.anchoredPosition = new Vector2(-24, -52);
        qrBtnRT.sizeDelta        = new Vector2(120, 120);
        Image qrImg = qrBtn.AddComponent<Image>();
        qrImg.color = new Color(0.0f, 0.72f, 0.75f, 0.92f);
        Button qrBtnComp = qrBtn.AddComponent<Button>();
        qrBtnComp.targetGraphic = qrImg;
        ColorBlock qcb = qrBtnComp.colors;
        qcb.normalColor      = new Color(0.0f, 0.72f, 0.75f, 0.92f);
        qcb.highlightedColor = new Color(0.0f, 0.82f, 0.85f, 1f);
        qcb.pressedColor     = new Color(0.0f, 0.55f, 0.58f, 1f);
        qrBtnComp.colors = qcb;
        qrBtnComp.onClick.AddListener(() => { if (qrScanner != null) qrScanner.OpenScanner(); });
        GameObject qrIcon = new GameObject("QRIcon");
        qrIcon.transform.SetParent(qrBtn.transform, false);
        RectTransform qrIconRT = qrIcon.AddComponent<RectTransform>();
        qrIconRT.anchorMin = Vector2.zero; qrIconRT.anchorMax = Vector2.one;
        qrIconRT.offsetMin = Vector2.zero; qrIconRT.offsetMax = Vector2.zero;
        TextMeshProUGUI qrIconTMP = qrIcon.AddComponent<TextMeshProUGUI>();
        qrIconTMP.text = "⊞"; qrIconTMP.fontSize = 50;
        qrIconTMP.fontStyle = FontStyles.Bold;
        qrIconTMP.color = Color.white; qrIconTMP.alignment = TextAlignmentOptions.Center;

        BuildMenuButton(root);
        BuildMenuPanel(root);
        BuildBottomBar(root);
        BuildChatPanel(root);
        BuildQRPanel(root);

        m_MenuPanel.SetActive(false);
        m_ChatPanel.SetActive(false);
    }

    // ── Floating menu button (top-left) ───────────────────────────────────────
    private void BuildMenuButton(Transform root)
    {
        m_MenuButton = new GameObject("MenuBtn");
        m_MenuButton.transform.SetParent(root, false);

        RectTransform rt = m_MenuButton.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(24, -52);
        rt.sizeDelta        = new Vector2(120, 120);

        // Frosted dark background
        Image img = m_MenuButton.AddComponent<Image>();
        img.color = new Color(0.06f, 0.06f, 0.10f, 0.92f);

        Button btn = m_MenuButton.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.06f, 0.06f, 0.10f, 0.92f);
        cb.highlightedColor = new Color(0.12f, 0.12f, 0.18f, 0.97f);
        cb.pressedColor     = new Color(0.03f, 0.03f, 0.06f, 1f);
        btn.colors = cb;

        // Hamburger lines — staggered widths for modern look
        float[] lineWidths = { 50f, 38f, 50f };
        for (int i = 0; i < 3; i++)
        {
            GameObject line = new GameObject($"Line{i}");
            line.transform.SetParent(m_MenuButton.transform, false);
            RectTransform lrt = line.AddComponent<RectTransform>();
            lrt.anchorMin        = new Vector2(0.5f, 0.5f);
            lrt.anchorMax        = new Vector2(0.5f, 0.5f);
            lrt.pivot            = new Vector2(0.5f, 0.5f);
            lrt.sizeDelta        = new Vector2(lineWidths[i], 5);
            lrt.anchoredPosition = new Vector2(0, (i - 1) * 16f);
            line.AddComponent<Image>().color = new Color(0.75f, 0.92f, 0.98f);
        }

        btn.onClick.AddListener(ToggleMenu);
    }

    // ── Navigation panel ──────────────────────────────────────────────────────
    private void BuildMenuPanel(Transform root)
    {
        m_MenuPanel = new GameObject("MenuPanel");
        m_MenuPanel.transform.SetParent(root, false);

        RectTransform rt = m_MenuPanel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 1);
        rt.anchorMax        = new Vector2(0, 1);
        rt.pivot            = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(24, -186);
        rt.sizeDelta        = new Vector2(560, 630);

        m_MenuPanel.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f, 0.97f);

        Transform p = m_MenuPanel.transform;

        // ── Header ────────────────────────────────────────────────────────────
        Label(p, "Title", "Navigate To",
            AnchorPreset.TopStretch, new Vector2(28, -28), new Vector2(-28, -90),
            46, new Color(0.85f, 0.95f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);

        // Accent divider line
        Divider(p, new Vector2(28, -96), new Vector2(-28, -100));

        // ── Floor ─────────────────────────────────────────────────────────────
        Label(p, "FloorLbl", "FLOOR",
            AnchorPreset.TopStretch, new Vector2(28, -112), new Vector2(-28, -148),
            24, new Color(0.0f, 0.72f, 0.75f), TextAlignmentOptions.Left, FontStyles.Bold);

        m_FloorDropdown = Dropdown(p, "FloorDD",
            new Vector2(28, -154), new Vector2(-28, -234));
        PopulateFloorDropdown();
        m_FloorDropdown.onValueChanged.AddListener(OnFloorChanged);

        // ── Entrance ──────────────────────────────────────────────────────────
        Label(p, "EntLbl", "NEAREST ENTRANCE",
            AnchorPreset.TopStretch, new Vector2(28, -248), new Vector2(-28, -284),
            24, new Color(0.0f, 0.72f, 0.75f), TextAlignmentOptions.Left, FontStyles.Bold);

        m_EntranceDropdown = Dropdown(p, "EntDD",
            new Vector2(28, -290), new Vector2(-28, -370));
        PopulateEntranceDropdown();

        // ── Room ──────────────────────────────────────────────────────────────
        Label(p, "RoomLbl", "DESTINATION",
            AnchorPreset.TopStretch, new Vector2(28, -384), new Vector2(-28, -420),
            24, new Color(0.0f, 0.72f, 0.75f), TextAlignmentOptions.Left, FontStyles.Bold);

        m_RoomDropdown = Dropdown(p, "RoomDD",
            new Vector2(28, -426), new Vector2(-28, -506));
        PopulateRoomDropdown(m_FloorKeys.Count > 0 ? m_FloorKeys[0] : 0);

        // ── Navigate button ───────────────────────────────────────────────────
        GameObject navBtn = NavButton(p, "NavBtn", "  ➤  Navigate",
            new Vector2(28, -526), new Vector2(-28, -606));
        navBtn.GetComponent<Button>().onClick.AddListener(OnNavigatePressed);
    }

    // ── Bottom status + AI ask bar ─────────────────────────────────────────────
    private void BuildBottomBar(Transform root)
    {
        GameObject bar = new GameObject("BottomBar");
        bar.transform.SetParent(root, false);

        RectTransform rt = bar.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0);
        rt.anchorMax        = new Vector2(1, 0);
        rt.pivot            = new Vector2(0.5f, 0f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta        = new Vector2(0, 190);

        bar.AddComponent<Image>().color = new Color(0.03f, 0.03f, 0.06f, 0.96f);

        Transform b = bar.transform;

        // Top glow accent line
        GameObject glowLine = new GameObject("GlowLine");
        glowLine.transform.SetParent(b, false);
        RectTransform glRT = glowLine.AddComponent<RectTransform>();
        glRT.anchorMin = new Vector2(0, 1); glRT.anchorMax = new Vector2(1, 1);
        glRT.offsetMin = new Vector2(0, -3); glRT.offsetMax = new Vector2(0, 0);
        glowLine.AddComponent<Image>().color = new Color(0.0f, 0.72f, 0.75f, 0.6f);

        // Status text
        statusText = Label(b, "Status", "Scan QR code to begin",
            AnchorPreset.TopStretch, new Vector2(24, -14), new Vector2(-24, -64),
            32, new Color(0.0f, 0.88f, 0.90f), TextAlignmentOptions.Center, FontStyles.Bold);

        // Direction text
        directionText = Label(b, "Directions", "",
            AnchorPreset.TopStretch, new Vector2(24, -68), new Vector2(-24, -108),
            26, new Color(0.7f, 0.78f, 0.85f), TextAlignmentOptions.Center);
        directionText.enableWordWrapping = false;
        directionText.overflowMode = TextOverflowModes.Ellipsis;

        // Chat toggle button — wide pill with gradient feel
        GameObject askBtn = new GameObject("AskBtn");
        askBtn.transform.SetParent(b, false);
        RectTransform askRT = askBtn.AddComponent<RectTransform>();
        askRT.anchorMin        = new Vector2(0, 1);
        askRT.anchorMax        = new Vector2(1, 1);
        askRT.pivot            = new Vector2(0.5f, 1);
        askRT.anchoredPosition = new Vector2(0, -116);
        askRT.sizeDelta        = new Vector2(-48, 64);

        Image askImg = askBtn.AddComponent<Image>();
        askImg.color = new Color(0.10f, 0.10f, 0.16f, 0.95f);

        Button askBtnComp = askBtn.AddComponent<Button>();
        askBtnComp.targetGraphic = askImg;
        ColorBlock acb = askBtnComp.colors;
        acb.normalColor      = new Color(0.10f, 0.10f, 0.16f, 0.95f);
        acb.highlightedColor = new Color(0.14f, 0.14f, 0.22f, 1f);
        acb.pressedColor     = new Color(0.06f, 0.06f, 0.10f, 1f);
        askBtnComp.colors = acb;
        askBtnComp.onClick.AddListener(OnAskPressed);

        // Left accent bar on the chat button
        GameObject accentBar = new GameObject("Accent");
        accentBar.transform.SetParent(askBtn.transform, false);
        RectTransform abRT = accentBar.AddComponent<RectTransform>();
        abRT.anchorMin = new Vector2(0, 0); abRT.anchorMax = new Vector2(0, 1);
        abRT.pivot = new Vector2(0, 0.5f);
        abRT.anchoredPosition = Vector2.zero; abRT.sizeDelta = new Vector2(4, 0);
        accentBar.AddComponent<Image>().color = new Color(0.0f, 0.72f, 0.75f);

        GameObject askTxt = new GameObject("Text");
        askTxt.transform.SetParent(askBtn.transform, false);
        RectTransform askTxtRT = askTxt.AddComponent<RectTransform>();
        askTxtRT.anchorMin = Vector2.zero; askTxtRT.anchorMax = Vector2.one;
        askTxtRT.offsetMin = Vector2.zero; askTxtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI askTMP = askTxt.AddComponent<TextMeshProUGUI>();
        askTMP.text = "Ask AI anything..."; askTMP.fontSize = 30;
        askTMP.fontStyle = FontStyles.Italic;
        askTMP.color = new Color(0.55f, 0.60f, 0.70f);
        askTMP.alignment = TextAlignmentOptions.Center;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DROPDOWN LOGIC
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateFloorDropdown()
    {
        m_FloorDropdown.ClearOptions();
        var opts = new List<string>();
        foreach (int f in m_FloorKeys)
            opts.Add(f == 0 ? "Ground Floor" : $"Floor {f}");
        if (opts.Count == 0) opts.Add("No floors");
        m_FloorDropdown.AddOptions(opts);
    }

    private void PopulateEntranceDropdown()
    {
        m_EntranceDropdown.ClearOptions();
        var opts = new List<string>();
        foreach (var loc in m_EntranceList) opts.Add(loc.displayName);
        if (opts.Count == 0) opts.Add("None");
        m_EntranceDropdown.AddOptions(opts);
    }

    private void PopulateRoomDropdown(int floor)
    {
        m_RoomDropdown.ClearOptions();
        var opts = new List<string>();
        if (m_FloorMap.ContainsKey(floor))
            foreach (var loc in m_FloorMap[floor]) opts.Add(loc.displayName);
        if (opts.Count == 0) opts.Add("No rooms");
        m_RoomDropdown.AddOptions(opts);
    }

    private void OnFloorChanged(int index)
    {
        if (index >= 0 && index < m_FloorKeys.Count)
            PopulateRoomDropdown(m_FloorKeys[index]);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ACTIONS
    // ─────────────────────────────────────────────────────────────────────────

    private void ToggleMenu()
    {
        m_MenuOpen = !m_MenuOpen;
        m_MenuPanel.SetActive(m_MenuOpen);
    }

    private void OnNavigatePressed()
    {
        int fi = m_FloorDropdown.value;
        int ri = m_RoomDropdown.value;

        if (fi < 0 || fi >= m_FloorKeys.Count) return;
        int floor = m_FloorKeys[fi];
        if (!m_FloorMap.ContainsKey(floor)) return;

        List<LocationData> rooms = m_FloorMap[floor];
        if (ri < 0 || ri >= rooms.Count) return;

        LocationData target = rooms[ri];

        statusText.text    = $"Heading to  {target.displayName}";
        directionText.text = "Calculating route...";

        m_MenuPanel.SetActive(false);
        m_MenuOpen = false;

        if (indoorBridge != null)
        {
            string mapName = floor == 0 ? "Floor_0" : $"Floor_{floor}";
            indoorBridge.NavigateTo(mapName, target.displayName);
        }
        else if (aiManager != null)
            aiManager.Ask($"take me to {target.displayName}");
        else
            NavigateDirectly(target.id);
    }

    private void NavigateDirectly(string locationId)
    {
        var nav  = AppController.Instance?.Navigation;
        var pf   = AppController.Instance?.Pathfinding;
        var vis  = AppController.Instance?.Visualizer;
        var dirs = AppController.Instance?.Directions;

        if (nav == null || pf == null || vis == null) return;

        List<NavigationNode> nodes = nav.GetAllNodes();
        if (nodes.Count < 1) return;

        // Use QR scanned location as start — required, no fallback
        NavigationNode start = null;
        if (QRLocationManager.Instance != null && QRLocationManager.Instance.HasLocation)
            start = nav.FindNodeById(QRLocationManager.Instance.CurrentNodeId);

        if (start == null)
        {
            statusText.text = "Scan a QR code first!";
            return;
        }

        NavigationNode target = nav.FindNodeById(locationId);

        if (target == null) { statusText.text = "Location not found."; return; }
        if (start == target) { statusText.text = "You are already there!"; return; }

        List<NavigationNode> path = pf.FindPath(start, target);

        if (path != null && path.Count > 0)
        {
            vis.DrawPath(path);
            if (dirs != null)
            {
                List<string> steps = dirs.GenerateDirections(path);
                directionText.text = steps.Count > 1 ? steps[1] : "Follow the arrows";
                statusText.text    = $"Heading to {target.DisplayName}";
            }
        }
        else
        {
            statusText.text    = "No path found.";
            directionText.text = "";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FACTORY HELPERS
    // ─────────────────────────────────────────────────────────────────────────

    private enum AnchorPreset { TopStretch }

    private TextMeshProUGUI Label(Transform parent, string name, string text,
        AnchorPreset anchor, Vector2 offsetMin, Vector2 offsetMax,
        float size, Color color, TextAlignmentOptions align,
        FontStyles style = FontStyles.Normal)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = size;
        tmp.color     = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        return tmp;
    }

    private void Divider(Transform parent, Vector2 oMin, Vector2 oMax)
    {
        GameObject go = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = oMin;
        rt.offsetMax = oMax;
        go.AddComponent<Image>().color = new Color(0.0f, 0.50f, 0.52f, 0.45f);
    }

    private TMP_Dropdown Dropdown(Transform parent, string name,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        go.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.13f);
        TMP_Dropdown dd = go.AddComponent<TMP_Dropdown>();

        // Caption label
        GameObject lbl = new GameObject("Label");
        lbl.transform.SetParent(go.transform, false);
        RectTransform lblRT = lbl.AddComponent<RectTransform>();
        lblRT.anchorMin = Vector2.zero; lblRT.anchorMax = Vector2.one;
        lblRT.offsetMin = new Vector2(18, 6); lblRT.offsetMax = new Vector2(-50, -6);
        TextMeshProUGUI lblTMP = lbl.AddComponent<TextMeshProUGUI>();
        lblTMP.fontSize = 32; lblTMP.color = Color.white;
        lblTMP.alignment = TextAlignmentOptions.Left;
        lblTMP.enableWordWrapping = false;

        // Arrow
        GameObject arr = new GameObject("Arrow");
        arr.transform.SetParent(go.transform, false);
        RectTransform arrRT = arr.AddComponent<RectTransform>();
        arrRT.anchorMin = new Vector2(1, 0.5f); arrRT.anchorMax = new Vector2(1, 0.5f);
        arrRT.anchoredPosition = new Vector2(-26, 0); arrRT.sizeDelta = new Vector2(36, 36);
        TextMeshProUGUI arrTMP = arr.AddComponent<TextMeshProUGUI>();
        arrTMP.text = "▾"; arrTMP.fontSize = 32;
        arrTMP.color = new Color(0.55f, 0.55f, 0.55f);
        arrTMP.alignment = TextAlignmentOptions.Center;

        // Template
        GameObject tmpl = new GameObject("Template");
        tmpl.transform.SetParent(go.transform, false);
        RectTransform tmplRT = tmpl.AddComponent<RectTransform>();
        tmplRT.anchorMin = new Vector2(0, 0); tmplRT.anchorMax = new Vector2(1, 0);
        tmplRT.pivot = new Vector2(0.5f, 1f);
        tmplRT.anchoredPosition = Vector2.zero; tmplRT.sizeDelta = new Vector2(0, 280);
        tmpl.AddComponent<Image>().color = new Color(0.06f, 0.06f, 0.10f);
        ScrollRect sr = tmpl.AddComponent<ScrollRect>();
        tmpl.SetActive(false);

        GameObject vp = new GameObject("Viewport");
        vp.transform.SetParent(tmpl.transform, false);
        RectTransform vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRT;

        GameObject ct = new GameObject("Content");
        ct.transform.SetParent(vp.transform, false);
        RectTransform ctRT = ct.AddComponent<RectTransform>();
        ctRT.anchorMin = new Vector2(0, 1); ctRT.anchorMax = new Vector2(1, 1);
        ctRT.pivot = new Vector2(0.5f, 1f); ctRT.sizeDelta = Vector2.zero;
        sr.content = ctRT;

        GameObject item = new GameObject("Item");
        item.transform.SetParent(ct.transform, false);
        RectTransform itemRT = item.AddComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f); itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 72);
        Toggle tog = item.AddComponent<Toggle>();

        GameObject iBG = new GameObject("Item Background");
        iBG.transform.SetParent(item.transform, false);
        RectTransform iBGRT = iBG.AddComponent<RectTransform>();
        iBGRT.anchorMin = Vector2.zero; iBGRT.anchorMax = Vector2.one;
        iBGRT.offsetMin = Vector2.zero; iBGRT.offsetMax = Vector2.zero;
        Image iBGImg = iBG.AddComponent<Image>();
        iBGImg.color = new Color(0.10f, 0.10f, 0.15f);

        GameObject iLbl = new GameObject("Item Label");
        iLbl.transform.SetParent(item.transform, false);
        RectTransform iLblRT = iLbl.AddComponent<RectTransform>();
        iLblRT.anchorMin = Vector2.zero; iLblRT.anchorMax = Vector2.one;
        iLblRT.offsetMin = new Vector2(18, 0); iLblRT.offsetMax = new Vector2(-18, 0);
        TextMeshProUGUI iLblTMP = iLbl.AddComponent<TextMeshProUGUI>();
        iLblTMP.fontSize = 30; iLblTMP.color = Color.white;
        iLblTMP.alignment = TextAlignmentOptions.Left;

        tog.targetGraphic = iBGImg; tog.graphic = iBGImg;
        dd.template = tmplRT; dd.captionText = lblTMP; dd.itemText = iLblTMP;

        return dd;
    }

    private GameObject NavButton(Transform parent, string name, string label,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
        rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;

        Image img = go.AddComponent<Image>();
        img.color = new Color(0.0f, 0.72f, 0.75f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.0f, 0.72f, 0.75f);
        cb.highlightedColor = new Color(0.0f, 0.82f, 0.85f);
        cb.pressedColor     = new Color(0.0f, 0.55f, 0.58f);
        btn.colors = cb;

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(go.transform, false);
        RectTransform trt = txt.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
        tmp.text = label; tmp.fontSize = 40;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private void OnAskPressed()
    {
        m_ChatOpen = !m_ChatOpen;
        m_ChatPanel.SetActive(m_ChatOpen);
        // Close nav menu if open
        if (m_ChatOpen && m_MenuOpen)
        {
            m_MenuOpen = false;
            m_MenuPanel.SetActive(false);
        }
    }

    private void BuildChatPanel(Transform root)
    {
        m_ChatPanel = new GameObject("ChatPanel");
        m_ChatPanel.transform.SetParent(root, false);

        RectTransform rt = m_ChatPanel.AddComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0, 0);
        rt.anchorMax        = new Vector2(1, 1);
        rt.offsetMin        = new Vector2(0, 190);  // above bottom bar
        rt.offsetMax        = new Vector2(0, -0);
        m_ChatPanel.AddComponent<Image>().color = new Color(0.02f, 0.02f, 0.05f, 0.98f);

        Transform p = m_ChatPanel.transform;

        // Header accent bar
        GameObject headerBar = new GameObject("HeaderBar");
        headerBar.transform.SetParent(p, false);
        RectTransform hbRT = headerBar.AddComponent<RectTransform>();
        hbRT.anchorMin = new Vector2(0, 1); hbRT.anchorMax = new Vector2(1, 1);
        hbRT.offsetMin = new Vector2(0, -82); hbRT.offsetMax = Vector2.zero;
        headerBar.AddComponent<Image>().color = new Color(0.04f, 0.04f, 0.08f);

        // Header glow line
        GameObject chatGlow = new GameObject("ChatGlow");
        chatGlow.transform.SetParent(p, false);
        RectTransform cgRT = chatGlow.AddComponent<RectTransform>();
        cgRT.anchorMin = new Vector2(0, 1); cgRT.anchorMax = new Vector2(1, 1);
        cgRT.offsetMin = new Vector2(0, -84); cgRT.offsetMax = new Vector2(0, -82);
        chatGlow.AddComponent<Image>().color = new Color(0.0f, 0.72f, 0.75f, 0.5f);

        // Header title
        Label(p, "ChatTitle", "✨ AI Navigation Assistant",
            AnchorPreset.TopStretch, new Vector2(24, -20), new Vector2(-80, -76),
            36, new Color(0.85f, 0.95f, 1f), TextAlignmentOptions.Left, FontStyles.Bold);

        // Close button
        GameObject closeBtn = new GameObject("CloseBtn");
        closeBtn.transform.SetParent(p, false);
        RectTransform closeRT = closeBtn.AddComponent<RectTransform>();
        closeRT.anchorMin        = new Vector2(1, 1);
        closeRT.anchorMax        = new Vector2(1, 1);
        closeRT.pivot            = new Vector2(1, 1);
        closeRT.anchoredPosition = new Vector2(-20, -20);
        closeRT.sizeDelta        = new Vector2(70, 70);
        closeBtn.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.18f);
        Button closeB = closeBtn.AddComponent<Button>();
        closeB.onClick.AddListener(() => { m_ChatOpen = false; m_ChatPanel.SetActive(false); });
        GameObject closeTxt = new GameObject("X");
        closeTxt.transform.SetParent(closeBtn.transform, false);
        RectTransform closeTxtRT = closeTxt.AddComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero; closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.offsetMin = Vector2.zero; closeTxtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTMP = closeTxt.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "✕"; closeTMP.fontSize = 36;
        closeTMP.color = Color.white; closeTMP.alignment = TextAlignmentOptions.Center;

        // Divider
        Divider(p, new Vector2(0, -82), new Vector2(0, -86));

        // Hint text
        Label(p, "Hint", "✨ Ask me anything — \"Where is the library?\" or \"Take me to the lab\"",
            AnchorPreset.TopStretch, new Vector2(24, -94), new Vector2(-24, -134),
            26, new Color(0.40f, 0.55f, 0.60f), TextAlignmentOptions.Left);

        // Chat scroll area
        GameObject scrollGO = new GameObject("ChatScroll");
        scrollGO.transform.SetParent(p, false);
        RectTransform scrollRT = scrollGO.AddComponent<RectTransform>();
        scrollRT.anchorMin = new Vector2(0, 0);
        scrollRT.anchorMax = new Vector2(1, 1);
        scrollRT.offsetMin = new Vector2(0, 110);
        scrollRT.offsetMax = new Vector2(0, -140);
        scrollGO.AddComponent<Image>().color = Color.clear;
        ScrollRect sr = scrollGO.AddComponent<ScrollRect>();
        sr.horizontal = false;

        // Viewport
        GameObject vp = new GameObject("Viewport");
        vp.transform.SetParent(scrollGO.transform, false);
        RectTransform vpRT = vp.AddComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero; vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero; vpRT.offsetMax = Vector2.zero;
        vp.AddComponent<Image>().color = Color.clear;
        vp.AddComponent<Mask>().showMaskGraphic = false;
        sr.viewport = vpRT;

        // Content (chat bubbles go here)
        GameObject content = new GameObject("ChatContent");
        content.transform.SetParent(vp.transform, false);
        RectTransform contentRT = content.AddComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot     = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment        = TextAnchor.UpperLeft;
        vlg.childForceExpandWidth = true;
        vlg.spacing               = 8;
        vlg.padding               = new RectOffset(12, 12, 12, 12);
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        sr.content = contentRT;

        // Input row at bottom of chat panel
        GameObject inputRow = new GameObject("InputRow");
        inputRow.transform.SetParent(p, false);
        RectTransform inputRowRT = inputRow.AddComponent<RectTransform>();
        inputRowRT.anchorMin        = new Vector2(0, 0);
        inputRowRT.anchorMax        = new Vector2(1, 0);
        inputRowRT.pivot            = new Vector2(0.5f, 0f);
        inputRowRT.anchoredPosition = new Vector2(0, 16);
        inputRowRT.sizeDelta        = new Vector2(-40, 90);
        inputRow.AddComponent<Image>().color = Color.clear;

        // Input field bg
        GameObject inputBG = new GameObject("InputBG");
        inputBG.transform.SetParent(inputRow.transform, false);
        RectTransform inputBGRT = inputBG.AddComponent<RectTransform>();
        inputBGRT.anchorMin = new Vector2(0, 0);
        inputBGRT.anchorMax = new Vector2(1, 1);
        inputBGRT.offsetMin = new Vector2(0, 0);
        inputBGRT.offsetMax = new Vector2(-160, 0);
        inputBG.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.12f);
        TMP_InputField chatInput = BuildInputField(inputBG.transform);

        // Send button
        GameObject sendBtn = new GameObject("SendBtn");
        sendBtn.transform.SetParent(inputRow.transform, false);
        RectTransform sendRT = sendBtn.AddComponent<RectTransform>();
        sendRT.anchorMin        = new Vector2(1, 0);
        sendRT.anchorMax        = new Vector2(1, 1);
        sendRT.pivot            = new Vector2(1, 0.5f);
        sendRT.anchoredPosition = new Vector2(0, 0);
        sendRT.sizeDelta        = new Vector2(140, 0);
        Image sendImg = sendBtn.AddComponent<Image>();
        sendImg.color = new Color(0.0f, 0.72f, 0.75f);
        Button sendBtnComp = sendBtn.AddComponent<Button>();
        sendBtnComp.targetGraphic = sendImg;
        sendBtnComp.onClick.AddListener(() =>
        {
            if (chatManager != null)
                chatManager.SendMessage(chatInput.text);
        });
        GameObject sendTxt = new GameObject("Text");
        sendTxt.transform.SetParent(sendBtn.transform, false);
        RectTransform sendTxtRT = sendTxt.AddComponent<RectTransform>();
        sendTxtRT.anchorMin = Vector2.zero; sendTxtRT.anchorMax = Vector2.one;
        sendTxtRT.offsetMin = Vector2.zero; sendTxtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI sendTMP = sendTxt.AddComponent<TextMeshProUGUI>();
        sendTMP.text = "➤"; sendTMP.fontSize = 38;
        sendTMP.fontStyle = FontStyles.Bold;
        sendTMP.color = Color.white;
        sendTMP.alignment = TextAlignmentOptions.Center;

        // Wire ChatManager
        if (chatManager != null)
        {
            chatManager.inputField    = chatInput;
            chatManager.chatContainer = content.transform;
            chatManager.chatScrollRect = sr;
        }
    }

    private TMP_InputField BuildInputField(Transform parent)
    {
        GameObject go = new GameObject("InputField");
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(14, 8); rt.offsetMax = new Vector2(-14, -8);

        TMP_InputField field = go.AddComponent<TMP_InputField>();

        GameObject area = new GameObject("TextArea");
        area.transform.SetParent(go.transform, false);
        RectTransform areaRT = area.AddComponent<RectTransform>();
        areaRT.anchorMin = Vector2.zero; areaRT.anchorMax = Vector2.one;
        areaRT.offsetMin = new Vector2(8, 4); areaRT.offsetMax = new Vector2(-8, -4);
        area.AddComponent<RectMask2D>();

        GameObject ph = new GameObject("Placeholder");
        ph.transform.SetParent(area.transform, false);
        RectTransform phRT = ph.AddComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero; phRT.offsetMax = Vector2.zero;
        TextMeshProUGUI phTMP = ph.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Ask AI anything..."; phTMP.fontSize = 30;
        phTMP.color = new Color(0.5f, 0.5f, 0.5f);
        phTMP.alignment = TextAlignmentOptions.Left;
        phTMP.enableWordWrapping = false;

        GameObject txt = new GameObject("Text");
        txt.transform.SetParent(area.transform, false);
        RectTransform txtRT = txt.AddComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero; txtRT.anchorMax = Vector2.one;
        txtRT.offsetMin = Vector2.zero; txtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txtTMP = txt.AddComponent<TextMeshProUGUI>();
        txtTMP.fontSize = 30; txtTMP.color = Color.white;
        txtTMP.alignment = TextAlignmentOptions.Left;
        txtTMP.enableWordWrapping = false;

        field.textViewport  = areaRT;
        field.textComponent = txtTMP;
        field.placeholder   = phTMP;

        return field;
    }

    private void BuildQRPanel(Transform root)
    {
        // Full-screen overlay — blocks all AR content behind it
        GameObject panel = new GameObject("QRScanPanel");
        panel.transform.SetParent(root, false);
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        panel.AddComponent<Image>().color = new Color(0.01f, 0.01f, 0.04f, 0.97f);

        Transform p = panel.transform;

        // ── Header ────────────────────────────────────────────────────────────
        Label(p, "ScanTitle", "⊞  Scan QR Code",
            AnchorPreset.TopStretch, new Vector2(24, -56), new Vector2(-100, -120),
            46, new Color(0.85f, 0.95f, 1f), TextAlignmentOptions.Center, FontStyles.Bold);

        // ── Close (X) button — top-right ─────────────────────────────────────
        GameObject closeBtn = new GameObject("CloseQR");
        closeBtn.transform.SetParent(p, false);
        RectTransform closeBtnRT = closeBtn.AddComponent<RectTransform>();
        closeBtnRT.anchorMin        = new Vector2(1, 1);
        closeBtnRT.anchorMax        = new Vector2(1, 1);
        closeBtnRT.pivot            = new Vector2(1, 1);
        closeBtnRT.anchoredPosition = new Vector2(-24, -48);
        closeBtnRT.sizeDelta        = new Vector2(80, 80);
        Image closeBtnImg = closeBtn.AddComponent<Image>();
        closeBtnImg.color = new Color(0.10f, 0.10f, 0.16f, 0.9f);
        Button closeBtnComp = closeBtn.AddComponent<Button>();
        closeBtnComp.targetGraphic = closeBtnImg;
        ColorBlock ccb = closeBtnComp.colors;
        ccb.highlightedColor = new Color(0.18f, 0.18f, 0.25f);
        ccb.pressedColor     = new Color(0.05f, 0.05f, 0.08f);
        closeBtnComp.colors = ccb;
        closeBtnComp.onClick.AddListener(() => { if (qrScanner != null) qrScanner.CloseScanner(); });
        // X text
        GameObject closeTxt = new GameObject("X");
        closeTxt.transform.SetParent(closeBtn.transform, false);
        RectTransform closeTxtRT = closeTxt.AddComponent<RectTransform>();
        closeTxtRT.anchorMin = Vector2.zero; closeTxtRT.anchorMax = Vector2.one;
        closeTxtRT.offsetMin = Vector2.zero; closeTxtRT.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTMP = closeTxt.AddComponent<TextMeshProUGUI>();
        closeTMP.text = "\u2715"; closeTMP.fontSize = 40;
        closeTMP.color = Color.white; closeTMP.alignment = TextAlignmentOptions.Center;

        // ── Status text ───────────────────────────────────────────────────────
        GameObject statusGO = new GameObject("ScanStatus");
        statusGO.transform.SetParent(p, false);
        RectTransform statusRT = statusGO.AddComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0, 1); statusRT.anchorMax = new Vector2(1, 1);
        statusRT.offsetMin = new Vector2(24, -128); statusRT.offsetMax = new Vector2(-24, -180);
        TextMeshProUGUI statusTMP = statusGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "Point camera at a campus QR code";
        statusTMP.fontSize = 28; statusTMP.color = new Color(0.0f, 0.88f, 0.90f);
        statusTMP.alignment = TextAlignmentOptions.Center;

        // ── Scan frame (centered square) ──────────────────────────────────────
        GameObject scanFrame = new GameObject("ScanFrame");
        scanFrame.transform.SetParent(p, false);
        RectTransform sfRT = scanFrame.AddComponent<RectTransform>();
        sfRT.anchorMin        = new Vector2(0.5f, 0.5f);
        sfRT.anchorMax        = new Vector2(0.5f, 0.5f);
        sfRT.pivot            = new Vector2(0.5f, 0.5f);
        sfRT.anchoredPosition = new Vector2(0, 40);
        sfRT.sizeDelta        = new Vector2(560, 560);
        // Transparent fill with subtle border
        Image sfImg = scanFrame.AddComponent<Image>();
        sfImg.color = new Color(1f, 1f, 1f, 0.04f);

        // Corner brackets — 4 corners, each made of 2 lines (L-shape)
        float cSize = 60f;  // corner arm length
        float cThick = 6f;  // corner arm thickness
        Color cornerColor = new Color(0.0f, 0.85f, 0.88f);
        // Corners: TL, TR, BL, BR
        Vector2[] cornerAnchors = { new Vector2(0,1), new Vector2(1,1), new Vector2(0,0), new Vector2(1,0) };
        Vector2[] cornerPivots  = { new Vector2(0,1), new Vector2(1,1), new Vector2(0,0), new Vector2(1,0) };
        // Each corner: horizontal bar + vertical bar
        for (int ci = 0; ci < 4; ci++)
        {
            float hDir = (ci % 2 == 0) ? 1f : -1f;  // left corners go right, right go left
            float vDir = (ci < 2)      ? -1f : 1f;   // top corners go down, bottom go up

            // Horizontal arm
            GameObject hBar = new GameObject($"Corner{ci}_H");
            hBar.transform.SetParent(scanFrame.transform, false);
            RectTransform hRT = hBar.AddComponent<RectTransform>();
            hRT.anchorMin = cornerAnchors[ci]; hRT.anchorMax = cornerAnchors[ci];
            hRT.pivot     = cornerPivots[ci];
            hRT.sizeDelta = new Vector2(cSize, cThick);
            hRT.anchoredPosition = Vector2.zero;
            hBar.AddComponent<Image>().color = cornerColor;
            hBar.AddComponent<ScanCornerPulse>();

            // Vertical arm
            GameObject vBar = new GameObject($"Corner{ci}_V");
            vBar.transform.SetParent(scanFrame.transform, false);
            RectTransform vRT = vBar.AddComponent<RectTransform>();
            vRT.anchorMin = cornerAnchors[ci]; vRT.anchorMax = cornerAnchors[ci];
            vRT.pivot     = cornerPivots[ci];
            vRT.sizeDelta = new Vector2(cThick, cSize);
            vRT.anchoredPosition = Vector2.zero;
            vBar.AddComponent<Image>().color = cornerColor;
            vBar.AddComponent<ScanCornerPulse>();
        }

        // Animated scan line
        GameObject scanLine = new GameObject("ScanLine");
        scanLine.transform.SetParent(scanFrame.transform, false);
        RectTransform slRT = scanLine.AddComponent<RectTransform>();
        // Anchor to center, stretch horizontally with small inset
        slRT.anchorMin        = new Vector2(0.05f, 0.5f);
        slRT.anchorMax        = new Vector2(0.95f, 0.5f);
        slRT.sizeDelta        = new Vector2(0, 4);
        slRT.anchoredPosition = Vector2.zero;
        scanLine.AddComponent<Image>().color = new Color(0.0f, 0.90f, 0.92f, 0.9f);
        // Glow trail — slightly wider, more transparent line behind the main one
        GameObject scanGlow = new GameObject("ScanGlow");
        scanGlow.transform.SetParent(scanFrame.transform, false);
        RectTransform sgRT = scanGlow.AddComponent<RectTransform>();
        sgRT.anchorMin        = new Vector2(0.03f, 0.5f);
        sgRT.anchorMax        = new Vector2(0.97f, 0.5f);
        sgRT.sizeDelta        = new Vector2(0, 16);
        sgRT.anchoredPosition = Vector2.zero;
        scanGlow.AddComponent<Image>().color = new Color(0.0f, 0.90f, 0.92f, 0.12f);
        // Attach animator to the main line (glow follows via same parent)
        ScanLineAnimator anim = scanLine.AddComponent<ScanLineAnimator>();
        // Mirror glow position each frame
        scanGlow.AddComponent<ScanGlowFollower>().target = slRT;

        // ── Location confirmed text ───────────────────────────────────────────
        GameObject locGO = new GameObject("LocationText");
        locGO.transform.SetParent(p, false);
        RectTransform locRT = locGO.AddComponent<RectTransform>();
        locRT.anchorMin = new Vector2(0, 0.5f); locRT.anchorMax = new Vector2(1, 0.5f);
        locRT.anchoredPosition = new Vector2(0, -340); locRT.sizeDelta = new Vector2(-48, 120);
        TextMeshProUGUI locTMP = locGO.AddComponent<TextMeshProUGUI>();
        locTMP.text = ""; locTMP.fontSize = 36;
        locTMP.color = new Color(0.0f, 0.92f, 0.60f);
        locTMP.alignment = TextAlignmentOptions.Center;
        locTMP.enableWordWrapping = true;

#if UNITY_EDITOR
        // Editor simulation buttons
        Label(p, "SimLabel", "EDITOR — Tap to simulate scan:",
            AnchorPreset.TopStretch, new Vector2(24, -780), new Vector2(-24, -820),
            24, new Color(1f, 0.8f, 0.2f), TextAlignmentOptions.Center);

        GameObject simScroll = new GameObject("SimScroll");
        simScroll.transform.SetParent(p, false);
        RectTransform simScrollRT = simScroll.AddComponent<RectTransform>();
        simScrollRT.anchorMin = new Vector2(0, 0);
        simScrollRT.anchorMax = new Vector2(1, 1);
        simScrollRT.offsetMin = new Vector2(24, 100);
        simScrollRT.offsetMax = new Vector2(-24, -830);
        simScroll.AddComponent<Image>().color = Color.clear;
        ScrollRect simSR = simScroll.AddComponent<ScrollRect>();
        simSR.horizontal = false;

        GameObject simVP = new GameObject("Viewport");
        simVP.transform.SetParent(simScroll.transform, false);
        RectTransform simVPRT = simVP.AddComponent<RectTransform>();
        simVPRT.anchorMin = Vector2.zero; simVPRT.anchorMax = Vector2.one;
        simVPRT.offsetMin = Vector2.zero; simVPRT.offsetMax = Vector2.zero;
        simVP.AddComponent<Image>().color = Color.clear;
        simVP.AddComponent<Mask>().showMaskGraphic = false;
        simSR.viewport = simVPRT;

        GameObject simContent = new GameObject("Content");
        simContent.transform.SetParent(simVP.transform, false);
        RectTransform simContentRT = simContent.AddComponent<RectTransform>();
        simContentRT.anchorMin = new Vector2(0, 1); simContentRT.anchorMax = new Vector2(1, 1);
        simContentRT.pivot = new Vector2(0.5f, 1f); simContentRT.sizeDelta = Vector2.zero;
        VerticalLayoutGroup simVLG = simContent.AddComponent<VerticalLayoutGroup>();
        simVLG.spacing = 10; simVLG.padding = new RectOffset(0, 0, 8, 8);
        simVLG.childForceExpandWidth = true;
        ContentSizeFitter simCSF = simContent.AddComponent<ContentSizeFitter>();
        simCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        simSR.content = simContentRT;

        if (AppController.Instance?.Locations != null)
        {
            foreach (LocationData loc in AppController.Instance.Locations.GetAllLocations())
            {
                string payload = $"{{\"building\":\"{loc.building}\",\"floor\":{loc.floor},\"node_id\":\"{loc.id}\"}}";
                string btnLabel = $"{loc.displayName}  ({(loc.floor == 0 ? "Ground" : $"Floor {loc.floor}")})";

                GameObject simBtn = new GameObject($"Sim_{loc.id}");
                simBtn.transform.SetParent(simContent.transform, false);
                LayoutElement le = simBtn.AddComponent<LayoutElement>();
                le.preferredHeight = 80;
                Image simBtnImg = simBtn.AddComponent<Image>();
                simBtnImg.color = new Color(0.08f, 0.08f, 0.13f);
                Button simBtnComp = simBtn.AddComponent<Button>();
                simBtnComp.targetGraphic = simBtnImg;
                ColorBlock scb = simBtnComp.colors;
                scb.normalColor      = new Color(0.08f, 0.08f, 0.13f);
                scb.highlightedColor = new Color(0.0f, 0.55f, 0.58f, 0.6f);
                scb.pressedColor     = new Color(0.04f, 0.04f, 0.08f);
                simBtnComp.colors = scb;

                string capturedPayload = payload;
                simBtnComp.onClick.AddListener(() =>
                {
                    if (qrScanner != null) qrScanner.SimulateScan(capturedPayload);
                });

                GameObject simBtnTxt = new GameObject("Text");
                simBtnTxt.transform.SetParent(simBtn.transform, false);
                RectTransform simBtnTxtRT = simBtnTxt.AddComponent<RectTransform>();
                simBtnTxtRT.anchorMin = Vector2.zero; simBtnTxtRT.anchorMax = Vector2.one;
                simBtnTxtRT.offsetMin = new Vector2(20, 0); simBtnTxtRT.offsetMax = new Vector2(-20, 0);
                TextMeshProUGUI simBtnTMP = simBtnTxt.AddComponent<TextMeshProUGUI>();
                simBtnTMP.text = btnLabel; simBtnTMP.fontSize = 30;
                simBtnTMP.color = Color.white; simBtnTMP.alignment = TextAlignmentOptions.Left;
            }
        }
#endif

        // Wire QRScanner UI references
        if (qrScanner != null)
            qrScanner.WireUI(panel, statusTMP, locTMP, null, null);

        panel.SetActive(false);
    }

    private void WireUIManager()
    {
        UIManager ui = FindObjectOfType<UIManager>();
        if (ui == null) return;
        var f = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        typeof(UIManager).GetField("m_DirectionText", f)?.SetValue(ui, directionText);
        typeof(UIManager).GetField("m_StatusText",    f)?.SetValue(ui, statusText);
    }
}
