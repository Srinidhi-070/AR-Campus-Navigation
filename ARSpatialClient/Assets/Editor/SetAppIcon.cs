using UnityEditor;
using UnityEngine;

public class SetAppIcon
{
    [MenuItem("Tools/Set App Icon")]
    public static void SetIcon()
    {
        string iconPath = "Assets/ProjectCore/Textures/AppIcon.png";
        
        // Import as sprite/GUI texture and enable Read/Write
        TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }

        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
        {
            Debug.LogError("Could not find icon at " + iconPath);
            return;
        }

        // Set default icon for all platforms
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });
        
        // Also set Android specifically just in case
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, new Texture2D[] { icon, icon, icon, icon, icon, icon });

        AssetDatabase.SaveAssets();
        Debug.Log("App Icon set successfully to " + iconPath);
    }
}
