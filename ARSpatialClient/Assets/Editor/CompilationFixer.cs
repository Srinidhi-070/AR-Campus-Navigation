using UnityEditor;
using UnityEngine;

/// <summary>
/// Forces Unity to refresh and recompile all scripts.
/// Use this if Unity shows compilation errors on project open.
/// </summary>
public class CompilationFixer : EditorWindow
{
    [MenuItem("Tools/Fix Compilation Issues")]
    public static void FixCompilation()
    {
        Debug.Log("[CompilationFixer] Refreshing asset database...");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        
        Debug.Log("[CompilationFixer] Forcing script recompilation...");
        UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        
        EditorUtility.DisplayDialog(
            "Compilation Fix", 
            "Asset database refreshed and recompilation requested.\n\nIf errors persist:\n1. Close Unity\n2. Delete Library folder\n3. Reopen project", 
            "OK");
    }
}
