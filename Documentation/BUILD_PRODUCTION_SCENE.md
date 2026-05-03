# Building Clean Production Scene - Step-by-Step Guide

**Target Scene**: `Assets/ProjectCore/Scenes/CampusNavigation.unity`  
**Estimated Time**: 30 minutes  
**Status**: Ready to Build

---

## 📋 **PREREQUISITES**

Before starting, ensure:
- ✅ All compilation errors fixed (check Console)
- ✅ Icons generated via `Tools → Generate UI Icons`
- ✅ At least one map exported from FloorMapEditor
- ✅ Backend nodes.json exists

---

## 🏗️ **STEP 1: Open Clean Scene** (2 min)

1. In Unity Project window, navigate to:
   ```
   Assets/ProjectCore/Scenes/
   ```

2. Double-click `CampusNavigation.unity` to open it

3. **Expected**: Empty scene or basic setup

4. If scene has old content:
   - Select all GameObjects in Hierarchy (Ctrl+A)
   - Delete (Delete key)
   - Save scene (Ctrl+S)

---

## 🏗️ **STEP 2: Add AR Foundation Components** (5 min)

### 2.1 Create AR Session
1. Right-click in Hierarchy → `XR → AR Session`
2. GameObject named "AR Session" appears
3. **Verify**: Has `ARSession` component attached

### 2.2 Create XR Origin
1. Right-click in Hierarchy → `XR → XR Origin (Mobile AR)`
2. GameObject named "XR Origin" appears with child "Main Camera"
3. **Verify**: XR Origin has:
   - `XR Origin` component
   - `AR Plane Manager` component
   - `AR Raycast Manager` component
   - `AR Anchor Manager` component

### 2.3 Configure AR Plane Manager
1. Select "XR Origin" in Hierarchy
2. In Inspector, find `AR Plane Manager` component
3. Set:
   - Detection Mode: `Everything`
   - Plane Prefab: (leave empty or assign if you have one)

### 2.4 Verify Main Camera
1. Expand "XR Origin" in Hierarchy
2. Select "Main Camera"
3. **Verify** it has:
   - `Camera` component
   - `AR Camera Manager` component
   - `AR Camera Background` component
   - Tag: `MainCamera`

---

## 🏗️ **STEP 3: Add Campus App Root** (5 min)

### 3.1 Create CampusApp GameObject
1. Right-click in Hierarchy → `Create Empty`
2. Rename to: `CampusApp`
3. Reset Transform:
   - Position: (0, 0, 0)
   - Rotation: (0, 0, 0)
   - Scale: (1, 1, 1)

### 3.2 Add CampusRuntimeInstaller
1. Select `CampusApp` in Hierarchy
2. In Inspector, click `Add Component`
3. Search: `CampusRuntimeInstaller`
4. Click to add

### 3.3 Add AppController
1. With `CampusApp` still selected
2. Click `Add Component`
3. Search: `AppController`
4. Click to add

### 3.4 Add LocationRegistry
1. With `CampusApp` still selected
2. Click `Add Component`
3. Search: `LocationRegistry`
4. Click to add

### 3.5 Add QRLocationManager
1. With `CampusApp` still selected
2. Click `Add Component`
3. Search: `QRLocationManager`
4. Click to add

---

## 🏗️ **STEP 4: Add PathVisualizer** (3 min)

### 4.1 Create PathVisualizer GameObject
1. Right-click in Hierarchy → `Create Empty`
2. Rename to: `PathVisualizer`
3. Reset Transform to (0, 0, 0)

### 4.2 Add PathVisualizer Component
1. Select `PathVisualizer` in Hierarchy
2. Click `Add Component`
3. Search: `PathVisualizer`
4. Click to add

### 4.3 Assign Arrow Prefab
1. With `PathVisualizer` selected
2. In Inspector, find `PathVisualizer` component
3. Find `Arrow Prefab` field
4. Drag from Project window:
   ```
   Assets/ProjectCore/Prefabs/ArrowPrefab.prefab
   ```
5. **Verify**: Arrow Prefab field is now assigned

---

## 🏗️ **STEP 5: Configure Scene Hierarchy** (2 min)

Your Hierarchy should now look like:

```
CampusNavigation
├── AR Session
├── XR Origin
│   └── Main Camera
├── CampusApp
│   ├── CampusRuntimeInstaller
│   ├── AppController
│   ├── LocationRegistry
│   └── QRLocationManager
└── PathVisualizer
    └── PathVisualizer (component)
```

---

## 🏗️ **STEP 6: Save and Test** (5 min)

### 6.1 Save Scene
1. Press `Ctrl+S` (Windows) or `Cmd+S` (Mac)
2. **Verify**: Scene saved (no asterisk in tab)

### 6.2 Enter Play Mode
1. Press Play button (or F5)
2. **Watch Console** for:
   - ✅ `[LocationRegistry] Loaded X nodes.`
   - ✅ `[CampusRuntimeInstaller] ...` messages
   - ✅ No red errors

### 6.3 Verify UI Appears
1. In Game view, you should see:
   - Top bar with MENU and QR buttons
   - Bottom bar with status text and CHAT button
   - Status text: "Loading campus map..." or "Scan QR code to begin"

### 6.4 Test Menu
1. Click MENU button (top-left)
2. **Expected**: Menu panel slides in with dropdowns
3. Click MENU again to close

### 6.5 Test QR Scanner
1. Click QR button (top-right)
2. **Expected**: Scanner panel appears
3. Click X to close

### 6.6 Exit Play Mode
1. Press Play button again (or F5)

---

## 🏗️ **STEP 7: Build Settings** (3 min)

### 7.1 Add Scene to Build
1. Go to `File → Build Settings`
2. Click `Add Open Scenes`
3. **Verify**: `CampusNavigation` appears in Scenes list
4. **Verify**: Index is 0 (first scene)

### 7.2 Remove Old Scenes (Optional)
1. If `AR_Scene` is in the list, select it
2. Click `Remove Selection`
3. Only `CampusNavigation` should remain

### 7.3 Configure Platform
1. Select `Android` in Platform list
2. If not already selected, click `Switch Platform`
3. Wait for Unity to finish switching

---

## 🏗️ **STEP 8: Project Settings** (5 min)

### 8.1 Player Settings
1. In Build Settings window, click `Player Settings`
2. In Inspector, configure:

**Company Name**: Your name/company  
**Product Name**: AR Campus Navigation  
**Version**: 1.0.0

### 8.2 Android Settings
1. Expand `Other Settings` section
2. Set:
   - **Package Name**: `com.yourcompany.arcampus`
   - **Minimum API Level**: `Android 7.0 (API 24)` or higher
   - **Target API Level**: `Automatic (highest installed)`

### 8.3 XR Settings
1. Expand `XR Plug-in Management`
2. Click Android tab (Android icon)
3. **Enable**: `ARCore`
4. **Verify**: Checkmark appears next to ARCore

### 8.4 Graphics Settings
1. Go to `Edit → Project Settings → Graphics`
2. **Verify**: `Scriptable Render Pipeline Settings` is assigned
3. If not, assign: `Assets/Settings/URP-Performant.asset`

---

## ✅ **VERIFICATION CHECKLIST**

Before proceeding, verify:

- [ ] Scene has AR Session
- [ ] Scene has XR Origin with Main Camera
- [ ] CampusApp has CampusRuntimeInstaller
- [ ] PathVisualizer has Arrow Prefab assigned
- [ ] Play mode shows UI (menu, QR, chat buttons)
- [ ] No red errors in Console
- [ ] Scene is in Build Settings
- [ ] ARCore is enabled in XR Plug-in Management

---

## 🚨 **TROUBLESHOOTING**

### Issue: "No UI appears in Play mode"
**Fix**:
1. Check Console for errors
2. Verify CampusRuntimeInstaller is on CampusApp
3. Verify EventSystem exists (auto-created by CampusRuntimeUI)

### Issue: "ARSession not found" error
**Fix**:
1. Verify AR Session GameObject exists in scene
2. Verify it has ARSession component

### Issue: "Arrow Prefab not assigned" warning
**Fix**:
1. Select PathVisualizer in Hierarchy
2. Drag ArrowPrefab.prefab to Arrow Prefab field

### Issue: "No locations loaded"
**Fix**:
1. Open FloorMapEditor
2. Create and export a test map
3. Verify `Assets/ProjectCore/Resources/nodes.json` exists
4. Verify `ARBackend/nodes.json` exists

### Issue: "Icons not showing"
**Fix**:
1. Run `Tools → Generate UI Icons`
2. Verify icons exist in `Assets/ProjectCore/Resources/Icons/`
3. Restart Play mode

---

## 🎯 **NEXT STEPS**

After completing this guide:

1. **Test QR Scanning** (Editor Mode)
   - Enter Play mode
   - Click QR button
   - Click a simulated location button
   - Verify location is set

2. **Test Navigation Flow**
   - After QR scan, click MENU
   - Select Building → Floor → Room
   - Click NAVIGATE
   - Verify arrows appear (if backend running)

3. **Test Chat**
   - Click CHAT button
   - Type "take me to library"
   - Click SEND
   - Verify AI responds (if backend running)

4. **Build to Android**
   - Connect Android device
   - `File → Build and Run`
   - Test on real device with printed QR codes

---

## 📝 **NOTES**

- **CampusRuntimeInstaller** automatically disables legacy components (ModernUIBuilder, AIManager, etc.)
- **CampusRuntimeUI** builds the entire UI at runtime (no prefabs needed)
- **ModeManager** handles scanner vs navigation mode switching
- **QRLocationManager** is the ONLY source of start location (no camera fallbacks)

---

**Scene is now production-ready!** 🎉
