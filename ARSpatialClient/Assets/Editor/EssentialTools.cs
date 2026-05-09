using UnityEditor;
using UnityEngine;

/// <summary>
/// Essential tools for AR Campus Navigation project.
/// Replaces all scattered tool scripts with one clean menu.
/// </summary>
public class EssentialTools : EditorWindow
{
    [MenuItem("AR Campus/Build APK")]
    public static void BuildAPK()
    {
        // Get build path
        string buildPath = EditorUtility.SaveFilePanel(
            "Save APK",
            "d:\\AR_Spatial_Client\\Builds",
            "ARCampusNav",
            "apk"
        );
        
        if (string.IsNullOrEmpty(buildPath))
        {
            Debug.Log("[Build] Cancelled by user");
            return;
        }
        
        Debug.Log($"[Build] Building to: {buildPath}");
        
        // Build options (no auto-run to avoid errors)
        BuildPlayerOptions buildOptions = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/ProjectCore/Scenes/CampusNavigation.unity" },
            locationPathName = buildPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };
        
        var report = BuildPipeline.BuildPlayer(buildOptions);
        
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log($"[Build] ✅ Success! Size: {report.summary.totalSize / (1024 * 1024)} MB");
            
            EditorUtility.DisplayDialog(
                "Build Successful",
                $"✅ APK built successfully!\\n\\n" +
                $"Location: {buildPath}\\n" +
                $"Size: {report.summary.totalSize / (1024 * 1024)} MB\\n\\n" +
                $"Install with:\\n" +
                $"adb install -r \"{buildPath}\"",
                "OK"
            );
            
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogError($"[Build] ❌ Failed: {report.summary.result}");
            EditorUtility.DisplayDialog("Build Failed", $"❌ Build failed!\\n\\nCheck Console for errors.", "OK");
        }
    }
    
    [MenuItem("AR Campus/Generate UI Icons")]
    public static void GenerateIcons()
    {
        // Call existing IconGenerator
        var iconGenType = System.Type.GetType("IconGenerator");
        if (iconGenType != null)
        {
            var method = iconGenType.GetMethod("GenerateAllIcons", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            if (method != null)
            {
                method.Invoke(null, null);
                return;
            }
        }
        
        EditorUtility.DisplayDialog("Icon Generator", "Icon generator not found.\\n\\nCheck if IconGenerator.cs exists.", "OK");
    }
    
    [MenuItem("AR Campus/Open Floor Map Editor")]
    public static void OpenFloorMapEditor()
    {
        EditorWindow.GetWindow(System.Type.GetType("FloorMapEditor"));
    }
}
