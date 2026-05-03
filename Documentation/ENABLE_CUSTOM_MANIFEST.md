# 🚨 CRITICAL FIX: App Not Appearing in Device

## ❌ THE PROBLEM

**App installs but doesn't appear in app drawer** because:
- AndroidManifest.xml doesn't have Activity declaration
- Unity is NOT using the custom manifest we created
- Need to enable "Custom Main Manifest" in Unity Player Settings

---

## ✅ THE SOLUTION (STEP-BY-STEP)

### STEP 1: Enable Custom Manifest in Unity

**In Unity Editor:**

1. Go to: **Edit → Project Settings**
2. Click **Player** (left sidebar)
3. Click **Android tab** (robot icon at top)
4. Scroll down and expand **Publishing Settings** section
5. Find checkbox: **Custom Main Manifest**
6. **CHECK IT** ✅
7. Close Project Settings window

**CRITICAL**: This checkbox MUST be checked for Unity to use our custom AndroidManifest.xml!

---

### STEP 2: Verify Manifest File Exists

Check that this file exists:
```
Assets/Plugins/Android/AndroidManifest.xml
```

If it doesn't exist, I already created it for you. If Unity asks to create it, click YES.

---

### STEP 3: Rebuild APK

**IMPORTANT**: You MUST rebuild after enabling custom manifest!

1. **File → Build Settings**
2. Make sure **Android** is selected
3. Click **Build** button
4. Save as: `D:\AR_Spatial_Client\arapp.apk` (overwrite)
5. Wait for "Build Successful"

---

### STEP 4: Install and Test

After build completes, tell me "done" and I'll install it for you.

---

## 🎯 WHY THIS HAPPENS

Unity has a setting called "Custom Main Manifest" that controls whether it uses your custom AndroidManifest.xml or generates its own.

**When UNCHECKED** (default):
- Unity generates its own manifest
- Your custom manifest is IGNORED
- Activity doesn't get `android:exported="true"`
- App installs but has no launcher icon

**When CHECKED**:
- Unity uses your custom manifest
- Activity gets proper declaration
- App appears in app drawer
- Can be launched

---

## 📋 CHECKLIST

Before rebuilding:
- [ ] Edit → Project Settings → Player → Android
- [ ] Publishing Settings → Custom Main Manifest is CHECKED ✅
- [ ] AndroidManifest.xml exists in Assets/Plugins/Android/
- [ ] File → Build Settings → Android selected

After rebuilding:
- [ ] Build Successful message appears
- [ ] arapp.apk file updated (check timestamp)
- [ ] Tell me "done" so I can install it

---

## 🔍 HOW TO VERIFY IT'S ENABLED

In Unity:
1. Edit → Project Settings → Player
2. Android tab
3. Publishing Settings section
4. Look for: **Custom Main Manifest** ✅

Should have a checkmark!

---

## 🎉 EXPECTED RESULT

After enabling custom manifest and rebuilding:
- ✅ App icon appears in device app drawer
- ✅ Icon labeled "ARSpatialClient"
- ✅ Tap icon → App launches
- ✅ UI appears on screen

---

## 🚨 IMPORTANT NOTES

1. **You MUST enable the checkbox BEFORE building**
2. **Old APK won't work** - must rebuild after enabling
3. **This is a one-time fix** - once enabled, stays enabled
4. **Don't skip this step** - it's critical for Android 12+

---

## 📞 NEXT STEPS

**RIGHT NOW:**

1. **Open Unity**
2. **Edit → Project Settings → Player → Android**
3. **Publishing Settings → CHECK "Custom Main Manifest"**
4. **File → Build Settings → Build**
5. **Tell me "done"**

I'll install it immediately and verify the app appears!

---

**This is the missing piece! Enable that checkbox and rebuild!** 🚀
