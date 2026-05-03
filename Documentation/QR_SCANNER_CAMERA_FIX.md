# QR Scanner Camera Fix - APPLIED

## Issue
QR scanner opens but shows blank screen on Android device.

## Root Causes Fixed

### 1. ✅ Camera Permission Not Requested
**Problem**: Android requires runtime permission request for camera access.

**Fix Applied**: Updated `QRScanner.cs` with proper Android permission handling:
```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Camera))
{
    UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Camera);
    // Wait for user response
}
#endif
```

### 2. ✅ Missing Android Manifest Permissions
**Problem**: AndroidManifest.xml didn't explicitly declare camera permission.

**Fix Applied**: Created `Assets/Plugins/Android/AndroidManifest.xml` with:
```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-feature android:name="android.hardware.camera" android:required="true" />
```

### 3. ✅ No Camera Initialization Logging
**Problem**: Couldn't debug why camera wasn't starting.

**Fix Applied**: Added comprehensive logging:
- Camera device detection
- Permission status
- Camera start/stop events
- Error conditions

### 4. ✅ Back Camera Selection
**Problem**: Was using first camera (often front-facing).

**Fix Applied**: Now prefers back camera for QR scanning:
```csharp
for (int i = 0; i < devices.Length; i++)
{
    if (!devices[i].isFrontFacing)
    {
        cameraName = devices[i].name;
        break;
    }
}
```

---

## Files Modified

1. **QRScanner.cs**
   - Added Android-specific permission request
   - Added detailed logging
   - Added camera start timeout handling
   - Prefer back camera for scanning

2. **AndroidManifest.xml** (NEW)
   - Declares CAMERA permission
   - Declares camera hardware requirement
   - Declares INTERNET permission

---

## Testing Steps

### 1. Rebuild APK
```
1. Close Unity
2. Reopen Unity (to reload scripts)
3. File → Build Settings → Build And Run
4. Wait for build to complete
```

### 2. On Device - First Launch
When you open the app and tap QR button:
1. **Permission Dialog** should appear: "Allow AR Campus Nav to take pictures and record video?"
2. Tap **"Allow"**
3. Camera feed should appear in scanner

### 3. Check Logs (If Still Not Working)
```bash
adb logcat -s Unity | findstr "QRScanner"
```

Expected logs:
```
[QRScanner] Starting camera scanner...
[QRScanner] Requesting camera permission...
[QRScanner] Camera permission granted
[QRScanner] Found 2 camera devices
[QRScanner] Camera 0: Camera 0 (Front: true)
[QRScanner] Camera 1: Camera 1 (Front: false)
[QRScanner] Using back camera: Camera 1
[QRScanner] Created WebCamTexture: Camera 1 1280x720@30fps
[QRScanner] Assigned texture to preview image
[QRScanner] Starting camera...
[QRScanner] Camera started: 1280x720
```

---

## Troubleshooting

### Issue: Permission Dialog Doesn't Appear
**Cause**: Permission already denied in previous install

**Fix**:
1. Uninstall app completely
2. Reinstall APK
3. Permission dialog will appear again

Or manually grant permission:
1. Settings → Apps → AR Campus Nav
2. Permissions → Camera → Allow

### Issue: Camera Still Blank After Permission
**Cause**: Camera initialization failed

**Check Logs**:
```bash
adb logcat -s Unity | findstr "QRScanner"
```

Look for:
- "No camera found" → Device has no camera
- "Camera failed to start" → Camera in use by another app
- "Preview image is null" → UI not initialized

**Fix**:
1. Close other camera apps
2. Restart device
3. Rebuild with clean Library folder

### Issue: Camera Shows But Doesn't Scan
**Cause**: ZXing library not working or QR code format wrong

**Check**:
1. Print QR code clearly (not from screen)
2. Good lighting
3. Hold steady for 2-3 seconds
4. QR code must contain valid JSON:
   ```json
   {"node_id":"ENTRANCE_MAIN","building":"Main Block","floor":0}
   ```

### Issue: "Camera permission denied" Message
**Cause**: User tapped "Deny" on permission dialog

**Fix**:
1. Settings → Apps → AR Campus Nav → Permissions → Camera → Allow
2. Restart app

---

## Verification Checklist

After rebuild, verify:
- [ ] Permission dialog appears on first QR button tap
- [ ] Camera feed visible in scanner (not black)
- [ ] Can see yourself/surroundings in camera
- [ ] Scan frame (blue border) visible
- [ ] Status text shows "Point camera at a campus QR code"
- [ ] Can close scanner with X button

---

## Next Steps

1. **Rebuild APK** with fixes
2. **Uninstall old app** from device
3. **Install new APK**
4. **Test QR scanner**:
   - Tap QR button
   - Grant permission
   - Verify camera feed appears
5. **Test QR scanning**:
   - Generate QR code in Unity (Floor Map Editor)
   - Print QR code
   - Scan with app

---

## Additional Notes

### Camera Resolution
- Requesting: 1280x720 @ 30fps
- Actual resolution may vary by device
- Lower resolution = faster scanning
- Higher resolution = better accuracy

### Camera Selection
- Prefers back camera (better for scanning)
- Falls back to front camera if no back camera
- Uses first camera if only one available

### Permission Handling
- Requests permission at runtime (Android 6.0+)
- Waits up to 10 seconds for user response
- Shows error if permission denied
- Can be granted later in Settings

---

**All fixes have been applied. Rebuild the APK and test on device.**
