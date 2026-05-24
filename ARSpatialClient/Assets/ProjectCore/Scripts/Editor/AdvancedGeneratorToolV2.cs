using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[InitializeOnLoad]
public class AdvancedGeneratorToolV2
{
    static AdvancedGeneratorToolV2()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (EditorPrefs.GetBool("AdvancedGeneratorV2RunOnce", false)) return;
        EditorPrefs.SetBool("AdvancedGeneratorV2RunOnce", true);

        GenerateMapPinDestination();
        GenerateStairsArrow();

        Debug.Log("[AdvancedGeneratorToolV2] Successfully generated Map Pin and Stairs prefabs!");

        // Delete script
        EditorApplication.delayCall += () => {
            AssetDatabase.DeleteAsset("Assets/ProjectCore/Scripts/Editor/AdvancedGeneratorToolV2.cs");
        };
    }

    private static void GenerateMapPinDestination()
    {
        string prefabsDir = "Assets/ProjectCore/Resources/Prefabs";
        if (!AssetDatabase.IsValidFolder(prefabsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore/Resources", "Prefabs");

        GameObject root = new GameObject("ProceduralDestination_V2");

        // The Top Sphere
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "PinSphere";
        sphere.transform.SetParent(root.transform);
        sphere.transform.localPosition = new Vector3(0, 1.8f, 0);
        sphere.transform.localScale = new Vector3(1f, 1f, 1f);
        GameObject.DestroyImmediate(sphere.GetComponent<Collider>());

        // The Bottom Cone
        GameObject cone = new GameObject("PinCone");
        cone.transform.SetParent(root.transform);
        cone.transform.localPosition = new Vector3(0, 1.4f, 0);
        MeshFilter mf = cone.AddComponent<MeshFilter>();
        MeshRenderer mr = cone.AddComponent<MeshRenderer>();
        mf.sharedMesh = CreateConeMesh(0.48f, 1.2f, 16);

        // Apply materials
        Material destMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/DestinationMaterial.mat");
        if (destMat != null)
        {
            sphere.GetComponent<Renderer>().sharedMaterial = destMat;
            mr.sharedMaterial = destMat;
        }

        string prefabPath = $"{prefabsDir}/ProceduralDestination_V2.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        GameObject.DestroyImmediate(root);
    }

    private static Mesh CreateConeMesh(float bottomRadius, float height, int segments)
    {
        Mesh mesh = new Mesh { name = "Cone" };
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        Vector3 tip = new Vector3(0, -height, 0);
        Vector3 baseCenter = Vector3.zero;

        verts.Add(tip); // 0
        verts.Add(baseCenter); // 1

        for (int i = 0; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle) * bottomRadius;
            float z = Mathf.Sin(angle) * bottomRadius;
            verts.Add(new Vector3(x, 0, z));
        }

        for (int i = 0; i < segments; i++)
        {
            int current = i + 2;
            int next = i + 3;

            // Side triangle
            tris.Add(0);
            tris.Add(next);
            tris.Add(current);

            // Base triangle
            tris.Add(1);
            tris.Add(current);
            tris.Add(next);
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        string modelsDir = "Assets/ProjectCore/Models";
        if (!AssetDatabase.IsValidFolder(modelsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore", "Models");
        AssetDatabase.CreateAsset(mesh, $"{modelsDir}/ConeMesh.asset");

        return mesh;
    }

    private static void GenerateStairsArrow()
    {
        Mesh mesh = CreateStairsArrowMesh(3, 0.4f, 0.4f, 0.2f, 0.6f, 1.0f);
        
        string modelsDir = "Assets/ProjectCore/Models";
        string prefabsDir = "Assets/ProjectCore/Resources/Prefabs";

        if (!AssetDatabase.IsValidFolder(modelsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore", "Models");
        if (!AssetDatabase.IsValidFolder(prefabsDir)) AssetDatabase.CreateFolder("Assets/ProjectCore/Resources", "Prefabs");

        string meshPath = $"{modelsDir}/ProceduralStairs_mesh.asset";
        AssetDatabase.CreateAsset(mesh, meshPath);
        AssetDatabase.SaveAssets();

        GameObject go = new GameObject("ProceduralStairs");
        MeshFilter mf = go.AddComponent<MeshFilter>();
        MeshRenderer mr = go.AddComponent<MeshRenderer>();
        mf.sharedMesh = mesh;

        Material mat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/Materials/StaircaseMaterial.mat");
        if (mat != null) mr.sharedMaterial = mat;

        string prefabPath = $"{prefabsDir}/ProceduralStairs.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
        GameObject.DestroyImmediate(go);
    }

    private static Mesh CreateStairsArrowMesh(int numSteps, float stepLength, float stepWidth, float stepHeight, float tipLength, float tipWidth)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        float hw = stepWidth / 2f;

        // Generate Steps
        for (int i = 0; i < numSteps; i++)
        {
            float zStart = i * stepLength;
            float zEnd = (i + 1) * stepLength;
            float yTop = (i + 1) * stepHeight;
            float yBottom = i * stepHeight;

            Vector3 bl = new Vector3(-hw, yBottom, zStart);
            Vector3 br = new Vector3(hw, yBottom, zStart);
            Vector3 tl = new Vector3(-hw, yTop, zStart);
            Vector3 tr = new Vector3(hw, yTop, zStart);
            Vector3 tbl = new Vector3(-hw, yTop, zEnd);
            Vector3 tbr = new Vector3(hw, yTop, zEnd);
            Vector3 bbl = new Vector3(-hw, yBottom, zEnd);
            Vector3 bbr = new Vector3(hw, yBottom, zEnd);

            int start = verts.Count;
            verts.AddRange(new[] { bl, br, tl, tr, tbl, tbr, bbl, bbr });

            // Riser (front)
            AddQuad(tris, start, start + 1, start + 3, start + 2);
            // Tread (top)
            AddQuad(tris, start + 2, start + 3, start + 5, start + 4);
            // Left
            AddQuad(tris, start, start + 2, start + 4, start + 6);
            // Right
            AddQuad(tris, start + 1, start + 7, start + 5, start + 3);
            // Bottom
            AddQuad(tris, start, start + 6, start + 7, start + 1);
            // Back (only if it's the last step and before tip, wait, tip will cover it)
            if (i == numSteps - 1)
            {
                // We leave the back open to attach the tip seamlessly
            }
        }

        // Generate Arrow Tip at the top step
        float tipZStart = numSteps * stepLength;
        float tipZEnd = tipZStart + tipLength;
        float tipYTop = numSteps * stepHeight;
        float tipYBottom = (numSteps - 1) * stepHeight;
        float htw = tipWidth / 2f;

        Vector3 tip_bl = new Vector3(-htw, tipYBottom, tipZStart);
        Vector3 tip_br = new Vector3(htw, tipYBottom, tipZStart);
        Vector3 tip_tl = new Vector3(-htw, tipYTop, tipZStart);
        Vector3 tip_tr = new Vector3(htw, tipYTop, tipZStart);
        
        Vector3 tip_apex_bottom = new Vector3(0, tipYBottom, tipZEnd);
        Vector3 tip_apex_top = new Vector3(0, tipYTop, tipZEnd);

        int ts = verts.Count;
        verts.AddRange(new[] { tip_bl, tip_br, tip_tl, tip_tr, tip_apex_bottom, tip_apex_top });

        // Base face (back of tip)
        AddQuad(tris, ts, ts + 2, ts + 3, ts + 1);
        // Bottom face
        AddTriangle(tris, ts, ts + 1, ts + 4);
        // Top face
        AddTriangle(tris, ts + 2, ts + 5, ts + 3);
        // Left face
        AddQuad(tris, ts, ts + 4, ts + 5, ts + 2);
        // Right face
        AddQuad(tris, ts + 1, ts + 3, ts + 5, ts + 4);

        Mesh mesh = new Mesh { name = "StairsArrow" };
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
