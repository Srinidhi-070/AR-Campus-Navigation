# Build APK - Quick Guide

## ✅ Build Settings Already Configured!

Your build settings are now ready. Here's what to do:

---

## 📱 Build to Android Device

### Step 1: Open Build Settings
1. In Unity, go to `File → Build Settings` (or press `Ctrl+Shift+B`)

### Step 2: Verify Settings
You should see:
- **Platform**: Android (selected/highlighted in blue)
- **Scenes In Build**: 
  - ☑ `ProjectCore/Scenes/CampusNavigation` (index 0)
  - ❌ No other scenes checked

If you see extra scenes (AR_Scene, MapAuthoring, SampleScene):
- Right-click them → Remove Selection
- Or uncheck them

### Step 3: Connect Your Device
1. Connect your Motorola moto g via USB
2. Enable **Developer Options** on phone:
   - Go to Settings → About Phone
   - Tap "Build Number" 7 times
3. Enable **USB Debugging**:
   - Settings → Developer Options → USB Debugging (ON)
4. Allow USB debugging when prompted on phone

### Step 4: Verify Device Detection
In Unity Build Settings window:
- **Run Device** dropdown should show: "Motorola moto g"
- If not detected, click "Refresh" or reconnect USB

### Step 5: Build and Run
Click **"Build And Run"** button

Unity will:
1. Compile scripts (2-5 min)
2. Build APK (5-10 min first time)
3. Install to device automatically
4. Launch app

---

## 💾 Build APK Only (No Install)

If you want to save APK file without installing:

1. Click **"Build"** button (not "Build And Run")
2. Choose save location: `d:\AR_Spatial_Client\Builds\ARCampusNav.apk`
3. Wait for build to complete
4. Manually install APK on device later

---

## ⚙️ Player Settings (Already Configured)

Your settings are already correct:
- ✅ Package Name: `com.srinidhi.arcampusnav`
- ✅ Min SDK: Android 7.0 (API 30)
- ✅ Target SDK: Automatic
- ✅ Scripting Backend: IL2CPP
- ✅ Target Architecture: ARM64
- ✅ Camera Permission: "AR Camera"
- ✅ Internet Permission: Enabled

---

## 🔧 XR Plugin Management

**IMPORTANT**: Verify ARCore is enabled:

1. Go to `Edit → Project Settings`
2. Select **XR Plug-in Management**
3. Click **Android** tab (Android icon)
4. Check ☑ **ARCore**

If ARCore is not in the list:
- Install via Window → Package Manager → Unity Registry → AR Foundation

---

## 📊 Build Time Estimates

| Task | First Build | Subsequent Builds |
|------|-------------|-------------------|
| Script Compilation | 2-5 min | 30 sec - 1 min |
| Asset Processing | 3-5 min | 1-2 min |
| APK Generation | 5-10 min | 2-3 min |
| **Total** | **10-20 min** | **3-6 min** |

---

## 🐛 Troubleshooting

### "No Android device detected"
- Reconnect USB cable
- Check USB debugging is enabled
- Try different USB port
- Install device drivers (Windows)

### "Build failed: Android SDK not found"
- Go to `Edit → Preferences → External Tools`
- Verify Android SDK path is set
- Install Android SDK via Unity Hub → Installs → Add Modules

### "Build failed: NDK not found"
- Install Android NDK via Unity Hub
- Or download from Android Studio

### "App crashes on launch"
- Check device has ARCore support
- Verify camera permissions granted
- Check Logcat for errors: `adb logcat -s Unity`

### "Compilation errors"
- Close Unity
- Delete `Library` folder
- Reopen Unity (will reimport everything)

---

## 📱 Testing on Device

After app launches:

1. **Grant Camera Permission** when prompted
2. **Click QR button** → Test scanner (will show editor message)
3. **Click ☰ menu** → Test navigation UI
4. **Click Chat button** → Test chat interface

For full testing:
- Start backend: `python ARBackend/main.py`
- Export floor map: Unity → Window → AR Navigation → Floor Map Editor → Export
- Print QR codes from `Assets/ProjectCore/Resources/QRCodes/`
- Scan QR → Navigate → See AR arrows

---

## 🚀 Quick Commands

### Build from Command Line (Advanced)
```bash
# Windows
"C:\Program Files\Unity\Hub\Editor\2022.3.XX\Editor\Unity.exe" ^
  -quit -batchmode -nographics ^
  -projectPath "d:\AR_Spatial_Client\ARSpatialClient" ^
  -buildTarget Android ^
  -executeMethod BuildScript.BuildAndroid
```

### Check Device Connection
```bash
adb devices
```

### Install APK Manually
```bash
adb install -r d:\AR_Spatial_Client\Builds\ARCampusNav.apk
```

### View Logs
```bash
adb logcat -s Unity
```

---

## ✅ Final Checklist

Before building:
- [ ] Only CampusNavigation scene in build settings
- [ ] Android platform selected
- [ ] ARCore enabled in XR settings
- [ ] Device connected and detected
- [ ] USB debugging enabled on device
- [ ] Camera permission in Player Settings

**You're ready to build!** Click "Build And Run" in Unity.

---

**Estimated time to first APK**: 10-20 minutes  
**App size**: ~50-80 MB
