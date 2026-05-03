# Device Build Troubleshooting Guide

## 🔴 ISSUE: UI and Camera Don't Work After Build

### Root Causes Fixed:

1. **Missing AR Foundation Components** ✅ FIXED
   - Added `ARFoundationBootstrap.cs` to add ARSession and ARCameraManager at runtime
   - Integrated into `CampusRuntimeInstaller.cs`

2. **Canvas Rendering Issues** ✅ FIXED
   - Updated `CampusRuntimeUI.cs` with explicit canvas scaler settings
   - Set proper plane distance for AR overlay

---

## 📋 Pre-Build Checklist

Before building, verify these in Unity Editor:

### 1. Scene Setup
- [ ] Open `Assets/ProjectCore/Scenes/CampusNavigation.unity`
- [ ] Scene has 3 GameObjects:
  - CampusApp (with CampusRuntimeInstaller)
  - Main Camera (tagged "MainCamera")
  - Directional Light

### 2. Build Settings
- [ ] `File → Build Settings`
- [ ] Only `CampusNavigation.unity` in "Scenes In Build"
- [ ] Android platform selected
- [ ] Run Device shows your phone

### 3. XR Plugin Management
- [ ] `Edit → Project Settings → XR Plug-in Management`
- [ ] Android tab → ☑ ARCore enabled

### 4. Player Settings
- [ ] `Edit → Project Settings → Player`
- [ ] Android tab → Other Settings:
  - Package Name: `com.srinidhi.arcampusnav`
  - Min API: Android 7.0 (API 24+)
  - Target API: Automatic
  - Scripting Backend: IL2CPP
  - Target Architectures: ARM64 ✅
  - Internet Access: Require
- [ ] Android tab → Publishing Settings:
  - Create new keystore (for release builds)

### 5. Graphics Settings
- [ ] `Edit → Project Settings → Graphics`
- [ ] Scriptable Render Pipeline: Universal Render Pipeline Asset
- [ ] `Edit → Project Settings → Quality`
- [ ] Rendering: URP-HighQuality

---

## 🔧 Build Process

### Step 1: Clean Build
```
1. Close Unity
2. Delete these folders:
   - ARSpatialClient/Library
   - ARSpatialClient/Temp
   - ARSpatialClient/obj
3. Reopen Unity (wait 5-10 min for reimport)
```

### Step 2: Generate Icons
```
Unity → Tools → Generate UI Icons
```
Wait for completion message.

### Step 3: Build APK
```
1. File → Build Settings
2. Click "Build And Run"
3. Save as: d:\AR_Spatial_Client\Builds\ARCampusNav.apk
4. Wait 10-20 minutes
```

---

## 📱 Device Setup

### Enable Developer Mode
1. Settings → About Phone
2. Tap "Build Number" 7 times
3. Enter PIN if prompted

### Enable USB Debugging
1. Settings → Developer Options
2. Enable "USB Debugging"
3. Enable "Install via USB"

### Grant Permissions
When app launches, grant:
- ✅ Camera permission (CRITICAL)
- ✅ Storage permission (if prompted)

---

## 🐛 Common Issues & Fixes

### Issue 1: Black Screen on Launch
**Symptoms**: App opens but shows only black screen

**Causes**:
- AR Foundation components not initialized
- Camera permission denied
- ARCore not installed on device

**Fixes**:
1. Check Logcat for errors:
   ```bash
   adb logcat -s Unity
   ```

2. Verify ARCore installed:
   - Open Google Play Store
   - Search "Google Play Services for AR"
   - Update if needed

3. Grant camera permission:
   - Settings → Apps → AR Campus Nav → Permissions → Camera → Allow

4. Rebuild with clean:
   - Delete Library folder
   - Rebuild APK

### Issue 2: UI Not Visible
**Symptoms**: App runs but no buttons/UI appear

**Causes**:
- Canvas not rendering
- EventSystem missing
- UI scale incorrect

**Fixes**:
1. Check if `CampusRuntimeInstaller` is on CampusApp GameObject
2. Verify icons generated: `Assets/ProjectCore/Resources/Icons/`
3. Check Logcat for UI build errors:
   ```bash
   adb logcat -s Unity | findstr "CampusRuntimeUI"
   ```

4. Verify screen resolution:
   - Canvas Scaler reference: 1080x1920
   - Should auto-scale to device

### Issue 3: Camera Shows But No AR
**Symptoms**: Camera feed visible but AR features don't work

**Causes**:
- ARSession not started
- Device doesn't support ARCore
- Tracking lost

**Fixes**:
1. Check device compatibility:
   - https://developers.google.com/ar/devices

2. Verify ARCore version:
   - Play Store → Google Play Services for AR → Update

3. Check Logcat for AR errors:
   ```bash
   adb logcat -s Unity | findstr "AR"
   ```

4. Restart app and grant camera permission again

### Issue 4: QR Scanner Doesn't Work
**Symptoms**: QR button opens scanner but doesn't scan

**Causes**:
- Camera permission denied
- ZXing library not included in build
- Camera feed not rendering

**Fixes**:
1. Verify camera permission granted
2. Check if ZXING_ENABLED is defined:
   - `Edit → Project Settings → Player → Scripting Define Symbols`
   - Should contain: `ZXING_ENABLED`

3. Test with printed QR code (not screen)
4. Ensure good lighting
5. Hold QR code steady for 2-3 seconds

### Issue 5: Backend Connection Failed
**Symptoms**: "Could not load campus data" error

**Causes**:
- Backend not running
- Wrong backend URL
- Network permission denied
- Firewall blocking

**Fixes**:
1. Start backend:
   ```bash
   cd ARBackend
   python main.py
   ```

2. Check backend URL in `CampusApiClient.cs`:
   - For device testing, use computer's IP (not localhost)
   - Example: `http://192.168.1.100:8000`

3. Update URL:
   ```csharp
   // In CampusApiClient.cs
   private const string BASE_URL = "http://YOUR_COMPUTER_IP:8000";
   ```

4. Ensure phone and computer on same WiFi network

5. Check Windows Firewall:
   - Allow Python through firewall
   - Allow port 8000

### Issue 6: App Crashes on Launch
**Symptoms**: App opens then immediately closes

**Causes**:
- Missing dependencies
- Corrupted build
- Incompatible device
- Memory issues

**Fixes**:
1. Check crash logs:
   ```bash
   adb logcat -s Unity AndroidRuntime
   ```

2. Verify device requirements:
   - Android 7.0+ (API 24+)
   - ARCore support
   - 2GB+ RAM

3. Clean rebuild:
   - Delete Library folder
   - Delete Builds folder
   - Rebuild from scratch

4. Check for null references in Logcat

### Issue 7: Buttons Don't Respond
**Symptoms**: UI visible but buttons don't work when tapped

**Causes**:
- EventSystem missing
- GraphicRaycaster missing
- Canvas blocking touches

**Fixes**:
1. Verify EventSystem exists:
   - Should be created by `CampusRuntimeUI.EnsureEventSystem()`

2. Check Logcat for touch events:
   ```bash
   adb logcat -s Unity | findstr "Button"
   ```

3. Verify Canvas has GraphicRaycaster component

4. Check if another UI is blocking touches

---

## 🔍 Debugging Tools

### View Logs in Real-Time
```bash
# All Unity logs
adb logcat -s Unity

# Filter by component
adb logcat -s Unity | findstr "CampusRuntimeInstaller"
adb logcat -s Unity | findstr "QRScanner"
adb logcat -s Unity | findstr "NavigationFlow"

# Save logs to file
adb logcat -s Unity > unity_logs.txt
```

### Check Device Info
```bash
# Device model
adb shell getprop ro.product.model

# Android version
adb shell getprop ro.build.version.release

# ARCore version
adb shell dumpsys package com.google.ar.core | findstr "versionName"
```

### Install APK Manually
```bash
adb install -r d:\AR_Spatial_Client\Builds\ARCampusNav.apk
```

### Uninstall App
```bash
adb uninstall com.srinidhi.arcampusnav
```

### Clear App Data
```bash
adb shell pm clear com.srinidhi.arcampusnav
```

---

## ✅ Verification Steps

After successful build and install:

### 1. Launch Test
- [ ] App opens without crashing
- [ ] No black screen
- [ ] UI visible (hamburger menu, QR button, chat button)

### 2. UI Test
- [ ] Tap hamburger menu → Menu panel slides out
- [ ] Tap QR button → Scanner opens
- [ ] Tap X on scanner → Scanner closes
- [ ] Tap CHAT button → Chat panel opens

### 3. Camera Test
- [ ] QR scanner shows camera feed
- [ ] Camera feed is clear (not black/frozen)
- [ ] Can close scanner

### 4. Backend Test (Optional)
- [ ] Start backend on computer
- [ ] Update CampusApiClient URL to computer IP
- [ ] Rebuild and install
- [ ] Check status text shows "Scan QR code to begin"

### 5. Navigation Test (Full)
- [ ] Export floor map in Unity
- [ ] Print QR code
- [ ] Scan QR code
- [ ] Select destination from menu
- [ ] Click NAVIGATE
- [ ] AR arrows appear

---

## 📞 Still Not Working?

### Collect Debug Info:
1. Device model and Android version
2. ARCore version
3. Unity logs: `adb logcat -s Unity > logs.txt`
4. Screenshot of issue
5. Steps to reproduce

### Check These Files:
- `CampusRuntimeInstaller.cs` - Should have ARFoundationBootstrap
- `ARFoundationBootstrap.cs` - Should exist
- `CampusRuntimeUI.cs` - Should have canvas scaler settings
- Build Settings - Only CampusNavigation scene

### Last Resort:
1. Delete entire `Library` folder
2. Delete `Builds` folder
3. Close Unity
4. Reopen Unity (wait for full reimport)
5. Generate icons
6. Clean build

---

## 🎯 Expected Behavior

**On Launch**:
1. App opens with camera view
2. UI overlay appears (hamburger menu, QR button, chat button)
3. Status text: "Loading campus map..."
4. After load: "Scan QR code to begin"

**QR Scanner**:
1. Tap QR button
2. Camera feed appears
3. Scan frame visible
4. Point at QR code
5. Detects and closes automatically

**Navigation**:
1. After QR scan: "You are at: [Location]"
2. Tap hamburger menu
3. Select building, floor, destination
4. Tap NAVIGATE
5. AR arrows appear on floor

---

**All fixes have been applied to your project. Rebuild the APK and test on device.**
