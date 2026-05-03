using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Simplified scene builder that works without AR Foundation.
/// Creates a basic scene with CampusApp and a regular camera.
/// Run via: Tools → Build Simple Campus Scene
/// </summary>
public class SimpleSceneBuilder : EditorWindow
{
    [MenuItem("Tools/Build Simple Campus Scene")]
    public static void BuildSimpleScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Build Simple Scene",
            "This will DELETE all GameObjects in the current scene and rebuild from scratch.\n\n" +
            "This version creates a BASIC scene without AR components.\n" +
            "Perfect for testing the UI in the editor!\n\n" +
            "Continue?",
            "Yes, Build Scene",
            "Cancel"))
        {
            return;
        }

        Debug.Log("[SimpleSceneBuilder] Starting simple scene build...");

        // Step 1: Delete everything
        DeleteAllGameObjects();

        // Step 2: Create core systems
        CreateCampusApp();
        CreateBasicCamera();
        CreateLighting();

        // Step 3: Save scene
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        Debug.Log("[SimpleSceneBuilder] ✓ Simple scene built successfully!");
        
        EditorUtility.DisplayDialog(
            "Scene Built Successfully!",
            "Simple CampusNavigation scene created!\n\n" +
            "GameObjects created:\n" +
            "• CampusApp (with CampusRuntimeInstaller)\n" +
            "• Main Camera (basic camera for testing)\n" +
            "• Directional Light\n\n" +
            "This is a BASIC scene for testing UI in the editor.\n" +
            "For AR on device, you'll need to add AR components manually.\n\n" +
            "Press Play to test the UI!",
            "OK");
    }

    private static void DeleteAllGameObjects()
    {
        Debug.Log("[SimpleSceneBuilder] Deleting all existing GameObjects...");
        
        // Get all root GameObjects
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        GameObject[] rootObjects = scene.GetRootGameObjects();
        int count = rootObjects.Length;
        
        // Delete them
        foreach (GameObject obj in rootObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        
        Debug.Log($"[SimpleSceneBuilder] Deleted {count} GameObjects");
    }

    private static void CreateCampusApp()
    {
        Debug.Log("[SimpleSceneBuilder] Creating CampusApp...");
        
        GameObject campusApp = new GameObject("CampusApp");
        campusApp.AddComponent<CampusRuntimeInstaller>();
        
        Debug.Log("[SimpleSceneBuilder] ✓ CampusApp created with CampusRuntimeInstaller");
    }

    private static void CreateBasicCamera()
    {
        Debug.Log("[SimpleSceneBuilder] Creating Main Camera...");
        
        GameObject cameraObj = new GameObject("Main Camera");
        Camera cam = cameraObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.2f, 0.2f, 0.2f); // Dark gray
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0, 1.5f, -5f);
        cameraObj.transform.rotation = Quaternion.identity;
        
        // Add audio listener
        cameraObj.AddComponent<AudioListener>();
        
        Debug.Log("[SimpleSceneBuilder] ✓ Main Camera created");
    }

    private static void CreateLighting()
    {
        Debug.Log("[SimpleSceneBuilder] Creating lighting...");
        
        GameObject light = new GameObject("Directional Light");
        Light lightComp = light.AddComponent<Light>();
        lightComp.type = LightType.Directional;
        lightComp.intensity = 1f;
        lightComp.color = Color.white;
        light.transform.rotation = Quaternion.Euler(50, -30, 0);
        
        Debug.Log("[SimpleSceneBuilder] ✓ Directional Light created");
    }

    [MenuItem("Tools/Validate Scene (Simple)")]
    public static void ValidateSimpleScene()
    {
        string report = "=== Simple Scene Validation Report ===\n\n";
        bool isValid = true;

        // Check for CampusApp
        GameObject campusApp = GameObject.Find("CampusApp");
        if (campusApp != null && campusApp.GetComponent<CampusRuntimeInstaller>() != null)
        {
            report += "✓ CampusApp with CampusRuntimeInstaller found\n";
        }
        else
        {
            report += "✗ CampusApp with CampusRuntimeInstaller MISSING\n";
            isValid = false;
        }

        // Check for Main Camera
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            report += "✓ Main Camera found\n";
        }
        else
        {
            report += "✗ Main Camera MISSING\n";
            isValid = false;
        }

        // Check for Light
        Light light = FindObjectOfType<Light>();
        if (light != null)
        {
            report += "✓ Directional Light found\n";
        }
        else
        {
            report += "⚠ No light found (scene will be dark)\n";
        }

        // Check for legacy components
        report += "\n=== Legacy Components Check ===\n";
        
        string[] legacyComponents = {
            "UIBuilder", "Canvas", "IndoorMap", "GridOrigin", 
            "IndoorNavBridge", "Managers", "ModernUIBuilder"
        };

        bool hasLegacy = false;
        foreach (string legacyName in legacyComponents)
        {
            GameObject legacy = GameObject.Find(legacyName);
            if (legacy != null)
            {
                report += $"⚠ Legacy component found: {legacyName} (should be removed)\n";
                hasLegacy = true;
            }
        }

        if (!hasLegacy)
        {
            report += "✓ No legacy components found\n";
        }

        // Summary
        report += "\n=== Summary ===\n";
        if (isValid && !hasLegacy)
        {
            report += "✓ Scene is correctly configured!\n";
            report += "\nPress Play to test the UI.\n";
        }
        else
        {
            report += "✗ Scene has issues. Run 'Tools → Build Simple Campus Scene' to fix.\n";
        }

        Debug.Log(report);
        
        EditorUtility.DisplayDialog(
            isValid && !hasLegacy ? "Scene Valid ✓" : "Scene Issues Found ✗",
            report,
            "OK");
    }

    [MenuItem("Tools/Clean Scene (Delete All)")]
    public static void CleanScene()
    {
        if (!EditorUtility.DisplayDialog(
            "Clean Scene",
            "This will DELETE ALL GameObjects in the current scene.\n\n" +
            "Are you sure?",
            "Yes, Delete All",
            "Cancel"))
        {
            return;
        }

        DeleteAllGameObjects();
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        
        EditorUtility.DisplayDialog(
            "Scene Cleaned",
            "All GameObjects deleted.\n\n" +
            "Now run 'Tools → Build Simple Campus Scene' to create a clean scene.",
            "OK");
    }
}
#endif
