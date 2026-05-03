# Device Build - Complete Functionality Fix Guide

## 🔴 ISSUE: Most Functionalities Don't Work on Android Device

### Root Causes & Solutions

---

## 1. ❌ BACKEND CONNECTION FAILS (MOST COMMON)

### Problem
`CampusApiClient.cs` uses `http://127.0.0.1:8000` which only works in Unity Editor.
On Android device, `127.0.0.1` refers to the PHONE itself, not your computer.

### ✅ Solution A: Use Computer's IP Address (Recommended for Testing)

#### Step 1: Find Your Computer's IP Address

**Windows**:
```bash
ipconfig
```
Look for "IPv4 Address" under your WiFi/Ethernet adapter.
Example: `192.168.1.100`

**Mac/Linux**:
```bash
ifconfig
```
Look for `inet` address.

#### Step 2: Update Backend URL in Unity

**Option 1: In Unity Inspector (Before Build)**
1. Open scene: `Assets/ProjectCore/Scenes/CampusNavigation.unity`
2. Select `CampusApp` GameObject
3. Find `CampusApiClient` component
4. Change `Base Url` from `http://127.0.0.1:8000` to `http://YOUR_COMPUTER_IP:8000`
   - Example: `http://192.168.1.100:8000`
5. Save scene
6. Rebuild APK

**Option 2: Change Default in Code**
Edit `CampusApiClient.cs` line 10:
```csharp
// OLD:
[SerializeField] private string m_BaseUrl = "http://127.0.0.1:8000";

// NEW (replace with YOUR computer's IP):
[SerializeField] private string m_BaseUrl = "http://192.168.1.100:8000";
```

#### Step 3: Ensure Backend is Running
```bash
cd ARBackend
python main.py
```

Should show:
```
INFO:     Uvicorn running on http://0.0.0.0:8000
```

#### Step 4: Allow Firewall Access

**Windows Firewall**:
1. Windows Security → Firewall & network protection
2. Allow an app through firewall
3. Find Python → Check "Private" and "Public"
4. Or temporarily disable firewall for testing

**Test Connection from Phone**:
Open browser on phone, visit: `http://YOUR_COMPUTER_IP:8000`
Should see: `{"status":"AR Campus Navigation API running",...}`

### ✅ Solution B: Deploy Backend to Cloud (Production)

Use Render, Heroku, or AWS to host backend, then update URL to:
```
http://your-backend.onrender.com
```

---

## 2. ❌ NO FLOOR MAP DATA

### Problem
App shows "No floor map exported yet" or empty dropdowns.

### ✅ Solution

#### Step 1: Export Floor Map in Unity
```
1. Window → AR Navigation → Floor Map Editor
2. Create/load a map
3. Add named nodes (entrances, rooms)
4. Click "Export → nodes.json"
```

#### Step 2: Verify Export
Check these files exist:
- `Assets/ProjectCore/Resources/nodes.json`
- `ARBackend/nodes.json`

#### Step 3: Restart Backend
```bash
cd ARBackend
python main.py
```

Backend should show:
```
"locations": 5  (or number of nodes you exported)
```

#### Step 4: Rebuild APK
The `nodes.json` in `Resources` folder gets embedded in APK.

---

## 3. ❌ UI DOESN'T APPEAR

### Problem
Black screen or no buttons visible.

### ✅ Solution

#### Check 1: Icons Generated
```
Unity → Tools → Generate UI Icons
```
Wait for "Icons generated successfully" message.

#### Check 2: Scene Setup
Scene should have:
- CampusApp (with CampusRuntimeInstaller)
- Main Camera (tagged "MainCamera")
- Directional Light

#### Check 3: Canvas Rendering
Check device logs:
```bash
adb logcat -s Unity | findstr "CampusRuntimeUI"
```

Should see:
```
[CampusRuntimeUI] Building canvas...
[CampusRuntimeUI] Canvas built successfully
```

---

## 4. ❌ QR SCANNER BLANK

### Problem
QR scanner opens but shows black screen.

### ✅ Solution (Already Applied)

Fixes already in code:
- Android camera permission request
- AndroidManifest.xml with camera permission
- Back camera selection

**If still blank**:
1. Uninstall app completely
2. Reinstall APK
3. Grant camera permission when prompted
4. Check logs: `adb logcat -s Unity | findstr "QRScanner"`

---

## 5. ❌ NAVIGATION DOESN'T WORK

### Problem
Can select destination but no arrows appear.

### Possible Causes & Solutions

#### Cause A: No QR Scan (No Start Location)
**Symptom**: "Scan QR code to begin" message
**Solution**: 
1. Generate QR codes in Floor Map Editor
2. Print QR code
3. Scan with app
4. Then navigate

#### Cause B: Backend Not Reachable
**Symptom**: "Could not load campus data"
**Solution**: Fix backend connection (see #1 above)

#### Cause C: Invalid Path
**Symptom**: "No path found"
**Solution**: 
- Ensure nodes are connected in floor map
- Check nodes.json has "neighbors" array
- Verify start and destination nodes exist

#### Cause D: AR Components Missing
**Symptom**: No errors but no arrows
**Solution**: Check logs for ARFoundationBootstrap:
```bash
adb logcat -s Unity | findstr "ARFoundation"
```

Should see:
```
[ARFoundationBootstrap] Created XR Origin
[ARFoundationBootstrap] Created ARSession
```

---

## 6. ❌ CHAT DOESN'T WORK

### Problem
Chat button doesn't respond or shows errors.

### ✅ Solution

#### Check 1: Backend Running with Ollama
```bash
# Start Ollama
ollama serve

# In another terminal, start backend
cd ARBackend
python main.py
```

#### Check 2: Backend URL Correct
Same as #1 - must use computer's IP, not localhost.

#### Check 3: Test Chat Endpoint
From phone browser:
```
http://YOUR_COMPUTER_IP:8000/chat
```

---

## 🔧 COMPLETE TESTING CHECKLIST

### Before Building APK

- [ ] **Backend URL Updated**
  - Changed from `127.0.0.1` to computer's IP
  - Or deployed to cloud

- [ ] **Floor Map Exported**
  - nodes.json exists in Resources
  - nodes.json exists in ARBackend
  - Backend shows correct location count

- [ ] **Icons Generated**
  - Run `Tools → Generate UI Icons`
  - Check `Assets/ProjectCore/Resources/Icons/` folder

- [ ] **Scene Setup Correct**
  - Only CampusNavigation.unity in build settings
  - CampusApp has CampusRuntimeInstaller
  - Main Camera tagged "MainCamera"

- [ ] **ARCore Enabled**
  - `Edit → Project Settings → XR Plug-in Management`
  - Android tab → ARCore checked

### After Installing APK

- [ ] **Backend Running**
  ```bash
  cd ARBackend
  python main.py
  ```

- [ ] **Phone on Same WiFi**
  - Phone and computer on same network
  - Test: Open `http://COMPUTER_IP:8000` in phone browser

- [ ] **Permissions Granted**
  - Camera permission (for QR scanner)
  - Internet permission (automatic)

### Testing Each Feature

#### 1. UI Test
- [ ] App launches without crash
- [ ] Hamburger menu button visible (top left)
- [ ] QR button visible (top right)
- [ ] Chat button visible (bottom)
- [ ] Status text visible (bottom)

#### 2. Menu Test
- [ ] Tap hamburger → Menu slides out
- [ ] Building dropdown shows options
- [ ] Floor dropdown shows options
- [ ] Destination dropdown shows options
- [ ] Navigate button present

#### 3. Backend Connection Test
- [ ] Status text changes from "Loading..." to "Scan QR code to begin"
- [ ] Dropdowns populated with real data
- [ ] No "Could not load campus data" error

#### 4. QR Scanner Test
- [ ] Tap QR button → Scanner opens
- [ ] Permission dialog appears (first time)
- [ ] Camera feed visible (not black)
- [ ] Scan frame (blue border) visible
- [ ] Can close with X button

#### 5. QR Scanning Test
- [ ] Print QR code from Unity
- [ ] Scan QR code with app
- [ ] Status shows "You are at: [Location]"
- [ ] Scanner closes automatically

#### 6. Navigation Test
- [ ] After QR scan, tap hamburger menu
- [ ] Select destination
- [ ] Tap NAVIGATE
- [ ] Status shows "Navigation active"
- [ ] AR arrows appear on floor

#### 7. Chat Test
- [ ] Tap CHAT button
- [ ] Chat panel opens
- [ ] Type message: "Take me to Room 101"
- [ ] Tap SEND
- [ ] Response appears
- [ ] Navigation starts automatically

---

## 🐛 DEBUGGING COMMANDS

### View All Logs
```bash
adb logcat -s Unity
```

### Filter by Component
```bash
# Backend connection
adb logcat -s Unity | findstr "CampusApiClient"

# UI issues
adb logcat -s Unity | findstr "CampusRuntimeUI"

# QR scanner
adb logcat -s Unity | findstr "QRScanner"

# Navigation
adb logcat -s Unity | findstr "NavigationFlow"

# AR components
adb logcat -s Unity | findstr "ARFoundation"
```

### Check Network Requests
```bash
adb logcat -s Unity | findstr "http"
```

Look for:
- Request URLs (should show your computer's IP, not 127.0.0.1)
- Response codes (200 = success, 404 = not found, 500 = server error)
- Error messages

### Save Logs to File
```bash
adb logcat -s Unity > device_logs.txt
```

---

## 🎯 MOST LIKELY FIX

**90% of device issues are caused by backend connection.**

### Quick Fix Steps:

1. **Find your computer's IP**:
   ```bash
   ipconfig  # Windows
   ifconfig  # Mac/Linux
   ```

2. **Update URL in Unity**:
   - Open CampusNavigation scene
   - Select CampusApp
   - CampusApiClient → Base Url: `http://YOUR_IP:8000`

3. **Start backend**:
   ```bash
   cd ARBackend
   python main.py
   ```

4. **Test from phone browser**:
   - Open: `http://YOUR_IP:8000`
   - Should see API status

5. **Rebuild APK**:
   - File → Build Settings → Build And Run

6. **Test on device**:
   - Launch app
   - Status should show "Scan QR code to begin" (not "Could not load campus data")

---

## 📞 STILL NOT WORKING?

### Collect Debug Info:

1. **Device logs**:
   ```bash
   adb logcat -s Unity > logs.txt
   ```

2. **Backend logs**:
   Check terminal where `python main.py` is running

3. **Network test**:
   - Phone browser → `http://YOUR_COMPUTER_IP:8000`
   - Screenshot result

4. **Scene setup**:
   - Screenshot of CampusApp inspector
   - Screenshot of Build Settings

5. **Specific error messages**:
   - What status text shows
   - What happens when you tap buttons
   - Any error dialogs

---

## 🚀 PRODUCTION DEPLOYMENT

For production (not testing), deploy backend to cloud:

### Option 1: Render (Free)
1. Push ARBackend to GitHub
2. Create Render account
3. New Web Service → Connect GitHub repo
4. Build command: `pip install -r requirements.txt`
5. Start command: `uvicorn main:app --host 0.0.0.0 --port $PORT`
6. Get URL: `https://your-app.onrender.com`
7. Update Unity: `Base Url = "https://your-app.onrender.com"`

### Option 2: Heroku
Similar process, use Procfile:
```
web: uvicorn main:app --host 0.0.0.0 --port $PORT
```

### Option 3: AWS EC2
Deploy FastAPI on EC2 instance, use public IP or domain.

---

**Start with fixing the backend URL - this solves 90% of device issues!**
