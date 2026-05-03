# How to Run AR Campus Navigation Project - Complete Guide

## 🎯 GOAL
Get the app running on your Android device with full functionality.

---

## 📋 PREREQUISITES

### Required Software
- ✅ Unity 2022.3+ (already installed)
- ✅ Python 3.8+ (already installed)
- ✅ Android device with ARCore support
- ✅ USB cable

### Optional (for AI chat)
- Ollama (for AI destination resolution)

---

## 🚀 STEP-BY-STEP SETUP

### PHASE 1: UNITY SETUP (10 minutes)

#### Step 1: Open Project in Unity
```
1. Open Unity Hub
2. Open project: d:\AR_Spatial_Client\ARSpatialClient
3. Wait for Unity to load (2-5 minutes)
4. If you see compilation errors dialog, click "Ignore"
   (These are Unity cache issues, will resolve automatically)
```

#### Step 2: Generate UI Icons
```
1. In Unity menu: Tools → Generate UI Icons
2. Wait for "Icons generated successfully" message
3. Verify: Assets/ProjectCore/Resources/Icons/ folder has PNG files
```

#### Step 3: Create Floor Map
```
1. Window → AR Navigation → Floor Map Editor
2. Click "New Map"
   - Map Name: TestMap
   - Building: Main Block
   - Floor: 0
   - Floor Height: 4
3. Click "✚ New Map"

4. Paint the map:
   - Select "□ Walkable" mode
   - Click/drag on grid to paint walkable areas (gray)
   - Create a simple path (e.g., 10x10 walkable area)

5. Add nodes:
   - Select "◉ Entrance" mode
   - Click on one cell → Name it: ENTRANCE_MAIN
   - Select "◈ Room Door" mode
   - Click on another cell → Name it: ROOM_101
   - Click on another cell → Name it: ROOM_102

6. Generate QR codes:
   - Select "✎ Select" mode
   - Click on ENTRANCE_MAIN cell
   - In right panel, click "⬛ Generate QR Code"
   - Repeat for ROOM_101 and ROOM_102

7. Save and Export:
   - Click "💾 Save Map"
   - Click "🚀 Export → nodes.json"
   - Should see: "Exported X nodes to Unity Resources and ARBackend/nodes.json"
```

#### Step 4: Update Backend URL
```
1. Open scene: Assets/ProjectCore/Scenes/CampusNavigation.unity
2. In Hierarchy, select "CampusApp"
3. In Inspector, find "Campus Api Client" component
4. Note current Base Url: http://127.0.0.1:8000

5. Find your computer's IP address:
   - Open Command Prompt (Windows Key + R, type "cmd")
   - Type: ipconfig
   - Look for "IPv4 Address" under your WiFi/Ethernet adapter
   - Example: 192.168.1.100

6. Update Base Url:
   - Change from: http://127.0.0.1:8000
   - Change to: http://YOUR_IP:8000
   - Example: http://192.168.1.100:8000

7. Save scene: Ctrl+S or File → Save
```

#### Step 5: Verify Build Settings
```
1. File → Build Settings
2. Platform: Android (should be selected/blue)
3. Scenes In Build: Only "CampusNavigation" should be checked
4. If other scenes are listed, uncheck or remove them
```

#### Step 6: Verify XR Settings
```
1. Edit → Project Settings
2. XR Plug-in Management
3. Click Android tab (Android icon)
4. Check ☑ ARCore
5. Close Project Settings
```

---

### PHASE 2: BACKEND SETUP (5 minutes)

#### Step 1: Install Python Dependencies
```bash
# Open Command Prompt or PowerShell
cd d:\AR_Spatial_Client\ARBackend

# Install dependencies
pip install -r requirements.txt

# Should install: fastapi, uvicorn, requests
```

#### Step 2: Verify nodes.json Exists
```bash
# Check if file exists
dir nodes.json

# Should show the file with size > 0 bytes
```

#### Step 3: Start Backend Server
```bash
# In ARBackend folder
python main.py

# Should see:
# INFO:     Uvicorn running on http://0.0.0.0:8000
# INFO:     Application startup complete
```

**Keep this terminal window open!** Backend must run while testing.

#### Step 4: Test Backend (Optional)
```
1. Open browser on your computer
2. Visit: http://localhost:8000
3. Should see JSON response:
   {
     "status": "AR Campus Navigation API running",
     "model": "llama3.2",
     "locations": 3
   }
```

---

### PHASE 3: ANDROID DEVICE SETUP (5 minutes)

#### Step 1: Enable Developer Mode
```
1. On your Android device:
   - Settings → About Phone
   - Tap "Build Number" 7 times
   - Should see "You are now a developer!"
```

#### Step 2: Enable USB Debugging
```
1. Settings → Developer Options
2. Enable "USB Debugging"
3. Enable "Install via USB" (if available)
```

#### Step 3: Connect Device
```
1. Connect phone to computer via USB cable
2. On phone, tap "Allow USB debugging" when prompted
3. Check "Always allow from this computer"
4. Tap "OK"
```

#### Step 4: Verify Connection
```
1. In Unity: File → Build Settings
2. Under "Run Device" dropdown
3. Should see your device name (e.g., "Motorola moto g")
4. If not visible, click "Refresh" or reconnect USB
```

#### Step 5: Ensure Same WiFi Network
```
CRITICAL: Phone and computer must be on same WiFi network!

1. On computer: Check WiFi network name
2. On phone: Settings → WiFi → Check connected network
3. Must match!
```

---

### PHASE 4: BUILD AND DEPLOY (15 minutes)

#### Step 1: Build APK
```
1. In Unity: File → Build Settings
2. Click "Build And Run" button
3. Choose save location: d:\AR_Spatial_Client\Builds\ARCampusNav.apk
4. Wait for build to complete (10-20 minutes first time)

Unity will:
- Compile scripts
- Process assets
- Build APK
- Install to device
- Launch app automatically
```

**What to expect during build**:
- Progress bar at bottom of Unity
- "Building Player" dialog
- May take 10-20 minutes first time
- Subsequent builds: 3-6 minutes

#### Step 2: Grant Permissions
```
When app launches on device:
1. Camera permission dialog appears
2. Tap "Allow" or "While using the app"
```

---

### PHASE 5: TESTING (30 minutes)

#### Test 1: UI Appears ✅
```
Expected:
- App opens with camera view
- Hamburger menu button (☰) top left
- QR button top right
- CHAT button bottom center
- Status text: "Loading campus map..."

If UI doesn't appear:
- Check Unity console for errors
- Check device logs: adb logcat -s Unity
```

#### Test 2: Backend Connection ✅
```
Expected:
- Status text changes to: "Scan QR code to begin"
- (Not "Could not load campus data")

If shows error:
- Verify backend is running (check terminal)
- Verify phone on same WiFi as computer
- Test in phone browser: http://YOUR_IP:8000
```

#### Test 3: Menu Navigation ✅
```
1. Tap hamburger menu (☰)
2. Menu panel slides out from left
3. See dropdowns:
   - Building: "Main Block"
   - Floor: "Ground Floor"
   - Destination: "Room 101", "Room 102"
4. Tap outside menu to close
```

#### Test 4: QR Scanner ✅
```
1. Tap QR button (top right)
2. Scanner opens
3. Camera permission dialog (if first time) → Tap "Allow"
4. Camera feed appears (not black)
5. See scan frame (blue border)
6. Status: "Point camera at a campus QR code"
7. Tap X to close
```

#### Test 5: QR Scanning ✅
```
1. Print QR codes:
   - Go to: Assets/ProjectCore/Resources/QRCodes/
   - Print ENTRANCE_MAIN.png
   - Print ROOM_101.png

2. Scan QR code:
   - Tap QR button
   - Point camera at printed ENTRANCE_MAIN QR code
   - Hold steady for 2-3 seconds
   - Should detect and close automatically
   - Status: "You are at: Entrance Main"
```

#### Test 6: Navigation ✅
```
1. After scanning QR (or skip if testing without QR):
2. Tap hamburger menu
3. Select destination: "Room 101"
4. Tap "NAVIGATE" button
5. Expected:
   - Status: "Calculating path to Room 101..."
   - Then: "Navigation active"
   - AR arrows appear on floor (if arrow prefab exists)
   - Direction text shows: "Start", "Go Straight", etc.
```

#### Test 7: Chat Navigation ✅
```
1. Tap CHAT button (bottom)
2. Chat panel opens
3. Type: "Take me to Room 101"
4. Tap SEND
5. Expected:
   - Response appears: "I'll navigate you to Room 101"
   - Navigation starts automatically
   - Arrows appear
```

---

## 🐛 TROUBLESHOOTING

### Issue: "Could not load campus data"
**Cause**: Backend not reachable

**Fix**:
```
1. Check backend is running:
   - Terminal should show: "Uvicorn running on http://0.0.0.0:8000"
   
2. Check phone can reach backend:
   - Open browser on phone
   - Visit: http://YOUR_COMPUTER_IP:8000
   - Should see API status (not error)
   
3. Check WiFi:
   - Phone and computer on same network
   
4. Check firewall:
   - Windows: Allow Python through firewall
   - Or temporarily disable firewall
```

### Issue: QR Scanner Shows Black Screen
**Cause**: Camera permission denied or not initialized

**Fix**:
```
1. Uninstall app completely
2. Reinstall APK
3. Grant camera permission when prompted

Or manually:
- Settings → Apps → AR Campus Nav
- Permissions → Camera → Allow
```

### Issue: No UI Appears
**Cause**: Icons not generated

**Fix**:
```
1. In Unity: Tools → Generate UI Icons
2. Rebuild APK
```

### Issue: No Arrows Appear
**Cause**: Arrow prefab not assigned

**Fix**:
```
1. Create simple arrow:
   - GameObject → 3D Object → Cube
   - Scale: (0.2, 0.1, 0.5)
   - Add material with bright color
   - Save as prefab: Assets/ProjectCore/Prefabs/Arrow.prefab

2. Assign to PathVisualizer:
   - In scene, CampusApp will have PathVisualizer component
   - Or it's created at runtime - check logs

3. For now, navigation works without arrows
   - Direction text still shows
   - Path is calculated
```

### Issue: Build Fails
**Cause**: Various

**Fix**:
```
1. Check Android SDK installed:
   - Edit → Preferences → External Tools
   - Verify Android SDK path

2. Check ARCore enabled:
   - Edit → Project Settings → XR Plug-in Management
   - Android tab → ARCore checked

3. Clean build:
   - Close Unity
   - Delete Library folder
   - Reopen Unity
   - Rebuild
```

---

## 📱 DEVICE LOGS

### View Real-Time Logs
```bash
# Open Command Prompt
adb logcat -s Unity

# Filter by component
adb logcat -s Unity | findstr "CampusRuntimeUI"
adb logcat -s Unity | findstr "QRScanner"
adb logcat -s Unity | findstr "NavigationFlow"
adb logcat -s Unity | findstr "CampusApiClient"
```

### Save Logs to File
```bash
adb logcat -s Unity > device_logs.txt
```

---

## ✅ SUCCESS CHECKLIST

After following all steps, you should have:

- [x] Unity project opens without errors
- [x] Icons generated in Resources/Icons/
- [x] Floor map created with 3+ nodes
- [x] QR codes generated for nodes
- [x] Backend URL updated to computer's IP
- [x] Backend running on http://0.0.0.0:8000
- [x] Device connected and detected in Unity
- [x] APK built and installed on device
- [x] App launches without crash
- [x] UI visible (menu, QR button, chat button)
- [x] Status shows "Scan QR code to begin" (not error)
- [x] Menu opens with populated dropdowns
- [x] QR scanner opens with camera feed
- [x] Can scan printed QR codes
- [x] Navigation calculates path
- [x] Direction text shows instructions

---

## 🎯 QUICK START (Minimal Testing)

If you just want to see it work quickly:

### 1. Generate Icons (1 min)
```
Tools → Generate UI Icons
```

### 2. Update Backend URL (2 min)
```
- Run: ipconfig
- Copy IPv4 Address
- Update CampusApp → CampusApiClient → Base Url
- Save scene
```

### 3. Start Backend (1 min)
```bash
cd ARBackend
python main.py
```

### 4. Build and Run (15 min)
```
File → Build Settings → Build And Run
```

### 5. Test on Device (5 min)
```
- App launches
- UI appears
- Status: "Scan QR code to begin"
- Menu opens with data
- Done!
```

---

## 📞 NEED HELP?

### Check Documentation
- `DEVICE_FUNCTIONALITY_FIX.md` - Device issues
- `QR_SCANNER_CAMERA_FIX.md` - Camera issues
- `BUILD_APK_GUIDE.md` - Build problems
- `INTEGRATION_TESTING_GUIDE.md` - Testing procedures

### Debug Commands
```bash
# View all logs
adb logcat -s Unity

# Check device connection
adb devices

# Reinstall app
adb install -r Builds\ARCampusNav.apk

# Uninstall app
adb uninstall com.srinidhi.arcampusnav
```

---

## 🎉 YOU'RE DONE!

Your AR Campus Navigation app should now be running on your Android device with:
- ✅ Working UI
- ✅ Backend connection
- ✅ QR scanning
- ✅ Navigation system
- ✅ Chat interface

**Next Steps**:
1. Create detailed floor map of your campus
2. Generate and print QR codes
3. Place QR codes at locations
4. Test navigation between locations
5. Show to users!

---

**Estimated Total Time**: 1-2 hours for first-time setup
