using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using ZXing;
using ZXing.QrCode;

/// <summary>
/// Unity Editor Window for drawing floor maps.
///
/// Open via: Window → AR Navigation → Floor Map Editor
///
/// HOW TO USE:
/// 1. Enter a map name and click "New Map"
/// 2. Select a paint mode (Wall, Walkable, Room, Stair, Lift, Entrance)
/// 3. Click/drag on the grid to paint cells
/// 4. Click a cell + enter a name to assign node_id to key locations
/// 5. Click "Save Map" to persist to JSON
/// 6. Click "Export to nodes.json" to update the navigation graph
/// </summary>
public class FloorMapEditor : EditorWindow
{
    // ── State ─────────────────────────────────────────────────────────────────
    private MapManager  m_MapManager;
    private GridManager m_GridManager;

    private string m_NewMapName    = "Floor_0";
    private string m_BuildingName  = "Main Block";
    private int    m_FloorNumber   = 0;
    private float  m_FloorHeight   = 4f;
    private string m_NodeNameInput = "";

    private PaintMode m_PaintMode  = PaintMode.Wall;
    private bool      m_IsPainting = false;

    private Vector2 m_ScrollPos;
    private float   m_CellPx    = 14f;   // pixels per cell in editor grid

    private Node    m_SelectedNode = null;
    private Texture2D m_QRPreview   = null;
    private Vector2 m_ControlScrollPos;

    private enum PaintMode
    {
        Wall, Walkable, Room, Stair, Lift, Entrance, Select
    }

    // ── Colors ────────────────────────────────────────────────────────────────
    private static readonly Color COL_WALL     = new Color(0.15f, 0.15f, 0.15f);
    private static readonly Color COL_WALK     = new Color(0.75f, 0.75f, 0.75f);
    private static readonly Color COL_ROOM     = new Color(0.2f,  0.7f,  1f);
    private static readonly Color COL_STAIR    = new Color(1f,    0.35f, 0.35f);
    private static readonly Color COL_LIFT     = new Color(0.6f,  0.2f,  0.9f);
    private static readonly Color COL_ENTRANCE = new Color(0.2f,  0.9f,  0.4f);
    private static readonly Color COL_SELECT   = new Color(1f,    0.85f, 0.1f);
    private static readonly Color COL_GRID     = new Color(0.3f,  0.3f,  0.3f);

    // ── Open Window ───────────────────────────────────────────────────────────

    [MenuItem("Window/AR Navigation/Floor Map Editor")]
    public static void Open()
    {
        FloorMapEditor win = GetWindow<FloorMapEditor>("🗺️ Floor Map Editor");
        win.minSize = new Vector2(1200, 800);
        win.Show();
    }

    void OnEnable()
    {
        FindManagers();
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
    }

    void OnDisable()
    {
        EditorApplication.hierarchyChanged -= OnHierarchyChanged;
    }

    void OnHierarchyChanged()
    {
        if (m_MapManager == null || m_GridManager == null)
            FindManagers();
    }

    void FindManagers()
    {
        m_MapManager  = FindObjectOfType<MapManager>();
        m_GridManager = FindObjectOfType<GridManager>();
        if (m_MapManager != null && m_GridManager != null)
        {
            // Force reload maps from disk since Awake() doesn't run in Edit mode
            m_MapManager.LoadBuildingsFromFile();
            m_MapManager.LoadAllMapsFromDisk();
            
            // Force refresh the maps dictionary by calling GetAllMaps
            // This ensures the editor sees all saved maps
            var maps = m_MapManager.GetAllMaps();
            Debug.Log($"[FloorMapEditor] Found {maps.Count} saved maps");
            
            Repaint();
        }
    }

    // ── GUI ───────────────────────────────────────────────────────────────────

    void OnGUI()
    {
        if (m_MapManager == null || m_GridManager == null)
        {
            DrawNoManagerWarning();
            return;
        }

        // Top status bar
        DrawStatusBar();

        EditorGUILayout.BeginHorizontal();

        // Left panel — controls (wider now)
        EditorGUILayout.BeginVertical(GUILayout.Width(320));
        DrawControlPanel();
        EditorGUILayout.EndVertical();

        // Right panel — grid
        DrawGrid();

        EditorGUILayout.EndHorizontal();
    }

    // ── Status Bar ────────────────────────────────────────────────────────────

    void DrawStatusBar()
    {
        Rect statusRect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        
        // Current map info
        string mapInfo = string.IsNullOrEmpty(m_MapManager.currentMapName) 
            ? "No map loaded" 
            : $"📍 {m_MapManager.currentMapName}";
        
        GUILayout.Label(mapInfo, EditorStyles.miniLabel);
        
        GUILayout.FlexibleSpace();
        
        // Node count
        int namedNodeCount = 0;
        if (m_GridManager?.grid != null)
        {
            for (int x = 0; x < m_GridManager.width; x++)
                for (int y = 0; y < m_GridManager.height; y++)
                    if (m_GridManager.grid[x, y] != null && !string.IsNullOrEmpty(m_GridManager.grid[x, y].nodeName))
                        namedNodeCount++;
        }
        
        GUILayout.Label($"🏷️ Named Nodes: {namedNodeCount}", EditorStyles.miniLabel);
        GUILayout.Label($"📐 Grid: {m_GridManager.width}x{m_GridManager.height}", EditorStyles.miniLabel);
        
        EditorGUILayout.EndHorizontal();
    }

    // ── No Manager Warning ────────────────────────────────────────────────────

    void DrawNoManagerWarning()
    {
        EditorGUILayout.Space(20);
        EditorGUILayout.HelpBox(
            "MapManager and GridManager not found in scene.\n\n" +
            "Make sure your scene has:\n" +
            "• A GameObject with MapManager.cs\n" +
            "• A GameObject with GridManager.cs\n\n" +
            "Then press Play or click Refresh.",
            MessageType.Warning);

        if (GUILayout.Button("Refresh", GUILayout.Height(40)))
            FindManagers();
    }

    // ── Control Panel ─────────────────────────────────────────────────────────

    void DrawControlPanel()
    {
        GUIStyle sectionStyle = new GUIStyle(EditorStyles.helpBox);
        sectionStyle.padding = new RectOffset(10, 10, 10, 10);
        
        Vector2 controlScrollPos = EditorGUILayout.BeginScrollView(m_ControlScrollPos);
        m_ControlScrollPos = controlScrollPos;
        
        // ── Map Management ────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label("🗺️ MAP MANAGEMENT", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        m_NewMapName   = EditorGUILayout.TextField("Map Name", m_NewMapName);
        m_BuildingName = EditorGUILayout.TextField("Building",  m_BuildingName);
        m_FloorNumber  = EditorGUILayout.IntField("Floor",      m_FloorNumber);
        m_FloorHeight  = EditorGUILayout.FloatField("Floor Height (m)", m_FloorHeight);

        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("✚ New Map", GUILayout.Height(32)))
            CreateNewMap();
        
        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button("💾 Save Map", GUILayout.Height(32)))
            SaveMap();
            
        GUI.backgroundColor = new Color(1f, 0.8f, 0.3f);
        if (GUILayout.Button("📋 Copy Map", GUILayout.Height(32)))
        {
            if (m_NewMapName == m_MapManager.currentMapName || string.IsNullOrEmpty(m_NewMapName))
            {
                EditorUtility.DisplayDialog("Notice", "Please change the 'Map Name' text field to a new name before copying!", "OK");
            }
            else
            {
                m_MapManager.CopyCurrentMap(m_NewMapName);
                Repaint();
            }
        }
        
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ── Load Existing Maps ────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label("📂 LOAD EXISTING", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        
        List<string> maps = m_MapManager.GetAllMaps();
        
        if (maps.Count == 0)
        {
            EditorGUILayout.HelpBox("No saved maps. Create a new map to get started.", MessageType.Info);
        }
        else
        {
            foreach (string mapName in maps)
            {
                EditorGUILayout.BeginHorizontal();

                Color prev = GUI.backgroundColor;
                if (mapName == m_MapManager.currentMapName)
                    GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);

                if (GUILayout.Button(mapName, GUILayout.Height(28)))
                {
                    // Save current work before loading different map
                    if (!string.IsNullOrEmpty(m_MapManager.currentMapName) && 
                        m_MapManager.currentMapName != mapName &&
                        m_GridManager != null && m_GridManager.grid != null)
                    {
                        bool saveBeforeLoad = EditorUtility.DisplayDialog(
                            "Save Current Work?", 
                            $"Save changes to '{m_MapManager.currentMapName}' before loading '{mapName}'?", 
                            "Save & Load", "Discard & Load");
                        
                        if (saveBeforeLoad)
                        {
                            m_MapManager.SaveCurrentMap();
                        }
                    }
                    
                    // Load the selected map
                    m_MapManager.LoadMap(mapName);
                    m_NewMapName  = mapName;
                    m_FloorNumber = m_MapManager.mapToFloor.ContainsKey(mapName)
                        ? m_MapManager.mapToFloor[mapName] : 0;
                    
                    Debug.Log($"[FloorMapEditor] Loaded map: {mapName}");
                    Repaint();
                }

                GUI.backgroundColor = prev;

                GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
                if (GUILayout.Button("🗑️", GUILayout.Width(32), GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("Delete Map",
                        $"Delete '{mapName}'? This cannot be undone.", "Delete", "Cancel"))
                    {
                        // Clear walls and grid before deleting
                        if (m_MapManager.currentMapName == mapName)
                        {
                            m_GridManager.ClearWalls();
                            m_GridManager.grid = null;
                        }
                        
                        m_MapManager.DeleteMap(mapName);
                        
                        if (m_NewMapName == mapName) m_NewMapName = "";
                        m_SelectedNode = null;
                        m_QRPreview = null;
                        
                        // Reload the editor state
                        FindManagers();
                        Repaint();
                    }
                }
                GUI.backgroundColor = prev;

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ── Paint Mode ────────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label("🎨 PAINT MODE", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawModeButton(PaintMode.Wall,     "■ Wall",     COL_WALL);
        DrawModeButton(PaintMode.Walkable, "□ Walkable", COL_WALK);
        DrawModeButton(PaintMode.Entrance, "◉ Entrance", COL_ENTRANCE);
        DrawModeButton(PaintMode.Room,     "◈ Room Door",COL_ROOM);
        DrawModeButton(PaintMode.Stair,    "▲ Staircase",COL_STAIR);
        DrawModeButton(PaintMode.Lift,     "⬆ Lift",     COL_LIFT);
        DrawModeButton(PaintMode.Select,   "✎ Select",   COL_SELECT);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ── Selected Node ─────────────────────────────────────────────────────
        if (m_SelectedNode != null)
        {
            EditorGUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("🎯 SELECTED NODE", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);
            
            EditorGUILayout.LabelField("Position", $"({m_SelectedNode.x}, {m_SelectedNode.y})");
            EditorGUILayout.LabelField("Type", m_SelectedNode.nodeType.ToString());
            EditorGUILayout.LabelField("Walkable", m_SelectedNode.isWalkable ? "Yes" : "No");

            if (m_SelectedNode.nodeType == NodeType.StairEntry || m_SelectedNode.nodeType == NodeType.LiftEntry)
            {
                EditorGUILayout.Space(4);
                GUILayout.Label("🔗 CROSS-FLOOR CONNECTION", EditorStyles.boldLabel);
                m_SelectedNode.connectedMap = EditorGUILayout.TextField("Connected Map Name", m_SelectedNode.connectedMap);
                m_SelectedNode.connectedNode = EditorGUILayout.Vector2IntField("Connected Node (X,Y)", m_SelectedNode.connectedNode);
                
                EditorGUILayout.HelpBox("Set to the exact Map Name and grid coordinates of the stairs/lift on the other floor.", MessageType.Info);
            }

            EditorGUILayout.Space(4);
            GUILayout.Label("Node ID / Name:", EditorStyles.miniLabel);
            m_NodeNameInput = EditorGUILayout.TextField(m_NodeNameInput);

            GUI.backgroundColor = new Color(0.3f, 0.8f, 1f);
            if (GUILayout.Button("✓ Assign Name", GUILayout.Height(32)))
            {
                m_SelectedNode.nodeName = m_NodeNameInput.Trim().ToUpper();
                Debug.Log($"[FloorMapEditor] Node named: {m_SelectedNode.nodeName}");
                m_QRPreview = LoadExistingQR(m_SelectedNode.nodeName);
                Repaint();
            }

            GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
            if (GUILayout.Button("🗑️ Clear Node Data", GUILayout.Height(24)))
            {
                m_SelectedNode.nodeName = "";
                m_NodeNameInput = "";
                m_SelectedNode.connectedMap = "";
                m_SelectedNode.connectedNode = Vector2Int.zero;
                m_QRPreview = null;
                Debug.Log("[FloorMapEditor] Cleared node data.");
                Repaint();
            }
            GUI.backgroundColor = Color.white;

            EditorGUILayout.Space(4);
            string currentName = string.IsNullOrEmpty(m_SelectedNode.nodeName) ? "(none)" : m_SelectedNode.nodeName;
            EditorGUILayout.HelpBox($"Current: {currentName}", MessageType.None);

            // QR Code generation — only for Entrance and Room Door nodes
            bool isQRNode = m_SelectedNode.nodeType == NodeType.RoomDoor ||
                            (!string.IsNullOrEmpty(m_SelectedNode.nodeName) &&
                             m_SelectedNode.nodeName.Contains("ENTRANCE"));

            if (!string.IsNullOrEmpty(m_SelectedNode.nodeName))
            {
                EditorGUILayout.Space(6);
                GUILayout.Label("📷 QR CODE", EditorStyles.boldLabel);

                GUI.backgroundColor = new Color(0.2f, 0.7f, 0.3f);
                if (GUILayout.Button("⬛ Generate QR Code", GUILayout.Height(32)))
                {
                    m_QRPreview = GenerateQRCode(m_SelectedNode);
                    Repaint();
                }
                GUI.backgroundColor = Color.white;

                if (m_QRPreview != null)
                {
                    float previewSize = 200f;
                    Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.ExpandWidth(false));
                    EditorGUI.DrawPreviewTexture(previewRect, m_QRPreview);
                    EditorGUILayout.LabelField("Saved to: QRCodes/" + m_SelectedNode.nodeName + ".png",
                        EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(8);
        }

        // ── Export ────────────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label("📤 EXPORT", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        GUI.backgroundColor = new Color(1f, 0.6f, 0.2f);
        if (GUILayout.Button("🚀 Export → nodes.json", GUILayout.Height(40)))
            ExportToNodesJson();
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.HelpBox("Exports all named nodes to nodes.json and data.json for AR navigation.", MessageType.Info);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(8);

        // ── View Settings ─────────────────────────────────────────────────────
        EditorGUILayout.BeginVertical(sectionStyle);
        GUILayout.Label("👁️ VIEW", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        
        m_CellPx = EditorGUILayout.Slider("Cell Size", m_CellPx, 8f, 32f);

        if (GUILayout.Button("🔄 Refresh", GUILayout.Height(28)))
        {
            FindManagers();
            Repaint();
        }
        
        EditorGUILayout.Space(4);
        GUI.backgroundColor = new Color(1f, 0.5f, 0.2f);
        if (GUILayout.Button("🧹 Clear All Walls", GUILayout.Height(28)))
        {
            if (EditorUtility.DisplayDialog("Clear Walls",
                "Remove all wall GameObjects from the scene?", "Clear", "Cancel"))
            {
                m_GridManager.ClearWalls();
                Debug.Log("[FloorMapEditor] Cleared all wall objects");
            }
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.EndScrollView();
    }

    void DrawModeButton(PaintMode mode, string label, Color col)
    {
        Color prev = GUI.backgroundColor;
        
        if (m_PaintMode == mode)
        {
            GUI.backgroundColor = col;
            GUIStyle activeStyle = new GUIStyle(GUI.skin.button);
            activeStyle.fontStyle = FontStyle.Bold;
            if (GUILayout.Button("▶ " + label, activeStyle, GUILayout.Height(32)))
                m_PaintMode = mode;
        }
        else
        {
            GUI.backgroundColor = new Color(0.3f, 0.3f, 0.3f);
            if (GUILayout.Button(label, GUILayout.Height(32)))
                m_PaintMode = mode;
        }
        
        GUI.backgroundColor = prev;
    }



    // compass arrow size constants
    private const float COMPASS_SIZE   = 48f;   // bounding box of each arrow badge
    private const float COMPASS_MARGIN = 6f;    // gap between grid edge and badge

    void DrawGrid()
    {
        if (m_GridManager?.grid == null) return;

        int w = m_GridManager.width;
        int h = m_GridManager.height;

        EditorGUILayout.BeginVertical();

        // Grid header toolbar
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        string mapLabel = string.IsNullOrEmpty(m_MapManager.currentMapName) ? "No map" : m_MapManager.currentMapName;
        GUILayout.Label($"  {mapLabel}  |  {w}x{h}  |  1 cell = 1 metre  |  Mode: {m_PaintMode}", EditorStyles.miniLabel);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        // Scrollable grid area
        m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);

        // Reserve extra space around the grid for compass badges
        float pad = COMPASS_SIZE + COMPASS_MARGIN * 2f;

        // Outer rect that includes compass padding on all 4 sides
        Rect outerRect = GUILayoutUtility.GetRect(
            w * m_CellPx + pad * 2f,
            h * m_CellPx + pad * 2f,
            GUILayout.ExpandWidth(false),
            GUILayout.ExpandHeight(false)
        );

        // Actual grid rect — inset by pad
        Rect gridRect = new Rect(
            outerRect.x + pad,
            outerRect.y + pad,
            w * m_CellPx,
            h * m_CellPx
        );

        // Dark background for whole outer area
        EditorGUI.DrawRect(outerRect, new Color(0.06f, 0.06f, 0.06f));
        // Slightly lighter for grid itself
        EditorGUI.DrawRect(gridRect,  new Color(0.08f, 0.08f, 0.08f));

        // ── Draw compass arrows ───────────────────────────────────────────────
        DrawCompassArrows(gridRect);

        // Draw each cell
        for (int x = 0; x < w; x++)
        {
            for (int y = 0; y < h; y++)
            {
                Node node = m_GridManager.grid[x, y];
                if (node == null) continue;

                Rect cellRect = new Rect(
                    gridRect.x + x * m_CellPx,
                    gridRect.y + (h - 1 - y) * m_CellPx,
                    m_CellPx - 1,
                    m_CellPx - 1
                );

                Color cellColor = GetNodeColor(node);
                if (m_SelectedNode != null && m_SelectedNode.x == x && m_SelectedNode.y == y)
                    cellColor = COL_SELECT;

                EditorGUI.DrawRect(cellRect, cellColor);

                // Node name label — white text, bold
                if (m_CellPx >= 18 && !string.IsNullOrEmpty(node.nodeName))
                {
                    GUI.Label(cellRect,
                        node.nodeName.Substring(0, Mathf.Min(3, node.nodeName.Length)),
                        new GUIStyle(EditorStyles.miniLabel)
                        {
                            fontSize  = 7,
                            fontStyle = FontStyle.Bold,
                            normal    = { textColor = Color.white }
                        });
                }
            }
        }

        // Grid lines
        Handles.color = new Color(0.22f, 0.22f, 0.22f);
        for (int x = 0; x <= w; x++)
        {
            float px = gridRect.x + x * m_CellPx;
            Handles.DrawLine(new Vector3(px, gridRect.y), new Vector3(px, gridRect.y + h * m_CellPx));
        }
        for (int y = 0; y <= h; y++)
        {
            float py = gridRect.y + y * m_CellPx;
            Handles.DrawLine(new Vector3(gridRect.x, py), new Vector3(gridRect.x + w * m_CellPx, py));
        }

        HandleGridInput(gridRect, w, h);

        EditorGUILayout.EndScrollView();

        // Legend bar at bottom
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        DrawLegendChip("Wall",      COL_WALL);
        DrawLegendChip("Walkable",  COL_WALK);
        DrawLegendChip("Entrance",  COL_ENTRANCE);
        DrawLegendChip("Room Door", COL_ROOM);
        DrawLegendChip("Staircase", COL_STAIR);
        DrawLegendChip("Lift",      COL_LIFT);
        DrawLegendChip("Selected",  COL_SELECT);
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
    }

    // ── Compass Arrows ────────────────────────────────────────────────────────
    // Grid convention: X+ = East (right), Y+ = North (top of screen, since we
    // flip y when drawing: screenY = gridRect.y + (h-1-y)*cellPx)

    void DrawCompassArrows(Rect gridRect)
    {
        float cx = gridRect.x + gridRect.width  * 0.5f;  // horizontal center
        float cy = gridRect.y + gridRect.height * 0.5f;  // vertical center
        float s  = COMPASS_SIZE;
        float m  = COMPASS_MARGIN;

        // North — top edge center  (Y+ in grid = top of screen)
        DrawCompassBadge(
            new Rect(cx - s * 0.5f, gridRect.y - s - m, s, s),
            "N", "▲", new Color(0.2f, 0.85f, 0.4f));

        // South — bottom edge center
        DrawCompassBadge(
            new Rect(cx - s * 0.5f, gridRect.yMax + m, s, s),
            "S", "▼", new Color(0.9f, 0.35f, 0.35f));

        // East — right edge center  (X+ = right)
        DrawCompassBadge(
            new Rect(gridRect.xMax + m, cy - s * 0.5f, s, s),
            "E", "▶", new Color(0.3f, 0.7f, 1f));

        // West — left edge center
        DrawCompassBadge(
            new Rect(gridRect.x - s - m, cy - s * 0.5f, s, s),
            "W", "◀", new Color(1f, 0.75f, 0.2f));

        // Small NE / NW / SE / SW corner dots for reference
        DrawCornerDot(new Vector2(gridRect.x,        gridRect.y),        "NW");
        DrawCornerDot(new Vector2(gridRect.xMax,      gridRect.y),        "NE");
        DrawCornerDot(new Vector2(gridRect.x,        gridRect.yMax),      "SW");
        DrawCornerDot(new Vector2(gridRect.xMax,      gridRect.yMax),      "SE");
    }

    void DrawCompassBadge(Rect rect, string letter, string arrow, Color accentColor)
    {
        // Dark pill background
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 0.95f));

        // Colored top strip (4 px)
        EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 4f), accentColor);

        GUIStyle arrowStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = accentColor }
        };

        GUIStyle letterStyle = new GUIStyle(EditorStyles.label)
        {
            fontSize  = 11,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal    = { textColor = Color.white }
        };

        // Arrow glyph in upper half
        GUI.Label(new Rect(rect.x, rect.y + 4f,  rect.width, rect.height * 0.55f), arrow,  arrowStyle);
        // Direction letter in lower half
        GUI.Label(new Rect(rect.x, rect.y + rect.height * 0.55f, rect.width, rect.height * 0.4f), letter, letterStyle);
    }

    void DrawCornerDot(Vector2 pos, string label)
    {
        float dotSize = 14f;
        Rect dotRect = new Rect(pos.x - dotSize * 0.5f, pos.y - dotSize * 0.5f, dotSize, dotSize);
        EditorGUI.DrawRect(dotRect, new Color(0.4f, 0.4f, 0.4f, 0.7f));

        GUI.Label(
            new Rect(pos.x - 16f, pos.y - 22f, 32f, 16f),
            label,
            new GUIStyle(EditorStyles.miniLabel)
            {
                fontSize  = 8,
                alignment = TextAnchor.MiddleCenter,
                normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
            });
    }

    void DrawLegendChip(string label, Color col)
    {
        Color prev = GUI.backgroundColor;
        GUI.backgroundColor = col;
        GUILayout.Box(label, EditorStyles.miniButton, GUILayout.Height(18));
        GUI.backgroundColor = prev;
    }

    Color GetNodeColor(Node node)
    {
        if (!node.isWalkable || node.nodeType == NodeType.Obstacle) return COL_WALL;
        switch (node.nodeType)
        {
            case NodeType.RoomDoor:   return COL_ROOM;
            case NodeType.StairEntry: return COL_STAIR;
            case NodeType.LiftEntry:  return COL_LIFT;
            default:
                // Entrance = named normal node
                if (!string.IsNullOrEmpty(node.nodeName)) return COL_ENTRANCE;
                return COL_WALK;
        }
    }

    void HandleGridInput(Rect gridRect, int w, int h)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
            m_IsPainting = true;
        if (e.type == EventType.MouseUp)
            m_IsPainting = false;

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
            e.button == 0 && gridRect.Contains(e.mousePosition))
        {
            int cx = Mathf.FloorToInt((e.mousePosition.x - gridRect.x) / m_CellPx);
            int cy = h - 1 - Mathf.FloorToInt((e.mousePosition.y - gridRect.y) / m_CellPx);

            if (cx >= 0 && cx < w && cy >= 0 && cy < h)
            {
                PaintCell(cx, cy);
                e.Use();
                Repaint();
            }
        }
    }

    void PaintCell(int x, int y)
    {
        Node node = m_GridManager.grid[x, y];
        if (node == null) return;

        switch (m_PaintMode)
        {
            case PaintMode.Wall:
                node.isWalkable = false;
                node.nodeType   = NodeType.Obstacle;
                m_GridManager.UpdateWalls();
                break;

            case PaintMode.Walkable:
                node.isWalkable = true;
                node.nodeType   = NodeType.Normal;
                m_GridManager.UpdateWalls();
                break;

            case PaintMode.Room:
                node.isWalkable = true;
                node.nodeType   = NodeType.RoomDoor;
                break;

            case PaintMode.Stair:
                node.isWalkable = true;
                node.nodeType   = NodeType.StairEntry;
                break;

            case PaintMode.Lift:
                node.isWalkable = true;
                node.nodeType   = NodeType.LiftEntry;
                break;

            case PaintMode.Entrance:
                node.isWalkable = true;
                node.nodeType   = NodeType.Normal;
                if (string.IsNullOrEmpty(node.nodeName))
                    node.nodeName = $"ENTRANCE_{x}_{y}";
                break;

            case PaintMode.Select:
                m_SelectedNode  = node;
                m_NodeNameInput = node.nodeName;
                m_QRPreview     = LoadExistingQR(node.nodeName);
                break;
        }
    }

    // ── QR Code Generation ────────────────────────────────────────────────────

    Texture2D GenerateQRCode(Node node)
    {
        string building = m_MapManager.mapToBuilding.ContainsKey(m_MapManager.currentMapName)
            ? m_MapManager.mapToBuilding[m_MapManager.currentMapName] : "Main Block";
        int floor = m_MapManager.mapToFloor.ContainsKey(m_MapManager.currentMapName)
            ? m_MapManager.mapToFloor[m_MapManager.currentMapName] : 0;

        string payload = $"{{\"node_id\":\"{node.nodeName}\",\"building\":\"{building}\",\"floor\":{floor}}}";

        int size = 256;
        var writer = new BarcodeWriterPixelData
        {
            Format = BarcodeFormat.QR_CODE,
            Options = new QrCodeEncodingOptions { Height = size, Width = size, Margin = 1 }
        };

        var pixelData = writer.Write(payload);
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        // ZXing gives top-left origin; Unity textures are bottom-left — flip Y
        Color32[] colors = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int srcIdx = (y * size + x) * 4;
                colors[(size - 1 - y) * size + x] = new Color32(
                    pixelData.Pixels[srcIdx],
                    pixelData.Pixels[srcIdx + 1],
                    pixelData.Pixels[srcIdx + 2],
                    pixelData.Pixels[srcIdx + 3]);
            }

        tex.SetPixels32(colors);
        tex.Apply();

        string dir  = "Assets/ProjectCore/Resources/QRCodes";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string path = $"{dir}/{node.nodeName}.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        AssetDatabase.Refresh();

        Debug.Log($"[FloorMapEditor] QR saved: {path} | payload: {payload}");
        return tex;
    }

    Texture2D LoadExistingQR(string nodeName)
    {
        string path = $"Assets/ProjectCore/Resources/QRCodes/{nodeName}.png";
        if (!File.Exists(path)) return null;
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);
        return tex;
    }

    // ── Map Operations ────────────────────────────────────────────────────────

    void CreateNewMap()
    {
        if (string.IsNullOrEmpty(m_NewMapName))
        {
            EditorUtility.DisplayDialog("Error", "Enter a map name first.", "OK");
            return;
        }

        // IMPORTANT: Save current work BEFORE creating new map
        if (!string.IsNullOrEmpty(m_MapManager.currentMapName) && 
            m_GridManager.grid != null)
        {
            // Ask user if they want to save current work
            bool saveCurrentWork = EditorUtility.DisplayDialog(
                "Save Current Work?", 
                $"Save changes to '{m_MapManager.currentMapName}' before creating new map?", 
                "Save & Continue", "Discard & Continue");
            
            if (saveCurrentWork)
            {
                m_MapManager.SaveCurrentMap();
                Debug.Log($"[FloorMapEditor] Saved current work: {m_MapManager.currentMapName}");
            }
        }

        // Now create the new map
        m_MapManager.currentMapName = m_NewMapName;
        m_MapManager.mapToFloor[m_NewMapName] = m_FloorNumber;
        m_MapManager.RegisterMapToBuilding(m_NewMapName, m_BuildingName);
        
        // Reset grid AFTER saving previous work
        m_GridManager.ResetGrid();
        
        // Save the fresh empty grid
        m_MapManager.SaveCurrentMap();
        m_MapManager.SaveBuildingsToFile();

        Debug.Log($"[FloorMapEditor] Created new map: {m_NewMapName} | Floor {m_FloorNumber} | Building: {m_BuildingName}");
        Repaint();
    }

    void SaveMap()
    {
        if (string.IsNullOrEmpty(m_MapManager.currentMapName))
        {
            EditorUtility.DisplayDialog("Error", "No map loaded. Create or load a map first.", "OK");
            return;
        }

        m_MapManager.SaveCurrentMap();
        AssetDatabase.Refresh();
        Debug.Log($"[FloorMapEditor] Saved map: {m_MapManager.currentMapName}");
        EditorUtility.DisplayDialog("Saved", $"Map '{m_MapManager.currentMapName}' saved successfully.", "OK");
    }

    // ── Export to nodes.json ──────────────────────────────────────────────────

    void ExportToNodesJson()
    {
        // Collect all walkable nodes across all maps (both named rooms and unnamed corridors)
        List<ExportNode> exportNodes = new List<ExportNode>();
        List<string> allMaps = m_MapManager.GetAllMaps();
        HashSet<string> seenIds = new HashSet<string>();

        foreach (string mapName in allMaps)
        {
            Node[,] grid = m_MapManager.GetMap(mapName);
            if (grid == null)
                continue;

            int floor    = m_MapManager.mapToFloor.ContainsKey(mapName) ? m_MapManager.mapToFloor[mapName] : 0;
            string bldg  = m_MapManager.mapToBuilding.ContainsKey(mapName) ? m_MapManager.mapToBuilding[mapName] : "Main Block";

            for (int x = 0; x < grid.GetLength(0); x++)
            {
                for (int y = 0; y < grid.GetLength(1); y++)
                {
                    Node n = grid[x, y];
                    if (n == null || !n.isWalkable) continue;

                    string normalizedId = "";
                    bool isNamed = !string.IsNullOrEmpty(n.nodeName);
                    
                    if (isNamed)
                    {
                        normalizedId = n.nodeName.Trim().ToUpper();
                        if (!seenIds.Add(normalizedId))
                        {
                            EditorUtility.DisplayDialog(
                                "Duplicate Node ID",
                                $"Duplicate node_id detected: {normalizedId}\n\nEach exported node_id must be unique across all maps.",
                                "OK");
                            return;
                        }
                    }
                    else
                    {
                        normalizedId = $"WAYPOINT_{mapName.Replace(" ", "_")}_{x}_{y}".ToUpper();
                    }

                    ExportNode en = new ExportNode();
                    en.id          = normalizedId;
                    en.displayName = isNamed ? FormatDisplayName(normalizedId) : "";
                    en.type        = GetTypeString(n);
                    en.building    = bldg;
                    en.floor       = floor;
                    en.qr_point    = isNamed;
                    en.description = isNamed ? $"{en.displayName} on floor {floor} of {bldg}." : "";
                    en.gridX       = x;
                    en.gridY       = y;
                    en.mapName     = mapName;
                    en.rotation_y  = 0f;

                    exportNodes.Add(en);
                }
            }
        }

        if (exportNodes.Count == 0)
        {
            WriteNodesJsonFiles("{\"nodes\":[]}");
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Exported Empty Graph", "No walkable nodes found.", "OK");
            return;
        }

        // Build graph edges by connecting adjacent walkable cells
        BuildNeighbors(exportNodes);

        // Serialize to nodes.json format
        NodesJsonWrapper wrapper = new NodesJsonWrapper();
        wrapper.nodes = new List<NodesJsonNode>();

        float cellSize  = m_GridManager != null ? m_GridManager.cellSize : 1f;
        float floorHeight = m_FloorHeight;

        foreach (ExportNode en in exportNodes)
        {
            NodesJsonNode jn = new NodesJsonNode();
            jn.id          = en.id;
            jn.displayName = en.displayName;
            jn.type        = en.type;
            jn.building    = en.building;
            jn.floor       = en.floor;
            jn.x           = en.gridX * cellSize;
            jn.y           = en.floor * floorHeight;
            jn.z           = en.gridY * cellSize;
            jn.rotation_y  = en.rotation_y;
            jn.qr_point    = en.qr_point;
            jn.description = en.description;
            jn.neighbors   = en.neighbors.ToArray();
            wrapper.nodes.Add(jn);
        }

        string json = JsonUtility.ToJson(wrapper, true);
        WriteNodesJsonFiles(json);
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Export Complete",
            $"Exported {exportNodes.Count} nodes (including corridors) to Unity Resources and ARBackend/nodes.json", "OK");

        Debug.Log($"[FloorMapEditor] Exported {exportNodes.Count} nodes to Unity Resources + backend nodes.json");
    }

    void BuildNeighbors(List<ExportNode> nodes)
    {
        // Build lookup: mapName + gridPos → node
        Dictionary<string, ExportNode> posToNode = new Dictionary<string, ExportNode>();
        foreach (ExportNode en in nodes)
        {
            string key = $"{en.mapName}_{en.gridX}_{en.gridY}";
            posToNode[key] = en;
        }

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        // For each node, just connect to its 4 adjacent walkable neighbors
        foreach (ExportNode en in nodes)
        {
            Node[,] grid = m_MapManager.GetMap(en.mapName);
            if (grid == null) continue;

            for (int d = 0; d < 4; d++)
            {
                int nx = en.gridX + dx[d];
                int ny = en.gridY + dy[d];

                if (nx < 0 || ny < 0 || nx >= grid.GetLength(0) || ny >= grid.GetLength(1)) continue;

                string neighborKey = $"{en.mapName}_{nx}_{ny}";
                if (posToNode.TryGetValue(neighborKey, out ExportNode neighborNode))
                {
                    AddBidirectionalNeighbor(en, neighborNode);
                }
            }

            // Check cross-map connections (stairs/lifts)
            Node sourceNode = grid[en.gridX, en.gridY];
            if (sourceNode != null && !string.IsNullOrEmpty(sourceNode.connectedMap))
            {
                string connKey = $"{sourceNode.connectedMap}_{sourceNode.connectedNode.x}_{sourceNode.connectedNode.y}";
                if (posToNode.TryGetValue(connKey, out ExportNode linkedNode))
                    AddBidirectionalNeighbor(en, linkedNode);
            }
        }
    }

    void AddBidirectionalNeighbor(ExportNode a, ExportNode b)
    {
        if (!a.neighbors.Contains(b.id))
            a.neighbors.Add(b.id);
        if (!b.neighbors.Contains(a.id))
            b.neighbors.Add(a.id);
    }

    void WriteNodesJsonFiles(string json)
    {
        string unityPath = Path.Combine(Application.dataPath, "ProjectCore", "Resources", "nodes.json");
        File.WriteAllText(unityPath, json);

        string backendPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "ARBackend", "nodes.json"));
        Directory.CreateDirectory(Path.GetDirectoryName(backendPath));
        File.WriteAllText(backendPath, json);
    }

    string FormatDisplayName(string nodeName)
    {
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(nodeName.Replace("_", " ").ToLower());
    }

    string GetTypeString(Node node)
    {
        if (node == null)
            return "corridor";

        if (!string.IsNullOrEmpty(node.nodeName) && node.nodeName.ToUpper().Contains("ENTRANCE"))
            return "entrance";

        switch (node.nodeType)
        {
            case NodeType.StairEntry: return "staircase";
            case NodeType.LiftEntry:  return "lift";
            case NodeType.RoomDoor:   return "room";
            case NodeType.Obstacle:   return "wall";
            default:                  return "corridor";
        }
    }

    // ── Helper classes for export ─────────────────────────────────────────────

    private class ExportNode
    {
        public string       id;
        public string       displayName;
        public string       type;
        public string       building;
        public int          floor;
        public bool         qr_point;
        public string       description;
        public int          gridX;
        public int          gridY;
        public string       mapName;
        public float        rotation_y;
        public List<string> neighbors = new List<string>();
    }

    [System.Serializable]
    private class NodesJsonWrapper
    {
        public List<NodesJsonNode> nodes;
    }

    [System.Serializable]
    private class NodesJsonNode
    {
        public string   id;
        public string   displayName;
        public string   type;
        public string   building;
        public int      floor;
        public float    x;
        public float    y;
        public float    z;
        public float    rotation_y;
        public bool     qr_point;
        public string   description;
        public string[] neighbors;
    }
}
