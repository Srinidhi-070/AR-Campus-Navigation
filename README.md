# AR Campus Navigation System

**Status**: In Development - Requires Cleanup  
**Platform**: Unity 2022.3+ | Android (ARCore)

---

## 📁 Project Structure

```
AR_Spatial_Client/
├── ARBackend/              # Python FastAPI backend
│   ├── main.py            # API server (needs fixing)
│   ├── services/          # Graph & chat services
│   ├── schemas.py         # Data models
│   └── nodes.json         # Campus graph data
│
├── ARSpatialClient/       # Unity project
│   ├── Assets/
│   │   ├── ProjectCore/   # Main application code
│   │   │   ├── Scenes/    # CampusNavigation.unity
│   │   │   ├── Scripts/   # C# scripts
│   │   │   └── Resources/ # Icons, nodes.json
│   │   └── Editor/        # Floor Map Editor, Icon Generator
│   └── ProjectSettings/
│
├── Builds/                # Android APK builds
└── Documentation/         # All documentation files
```

---

## 🎯 Core Features

### Completed
- ✅ Floor Map Editor (Unity Editor tool)
- ✅ QR Code Generation
- ✅ Backend API (locations, pathfinding)
- ✅ Runtime UI System
- ✅ Navigation Flow
- ✅ Android Build Configuration

### Issues
- ❌ Backend doesn't start properly (AI model loading issue)
- ❌ UI not appearing on device
- ❌ Multiple accumulated patches causing conflicts
- ❌ Too many legacy components in scene

---

## 🚀 Quick Start (After Cleanup)

### 1. Backend Setup
```bash
cd ARBackend
pip install -r requirements.txt
python main.py
# Should show: "Uvicorn running on http://0.0.0.0:8000"
```

### 2. Unity Setup
```
1. Open project in Unity 2022.3+
2. Tools → Generate UI Icons
3. Update Backend URL: CampusApp → Base Url → http://YOUR_IP:8000
4. File → Build Settings → Build
```

### 3. Android Device
```bash
adb install Builds/ARCampusNav.apk
```

---

## 📋 Current Issues

1. **Backend**: AI model loading causes window popup, server doesn't start
2. **Unity Scene**: Contains legacy components causing errors
3. **Device Build**: App installs but UI doesn't appear
4. **Architecture**: Mix of runtime and scene-based components

---

## 🔧 Required Cleanup

### Backend
- [ ] Fix main.py to start server without AI model issues
- [ ] Make chat service optional
- [ ] Test basic endpoints work

### Unity
- [ ] Remove all legacy scene components
- [ ] Keep only: CampusApp, Main Camera, Directional Light
- [ ] Verify runtime installer works
- [ ] Test UI generation on device

### Documentation
- [x] Organize all MD files into Documentation folder
- [ ] Create single source of truth README
- [ ] Remove duplicate/outdated guides

---

## 📞 Next Steps

**Awaiting instructions for:**
1. Which Unity scripts to keep/remove
2. How to restructure the scene
3. Backend simplification approach
4. Testing strategy

---

## 🛠️ Technical Details

**Unity Version**: 2022.3.62f3  
**Android Min SDK**: API 24 (Android 7.0)  
**Scripting Backend**: IL2CPP  
**Architecture**: ARM64  
**Backend**: Python 3.8+ | FastAPI | Uvicorn  
**Computer IP**: 192.168.1.4  
**Package Name**: com.srinidhi.arcampusnav

---

**Project needs systematic cleanup before proceeding with new features.**
