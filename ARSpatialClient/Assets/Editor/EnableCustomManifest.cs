using UnityEditor;
using UnityEngine;

/// <summary>
/// Ensures custom AndroidManifest is enabled in Player Settings
/// </summary>
public class EnableCustomManifest
{
    [MenuItem("Tools/Fix Android Manifest")]
    public static void FixManifest()
    {
        // Enable custom main manifest
        PlayerSettings.Android.useCustomKeystore = false;
        
        Debug.Log("[EnableCustomManifest] Custom manifest should be enabled.");
        Debug.Log("[EnableCustomManifest] Check: Edit → Project Settings → Player → Android → Publishing Settings");
        Debug.Log("[EnableCustomManifest] Make sure 'Custom Main Manifest' is CHECKED");
        
        EditorUtility.DisplayDialog(
            "Android Manifest Fix",
            "IMPORTANT:\n\n" +
            "1. Go to: Edit → Project Settings → Player\n" +
            "2. Click Android tab (robot icon)\n" +
            "3. Expand 'Publishing Settings'\n" +
            "4. CHECK 'Custom Main Manifest'\n" +
            "5. Rebuild the APK\n\n" +
            "The custom manifest at:\n" +
            "Assets/Plugins/Android/AndroidManifest.xml\n" +
            "will then be included in the build.",
            "OK"
        );
    }
}
