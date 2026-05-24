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
        PlayerSettings.defaultIcon = icon;

        // 2. Set Adaptive Icons for Android
        UnityEditor.Android.AndroidPlatformIcon[] adaptiveIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive) as UnityEditor.Android.AndroidPlatformIcon[];
        if (adaptiveIcons != null)
        {
            foreach (var i in adaptiveIcons)
            {
                // Background icon is index 0, foreground is index 1.
                // We set the foreground to our icon.
                i.SetTextures(new Texture2D[] { null, icon });
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Adaptive, adaptiveIcons);
        }

        // 3. Set Legacy Icons for Android
        UnityEditor.Android.AndroidPlatformIcon[] legacyIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy) as UnityEditor.Android.AndroidPlatformIcon[];
        if (legacyIcons != null)
        {
            foreach (var i in legacyIcons)
            {
                i.SetTextures(new Texture2D[] { icon });
            }
            PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Legacy, legacyIcons);
        }

        // 4. Set Round Icons for Android
        UnityEditor.Android.AndroidPlatformIcon[] roundIcons = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, UnityEditor.Android.AndroidPlatformIconKind.Round) as UnityEditor.Android.AndroidPlatformIcon[];
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
