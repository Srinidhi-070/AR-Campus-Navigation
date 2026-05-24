using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private GameObject curvedArrow45Prefab;
    [SerializeField] private GameObject curvedArrow90Prefab;
    [SerializeField] private GameObject curvedArrow135Prefab;
    [SerializeField] private GameObject destinationPrefab; // New prefab for the destination
    [SerializeField] private GameObject stairPrefab; // Original prefab used specifically for stairs
    [SerializeField] private float spacing = 1.5f; // Reverted spacing for old arrows

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();
    private Material arrowMaterial;
    private Material staircaseMaterial;
    private Material liftMaterial;
    private Material destinationMaterial;
    public bool HasActivePath => spawnedArrows.Count > 0;

    // ── Floor Transition Data ────────────────────────────────────────────
    public enum TransitionType { Staircase, Lift }

    public struct FloorTransition
    {
        /// <summary>Index in the world path where the transition STARTS (lower floor node).</summary>
        public int segmentStartIndex;
        public TransitionType type;
        public int fromFloor;
        public int toFloor;
        public bool goingUp;
    }

    void Awake()
    {
        EnsureArrowMaterial();
        EnsureTransitionMaterials();

        // Load the procedurally generated 3D arrows
        if (arrowPrefab == null)
            arrowPrefab = CreateSanitizedTemplate("Prefabs/ProceduralArrow", "ArrowTemplate", arrowMaterial, 0.3f);
        
        if (curvedArrow45Prefab == null)
            curvedArrow45Prefab = CreateSanitizedTemplate("Prefabs/ProceduralCurvedArrow_45", "CurvedArrow45Template", arrowMaterial, 0.3f);
        if (curvedArrow90Prefab == null)
            curvedArrow90Prefab = CreateSanitizedTemplate("Prefabs/ProceduralCurvedArrow_90", "CurvedArrow90Template", arrowMaterial, 0.3f);
        if (curvedArrow135Prefab == null)
            curvedArrow135Prefab = CreateSanitizedTemplate("Prefabs/ProceduralCurvedArrow_135", "CurvedArrow135Template", arrowMaterial, 0.3f);
        
        if (destinationPrefab == null)
            destinationPrefab = CreateSanitizedTemplate("Prefabs/ProceduralDestination_V2", "DestinationTemplate", destinationMaterial, 0.6f);

        if (stairPrefab == null)
            stairPrefab = CreateSanitizedTemplate("Prefabs/ProceduralStairs", "StairTemplate", staircaseMaterial, 0.4f);

        // Fallback to legacy behavior if custom prefabs fail
        if (arrowPrefab == null)
            CreateFallbackArrow();

        // Ensure materials were already called at the start of Awake
    }

    private GameObject CreateSanitizedTemplate(string resourcePath, string templateName, Material mat, float scale = 1.0f)
    {
        GameObject loaded = Resources.Load<GameObject>(resourcePath);
        if (loaded == null)
        {
            Debug.LogWarning($"[PathVisualizer] Prefab not found at {resourcePath}");
            return null;
        }

        // Instantiate inactive to completely prevent any scripts/cameras from running
        GameObject template = Instantiate(loaded);
        template.name = templateName;
        template.SetActive(false);
        template.transform.localScale = new Vector3(scale, scale, scale);
        
        // Strip rogue components permanently from the template
        StripRogueComponents(template);

        // Apply material directly to the template so all clones inherit it perfectly
        if (mat != null)
        {
            var renderers = template.GetComponentsInChildren<Renderer>(true);
            foreach (var r in renderers) r.material = mat;
        }
        
        DontDestroyOnLoad(template);
        Debug.Log($"[PathVisualizer] {templateName} sanitized and ready.");
        return template;
    }

    // ── Fallback Arrow ───────────────────────────────────────────────────
    private void CreateFallbackArrow()
    {
        GameObject arrow = new GameObject("FallbackArrow");
        
        // Create cone for arrow head
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.SetParent(arrow.transform, false);
        cone.transform.localPosition = new Vector3(0, 0, 0.2f);
        cone.transform.localRotation = Quaternion.Euler(90, 0, 0);
        cone.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
        
        // Create cylinder for arrow shaft
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.transform.SetParent(arrow.transform, false);
        shaft.transform.localPosition = Vector3.zero;
        shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
        shaft.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
        
        Material arrowMat = EnsureArrowMaterial();
        if (arrowMat != null)
        {
            cone.GetComponent<Renderer>().material = arrowMat;
            shaft.GetComponent<Renderer>().material = arrowMat;
        }
        
        // Remove colliders (not needed for visual arrows)
        Destroy(cone.GetComponent<Collider>());
        Destroy(shaft.GetComponent<Collider>());
        
        arrowPrefab = arrow;
        arrow.SetActive(false);
        DontDestroyOnLoad(arrow);
        
        Debug.Log("[PathVisualizer] Fallback arrow created (Oriented to Z-Forward)");
    }

    // ── Materials ─────────────────────────────────────────────────────────
    private Material EnsureArrowMaterial()
    {
        if (arrowMaterial != null)
            return arrowMaterial;

        Shader shader = FindBestShader();
        if (shader == null) return null;

        arrowMaterial = new Material(shader);
        Color color = new Color(1f, 0.05f, 0.8f, 1f); // Neon Pink (Highly visible in AR)
        SetMaterialColor(arrowMaterial, color);
        return arrowMaterial;
    }

    private void EnsureTransitionMaterials()
    {
        Shader shader = FindBestShader();
        if (shader == null) return;

        // Staircase: High-visibility neon orange/gold
        if (staircaseMaterial == null)
        {
            staircaseMaterial = new Material(shader);
            SetMaterialColor(staircaseMaterial, new Color(1f, 0.7f, 0f, 1f));
        }

        // Lift: Electric Violet
        if (liftMaterial == null)
        {
            liftMaterial = new Material(shader);
            SetMaterialColor(liftMaterial, new Color(0.7f, 0.1f, 1f, 1f));
        }

        // Destination marker: Bright Neon Green
        if (destinationMaterial == null)
        {
            destinationMaterial = new Material(shader);
            SetMaterialColor(destinationMaterial, new Color(0.1f, 1f, 0.2f, 1f));
        }
    }

    private Shader FindBestShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
            Debug.LogError("[PathVisualizer] Could not find a shader for AR path visuals.");
        return shader;
    }

    private void SetMaterialColor(Material mat, Color color)
    {
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        mat.color = color;
        
        // Add emission for an attractive AR glow effect
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * 1.5f); // Boost intensity for AR visibility
        }
    }

    private void ApplyMaterial(GameObject instance, Material material)
    {
        if (instance == null || material == null)
            return;

        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>(true))
            renderer.material = material;
    }

    // ── Main Draw Methods ─────────────────────────────────────────────────

    /// <summary>
    /// Draws a path with regular arrows only (no floor transition info).
    /// </summary>
    public void DrawPath(List<Vector3> worldPath)
    {
        DrawPath(worldPath, null);
    }

    /// <summary>
    /// Draws a path with floor transition markers at staircase/lift segments.
    /// </summary>
    public void DrawPath(List<Vector3> worldPath, List<FloorTransition> transitions)
    {
        ClearPath();
        gameObject.SetActive(true);

        if (worldPath == null || worldPath.Count < 2)
        {
            Debug.LogWarning("[PathVisualizer] Path has less than 2 points, nothing to draw");
            return;
        }

        if (arrowPrefab == null)
        {
            Debug.LogError("[PathVisualizer] Arrow Prefab is still null after Awake! Cannot draw path.");
            return;
        }

        // Build a set of segment indices that are floor transitions
        HashSet<int> transitionSegments = new HashSet<int>();
        Dictionary<int, FloorTransition> transitionMap = new Dictionary<int, FloorTransition>();
        if (transitions != null)
        {
            foreach (var t in transitions)
            {
                transitionSegments.Add(t.segmentStartIndex);
                transitionMap[t.segmentStartIndex] = t;
            }
        }

        Debug.Log($"[PathVisualizer] Drawing path: {worldPath.Count} waypoints, {transitionSegments.Count} floor transitions, spacing={spacing}");

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 start = worldPath[i];
            Vector3 end = worldPath[i + 1];
            float distance = Vector3.Distance(start, end);
            if (distance < 0.01f)
                continue;

            // ── Floor transition segment — render special markers ──
            if (transitionSegments.Contains(i) && transitionMap.TryGetValue(i, out FloorTransition transition))
            {
                if (transition.type == TransitionType.Staircase)
                    DrawStaircaseSegment(start, end, transition);
                else
                    DrawLiftSegment(start, end, transition);
                continue;
            }

            // ── Normal segment — render regular arrows ──
            Vector3 dir = (end - start) / distance;

            // Optional: Draw a curved arrow at the corner (if there is a next segment)
            if (i < worldPath.Count - 2 && curvedArrow90Prefab != null && !transitionSegments.Contains(i+1))
            {
                Vector3 nextEnd = worldPath[i + 2];
                Vector3 nextDir = (nextEnd - end).normalized;
                
                // Determine angle between incoming and outgoing direction
                float angle = Vector3.SignedAngle(dir, nextDir, Vector3.up);
                float absAngle = Mathf.Abs(angle);
                
                if (absAngle > 20f) // Sharp turn
                {
                    GameObject curvedPrefab = curvedArrow45Prefab;
                    if (absAngle > 60f) curvedPrefab = curvedArrow90Prefab;
                    if (absAngle > 110f) curvedPrefab = curvedArrow135Prefab;

                    if (curvedPrefab != null)
                    {
                        GameObject curvedInstance = Instantiate(curvedPrefab, end, Quaternion.LookRotation(dir), transform);
                        
                        if (angle < 0) // Left turn
                        {
                            // Mirror the curved arrow by flipping local scale X
                            curvedInstance.transform.localScale = new Vector3(-1, 1, 1);
                        }
                        
                        curvedInstance.SetActive(true);
                        spawnedArrows.Add(curvedInstance);
                    }
                }
            }

            int steps = Mathf.Max(1, Mathf.FloorToInt(distance / spacing));
            for (int j = 0; j < steps; j++)
            {
                // Skip spawning a straight arrow right exactly at the end if we put a curved one
                if (j == steps - 1 && i < worldPath.Count - 2)
                {
                    float angle = Vector3.Angle(dir, (worldPath[i+2] - end).normalized);
                    if (angle > 20f) continue;
                }

                Vector3 pos = Vector3.Lerp(start, end, j / (float)steps);
                GameObject arrowInstance = Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir), transform);
                arrowInstance.SetActive(true);
                spawnedArrows.Add(arrowInstance);
            }
        }

        // Final arrow at destination
        Vector3 lastSegment = worldPath[worldPath.Count - 1] - worldPath[worldPath.Count - 2];
        Vector3 lastDir = lastSegment.sqrMagnitude > 0.0001f ? lastSegment.normalized : Vector3.forward;
        
        GameObject prefabToUse = destinationPrefab != null ? destinationPrefab : arrowPrefab;
        
        GameObject lastArrow = Instantiate(
            prefabToUse,
            worldPath[worldPath.Count - 1],
            Quaternion.LookRotation(lastDir),
            transform);

        lastArrow.SetActive(true);
        spawnedArrows.Add(lastArrow);
        
        Debug.Log($"[PathVisualizer] ✅ Spawned {spawnedArrows.Count} path elements");
    }

    private void StripRogueComponents(GameObject obj)
    {
        // Destroy the component only, not the gameObject, otherwise we might delete the whole model!
        var cameras = obj.GetComponentsInChildren<Camera>(true);
        foreach (var cam in cameras) 
        {
            cam.enabled = false; // Disable immediately to prevent 1-frame hijack
            Destroy(cam);
        }

        var lights = obj.GetComponentsInChildren<Light>(true);
        foreach (var light in lights) 
        {
            light.enabled = false;
            Destroy(light);
        }
    }

    // ── Staircase Visualization ──────────────────────────────────────────
    // Renders a series of step-like ascending/descending arrows between two points.
    // Each "step" rises vertically then moves forward, mimicking real stair geometry.
    private void DrawStaircaseSegment(Vector3 start, Vector3 end, FloorTransition transition)
    {
        float distance = Vector3.Distance(start, end);
        if (distance < 0.1f) return;

        Vector3 dir = (end - start) / distance;
        
        // Create a "staircase" label at the midpoint
        Vector3 midpoint = Vector3.Lerp(start, end, 0.5f);
        string label = transition.goingUp
            ? $"▲ Stairs to Floor {transition.toFloor}"
            : $"▼ Stairs to Floor {transition.toFloor}";
        SpawnTextLabel(midpoint + Vector3.up * 0.4f, label, staircaseMaterial);

        // Draw simple diagonal arrows using the stair prefab
        GameObject prefabToUse = stairPrefab != null ? stairPrefab : arrowPrefab;
        int steps = Mathf.Max(1, Mathf.FloorToInt(distance / spacing));

        for (int j = 0; j < steps; j++)
        {
            Vector3 pos = Vector3.Lerp(start, end, j / (float)steps);
            GameObject arrowInstance = Instantiate(prefabToUse, pos, Quaternion.LookRotation(dir), transform);
            arrowInstance.SetActive(true);
            spawnedArrows.Add(arrowInstance);
        }

        Debug.Log($"[PathVisualizer] 🪜 Staircase: {(transition.goingUp ? "UP" : "DOWN")} to Floor {transition.toFloor}");
    }

    // ── Lift Visualization ───────────────────────────────────────────────
    // Renders a vertical shaft with a pulsing platform and directional arrow.
    // Clearly different from stairs — uses a box shape with vertical line markers.
    private void DrawLiftSegment(Vector3 start, Vector3 end, FloorTransition transition)
    {
        float totalHeight = Mathf.Abs(end.y - start.y);
        Vector3 bottom = start.y < end.y ? start : end;
        Vector3 top = start.y < end.y ? end : start;
        Vector3 center = (bottom + top) * 0.5f;

        // ── Lift shaft (tall thin box) ──
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
        shaft.transform.SetParent(transform, false);
        shaft.transform.position = center;
        shaft.transform.localScale = new Vector3(0.4f, totalHeight, 0.4f);
        Destroy(shaft.GetComponent<Collider>());
        
        // Semi-transparent shaft
        Shader shader = FindBestShader();
        if (shader != null)
        {
            Material shaftMat = new Material(shader);
            SetMaterialColor(shaftMat, new Color(0.5f, 0.25f, 0.9f, 0.3f));
            shaftMat.renderQueue = 3000;
            shaft.GetComponent<Renderer>().material = shaftMat;
        }
        shaft.SetActive(true);
        spawnedArrows.Add(shaft);

        // ── Vertical guide arrows inside the shaft ──
        int arrowCount = Mathf.Max(3, Mathf.RoundToInt(totalHeight / 0.5f));
        Vector3 arrowDir = transition.goingUp ? Vector3.up : Vector3.down;
        
        for (int i = 0; i < arrowCount; i++)
        {
            float t = (i + 0.5f) / arrowCount;
            Vector3 pos = Vector3.Lerp(bottom, top, t);
            
            GameObject prefabToUse = stairPrefab != null ? stairPrefab : arrowPrefab;

            // Use forward direction for LookRotation since arrows point up/down
            GameObject arrow = Instantiate(prefabToUse, pos, Quaternion.LookRotation(arrowDir, Vector3.forward), transform);
            arrow.SetActive(true);
            spawnedArrows.Add(arrow);
        }

        // ── Platform markers at entry/exit ──
        SpawnPlatformMarker(bottom, liftMaterial);
        SpawnPlatformMarker(top, liftMaterial);

        // ── Label ──
        string label = transition.goingUp
            ? $"▲ Lift to Floor {transition.toFloor}"
            : $"▼ Lift to Floor {transition.toFloor}";
        SpawnTextLabel(center + Vector3.up * (totalHeight * 0.5f + 0.3f), label, liftMaterial);

        Debug.Log($"[PathVisualizer] 🛗 Lift: {(transition.goingUp ? "UP" : "DOWN")} from Floor {transition.fromFloor} → Floor {transition.toFloor}, height={totalHeight:F1}m");
    }

    // ── Helper: Platform Marker (flat disc at entry/exit) ────────────────
    private void SpawnPlatformMarker(Vector3 position, Material material)
    {
        GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        platform.transform.SetParent(transform, false);
        platform.transform.position = position;
        platform.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
        Destroy(platform.GetComponent<Collider>());
        
        if (material != null)
            platform.GetComponent<Renderer>().material = material;
        
        platform.SetActive(true);
        spawnedArrows.Add(platform);
    }

    // ── Helper: 3D Text Label ─────────────────────────────────────────────
    private void SpawnTextLabel(Vector3 position, string text, Material colorSource)
    {
        GameObject labelObj = new GameObject("TransitionLabel");
        labelObj.transform.SetParent(transform, false);
        labelObj.transform.position = position;

        // Billboard: always face the camera
        labelObj.AddComponent<BillboardLabel>();

        TextMesh textMesh = labelObj.AddComponent<TextMesh>();
        textMesh.text = text;
        textMesh.fontSize = 80;
        textMesh.characterSize = 0.02f;
        textMesh.anchor = TextAnchor.MiddleCenter;
        textMesh.alignment = TextAlignment.Center;
        textMesh.fontStyle = FontStyle.Bold;
        textMesh.color = colorSource != null ? colorSource.color : Color.white;

        // Background panel behind text for readability
        GameObject bg = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bg.transform.SetParent(labelObj.transform, false);
        bg.transform.localPosition = new Vector3(0f, 0f, 0.01f);
        bg.transform.localScale = new Vector3(text.Length * 0.018f, 0.06f, 1f);
        Destroy(bg.GetComponent<Collider>());

        Shader bgShader = FindBestShader();
        if (bgShader != null)
        {
            Material bgMat = new Material(bgShader);
            SetMaterialColor(bgMat, new Color(0.05f, 0.05f, 0.1f, 0.85f));
            bgMat.renderQueue = 3000;
            bg.GetComponent<Renderer>().material = bgMat;
        }

        labelObj.SetActive(true);
        spawnedArrows.Add(labelObj);
    }

    // ── Clear All ─────────────────────────────────────────────────────────
    public void ClearPath()
    {
        foreach (var obj in spawnedArrows)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedArrows.Clear();
    }
}

/// <summary>
/// Simple billboard behavior — rotates the object to always face the main camera.
/// Used for transition labels so they're readable from any angle.
/// </summary>
public class BillboardLabel : MonoBehaviour
{
    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null) return;
        
        // Face the camera but stay upright (only rotate around Y axis)
        Vector3 lookDir = cam.transform.position - transform.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-lookDir, Vector3.up);
    }
}
