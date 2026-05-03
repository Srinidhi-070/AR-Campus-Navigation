# ✅ BACKEND URL UPDATED

## Your Computer's IP: **192.168.1.4**

Backend URL has been updated in the code to: `http://192.168.1.4:8000`

---

## 🚀 REMAINING STEPS

### 1. Generate Icons (1 minute)
```
In Unity: Tools → Generate UI Icons
Wait for: "Icons generated successfully"
```

### 2. Build APK (15-20 minutes)
```
1. File → Build Settings
2. Click "Build" button
3. Save as: d:\AR_Spatial_Client\Builds\ARCampusNav_Clean.apk
4. Wait 15-20 minutes
5. Watch for: "Build completed with a result of 'Succeeded'"
```

### 3. Start Backend (1 minute)
```
Open NEW Command Prompt:
cd d:\AR_Spatial_Client\ARBackend
python main.py

Should see: "Uvicorn running on http://0.0.0.0:8000"
KEEP THIS WINDOW OPEN!
```

### 4. Install APK (1 minute)
```
adb install "d:\AR_Spatial_Client\Builds\ARCampusNav_Clean.apk"
Should see: "Success"
```

### 5. Test on Device
```
1. On phone: Find and open "ARCampusNav" app
2. Grant camera permission
3. Check:
   - Does UI appear? (hamburger menu, QR button, chat button)
   - What does status text say?
   - Do buttons respond to taps?
```

---

## 🎯 EXPECTED STATUS TEXT

**Good**: "Scan QR code to begin"  
**Bad**: "Could not load campus data"

If you see "Could not load campus data":
- Check backend is running
- Check phone is on same WiFi (192.168.1.x)
- Test in phone browser: http://192.168.1.4:8000

---

## 📱 ENSURE SAME WIFI

**CRITICAL**: Phone and computer must be on same WiFi network!

Your computer is on: **192.168.1.4**  
Your phone should be on: **192.168.1.x** (same network)

Check phone WiFi:
- Settings → WiFi → Check connected network
- Should match your computer's WiFi

---

## ⏱️ TIME REMAINING

- Generate icons: 1 min
- Build APK: 15-20 min
- Start backend: 1 min
- Install & test: 5 min

**Total: ~25 minutes**

---

**START WITH: Tools → Generate UI Icons**
