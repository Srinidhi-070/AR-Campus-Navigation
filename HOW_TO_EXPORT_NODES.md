# 📤 HOW TO EXPORT nodes.json FROM FLOOR MAP EDITOR

## 🎯 Quick Answer
In Unity Editor:
1. **Window → AR Navigation → Floor Map Editor**
2. Create/edit your floor map
3. Click **"🚀 Export → nodes.json"** button (orange button at bottom)

---

## 📋 COMPLETE WORKFLOW

### STEP 1: Open Floor Map Editor
```
Unity Editor → Window → AR Navigation → Floor Map Editor
```

### STEP 2: Create or Load a Map
**Option A: Create New Map**
- Enter Map Name (e.g., "Floor_0")
- Enter Building Name (e.g., "Main Block")
- Enter Floor Number (e.g., 0)
- Click **"✚ New Map"**

**Option B: Load Existing Map**
- Scroll to "📂 LOAD EXISTING" section
- Click on a saved map name to load it

### STEP 3: Design Your Floor Map
1. **Select Paint Mode** (left panel):
   - **■ Wall** - Non-walkable obstacles
   - **□ Walkable** - Corridors/paths
   - **◉ Entrance** - Building entrances
   - **◈ Room Door** - Room entrances
   - **▲ Staircase** - Stairs between floors
   - **⬆ Lift** - Elevators

2. **Paint on Grid** (right panel):
   - Click/drag to paint cells
   - Each cell = 1 meter in real world

3. **Name Important Nodes**:
   - Select **"✎ Select"** mode
   - Click a cell (entrance, room, stair, etc.)
   - Enter a unique name (e.g., "ENTRANCE", "ROOM_101")
   - Click **"✓ Assign Name"**

4. **Generate QR Codes** (optional):
   - After naming a node
   - Click **"⬛ Generate QR Code"**
   - QR saved to: `Assets/ProjectCore/Resources/QRCodes/`

### STEP 4: Save Your Map
Click **"💾 Save Map"** (blue button, top-left)

### STEP 5: Export to nodes.json
Click **"🚀 Export → nodes.json"** (orange button, bottom-left)

---

## ✅ WHAT HAPPENS WHEN YOU EXPORT

### Files Created/Updated:
1. **Unity Resources**:
   ```
   Assets/ProjectCore/Resources/nodes.json
   ```
   - Used by Unity app at runtime

2. **Backend**:
   ```
   ARBackend/nodes.json
   ```
   - Used by FastAPI backend for pathfinding

### What Gets Exported:
✅ All **named nodes** across all maps
✅ Node positions (x, y, z coordinates)
✅ Node types (entrance, room, staircase, lift, corridor)
✅ Building and floor information
✅ **Neighbors** (automatically calculated via BFS pathfinding)
✅ QR point markers

### What Does NOT Get Exported:
❌ Unnamed walkable cells (corridors without names)
❌ Wall cells
❌ Grid visualization data

---

## 📊 EXPORTED JSON FORMAT

```json
{
  "nodes": [
    {
      "id": "ENTRANCE",
      "displayName": "Entrance",
      "type": "entrance",
      "building": "Main Block",
      "floor": 0,
      "x": 5.0,
      "y": 0.0,
      "z": 10.0,
      "qr_point": true,
      "description": "Entrance on floor 0 of Main Block.",
      "neighbors": ["ROOM_101", "CORRIDOR_1"]
    },
    {
      "id": "ROOM_101",
      "displayName": "Room 101",
      "type": "room",
      "building": "Main Block",
      "floor": 0,
      "x": 15.0,
      "y": 0.0,
      "z": 10.0,
      "qr_point": true,
      "description": "Room 101 on floor 0 of Main Block.",
      "neighbors": ["ENTRANCE", "CORRIDOR_2"]
    }
  ]
}
```

---

## 🔍 HOW NEIGHBORS ARE CALCULATED

The editor uses **BFS (Breadth-First Search)** to find connections:

1. **Start at each named node**
2. **Traverse through walkable cells** (corridors)
3. **Stop when reaching another named node**
4. **Add as neighbor** (bidirectional)
5. **Repeat for all named nodes**

### Example:
```
[ENTRANCE] → (walkable) → (walkable) → [ROOM_101]
```
Result: `ENTRANCE.neighbors = ["ROOM_101"]`

---

## ⚠️ IMPORTANT RULES

### ✅ DO:
- Give **unique names** to all important locations
- Use **UPPERCASE** for consistency (e.g., "ENTRANCE", "ROOM_101")
- Connect nodes with **walkable corridors**
- Save map before exporting
- Test QR codes after generating

### ❌ DON'T:
- Use duplicate node names across different maps
- Leave gaps in walkable paths
- Export without saving first
- Forget to name entrance/room nodes

---

## 🐛 TROUBLESHOOTING

### Issue: "No named nodes were found"
**Solution**: 
- Select cells using "✎ Select" mode
- Assign names to important locations
- Save map and export again

### Issue: "Duplicate node_id detected"
**Solution**:
- Each node name must be unique across ALL maps
- Rename duplicate nodes (e.g., "ENTRANCE_BLOCK_A", "ENTRANCE_BLOCK_B")

### Issue: "Nodes not connected"
**Solution**:
- Ensure walkable corridor between nodes
- Check for wall cells blocking path
- Use "□ Walkable" mode to paint corridors

### Issue: "Export button does nothing"
**Solution**:
- Check Unity Console for errors
- Ensure MapManager and GridManager exist in scene
- Try saving map first, then export

---

## 📁 FILE LOCATIONS

### Unity Project:
```
Assets/
├── ProjectCore/
│   └── Resources/
│       ├── nodes.json          ← Exported here
│       └── QRCodes/            ← QR codes saved here
└── Editor/
    └── FloorMapEditor.cs       ← Editor tool script
```

### Backend:
```
ARBackend/
└── nodes.json                  ← Exported here (copy)
```

---

## 🎯 QUICK CHECKLIST

Before exporting, verify:
- [ ] Map is saved
- [ ] All entrances are named
- [ ] All rooms are named
- [ ] Stairs/lifts are named (if multi-floor)
- [ ] Walkable corridors connect all nodes
- [ ] No duplicate node names
- [ ] QR codes generated for key locations

Then click: **"🚀 Export → nodes.json"**

---

## 🚀 AFTER EXPORT

### Test in Unity:
1. Play mode in Unity
2. Check if locations load in dropdown

### Test in Backend:
```bash
cd ARBackend
python main.py
# Visit: http://localhost:8000
# Should show: "locations": [count]
```

### Test on Device:
1. Build APK
2. Install on device
3. Open app
4. Check Menu → Building/Floor/Room dropdowns populate

---

**✅ You're done! Your floor map is now ready for AR navigation.**
