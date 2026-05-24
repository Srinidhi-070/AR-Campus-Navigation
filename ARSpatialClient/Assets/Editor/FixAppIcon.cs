using UnityEditor;
using UnityEngine;

public class FixAppIcon
{
    [MenuItem("Tools/Fix App Icon")]
    public static void FixIcon()
    {
        string iconPath = "Assets/ProjectCore/Textures/AppIcon.png";
        Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
        if (icon == null)
        {
            Debug.LogError("Icon not found at " + iconPath);
            return;
        }

        // 1. Set global default icon
        PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new Texture2D[] { icon });

        // 2. Set Adaptive Icons for Android
        PlatformIcon[] adaptiveIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive);
        if (adaptiveIcons != null)
        {
            foreach (var i in adaptiveIcons)
            {
                // Background icon is index 0, foreground is index 1.
                // We set both to our icon to be safe, or just foreground
                i.SetTextures(new Texture2D[] { icon, icon });
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptiveIcons);
        }

        // 3. Set Legacy Icons for Android
        PlatformIcon[] legacyIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy);
        if (legacyIcons != null)
        {
            foreach (var i in legacyIcons)
            {
                i.SetTextures(new Texture2D[] { icon });
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy, legacyIcons);
        }

        // 4. Set Round Icons for Android
        PlatformIcon[] roundIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Round);
        if (roundIcons != null)
        {
            foreach (var i in roundIcons)
            {
                i.SetTextures(new Texture2D[] { icon });
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Round, roundIcons);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("App Icon forcefully set for Android (Legacy, Round, and Adaptive).");
    }
}
