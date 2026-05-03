# ✅ CLEANUP COMPLETE - NEXT STEPS

## ✅ WHAT WAS DONE

1. ✅ Deleted Unity Library folder (forces clean reimport)
2. ✅ Deleted old Builds folder
3. ✅ Created fresh Builds folder
4. ✅ Uninstalled app from device (verified removed)

---

## 🚀 NEXT STEPS - DO THESE NOW

### STEP 1: Reopen Unity (10-15 minutes)

**IMPORTANT**: Close Unity if it's still open!

```
1. Close Unity completely (if open)
2. Open Unity Hub
3. Click on: AR_Spatial_Client project
4. Unity will open and start reimporting EVERYTHING
5. YOU WILL SEE: "Importing Assets..." progress bar
6. WAIT 10-15 MINUTES - Do NOT touch anything!
7. When done, Unity Editor will be ready
```

**What to expect**:
- Progress bar at bottom: "Importing Assets (X/Y)"
- Console may show some warnings (ignore them)
- Wait until progress bar disappears completely

---

### STEP 2: Generate Icons (1 minute)

```
1. In Unity menu: Tools → Generate UI Icons
2. Wait for message: "Icons generated successfully"
3. Verify: Assets/ProjectCore/Resources/Icons/ has PNG files
```

---

### STEP 3: Update Backend URL (2 minutes)

**CRITICAL**: Change from localhost to your computer's IP

```
1. Open Command Prompt
2. Type: ipconfig
3. Find "IPv4 Address" (example: 192.168.1.100)
4. Write it down: ___________________

5. In Unity:
   - Open scene: Assets/ProjectCore/Scenes/CampusNavigation.unity
   - Click "CampusApp" in Hierarchy
   - In Inspector, find "Campus Api Client" component
   - Change "Base Url" from: http://127.0.0.1:8000
   - Change to: http://YOUR_IP:8000 (example: http://192.168.1.100:8000)
   
6. Save scene: Ctrl+S or File → Save
```

---

### STEP 4: Verify Scene is Clean (1 minute)

```
1. In Hierarchy, you should see ONLY:
   ✅ CampusApp
   ✅ Main Camera
   ✅ Directional Light

2. If you see other objects (ARFeatheredPlane, MapVisualizationSetup, etc):
   - Select them
   - Press Delete
   - Save scene (Ctrl+S)
```

---

### STEP 5: Build Settings (2 minutes)

```
1. File → Build Settings
2. Verify:
   - Platform: Android (blue/selected)
   - Scenes In Build: ONLY "CampusNavigation" is checked
   
3. If other scenes are listed:
   - Uncheck them or right-click → Remove

4. Click "Player Settings" button
5. Verify:
   - Product Name: ARCampusNav
   - Package Name: com.srinidhi.arcampusnav
   - Minimum API Level: Android 7.0 (API 24)
   - Scripting Backend: IL2CPP
   - Target Architectures: ARM64 ✅

6. Edit → Project Settings → XR Plug-in Management
7. Click Android tab (Android icon)
8. Check: ☑ ARCore
```

---

### STEP 6: Clean Build (15-20 minutes)

```
1. File → Build Settings
2. Click "Build" button (NOT "Build And Run")
3. Save as: d:\AR_Spatial_Client\Builds\ARCampusNav_Clean.apk
4. Click "Save"
5. WAIT 15-20 minutes
6. Watch Unity Console for progress
7. When done, you'll see: "Build completed with a result of 'Succeeded'"
```

**What to expect**:
- Progress bar: "Building Player"
- Console shows compilation progress
- May take 15-20 minutes first time
- Don't close Unity during build!

---

### STEP 7: Start Backend (1 minute)

**IMPORTANT**: Backend must be running!

```
1. Open NEW Command Prompt window
2. cd d:\AR_Spatial_Client\ARBackend
3. python main.py
4. Should see: "Uvicorn running on http://0.0.0.0:8000"
5. KEEP THIS WINDOW OPEN!
```

---

### STEP 8: Install and Test (5 minutes)

```
1. In Command Prompt:
   adb install "d:\AR_Spatial_Client\Builds\ARCampusNav_Clean.apk"
   
2. Should see: "Success"

3. On your phone:
   - Find app: "ARCampusNav" or "ARSpatialClient"
   - Tap to open
   
4. Grant camera permission when prompted

5. Check what you see:
   - UI should appear (hamburger menu, QR button, chat button)
   - Status text at bottom
   - What does status say? ___________________
```

---

### STEP 9: Test with Logs (Optional)

```
1. Open Command Prompt
2. adb logcat -c
3. adb logcat -s Unity
4. On phone: Open app
5. Watch logs for errors
```

**Good logs**:
```
[QRLocationManager] Ready
[CampusRuntimeInstaller] Installing runtime
[CampusRuntimeUI] Building canvas
```

**Bad logs**:
```
IOException
NullReferenceException
Could not load
```

---

## 🎯 EXPECTED RESULT

After these steps:
- ✅ App installs successfully
- ✅ App opens when tapped
- ✅ UI appears (buttons visible)
- ✅ Status text shows "Loading..." then "Scan QR code to begin"
- ✅ Buttons respond to taps
- ✅ No crashes

---

## 📞 IF ISSUES PERSIST

After clean rebuild, if still not working:

1. **Capture logs**:
   ```
   adb logcat -d -s Unity > clean_build_logs.txt
   ```

2. **Check what you see**:
   - Does app open? Yes/No
   - Do you see any UI? Yes/No
   - What does status text say?
   - Any error messages?

3. **Share the logs** and I'll diagnose the specific issue

---

## ⏱️ TIME BREAKDOWN

- Unity reimport: 10-15 min
- Generate icons: 1 min
- Update URL: 2 min
- Verify scene: 1 min
- Build settings: 2 min
- Build APK: 15-20 min
- Install & test: 5 min

**Total: 35-45 minutes**

---

**START WITH STEP 1: REOPEN UNITY AND WAIT FOR REIMPORT**

The cleanup is complete. Now follow these steps carefully and the app should work!
