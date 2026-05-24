using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class ArrowGeneratorTool
{
    static ArrowGeneratorTool()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorPrefs.GetBool("ArrowGeneratorRunOnce", false)) return;
        EditorPrefs.SetBool("ArrowGeneratorRunOnce", true);

        GenerateStraightArrow();
        GenerateCurvedArrow();

        Debug.Log("[ArrowGeneratorTool] Successfully generated 3D arrow meshes and prefabs!");

        // Delete script
        EditorApplication.delayCall += () => {
            AssetDatabase.DeleteAsset("Assets/ProjectCore/Scripts/Editor/ArrowGeneratorTool.cs");
            Debug.Log("[ArrowGeneratorTool] Script deleted itself as requested.");
        };
    }

    private static void GenerateStraightArrow()
    {
        Mesh mesh = CreateStraightArrowMesh(2.0f, 0.4f, 0.8f, 0.8f, 0.1f);
        SaveAssetAndPrefab(mesh, "ProceduralArrow");
    }

    private static void GenerateCurvedArrow()
    {
        Mesh mesh = CreateCurvedArrowMesh(2.0f, 0.4f, 0.8f, 0.8f, 0.1f);
        SaveAssetAndPrefab(mesh, "ProceduralCurvedArrow");
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

        // Try to assign the arrow material
        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/ArrowMaterial.mat"); // Adjust if needed
        if (mat != null) mr.sharedMaterial = mat;

        string prefabPath = $"{prefabsDir}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }

    private static Mesh CreateStraightArrowMesh(float stemLength, float stemWidth, float tipLength, float tipWidth, float thickness)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float hw = stemWidth / 2f;
        float ht = thickness / 2f;

        // Stem Box
        AddBox(verts, tris, new Vector3(-hw, -ht, 0), new Vector3(hw, ht, stemLength));

        // Tip Pyramid
        float htw = tipWidth / 2f;
        Vector3 tipBaseCenter = new Vector3(0, 0, stemLength);
        
        // Base of tip
        Vector3 bl = tipBaseCenter + new Vector3(-htw, -ht, 0);
        Vector3 br = tipBaseCenter + new Vector3(htw, -ht, 0);
        Vector3 tl = tipBaseCenter + new Vector3(-htw, ht, 0);
        Vector3 tr = tipBaseCenter + new Vector3(htw, ht, 0);
        
        // Apex of tip
        Vector3 apex = tipBaseCenter + new Vector3(0, 0, tipLength);

        int startIndex = verts.Count;
        verts.AddRange(new[] { bl, br, tl, tr, apex });

        // Base face
        AddQuad(tris, startIndex, startIndex + 1, startIndex + 3, startIndex + 2);
        // Bottom face
        AddTriangle(tris, startIndex, startIndex + 1, startIndex + 4);
        // Top face
        AddTriangle(tris, startIndex + 3, startIndex + 2, startIndex + 4);
        // Left face
        AddTriangle(tris, startIndex + 2, startIndex, startIndex + 4);
        // Right face
        AddTriangle(tris, startIndex + 1, startIndex + 3, startIndex + 4);

        Mesh mesh = new Mesh { name = "StraightArrow" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    private static Mesh CreateCurvedArrowMesh(float curveRadius, float stemWidth, float tipLength, float tipWidth, float thickness)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float hw = stemWidth / 2f;
        float ht = thickness / 2f;
        int segments = 12;

        // Curve sweeps from 0 to 90 degrees to the right
        // Center of arc is at X = curveRadius, Z = 0
        Vector3 center = new Vector3(curveRadius, 0, 0);

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI / 2f; // 0 to 90 deg in radians
            // At angle 0, cos=1, sin=0 -> Z=0, X=0
            float x = center.x - Mathf.Cos(angle) * curveRadius;
            float z = center.z + Mathf.Sin(angle) * curveRadius;
            
            // Direction tangent to the curve
            Vector3 forward = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
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
                // Bottom
                AddQuad(tris, prev, curr, curr + 1, prev + 1);
                // Top
                AddQuad(tris, prev + 3, curr + 3, curr + 2, prev + 2);
                // Left
                AddQuad(tris, prev + 2, curr + 2, curr, prev);
                // Right
                AddQuad(tris, prev + 1, curr + 1, curr + 3, prev + 3);
            }
        }

        // Back cap
        AddQuad(tris, 2, 3, 1, 0);
        // Front cap (where tip connects)
        int last = segments * 4;
        AddQuad(tris, last, last + 1, last + 3, last + 2);

        // Tip Pyramid
        float htw = tipWidth / 2f;
        
        float endAngle = Mathf.PI / 2f;
        float tipX = center.x - Mathf.Cos(endAngle) * curveRadius;
        float tipZ = center.z + Mathf.Sin(endAngle) * curveRadius;
        Vector3 tipBaseCenter = new Vector3(tipX, 0, tipZ);
        
        Vector3 tForward = new Vector3(Mathf.Sin(endAngle), 0, Mathf.Cos(endAngle));
        Vector3 tRight = new Vector3(Mathf.Cos(endAngle), 0, -Mathf.Sin(endAngle));

        Vector3 bl = tipBaseCenter - tRight * htw - Vector3.up * ht;
        Vector3 br = tipBaseCenter + tRight * htw - Vector3.up * ht;
        Vector3 tl = tipBaseCenter - tRight * htw + Vector3.up * ht;
        Vector3 tr = tipBaseCenter + tRight * htw + Vector3.up * ht;
        Vector3 apex = tipBaseCenter + tForward * tipLength;

        int startIndex = verts.Count;
        verts.AddRange(new[] { bl, br, tl, tr, apex });

        // Base face
        AddQuad(tris, startIndex + 2, startIndex + 3, startIndex + 1, startIndex);
        // Bottom face
        AddTriangle(tris, startIndex, startIndex + 1, startIndex + 4);
        // Top face
        AddTriangle(tris, startIndex + 3, startIndex + 2, startIndex + 4);
        // Left face
        AddTriangle(tris, startIndex + 2, startIndex, startIndex + 4);
        // Right face
        AddTriangle(tris, startIndex + 1, startIndex + 3, startIndex + 4);

        Mesh mesh = new Mesh { name = "CurvedArrow" };
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();
        return mesh;
    }

    private static void AddBox(List<Vector3> verts, List<int> tris, Vector3 min, Vector3 max)
    {
        int startIndex = verts.Count;
        Vector3[] corners = {
            new Vector3(min.x, min.y, min.z), // 0
            new Vector3(max.x, min.y, min.z), // 1
            new Vector3(min.x, max.y, min.z), // 2
            new Vector3(max.x, max.y, min.z), // 3
            new Vector3(min.x, min.y, max.z), // 4
            new Vector3(max.x, min.y, max.z), // 5
            new Vector3(min.x, max.y, max.z), // 6
            new Vector3(max.x, max.y, max.z)  // 7
        };
        verts.AddRange(corners);

        // Back
        AddQuad(tris, startIndex + 2, startIndex + 3, startIndex + 1, startIndex);
        // Front
        AddQuad(tris, startIndex + 4, startIndex + 5, startIndex + 7, startIndex + 6);
        // Bottom
        AddQuad(tris, startIndex, startIndex + 1, startIndex + 5, startIndex + 4);
        // Top
        AddQuad(tris, startIndex + 6, startIndex + 7, startIndex + 3, startIndex + 2);
        // Left
        AddQuad(tris, startIndex + 2, startIndex, startIndex + 4, startIndex + 6);
        // Right
        AddQuad(tris, startIndex + 1, startIndex + 3, startIndex + 7, startIndex + 5);
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
