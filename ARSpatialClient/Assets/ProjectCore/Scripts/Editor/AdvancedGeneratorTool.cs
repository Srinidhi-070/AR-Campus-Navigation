using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class AdvancedGeneratorTool
{
    static AdvancedGeneratorTool()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorPrefs.GetBool("AdvancedGeneratorRunOnce", false)) return;
        EditorPrefs.SetBool("AdvancedGeneratorRunOnce", true);

        GenerateCurvedArrow(45f, "ProceduralCurvedArrow_45");
        GenerateCurvedArrow(90f, "ProceduralCurvedArrow_90");
        GenerateCurvedArrow(135f, "ProceduralCurvedArrow_135");
        GenerateDestinationMarker();

        Debug.Log("[AdvancedGeneratorTool] Successfully generated advanced meshes and prefabs!");

        // Delete script
        EditorApplication.delayCall += () => {
            AssetDatabase.DeleteAsset("Assets/ProjectCore/Scripts/Editor/AdvancedGeneratorTool.cs");
        };
    }

    private static void GenerateCurvedArrow(float angleDegrees, string name)
    {
        Mesh mesh = CreateCurvedArrowMesh(2.0f, 0.4f, 0.8f, 0.8f, 0.1f, angleDegrees);
        SaveAssetAndPrefab(mesh, name);
    }

    private static void GenerateDestinationMarker()
    {
        string prefabsDir = "Assets/ProjectCore/Resources/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore/Resources", "Prefabs");

        GameObject root = new GameObject("ProceduralDestination");

        // Base Ring (flattened cylinder)
        GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "BaseRing";
        ring.transform.SetParent(root.transform);
        ring.transform.localPosition = new Vector3(0, 0.05f, 0);
        ring.transform.localScale = new Vector3(1.5f, 0.05f, 1.5f);
        GameObject.DestroyImmediate(ring.GetComponent<Collider>());

        // Inner Dot (smaller flattened cylinder)
        GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dot.name = "InnerDot";
        dot.transform.SetParent(root.transform);
        dot.transform.localPosition = new Vector3(0, 0.06f, 0);
        dot.transform.localScale = new Vector3(0.5f, 0.05f, 0.5f);
        GameObject.DestroyImmediate(dot.GetComponent<Collider>());

        // Floating Arrow Stem (cylinder)
        GameObject stem = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        stem.name = "ArrowStem";
        stem.transform.SetParent(root.transform);
        stem.transform.localPosition = new Vector3(0, 1.5f, 0);
        stem.transform.localScale = new Vector3(0.3f, 0.5f, 0.3f);
        GameObject.DestroyImmediate(stem.GetComponent<Collider>());

        // Floating Arrow Tip (cone, pointing down)
        // Unity doesn't have a built-in cone primitive. We'll use a stretched sphere or imported cone.
        // For simplicity, a rotated pyramid or just a stretched sphere could work. 
        // A pyramid is easily made with a quad mesh script, or we can just use a sphere.
        // Actually, let's make a custom tip mesh for the arrow.
        GameObject tip = new GameObject("ArrowTip");
        tip.transform.SetParent(root.transform);
        tip.transform.localPosition = new Vector3(0, 0.8f, 0);
        // Pointing down
        tip.transform.localRotation = Quaternion.Euler(180, 0, 0);
        MeshFilter mf = tip.AddComponent<MeshFilter>();
        MeshRenderer mr = tip.AddComponent<MeshRenderer>();
        mf.sharedMesh = CreatePyramidMesh(0.8f, 0.8f);

        // Apply materials
        Material destMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/DestinationMaterial.mat");
        if (destMat != null)
        {
            ring.GetComponent<Renderer>().sharedMaterial = destMat;
            dot.GetComponent<Renderer>().sharedMaterial = destMat;
            stem.GetComponent<Renderer>().sharedMaterial = destMat;
            mr.sharedMaterial = destMat;
        }

        string prefabPath = $"{prefabsDir}/ProceduralDestination.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
    }

    private static Mesh CreatePyramidMesh(float width, float height)
    {
        Mesh mesh = new Mesh { name = "Pyramid" };
        float hw = width / 2f;
        Vector3 top = new Vector3(0, height, 0);
        Vector3 bl = new Vector3(-hw, 0, -hw);
        Vector3 br = new Vector3(hw, 0, -hw);
        Vector3 fl = new Vector3(-hw, 0, hw);
        Vector3 fr = new Vector3(hw, 0, hw);

        mesh.vertices = new Vector3[] { bl, br, fl, fr, top };
        mesh.triangles = new int[] {
            // Base
            0, 2, 1,
            2, 3, 1,
            // Back
            0, 1, 4,
            // Right
            1, 3, 4,
            // Front
            3, 2, 4,
            // Left
            2, 0, 4
        };
        mesh.RecalculateNormals();
        
        string modelsDir = "Assets/ProjectCore/Models";
        if (!AssetDatabase.IsValidFolder(modelsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore", "Models");
        AssetDatabase.CreateAsset(mesh, $"{modelsDir}/PyramidMesh.asset");
        return mesh;
    }

    private static void SaveAssetAndPrefab(Mesh mesh, string name)
    {
        string modelsDir = "Assets/ProjectCore/Models";
        string prefabsDir = "Assets/ProjectCore/Resources/Prefabs";

        if (!AssetDatabase.IsValidFolder(modelsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore", "Models");
        if (!AssetDatabase.IsValidFolder("Assets/ProjectCore/Resources")) AssetDatabase.CreateFolder("Assets/ProjectCore", "Resources");
        if (!AssetDatabase.IsValidFolder(prefabsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore/Resources", "Prefabs");

        string meshPath = $"{modelsDir}/{name}_mesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();

        GameObject go = new GameObject(name);
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/ArrowMaterial.mat");
        if (mat != null) mr.sharedMaterial = mat;

        string prefabPath = $"{prefabsDir}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }

    private static Mesh CreateCurvedArrowMesh(float curveRadius, float stemWidth, float tipLength, float tipWidth, float thickness, float angleDegrees)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float hw = stemWidth / 2f;
        float ht = thickness / 2f;
        int segments = Mathf.Max(6, Mathf.RoundToInt(12f * (angleDegrees / 90f)));

        float endAngleRad = angleDegrees * Mathf.Deg2Rad;
        Vector3 center = new Vector3(curveRadius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * endAngleRad; 
            float x = center.x - Mathf.Cos(angle) * curveRadius;
            float z = center.z + Mathf.Sin(angle) * curveRadius;
            
            Vector3 right = new Vector3(Mathf.Cos(angle), 0, -Mathf.Sin(angle));
            Vector3 pos = new Vector3(x, 0, z);

            verts.Add(pos - right * hw - Vector3.up * ht); // BL
            verts.Add(pos + right * hw - Vector3.up * ht); // BR
            verts.Add(pos - right * hw + Vector3.up * ht); // TL
            verts.Add(pos + right * hw + Vector3.up * ht); // TR

            if (i > 0)
            {
                int curr = i * 4;
                int prev = (i - 1) * 4;
                AddQuad(tris, prev, curr, curr + 1, prev + 1); // Bottom
                AddQuad(tris, prev + 3, curr + 3, curr + 2, prev + 2); // Top
                AddQuad(tris, prev + 2, curr + 2, curr, prev); // Left
                AddQuad(tris, prev + 1, curr + 1, curr + 3, prev + 3); // Right
            }
        }

        AddQuad(tris, 2, 3, 1, 0); // Back cap
        int last = segments * 4;
        AddQuad(tris, last, last + 1, last + 3, last + 2); // Front cap

        float htw = tipWidth / 2f;
        float tipX = center.x - Mathf.Cos(endAngleRad) * curveRadius;
        float tipZ = center.z + Mathf.Sin(endAngleRad) * curveRadius;
        Vector3 tipBaseCenter = new Vector3(tipX, 0, tipZ);
        
        Vector3 tForward = new Vector3(Mathf.Sin(endAngleRad), 0, Mathf.Cos(endAngleRad));
        Vector3 tRight = new Vector3(Mathf.Cos(endAngleRad), 0, -Mathf.Sin(endAngleRad));

        Vector3 bl = tipBaseCenter - tRight * htw - Vector3.up * ht;
        Vector3 br = tipBaseCenter + tRight * htw - Vector3.up * ht;
        Vector3 tl = tipBaseCenter - tRight * htw + Vector3.up * ht;
        Vector3 tr = tipBaseCenter + tRight * htw + Vector3.up * ht;
        Vector3 apex = tipBaseCenter + tForward * tipLength;

        int startIndex = verts.Count;
        verts.AddRange(new[] { bl, br, tl, tr, apex });

        AddQuad(tris, startIndex + 2, startIndex + 3, startIndex + 1, startIndex); // Base face
        AddTriangle(tris, startIndex, startIndex + 1, startIndex + 4); // Bottom face
        AddTriangle(tris, startIndex + 3, startIndex + 2, startIndex + 4); // Top face
        AddTriangle(tris, startIndex + 2, startIndex, startIndex + 4); // Left face
        AddTriangle(tris, startIndex + 1, startIndex + 3, startIndex + 4); // Right face

        Mesh mesh = new Mesh { name = $"CurvedArrow_{angleDegrees}" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void AddQuad(List<int> tris, int v0, int v1, int v2, int v3)
    {
        tris.Add(v0); tris.Add(v1); tris.Add(v2);
        tris.Add(v0); tris.Add(v2); tris.Add(v3);
    }

    private static void AddTriangle(List<int> tris, int v0, int v1, int v2)
    {
        tris.Add(v0); tris.Add(v1); tris.Add(v2);
    }
}
