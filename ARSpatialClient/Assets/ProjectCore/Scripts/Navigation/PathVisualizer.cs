using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private GameObject destinationPrefab;
    [SerializeField] private GameObject stairPrefab;
    [SerializeField] private float spacing = 0.35f; // Tighter spacing for chevron arrows

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

        // Use the new procedural Chevron arrow
        if (arrowPrefab == null)
            CreateChevronArrow();
        
        if (destinationPrefab == null)
            destinationPrefab = CreateSanitizedTemplate("Prefabs/ProceduralDestination_V2", "DestinationTemplate", destinationMaterial, 0.6f);

        if (stairPrefab == null)
            stairPrefab = CreateSanitizedTemplate("Prefabs/ProceduralStairs", "StairTemplate", staircaseMaterial, 0.4f);
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

    // ── Procedural Chevron Arrow ─────────────────────────────────────────
    private void CreateChevronArrow()
    {
        GameObject arrow = new GameObject("ChevronArrow");
        
        // Material
        Material arrowMat = EnsureArrowMaterial();

        // Left Arm of the chevron
        GameObject leftArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        leftArm.transform.SetParent(arrow.transform, false);
        leftArm.transform.localScale = new Vector3(0.12f, 0.01f, 0.35f);
        leftArm.transform.localPosition = new Vector3(-0.11f, 0f, 0f);
        leftArm.transform.localRotation = Quaternion.Euler(0, 55, 0);
        
        // Right Arm of the chevron
        GameObject rightArm = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rightArm.transform.SetParent(arrow.transform, false);
        rightArm.transform.localScale = new Vector3(0.12f, 0.01f, 0.35f);
        rightArm.transform.localPosition = new Vector3(0.11f, 0f, 0f);
        rightArm.transform.localRotation = Quaternion.Euler(0, -55, 0);
        
        if (arrowMat != null)
        {
            leftArm.GetComponent<Renderer>().material = arrowMat;
            rightArm.GetComponent<Renderer>().material = arrowMat;
        }
        
        Destroy(leftArm.GetComponent<Collider>());
        Destroy(rightArm.GetComponent<Collider>());
        
        // Add the animator to make the arrows flow seamlessly along the path
        ArrowAnimator animator = arrow.AddComponent<ArrowAnimator>();
        animator.spacing = this.spacing; // Sync with PathVisualizer's spacing
        animator.speed = 0.6f; // Flow speed in meters per second
        
        arrowPrefab = arrow;
        arrow.SetActive(false);
        DontDestroyOnLoad(arrow);
        
        Debug.Log("[PathVisualizer] Chevron arrow created successfully");
    }

    // ── Materials ─────────────────────────────────────────────────────────
    private Material EnsureArrowMaterial()
    {
        if (arrowMaterial != null)
            return arrowMaterial;

        Shader shader = FindBestShader();
        if (shader == null) return null;

        arrowMaterial = new Material(shader);
        Color color = new Color(1f, 0.45f, 0f, 1f); // Vibrant Orange
        SetMaterialColor(arrowMaterial, color);
        return arrowMaterial;
    }

    private void EnsureTransitionMaterials()
    {
        Shader shader = FindBestShader();
        if (shader == null) return;

        // Staircase: High-visibility neon yellow
        if (staircaseMaterial == null)
        {
            staircaseMaterial = new Material(shader);
            SetMaterialColor(staircaseMaterial, new Color(1f, 1f, 0f, 1f));
        }

        // Lift: Electric Violet
        if (liftMaterial == null)
        {
            liftMaterial = new Material(shader);
            SetMaterialColor(liftMaterial, new Color(1f, 0f, 1f, 1f));
        }

        // Destination marker: Bright Neon Green
        if (destinationMaterial == null)
        {
            destinationMaterial = new Material(shader);
            SetMaterialColor(destinationMaterial, new Color(0f, 1f, 0f, 1f));
        }
    }

    private Shader FindBestShader()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Color"); // Crucial: AR needs unlit!
        if (shader == null) shader = Shader.Find("Mobile/Unlit (Supports Lightmap)");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Standard"); // Fallback if nothing else exists
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
        
        Vector3 lastCurvedArrowPos = new Vector3(-9999, -9999, -9999);

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

        }

        // Apply Chaikin's corner cutting algorithm to smooth out sharp zig-zags from the grid map
        // This ensures the continuous spawning doesn't squash arrows together at tight corners.
        List<Vector3> smoothedPath = SmoothPath(worldPath, 3);

        // ── Normal segment — render regular arrows continuously ──
        // This prevents clumping at corners by walking the path evenly and using look-ahead for smooth rotation.
        float totalDist = GetTotalPathDistance(smoothedPath);
        float currentDist = 0f;
        
        while (currentDist < totalDist - 0.2f)
        {
            Vector3 pos = GetPointAtDistance(smoothedPath, currentDist);
            // Look ahead to smooth out corners
            Vector3 lookAheadPos = GetPointAtDistance(smoothedPath, Mathf.Min(currentDist + 0.6f, totalDist));
            
            Vector3 dir = (lookAheadPos - pos).normalized;
            if (dir.sqrMagnitude < 0.001f)
            {
                // Fallback to strict segment direction if look-ahead fails (e.g. at very end of path)
                int segIdx = GetSegmentIndexAtDistance(smoothedPath, currentDist);
                if (segIdx < smoothedPath.Count - 1)
                    dir = (smoothedPath[segIdx + 1] - smoothedPath[segIdx]).normalized;
                else
                    dir = Vector3.forward;
            }
            
            // Spawn the regular arrow
            GameObject arrowInstance = Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir), transform);
            arrowInstance.SetActive(true);
            spawnedArrows.Add(arrowInstance);
            
            currentDist += spacing;
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

    private List<Vector3> SmoothPath(List<Vector3> path, int iterations = 2)
    {
        if (path == null || path.Count < 3) return path;

        List<Vector3> currentPath = new List<Vector3>(path);
        
        for (int iter = 0; iter < iterations; iter++)
        {
            List<Vector3> smoothed = new List<Vector3>();
            smoothed.Add(currentPath[0]); // Keep the first point
            
            for (int i = 0; i < currentPath.Count - 1; i++)
            {
                Vector3 p0 = currentPath[i];
                Vector3 p1 = currentPath[i + 1];
                
                // Cut corners at 25% and 75% to smooth zig-zags into curves/diagonals
                smoothed.Add(Vector3.Lerp(p0, p1, 0.25f));
                smoothed.Add(Vector3.Lerp(p0, p1, 0.75f));
            }
            
            smoothed.Add(currentPath[currentPath.Count - 1]); // Keep the last point
            currentPath = smoothed;
        }
        
        return currentPath;
    }

    private float GetTotalPathDistance(List<Vector3> path)
    {
        float dist = 0f;
        for (int i = 0; i < path.Count - 1; i++)
            dist += Vector3.Distance(path[i], path[i+1]);
        return dist;
    }

    private int GetSegmentIndexAtDistance(List<Vector3> path, float targetDist)
    {
        float dist = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float segLen = Vector3.Distance(path[i], path[i+1]);
            if (targetDist <= dist + segLen)
                return i;
            dist += segLen;
        }
        return Mathf.Max(0, path.Count - 2);
    }

    private Vector3 GetPointAtDistance(List<Vector3> path, float targetDist)
    {
        if (path == null || path.Count == 0) return Vector3.zero;
        if (path.Count == 1 || targetDist <= 0f) return path[0];

        float dist = 0f;
        for (int i = 0; i < path.Count - 1; i++)
        {
            float segLen = Vector3.Distance(path[i], path[i + 1]);
            if (targetDist <= dist + segLen)
            {
                float t = (targetDist - dist) / segLen;
                return Vector3.Lerp(path[i], path[i + 1], t);
            }
            dist += segLen;
        }
        return path[path.Count - 1];
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

public class ArrowAnimator : MonoBehaviour
{
    public float speed = 0.5f;
    public float spacing = 0.35f;
    private Vector3 initialPosition;

    void Start()
    {
        initialPosition = transform.position;
    }

    void Update()
    {
        // Move forward along the local Z axis, wrapping around exactly at the spacing distance
        float offset = (Time.time * speed) % spacing;
        transform.position = initialPosition + transform.forward * offset;
    }
}
