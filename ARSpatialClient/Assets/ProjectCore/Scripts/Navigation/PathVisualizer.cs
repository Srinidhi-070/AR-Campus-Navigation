using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float spacing = 0.3f;

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();
    private Material arrowMaterial;
    public bool HasActivePath => spawnedArrows.Count > 0;

    void Awake()
    {
        // Auto-load arrow prefab if not assigned
        if (arrowPrefab == null)
        {
            arrowPrefab = Resources.Load<GameObject>("Prefabs/ArrowPrefab");
            if (arrowPrefab != null)
            {
                Debug.Log("[PathVisualizer] Arrow prefab loaded from Resources");
            }
            else
            {
                Debug.LogWarning("[PathVisualizer] Arrow prefab not found in Resources/Prefabs/. Creating fallback arrow.");
                CreateFallbackArrow();
            }
        }

        EnsureArrowMaterial();
    }

    private void CreateFallbackArrow()
    {
        // Create a simple arrow using primitives
        GameObject arrow = new GameObject("FallbackArrow");
        
        // Create cone for arrow head
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.SetParent(arrow.transform, false);
        cone.transform.localPosition = new Vector3(0, 0, 0.2f);
        cone.transform.localRotation = Quaternion.Euler(90, 0, 0); // Pointing forward (Z)
        cone.transform.localScale = new Vector3(0.15f, 0.05f, 0.15f);
        
        // Create cylinder for arrow shaft
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.transform.SetParent(arrow.transform, false);
        shaft.transform.localPosition = Vector3.zero;
        shaft.transform.localRotation = Quaternion.Euler(90, 0, 0); // Pointing forward (Z)
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
        arrow.SetActive(false); // Hide the template
        DontDestroyOnLoad(arrow); // Keep it alive
        
        Debug.Log("[PathVisualizer] Fallback arrow created (Oriented to Z-Forward)");
    }

    private Material EnsureArrowMaterial()
    {
        if (arrowMaterial != null)
            return arrowMaterial;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogError("[PathVisualizer] Could not find a shader for AR arrows.");
            return null;
        }

        arrowMaterial = new Material(shader);
        Color color = new Color(0f, 0.95f, 1f, 1f);
        if (arrowMaterial.HasProperty("_BaseColor"))
            arrowMaterial.SetColor("_BaseColor", color);
        if (arrowMaterial.HasProperty("_Color"))
            arrowMaterial.SetColor("_Color", color);
        arrowMaterial.color = color;
        return arrowMaterial;
    }

    private void ApplyArrowMaterial(GameObject arrowInstance)
    {
        if (arrowInstance == null)
            return;

        Material material = EnsureArrowMaterial();
        if (material == null)
            return;

        foreach (Renderer renderer in arrowInstance.GetComponentsInChildren<Renderer>(true))
            renderer.material = material;
    }

    public void DrawPath(List<Vector3> worldPath)
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

        Debug.Log($"[PathVisualizer] Drawing path with {worldPath.Count} waypoints, spacing={spacing}");

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 start = worldPath[i];
            Vector3 end = worldPath[i + 1];
            float distance = Vector3.Distance(start, end);
            if (distance < 0.01f)
                continue;

            Vector3 dir = (end - start) / distance;
            int steps = Mathf.Max(1, Mathf.FloorToInt(distance / spacing));

            for (int j = 0; j < steps; j++)
            {
                Vector3 pos = Vector3.Lerp(start, end, j / (float)steps);
                GameObject arrowInstance = Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir), transform);
                ApplyArrowMaterial(arrowInstance);
                arrowInstance.SetActive(true); // Ensure arrow is visible
                spawnedArrows.Add(arrowInstance);
            }
        }

        Vector3 lastSegment = worldPath[worldPath.Count - 1] - worldPath[worldPath.Count - 2];
        Vector3 lastDir = lastSegment.sqrMagnitude > 0.0001f ? lastSegment.normalized : Vector3.forward;
        GameObject lastArrow = Instantiate(
            arrowPrefab,
            worldPath[worldPath.Count - 1],
            Quaternion.LookRotation(lastDir),
            transform);
        ApplyArrowMaterial(lastArrow);
        lastArrow.SetActive(true);
        spawnedArrows.Add(lastArrow);
        
        Debug.Log($"[PathVisualizer] ✅ Spawned {spawnedArrows.Count} arrows along path");
    }

    public void ClearPath()
    {
        foreach (var obj in spawnedArrows)
        {
            if (obj != null) Destroy(obj);
        }
        spawnedArrows.Clear();
    }
}
