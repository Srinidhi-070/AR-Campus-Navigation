# QUICK FIX - REBUILD REQUIRED

## ❌ PROBLEM FOUND

The AndroidManifest.xml was missing `android:exported="true"` for the Unity activity.

This is required for Android 12+ devices to launch the app.

## ✅ FIXED

I've updated: `Assets/Plugins/Android/AndroidManifest.xml`

Added:
```xml
<activity android:name="com.unity3d.player.UnityPlayerActivity"
          android:exported="true">
</activity>
```

## 🔧 NOW REBUILD

### In Unity:

1. **File → Build Settings**
2. Click **"Build"** (NOT "Build And Run")
3. Save as: `ARCampusNav.apk` (overwrite existing)
4. Wait for build...

### Then Install:

```bash
adb uninstall com.srinidhi.arcampusnav
adb install "D:\AR_Spatial_Client\ARSpatialClient\Builds\ARCampusNav.apk"
```

### Then Launch:

```bash
adb shell am start -n com.srinidhi.arcampusnav/com.unity3d.player.UnityPlayerActivity
```

OR just tap the app icon on your device.

---

**This is a common Android 12+ requirement. Rebuild with the fixed manifest and it will work!**
