using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// Validates that the AR Campus Navigation project is set up correctly.
/// Run via: Tools → Validate Project Setup
/// </summary>
public class ProjectValidator : EditorWindow
{
    [MenuItem("Tools/Validate Project Setup")]
    public static void ValidateProject()
    {
        bool allGood = true;
        string report = "=== AR Campus Navigation - Project Validation ===\n\n";

        // Check icons
        report += "ICONS:\n";
        string[] requiredIcons = { "menu", "qr", "close", "send" };
        foreach (string icon in requiredIcons)
        {
            string path = $"Assets/ProjectCore/Resources/Icons/{icon}.png";
            if (File.Exists(path))
                report += $"  ✓ {icon}.png exists\n";
            else
            {
                report += $"  ✗ {icon}.png MISSING - Run Tools → Generate UI Icons\n";
                allGood = false;
            }
        }

        // Check nodes.json
        report += "\nMAP DATA:\n";
        string nodesPath = "Assets/ProjectCore/Resources/nodes.json";
        if (File.Exists(nodesPath))
        {
            report += $"  ✓ nodes.json exists\n";
            string json = File.ReadAllText(nodesPath);
            if (json.Contains("\"nodes\""))
                report += $"  ✓ nodes.json has valid structure\n";
            else
            {
                report += $"  ⚠ nodes.json may be empty or invalid\n";
                allGood = false;
            }
        }
        else
        {
            report += $"  ✗ nodes.json MISSING - Export map from Floor Map Editor\n";
            allGood = false;
        }

        // Check key scripts
        report += "\nKEY SCRIPTS:\n";
        string[] requiredScripts = {
            "Assets/ProjectCore/Scripts/Core/CampusRuntimeInstaller.cs",
            "Assets/ProjectCore/Scripts/Core/AppController.cs",
            "Assets/ProjectCore/Scripts/UI/CampusRuntimeUI.cs",
            "Assets/ProjectCore/Scripts/Navigation/NavigationFlowController.cs",
            "Assets/ProjectCore/Scripts/AR/QRScanner.cs"
        };
        
        foreach (string script in requiredScripts)
        {
            if (File.Exists(script))
                report += $"  ✓ {Path.GetFileName(script)}\n";
            else
            {
                report += $"  ✗ {Path.GetFileName(script)} MISSING\n";
                allGood = false;
            }
        }

        // Check scene
        report += "\nSCENE:\n";
        string scenePath = "Assets/ProjectCore/Scenes/CampusNavigation.unity";
        if (File.Exists(scenePath))
            report += $"  ✓ CampusNavigation.unity exists\n";
        else
        {
            report += $"  ✗ CampusNavigation.unity MISSING\n";
            allGood = false;
        }

        // Check packages
        report += "\nPACKAGES:\n";
        bool hasTMP = System.Type.GetType("TMPro.TextMeshProUGUI, Unity.TextMeshPro") != null;
        bool hasInputSystem = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem") != null;
        
        if (hasTMP)
            report += "  ✓ TextMeshPro installed\n";
        else
        {
            report += "  ✗ TextMeshPro MISSING - Install via Window → TextMeshPro\n";
            allGood = false;
        }

        if (hasInputSystem)
            report += "  ✓ Input System installed\n";
        else
        {
            report += "  ⚠ Input System not detected (optional)\n";
        }

        // Check backend
        report += "\nBACKEND:\n";
        string backendPath = "ARBackend/main.py";
        string fullBackendPath = Path.Combine(Application.dataPath, "..", "..", backendPath);
        if (File.Exists(fullBackendPath))
            report += $"  ✓ Backend main.py exists\n";
        else
            report += $"  ⚠ Backend main.py not found at expected location\n";

        // Summary
        report += "\n=== SUMMARY ===\n";
        if (allGood)
        {
            report += "✓ All critical components present!\n";
            report += "\nNext steps:\n";
            report += "1. Generate icons if not done: Tools → Generate UI Icons\n";
            report += "2. Export map: Window → AR Navigation → Floor Map Editor\n";
            report += "3. Start backend: cd ARBackend && python main.py\n";
            report += "4. Open scene and press Play\n";
        }
        else
        {
            report += "✗ Some components are missing. See details above.\n";
        }

        Debug.Log(report);
        
        EditorUtility.DisplayDialog(
            allGood ? "Project Valid ✓" : "Issues Found ✗",
            allGood 
                ? "All critical components are present!\n\nCheck Console for full report."
                : "Some components are missing.\n\nCheck Console for details.",
            "OK");
    }
}
