using System.Collections.Generic;
using UnityEngine;

public class PathVisualizer : MonoBehaviour
{
    [SerializeField] private GameObject arrowPrefab;
    [SerializeField] private float spacing = 0.3f;

    private readonly List<GameObject> spawnedArrows = new List<GameObject>();
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
    }

    private void CreateFallbackArrow()
    {
        // Create a simple arrow using primitives
        GameObject arrow = new GameObject("FallbackArrow");
        
        // Create cone for arrow head
        GameObject cone = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cone.transform.SetParent(arrow.transform, false);
        cone.transform.localPosition = new Vector3(0, 0, 0.15f);
        cone.transform.localRotation = Quaternion.Euler(90, 0, 0);
        cone.transform.localScale = new Vector3(0.15f, 0.1f, 0.15f);
        
        // Create cylinder for arrow shaft
        GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        shaft.transform.SetParent(arrow.transform, false);
        shaft.transform.localPosition = Vector3.zero;
        shaft.transform.localRotation = Quaternion.Euler(90, 0, 0);
        shaft.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
        
        // Set bright cyan color
        Material arrowMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        arrowMat.color = new Color(0f, 0.83f, 0.88f, 1f); // Cyan
        cone.GetComponent<Renderer>().material = arrowMat;
        shaft.GetComponent<Renderer>().material = arrowMat;
        
        // Remove colliders (not needed for visual arrows)
        Destroy(cone.GetComponent<Collider>());
        Destroy(shaft.GetComponent<Collider>());
        
        arrowPrefab = arrow;
        arrow.SetActive(false); // Hide the template
        DontDestroyOnLoad(arrow); // Keep it alive
        
        Debug.Log("[PathVisualizer] Fallback arrow created");
    }

    public void DrawPath(List<NavigationNode> path)
    {
        List<Vector3> positions = new List<Vector3>();
        if (path != null)
        {
            foreach (NavigationNode node in path)
                positions.Add(node.Position);
        }

        DrawPath(positions);
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

        for (int i = 0; i < worldPath.Count - 1; i++)
        {
            Vector3 start = worldPath[i];
            Vector3 end = worldPath[i + 1];
            Vector3 dir = (end - start).normalized;
            int steps = Mathf.Max(1, Mathf.FloorToInt(Vector3.Distance(start, end) / spacing));

            for (int j = 0; j < steps; j++)
            {
                Vector3 pos = Vector3.Lerp(start, end, j / (float)steps);
                spawnedArrows.Add(Instantiate(arrowPrefab, pos, Quaternion.LookRotation(dir), transform));
            }
        }

        Vector3 lastDir = (worldPath[worldPath.Count - 1] - worldPath[worldPath.Count - 2]).normalized;
        spawnedArrows.Add(Instantiate(
            arrowPrefab,
            worldPath[worldPath.Count - 1],
            Quaternion.LookRotation(lastDir),
            transform));
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
