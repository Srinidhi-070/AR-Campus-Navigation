# 🚀 QUICK START GUIDE - GET IT WORKING NOW!

## ⏰ **15 MINUTES TO WORKING DEMO**

---

## 📋 **STEP 1: START BACKEND (2 minutes)**

### **Open Command Prompt:**
```
Press Windows Key + R
Type: cmd
Press Enter
```

### **Navigate to backend folder:**
```bash
cd d:\AR_Spatial_Client\ARBackend
```

### **Start the server:**
```bash
python main.py
```

### **✅ SUCCESS LOOKS LIKE:**
```
Starting AR Campus Navigation API...
Loaded 8 nodes from nodes.json
Server will run on: http://0.0.0.0:8000
Access from phone: http://192.168.1.4:8000
INFO:     Uvicorn running on http://0.0.0.0:8000
```

### **❌ IF YOU SEE ERRORS:**

**Error: "python is not recognized"**
```bash
# Try:
py main.py
# OR
python3 main.py
```

**Error: "No module named 'fastapi'"**
```bash
pip install fastapi uvicorn pydantic
```

**✅ LEAVE THIS WINDOW OPEN - Backend must stay running!**

---

## 📋 **STEP 2: TEST BACKEND (1 minute)**

### **Open browser:**
Go to: `http://localhost:8000`

### **✅ SUCCESS LOOKS LIKE:**
```json
{
  "status": "AR Campus Navigation API running",
  "model": "llama3.2",
  "nodes_path": "...",
  "locations": 8
}
```

### **✅ If you see this → Backend is working! Continue to Step 3.**

---

## 📋 **STEP 3: REBUILD APK (5 minutes)**

### **Open Unity:**
1. Double-click Unity icon
2. Open project: `d:\AR_Spatial_Client\ARSpatialClient`
3. Wait for Unity to load...

### **Open correct scene:**
1. In Project panel (bottom), navigate to:
   ```
   Assets → ProjectCore → Scenes
   ```
2. Double-click: `CampusNavigation.unity`

### **Verify scene:**
1. Look at Hierarchy panel (left)
2. Should see:
   - Main Camera
   - Directional Light
   - CampusApp

### **Build APK:**
1. Click: `File` (top menu)
2. Click: `Build Settings`
3. Verify:
   - Platform: Android (should be selected)
   - Scenes In Build: CampusNavigation (checked)
4. Click: `Build` button
5. Save as: `ARCampusNav_Final.apk` in `Builds` folder
6. Wait for build to complete (3-5 minutes)...

### **✅ SUCCESS LOOKS LIKE:**
- Unity Console shows: "Build completed successfully"
- File exists: `d:\AR_Spatial_Client\Builds\ARCampusNav_Final.apk`

---

## 📋 **STEP 4: INSTALL ON PHONE (2 minutes)**

### **Connect phone to computer:**
1. Use USB cable
2. On phone: Enable USB Debugging
   - Settings → About Phone → Tap "Build Number" 7 times
   - Settings → Developer Options → Enable "USB Debugging"
3. Allow USB debugging when prompted

### **Install APK:**

**Open Command Prompt (new window):**
```bash
cd d:\AR_Spatial_Client\Builds
adb install -r ARCampusNav_Final.apk
```

### **✅ SUCCESS LOOKS LIKE:**
```
Performing Streamed Install
Success
```

### **❌ IF YOU SEE ERRORS:**

**Error: "adb is not recognized"**
- Install Android SDK Platform Tools
- OR use Android Studio's adb

**Error: "device not found"**
- Check USB cable
- Check USB debugging enabled
- Try different USB port

---

## 📋 **STEP 5: TEST ON PHONE (5 minutes)**

### **Open app on phone:**
1. Find app icon: "ARCampusNav"
2. Tap to open
3. Grant camera permission when asked

### **Wait 10 seconds...**

### **✅ SUCCESS LOOKS LIKE:**
- Status at bottom shows: **"Scan QR code to begin"**
- Menu button (hamburger icon) visible at top-left
- QR button visible at top-right
- Chat button visible at bottom

### **❌ IF YOU SEE:**

**"Backend offline. Use QR to navigate."**
- Backend not running OR
- Wrong IP address

**Solution:**
1. Check backend terminal - should still be running
2. Check your computer's IP:
   ```bash
   ipconfig
   ```
   Look for "IPv4 Address" (e.g., 192.168.1.4)
3. In Unity: CampusApp → Inspector → Base Url → Update IP
4. Rebuild APK

**"Loading..." forever**
- App is frozen
- Close app and reopen
- If still frozen, check logs

---

## 📋 **STEP 6: TEST MENU (1 minute)**

### **Click Menu button (hamburger icon, top-left)**

### **✅ SUCCESS LOOKS LIKE:**
- Menu panel slides out from left
- Building dropdown shows: **"House"**
- Floor dropdown shows: **"Floor 1"**, **"Floor 2"**
- Room dropdown shows: **"Toilet 2"**, **"Bathroom 1"**, **"Kitchen 1"**, etc.

### **❌ IF YOU SEE:**
**"No Buildings"**
- Backend not returning data
- Check backend terminal for errors
- Check Unity logs: `adb logcat -s Unity`

---

## 📋 **STEP 7: GENERATE QR CODE (2 minutes)**

### **Go to QR code generator:**
https://www.qr-code-generator.com/

### **Select "Text" type**

### **Paste this EXACT text:**
```json
{"node_id":"HOUSE_ENTRANCE_1","building":"House","floor":1}
```

### **Click "Create QR Code"**

### **Download QR code image**

### **Display on computer screen:**
- Open image
- Press F11 for full screen
- OR just make it large enough to scan

---

## 📋 **STEP 8: TEST QR SCANNING (2 minutes)**

### **In app, click QR button (top-right)**

### **✅ SUCCESS LOOKS LIKE:**
- Camera preview appears
- You see camera feed (not black screen)
- Camera is smooth (not laggy)

### **Point phone at QR code on computer screen**

### **✅ SUCCESS LOOKS LIKE:**
- Scanner detects QR code
- Shows: **"Location detected"**
- Shows: **"You are at: House Entrance 1"**
- Scanner closes automatically after 1 second

### **❌ IF YOU SEE:**

**Black screen**
- Camera permission denied
- Settings → Apps → Your App → Permissions → Camera → Allow

**Slow/laggy camera**
- This is fixed in the rebuild
- Should be smooth at 640x480 resolution

**Doesn't scan QR code**
- QR code too small - make it bigger
- QR code blurry - hold phone steady
- Bad lighting - move to brighter area

---

## 📋 **STEP 9: TEST NAVIGATION (2 minutes)**

### **After scanning QR (you're at HOUSE_ENTRANCE_1):**

1. Click **Menu button**
2. Select:
   - Building: **"House"**
   - Floor: **"Floor 1"**
   - Destination: **"Kitchen 1"**
3. Click **"NAVIGATE"** button

### **✅ SUCCESS LOOKS LIKE:**
- Status shows: **"Calculating path to Kitchen 1..."**
- Status changes to: **"Navigation active"**
- Directions appear: **"Walk forward X meters..."**

### **❌ IF YOU SEE:**
**"No path found"**
- Backend pathfinding issue
- Check backend terminal for errors

**Nothing happens**
- Navigate button not enabled
- Must scan QR first
- Must select valid destination

---

## 📋 **STEP 10: TEST AR PLANES (2 minutes)**

### **Point camera at floor**

### **Move phone slowly in circular motion**

### **Keep phone pointed at floor for 10 seconds**

### **✅ SUCCESS LOOKS LIKE:**
- **Cyan semi-transparent planes** appear on floor
- **White dots** at plane boundaries
- Planes **grow** as you scan more area

### **❌ IF YOU SEE:**

**Nothing appears**
- Device doesn't support ARCore
- Check: https://developers.google.com/ar/devices
- Install ARCore from Play Store

**App crashes**
- AR Foundation issue
- Check Unity logs: `adb logcat -s Unity`

**Planes appear but disappear quickly**
- Bad lighting - move to brighter area
- Plain surface - try textured floor (carpet, tiles)

---

## ✅ **SUCCESS CHECKLIST:**

- [ ] Backend running (Step 1)
- [ ] Backend accessible in browser (Step 2)
- [ ] APK built successfully (Step 3)
- [ ] APK installed on phone (Step 4)
- [ ] App opens and shows "Scan QR code to begin" (Step 5)
- [ ] Menu shows "House" in dropdown (Step 6)
- [ ] QR code generated (Step 7)
- [ ] QR scanner works and detects location (Step 8)
- [ ] Navigation calculates path (Step 9)
- [ ] AR planes appear on floor (Step 10)

---

## 🎓 **FOR YOUR PRESENTATION:**

### **What to demonstrate:**

1. **Backend** (30 sec)
   - Show terminal with server running
   - Show browser with API response

2. **App UI** (30 sec)
   - Show menu with dropdowns
   - Show QR button
   - Show chat button

3. **QR Scanning** (1 min)
   - Scan QR code
   - Show location detected

4. **Navigation** (1 min)
   - Select destination
   - Show path calculation
   - Show directions

5. **AR Planes** (1 min)
   - Point at floor
   - Show planes appearing
   - Explain AR surface detection

**Total: 4 minutes**

---

## 🆘 **IF SOMETHING FAILS:**

### **Collect logs:**
```bash
adb logcat -s Unity > logs.txt
```

### **Check backend logs:**
- Look at terminal where `python main.py` is running
- Any errors will show there

### **Common issues:**
1. Backend not running → Start it
2. Wrong IP address → Update in Unity
3. Camera permission denied → Grant in Settings
4. ARCore not installed → Install from Play Store
5. Bad lighting → Move to brighter area

---

## 📞 **NEED HELP?**

Tell me:
1. Which step failed?
2. What error message did you see?
3. Paste logs if available

**I'll help you fix it immediately!**

---

**🚀 START NOW WITH STEP 1!**
