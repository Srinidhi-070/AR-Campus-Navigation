# 🔧 ANDROID BUILD & INSTALL FIX

## ❌ Problem Identified

**Root Cause**: AndroidManifest.xml was missing the Activity declaration with `android:exported="true"`, which is **required for Android 12+**.

**Error**: `Permission Denial: starting Intent... not exported from uid`

---

## ✅ FIXES APPLIED

### 1. Fixed AndroidManifest.xml ✅

**File**: `Assets/Plugins/Android/AndroidManifest.xml`

**Changes**:
- ✅ Added proper package name: `com.srinidhi.arcampusnav`
- ✅ Added UnityPlayerActivity declaration
- ✅ Added `android:exported="true"` (required for Android 12+)
- ✅ Added LAUNCHER intent filter
- ✅ Added AR Core metadata
- ✅ Set screen orientation to portrait

**Critical Line**:
```xml
<activity
    android:name="com.unity3d.player.UnityPlayerActivity"
    android:exported="true"
    ...
```

### 2. Created Install Script ✅

**File**: `install_to_device.bat`

Automates the entire process:
1. Checks device connection
2. Uninstalls old version
3. Installs new APK
4. Launches app automatically

---

## 🚀 HOW TO BUILD & INSTALL NOW

### Method 1: Using Install Script (EASIEST)

**Step 1: Build in Unity**
1. Open Unity
2. File → Build Settings
3. Click **Build** (NOT "Build and Run")
4. Save as: `Builds/ARCampusNav.apk`
5. Wait for build to complete

**Step 2: Run Install Script**
1. Make sure device is connected via USB
2. Double-click: `install_to_device.bat`
3. Script will:
   - Uninstall old version
   - Install new APK
   - Launch app automatically

**Done!** App should open on your device.

---

### Method 2: Manual Install

**Step 1: Build in Unity**
```
File → Build Settings → Build
Save to: Builds/ARCampusNav.apk
```

**Step 2: Uninstall Old Version**
```cmd
adb uninstall com.srinidhi.arcampusnav
```

**Step 3: Install New APK**
```cmd
adb install -r Builds\ARCampusNav.apk
```

**Step 4: Launch App**
```cmd
adb shell am start -n com.srinidhi.arcampusnav/com.unity3d.player.UnityPlayerActivity
```

---

### Method 3: Unity "Build and Run"

**IMPORTANT**: After fixing AndroidManifest, Unity's "Build and Run" should now work!

1. File → Build Settings
2. Click **Build and Run**
3. Unity will build, install, and launch automatically

---

## 🐛 TROUBLESHOOTING

### Issue 1: "Installation failed"

**Cause**: Old version still installed

**Fix**:
```cmd
adb uninstall com.srinidhi.arcampusnav
adb install Builds\ARCampusNav.apk
```

---

### Issue 2: "App crashes immediately"

**Check logs**:
```cmd
adb logcat -s Unity
```

**Common causes**:
- Missing AR Core support on device
- Camera permission denied
- Backend not running

---

### Issue 3: "Device not found"

**Fix**:
1. Enable USB Debugging on device:
   - Settings → About Phone
   - Tap "Build Number" 7 times
   - Settings → Developer Options
   - Enable "USB Debugging"

2. Check connection:
```cmd
adb devices
```

Should show:
```
List of devices attached
XXXXXXXXXX    device
```

---

### Issue 4: "App installs but doesn't appear"

**Check if installed**:
```cmd
adb shell pm list packages | findstr arcampusnav
```

Should show:
```
package:com.srinidhi.arcampusnav
```

**Launch manually**:
```cmd
adb shell am start -n com.srinidhi.arcampusnav/com.unity3d.player.UnityPlayerActivity
```

---

### Issue 5: "Permission Denial" error

**This was the original problem - FIXED!**

If you still see this:
1. Make sure you rebuilt in Unity AFTER fixing AndroidManifest
2. Old APK won't have the fix
3. Must build fresh APK

---

## 📋 BUILD CHECKLIST

Before building, verify:
- [ ] Unity project is open
- [ ] Scene is CampusNavigation.unity
- [ ] Build Settings → Platform is Android
- [ ] AndroidManifest.xml has `android:exported="true"`
- [ ] Device is connected (adb devices shows device)
- [ ] USB Debugging is enabled on device

---

## 🎯 EXPECTED RESULT

After building and installing:

**On Device**:
1. ✅ App icon appears in app drawer
2. ✅ Tap icon → App launches
3. ✅ Unity splash screen appears
4. ✅ UI loads (menu button, QR button, chat button)
5. ✅ Status text: "Loading campus map..."

**In Logcat**:
```
Unity   : [CampusRuntimeInstaller] Initialized
Unity   : [PathVisualizer] Arrow prefab loaded from Resources
Unity   : [NavigationFlowController] Loading campus map...
```

---

## 🔍 VIEWING LOGS

### View Unity logs only:
```cmd
adb logcat -s Unity
```

### View all logs:
```cmd
adb logcat
```

### Clear logs first:
```cmd
adb logcat -c
adb logcat -s Unity
```

### Save logs to file:
```cmd
adb logcat -s Unity > device_logs.txt
```

---

## 📱 DEVICE REQUIREMENTS

**Minimum**:
- Android 7.0 (API 24)
- ARCore support
- Camera
- 2GB RAM

**Recommended**:
- Android 12+ (for best compatibility)
- ARCore 1.0+
- Good camera
- 4GB+ RAM

**Check ARCore support**:
https://developers.google.com/ar/devices

---

## 🎉 SUCCESS INDICATORS

**Build succeeded if**:
- ✅ Unity shows "Build Successful"
- ✅ APK file exists in Builds folder
- ✅ APK size is > 50MB

**Install succeeded if**:
- ✅ adb install shows "Success"
- ✅ App appears in device app drawer
- ✅ Can launch from device

**App working if**:
- ✅ App launches without crash
- ✅ UI appears on screen
- ✅ Buttons are visible and clickable
- ✅ No errors in logcat

---

## 🚀 QUICK START AFTER FIX

1. **Open Unity**
2. **File → Build Settings → Build**
3. **Save to: Builds/ARCampusNav.apk**
4. **Double-click: install_to_device.bat**
5. **Done!**

---

## 📞 NEXT STEPS

After successful install:

1. **Test UI**:
   - Tap Menu button → Menu should open
   - Tap QR button → Camera should open
   - Tap Chat button → Chat panel + keyboard

2. **Test Backend Connection**:
   - Make sure backend is running
   - Check status text for connection errors

3. **Test QR Scanning**:
   - Generate QR codes: `python generate_qr.py`
   - Print a QR code
   - Scan it with app

4. **Test Navigation**:
   - Create floor map in Unity
   - Export to backend
   - Select destination
   - Verify arrows appear

---

**The AndroidManifest fix is critical - you MUST rebuild in Unity for it to take effect!** 🚀
