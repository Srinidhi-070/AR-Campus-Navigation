using UnityEditor;
using UnityEngine;
using System.IO;

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

    public static void BuildAPKCommandLine()
    {
        string buildPath = GetCommandLineArgument("-buildPath");
        if (string.IsNullOrEmpty(buildPath))
            buildPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Builds", "ARCampusNav.apk"));

        string directory = Path.GetDirectoryName(buildPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        Debug.Log($"[Build] Building to: {buildPath}");

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
            Debug.Log($"[Build] Success. Size: {report.summary.totalSize / (1024 * 1024)} MB");
            EditorApplication.Exit(0);
            return;
        }

        Debug.LogError($"[Build] Failed: {report.summary.result}");
        EditorApplication.Exit(1);
    }

    private static string GetCommandLineArgument(string name)
    {
        string[] args = System.Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == name)
                return args[i + 1];
        }

        return null;
    }
    
    [MenuItem("AR Campus/Generate UI Icons")]
    public static void GenerateIcons()
    {
        IconGenerator.GenerateIcons();
    }
    
    [MenuItem("AR Campus/Open Floor Map Editor")]
    public static void OpenFloorMapEditor()
    {
        EditorWindow.GetWindow(System.Type.GetType("FloorMapEditor"));
    }
}
