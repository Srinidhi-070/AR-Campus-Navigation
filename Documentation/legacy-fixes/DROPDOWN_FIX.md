# 🔧 DROPDOWN EMPTY - DIAGNOSIS & FIX

## ✅ Backend Status
Backend IS running correctly at `http://192.168.1.4:8000`

Test result:
```json
{
  "locations": [
    {"id": "TOILET_2", "displayName": "Toilet 2", "type": "room", "building": "House", "floor": 1},
    {"id": "BATHROOM_1", "displayName": "Bathroom 1", "type": "room", "building": "House", "floor": 1},
    {"id": "KITCHEN_1", "displayName": "Kitchen 1", "type": "room", "building": "House", "floor": 1},
    {"id": "HOUSE_ENTRANCE_1", "displayName": "House Entrance 1", "type": "entrance", "building": "House", "floor": 1},
    {"id": "LATRIN_1", "displayName": "Latrin 1", "type": "room", "building": "House", "floor": 2},
    {"id": "ROOM_2", "displayName": "Room 2", "type": "room", "building": "House", "floor": 2},
    {"id": "HOUSE_ENTRANCE_2", "displayName": "House Entrance 2", "type": "entrance", "building": "House", "floor": 2},
    {"id": "ROOM_1", "displayName": "Room 1", "type": "room", "building": "House", "floor": 2}
  ]
}
```

## 📊 Expected Dropdown Content

**Building Dropdown:** House

**Floor Dropdown (when House selected):**
- Floor 1
- Floor 2

**Destination Dropdown:**
- **Floor 1:** Toilet 2, Bathroom 1, Kitchen 1 (3 rooms)
- **Floor 2:** Latrin 1, Room 2, Room 1 (3 rooms)

Note: Entrances are filtered out (by design)

## ❌ Problem: Device Can't Connect

If dropdowns are empty on device, the issue is:

### 1. Device Not on Same Network
- Computer: Connected to WiFi
- Phone: Must be on SAME WiFi network
- Check: Settings → WiFi → Same network name?

### 2. Firewall Blocking Port 8000
Windows Firewall might be blocking incoming connections.

### 3. Wrong IP Address
The IP `192.168.1.4` might have changed.

## 🔧 FIXES

### Fix 1: Check Device Logs
```bash
adb logcat -s Unity | findstr "CampusApiClient"
```

Look for:
- `Fetching locations from: http://192.168.1.4:8000/locations`
- `FetchLocations success` ✅ or `FetchLocations failed` ❌

### Fix 2: Verify Computer IP
```bash
ipconfig
```

Look for "IPv4 Address" under your WiFi adapter.
If it's NOT `192.168.1.4`, update the backend URL in Unity:
1. Unity → Hierarchy → CampusApp
2. Inspector → Campus Api Client → Base Url
3. Change to correct IP
4. Rebuild APK

### Fix 3: Allow Firewall
```bash
netsh advfirewall firewall add rule name="Python Backend" dir=in action=allow protocol=TCP localport=8000
```

### Fix 4: Test from Phone Browser
On your phone, open browser and go to:
```
http://192.168.1.4:8000/locations
```

If you see JSON data → Network is OK, Unity has a bug
If you see error → Network issue, fix firewall/WiFi

## 🎯 QUICK TEST

### On Computer:
```bash
# Check backend is running
curl http://localhost:8000/locations

# Check from network IP
curl http://192.168.1.4:8000/locations
```

### On Phone:
1. Open Chrome browser
2. Go to: `http://192.168.1.4:8000/locations`
3. Should see JSON data

If phone browser works but app doesn't → Unity issue
If phone browser fails → Network issue

## 📱 Expected App Behavior

When app loads successfully:
1. Status shows: "Loading campus map..."
2. Backend request sent
3. Status shows: "Scan QR code to begin"
4. Dropdowns populated:
   - Building: "House"
   - Floor: "Floor 1", "Floor 2"
   - Destination: (3 rooms per floor)

When backend fails:
1. Status shows: "Backend offline. Use QR to navigate."
2. Dropdowns show: "No Buildings", "No Floors", "No Destinations"
3. QR button still works (offline mode)

## 🔍 Debug Steps

1. Check device logs for connection error
2. Test backend from phone browser
3. Verify IP address matches
4. Check firewall settings
5. Ensure phone and computer on same WiFi
