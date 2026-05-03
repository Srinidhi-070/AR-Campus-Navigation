# Integration Testing Guide

**Purpose**: Verify end-to-end functionality of AR Campus Navigation  
**Estimated Time**: 1-2 hours  
**Prerequisites**: Clean production scene built, backend running

---

## 🎯 **TEST CATEGORIES**

1. Fresh Export State
2. QR Scan Tests
3. Mode Tests
4. Dropdown Navigation Tests
5. Chat Tests
6. Path Tests
7. UI Tests
8. Android Build Tests

---

## 📦 **TEST 1: Fresh Export State** (10 min)

### Purpose
Verify app behavior when no map data exists.

### Setup
1. Delete or rename `Assets/ProjectCore/Resources/nodes.json`
2. Delete or rename `ARBackend/nodes.json`
3. Restart Unity Editor

### Test Steps
1. Enter Play mode
2. **Expected Results**:
   - ✅ Status text: "No floor map exported yet." or similar
   - ✅ MENU button disabled or shows "No Buildings"
   - ✅ CHAT button disabled
   - ✅ QR button still enabled
   - ✅ No arrows visible
   - ✅ No red errors in Console

### Cleanup
1. Exit Play mode
2. Restore nodes.json files
3. Restart Unity Editor

---

## 📦 **TEST 2: QR Scan Tests** (20 min)

### Test 2.1: Valid Campus QR Code
**Setup**: Export a map with at least 2 named nodes

**Steps**:
1. Enter Play mode
2. Click QR button
3. Click a simulated location (e.g., "ENTRANCE_MAIN")
4. **Expected**:
   - ✅ Status shows "Location detected"
   - ✅ Location text shows "You are at: Entrance Main"
   - ✅ Scanner closes after 1 second
   - ✅ Status changes to "Scan QR code to begin" or "Navigation active"

### Test 2.2: Malformed JSON
**Setup**: Manually trigger with invalid JSON

**Steps**:
1. In QRScanner.cs, temporarily add test button:
   ```csharp
   qrScanner.SimulateScan("{invalid json}");
   ```
2. **Expected**:
   - ✅ Status shows "Invalid campus QR code"
   - ✅ Location text remains empty
   - ✅ Scanner stays open
   - ✅ No crash or exception

### Test 2.3: Unknown Node ID
**Setup**: Create QR with non-existent node_id

**Steps**:
1. Simulate scan with:
   ```json
   {"node_id":"DOES_NOT_EXIST","building":"Main Block","floor":0}
   ```
2. **Expected**:
   - ✅ Status shows "Invalid campus QR code" or "Node not found"
   - ✅ Location not set
   - ✅ Navigation remains disabled

### Test 2.4: Non-QR-Point Node
**Setup**: Export map, manually edit nodes.json to set `qr_point: false` for a node

**Steps**:
1. Scan that node's QR code
2. **Expected**:
   - ✅ Validation fails (if implemented)
   - ✅ Or: Location sets but with warning

**Note**: Current implementation sets all named nodes as `qr_point: true`, so this test may not apply.

---

## 📦 **TEST 3: Mode Tests** (15 min)

### Test 3.1: Scanner Mode Hides Navigation
**Steps**:
1. Enter Play mode
2. Click MENU to open navigation panel
3. Click QR button
4. **Expected**:
   - ✅ Menu panel closes
   - ✅ Scanner panel opens
   - ✅ Navigation UI (status, directions, chat button) hidden
   - ✅ Only scanner UI visible

### Test 3.2: Navigation Mode Hides Scanner
**Steps**:
1. Open scanner
2. Click X to close
3. **Expected**:
   - ✅ Scanner panel closes
   - ✅ Navigation UI reappears
   - ✅ Status and direction text visible
   - ✅ MENU and CHAT buttons visible

### Test 3.3: No Overlap
**Steps**:
1. Open scanner
2. Try clicking through scanner to AR content
3. **Expected**:
   - ✅ Scanner blocks all input
   - ✅ AR interaction disabled
   - ✅ No touch events pass through

---

## 📦 **TEST 4: Dropdown Navigation Tests** (20 min)

### Test 4.1: Building Filter
**Setup**: Export maps with multiple buildings

**Steps**:
1. Scan QR code
2. Open MENU
3. Select different buildings from Building dropdown
4. **Expected**:
   - ✅ Floor dropdown updates with floors for that building
   - ✅ Room dropdown updates with rooms on selected floor
   - ✅ No rooms from other buildings appear

### Test 4.2: Floor Filter
**Setup**: Export maps with multiple floors

**Steps**:
1. Open MENU
2. Select different floors from Floor dropdown
3. **Expected**:
   - ✅ Room dropdown updates with rooms on that floor only
   - ✅ Rooms from other floors don't appear

### Test 4.3: Navigate Before QR Scan
**Steps**:
1. Enter Play mode (no QR scan)
2. Open MENU
3. Select destination
4. Click NAVIGATE
5. **Expected**:
   - ✅ Status shows "Scan a QR code first!"
   - ✅ No path generated
   - ✅ No arrows appear
   - ✅ Menu stays open or closes gracefully

### Test 4.4: Navigate After QR Scan
**Setup**: Backend running, map exported

**Steps**:
1. Scan QR code (e.g., ENTRANCE_MAIN)
2. Open MENU
3. Select Building → Floor → Room (e.g., LIBRARY)
4. Click NAVIGATE
5. **Expected**:
   - ✅ Status shows "Calculating path to Library..."
   - ✅ Backend request sent
   - ✅ Arrows appear along path
   - ✅ Direction text shows first step
   - ✅ Menu closes

---

## 📦 **TEST 5: Chat Tests** (20 min)

### Test 5.1: Single Chat Panel
**Steps**:
1. Enter Play mode
2. Count chat panels in Hierarchy
3. **Expected**:
   - ✅ Only ONE chat panel exists
   - ✅ No duplicate panels

### Test 5.2: Single Input Field
**Steps**:
1. Open CHAT
2. Count input fields
3. **Expected**:
   - ✅ Only ONE input field visible
   - ✅ No duplicate inputs

### Test 5.3: Backend Destination Resolution
**Setup**: Backend running

**Steps**:
1. Scan QR code
2. Open CHAT
3. Type: "take me to library"
4. Click SEND
5. **Expected**:
   - ✅ User message appears in chat
   - ✅ Status shows "Thinking..."
   - ✅ AI response appears
   - ✅ Path request sent to backend
   - ✅ Arrows appear
   - ✅ Status shows "Navigation active"

### Test 5.4: Chat Before QR Scan
**Steps**:
1. Enter Play mode (no QR scan)
2. Open CHAT
3. Type: "take me to library"
4. Click SEND
5. **Expected**:
   - ✅ AI responds with destination
   - ✅ But navigation fails with "Scan QR code first"
   - ✅ No arrows appear

### Test 5.5: Ambiguous Query
**Steps**:
1. Scan QR code
2. Open CHAT
3. Type: "where is the thing?"
4. **Expected**:
   - ✅ AI responds with clarification request
   - ✅ Or: "Could not resolve destination"
   - ✅ No path generated

---

## 📦 **TEST 6: Path Tests** (25 min)

### Test 6.1: No Hardcoded Routes
**Setup**: Export a NEW map with different layout

**Steps**:
1. Export new map
2. Restart backend
3. Navigate between nodes
4. **Expected**:
   - ✅ Path follows NEW map layout
   - ✅ Not old hardcoded paths
   - ✅ Arrows match exported graph

### Test 6.2: Path Changes When Map Changes
**Steps**:
1. Export map with ROOM_A → CORRIDOR → ROOM_B
2. Navigate from ROOM_A to ROOM_B
3. Note path
4. Edit map: add shortcut between ROOM_A and ROOM_B
5. Re-export
6. Restart backend
7. Navigate again
8. **Expected**:
   - ✅ Path uses new shortcut
   - ✅ Fewer arrows
   - ✅ Different route

### Test 6.3: Multi-Floor Path (Stairs)
**Setup**: Export maps for Floor 0 and Floor 1 with stair connection

**Steps**:
1. Scan QR on Floor 0
2. Navigate to room on Floor 1
3. **Expected**:
   - ✅ Path includes stair node
   - ✅ Arrows lead to stairs
   - ✅ Direction text mentions "Take stairs to Floor 1"
   - ✅ Path continues on Floor 1

### Test 6.4: Multi-Floor Path (Lift)
**Setup**: Export maps with lift connection

**Steps**:
1. Scan QR on Floor 0
2. Navigate to room on Floor 2
3. **Expected**:
   - ✅ Path includes lift node
   - ✅ Direction text mentions "Take lift to Floor 2"

### Test 6.5: No Path Available
**Setup**: Export map with disconnected nodes

**Steps**:
1. Scan QR at isolated node
2. Navigate to node in different disconnected area
3. **Expected**:
   - ✅ Status shows "No path found"
   - ✅ No arrows appear
   - ✅ Clear error message
   - ✅ No crash

---

## 📦 **TEST 7: UI Tests** (15 min)

### Test 7.1: No Duplicate Inputs
**Steps**:
1. Enter Play mode
2. Search Hierarchy for "Input" or "ChatInput"
3. **Expected**:
   - ✅ Only ONE input field exists
   - ✅ Located in ChatPanel

### Test 7.2: Icon Display
**Steps**:
1. Verify icons generated: `Tools → Generate UI Icons`
2. Enter Play mode
3. Check buttons
4. **Expected**:
   - ✅ MENU button shows menu icon (3 lines)
   - ✅ QR button shows QR icon (grid)
   - ✅ Close buttons show X icon
   - ✅ SEND button shows arrow icon
   - ✅ No placeholder squares

### Test 7.3: No Sample XR Buttons
**Steps**:
1. Enter Play mode
2. Look for XR template buttons (Create, Delete, Options, etc.)
3. **Expected**:
   - ✅ No XR sample buttons visible
   - ✅ Only campus navigation UI

### Test 7.4: Responsive Layout
**Steps**:
1. Change Game view aspect ratio
2. Test: 16:9, 9:16, 4:3
3. **Expected**:
   - ✅ UI scales correctly
   - ✅ No overlapping elements
   - ✅ Text readable
   - ✅ Buttons accessible

---

## 📦 **TEST 8: Android Build Tests** (30 min)

### Test 8.1: Build Success
**Steps**:
1. Connect Android device (USB debugging enabled)
2. `File → Build and Run`
3. **Expected**:
   - ✅ Build completes without errors
   - ✅ APK installs on device
   - ✅ App launches

### Test 8.2: Camera Permission
**Steps**:
1. Launch app on device
2. Click QR button
3. **Expected**:
   - ✅ Camera permission prompt appears
   - ✅ After granting, camera feed shows
   - ✅ Scan frame visible

### Test 8.3: Real QR Scan
**Setup**: Print QR codes from `Assets/ProjectCore/Resources/QRCodes/`

**Steps**:
1. Open scanner
2. Point camera at printed QR code
3. **Expected**:
   - ✅ QR code detected
   - ✅ Location set correctly
   - ✅ Scanner closes
   - ✅ Status updates

### Test 8.4: AR Plane Detection
**Steps**:
1. After QR scan, navigate to destination
2. Move device to scan floor
3. **Expected**:
   - ✅ AR planes detected
   - ✅ Arrows appear on floor
   - ✅ Arrows track with device movement

### Test 8.5: End-to-End Demo Flow
**Steps**:
1. Launch app
2. Scan QR code at entrance
3. Open MENU
4. Select destination
5. Click NAVIGATE
6. Follow arrows
7. Reach destination
8. **Expected**:
   - ✅ Complete flow works
   - ✅ No crashes
   - ✅ Arrows guide correctly
   - ✅ Status updates appropriately

---

## 📊 **TEST RESULTS TEMPLATE**

Use this template to record results:

```
TEST: [Test Name]
DATE: [Date]
TESTER: [Your Name]
DEVICE: [Device Model / Unity Editor]

RESULT: ✅ PASS / ❌ FAIL / ⚠️ PARTIAL

NOTES:
- [Observation 1]
- [Observation 2]

ISSUES FOUND:
- [Issue 1]
- [Issue 2]

SCREENSHOTS:
- [Attach if needed]
```

---

## 🚨 **COMMON ISSUES & FIXES**

### Issue: Backend not responding
**Symptoms**: "Could not reach backend" error  
**Fix**:
1. Verify backend running: `python main.py`
2. Check backend URL in CampusApiClient
3. Verify nodes.json exists in ARBackend/

### Issue: Arrows not appearing
**Symptoms**: Path calculated but no arrows  
**Fix**:
1. Verify PathVisualizer has Arrow Prefab assigned
2. Check Console for "Arrow Prefab is NOT assigned" error
3. Verify arrows are not behind floor (Y position)

### Issue: QR scan not working on device
**Symptoms**: Camera shows but no detection  
**Fix**:
1. Verify ZXing plugin installed
2. Check camera permission granted
3. Ensure QR code is well-lit and in focus
4. Try different QR code size (larger)

### Issue: UI not showing
**Symptoms**: Blank screen in Play mode  
**Fix**:
1. Verify CampusRuntimeInstaller on CampusApp
2. Check Console for errors
3. Verify Canvas created (check Hierarchy)

---

## ✅ **SIGN-OFF CHECKLIST**

Before considering testing complete:

- [ ] All 8 test categories executed
- [ ] At least 80% tests passing
- [ ] Critical path (QR → Navigate → Arrows) works
- [ ] No red errors in Console
- [ ] Android build tested on real device
- [ ] Test results documented
- [ ] Known issues logged

---

**Testing complete! Ready for demo/submission.** 🎉
