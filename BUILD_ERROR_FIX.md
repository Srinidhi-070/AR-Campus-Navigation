# Unity Build Error - Quick Fix

## ❌ ERROR YOU'RE SEEING

```
NullReferenceException: Object reference not set to an instance of an object
UnityEditor.Android.AndroidDeploymentTargetsExtension.StartApplication
```

## ✅ WHAT THIS MEANS

**This is NOT a code error!**

- Your APK built successfully ✅
- Unity is trying to auto-launch on device ❌
- Unity can't find/connect to device properly ❌

## 🔧 SOLUTION 1: Build Only (Recommended)

### In Unity Build Settings:

1. **File → Build Settings**
2. **UNCHECK** "Build And Run"
3. Click **"Build"** button only
4. Save APK to: `d:\AR_Spatial_Client\Builds\ARCampusNav.apk`
5. Install manually:
   ```bash
   adb install -r d:\AR_Spatial_Client\Builds\ARCampusNav.apk
   ```

## 🔧 SOLUTION 2: Fix Device Connection

### If you want "Build And Run" to work:

1. **Check device is connected:**
   ```bash
   adb devices
   ```
   Should show:
   ```
   List of devices attached
   XXXXXXXXXX    device
   ```

2. **If no device shown:**
   - Reconnect USB cable
   - Enable USB Debugging on phone
   - Allow USB debugging popup on phone
   - Try different USB port

3. **If device shown but still error:**
   - Restart ADB:
     ```bash
     adb kill-server
     adb start-server
     adb devices
     ```

4. **Try Build And Run again**

## 🎯 RECOMMENDED WORKFLOW

**Just use "Build" button, not "Build And Run"**

### Why?
- More reliable
- You control when to install
- Can test multiple builds
- No Unity auto-launch issues

### Steps:
```
1. Unity → File → Build Settings
2. Click "Build" (NOT "Build And Run")
3. Save as: ARCampusNav.apk
4. Install manually: adb install -r ARCampusNav.apk
5. Launch manually on device
```

## ✅ YOUR APK IS FINE

The error happens AFTER build completes. Your APK is already created and working.

**Just install it manually and test!**

---

## 📋 QUICK COMMANDS

### Check if APK exists:
```bash
dir d:\AR_Spatial_Client\Builds\*.apk
```

### Install APK:
```bash
adb install -r d:\AR_Spatial_Client\Builds\ARCampusNav.apk
```

### Check device connected:
```bash
adb devices
```

### View logs:
```bash
adb logcat -s Unity
```

---

**TL;DR: Use "Build" button, not "Build And Run". Install APK manually with adb.**
