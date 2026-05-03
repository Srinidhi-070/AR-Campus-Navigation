# QUICK REBUILD INSTRUCTIONS

## ✅ FIXES APPLIED

1. **MapManager.cs** - Wrapped file operations in `#if UNITY_EDITOR` to prevent Android crashes
2. Scene is clean (only 3 GameObjects)

## 🚀 REBUILD NOW

### Step 1: In Unity
```
1. File → Save (Ctrl+S)
2. File → Build Settings
3. Click "Build" (NOT "Build And Run")
4. Save as: d:\AR_Spatial_Client\Builds\ARCampusNav_FIXED.apk
5. Wait for build (10-15 min)
```

### Step 2: Uninstall Old App
```bash
adb uninstall com.srinidhi.arcampusnav
```

### Step 3: Install New Build
```bash
adb install "d:\AR_Spatial_Client\Builds\ARCampusNav_FIXED.apk"
```

### Step 4: Test with Logs
```bash
adb logcat -c
adb logcat -s Unity
```

Launch app and check for:
- ✅ No IOException errors
- ✅ [CampusRuntimeUI] Building canvas...
- ✅ [CampusRuntimeInstaller] Installing runtime...

### Step 5: Test Features
1. Does UI appear?
2. Tap CHAT button - does it open?
3. Tap QR button - does camera open?
4. What does status text say?

---

## 🎯 EXPECTED LOGS (Good)

```
[QRLocationManager] Ready
[CampusRuntimeInstaller] Installing runtime...
[ARFoundationBootstrap] Created XR Origin
[ARFoundationBootstrap] Created ARSession
[CampusRuntimeUI] Building canvas...
[NavigationFlowController] Loading campus map...
```

## ❌ BAD LOGS (Should NOT see)

```
IOException: The file already exists  ← FIXED
NullReferenceException
Could not load
Failed to
```

---

**Rebuild now and test!**
