using UnityEditor;
using UnityEngine;

/// <summary>
/// Automatically adds ZXING_ENABLED scripting define symbol if zxing.dll is present.
/// This enables QR scanning functionality.
/// </summary>
[InitializeOnLoad]
public class ZXingDefineSymbol
{
    static ZXingDefineSymbol()
    {
        string defineSymbol = "ZXING_ENABLED";
        
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        
        if (!defines.Contains(defineSymbol))
        {
            defines += ";" + defineSymbol;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, defines);
            Debug.Log($"[ZXingDefineSymbol] Added '{defineSymbol}' to scripting define symbols for {buildTargetGroup}");
        }
    }
}
