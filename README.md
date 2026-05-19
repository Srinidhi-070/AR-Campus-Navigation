# AR Campus Navigation System

**Status**: Working — AR navigation functional on device  
**Platform**: Unity 2022.3+ | Android (ARCore)

---

## 📁 Project Structure

```
AR_Spatial_Client/
├── ARBackend/              # Python FastAPI backend
│   ├── main.py            # API server entry point
│   ├── services/          # Graph & chat services
│   ├── schemas.py         # Pydantic data models
│   └── nodes.json         # Campus graph data
│
├── ARSpatialClient/       # Unity project
│   ├── Assets/
│   │   ├── ProjectCore/   # Main application code
│   │   │   ├── Scenes/    # CampusNavigation.unity
│   │   │   ├── Scripts/   # C# scripts (Core, AR, Navigation, UI, etc.)
│   │   │   └── Resources/ # Icons, prefabs, nodes.json (offline fallback)
│   │   └── Editor/        # Floor Map Editor, Icon Generator
│   └── ProjectSettings/
│
├── Builds/                # Android APK builds
└── Documentation/         # All documentation files
```

---

## 🎯 Core Features

- ✅ AR Navigation with ARCore (QR-initiated, arrow-based path rendering)
- ✅ Floor Map Editor (Unity Editor tool for authoring campus graphs)
- ✅ QR Code Scanning (XRCpuImage-based, no WebCamTexture)
- ✅ Backend API (FastAPI — locations, A* pathfinding, AI chat)
- ✅ Runtime UI System (built entirely at runtime via CampusRuntimeInstaller)
- ✅ Offline Fallback (local Dijkstra pathfinding from bundled nodes.json)
- ✅ AI Chat Navigation (Ollama LLM + semantic matching)
- ✅ Multi-floor Navigation (staircase/lift node connectivity)

---

## 🚀 Quick Start

### 1. Backend Setup

#### Option A: Docker (recommended)
```bash
cd ARBackend
docker compose up --build
```

#### Option B: Local Python
```bash
cd ARBackend
pip install -r requirements.txt
python main.py
# Server runs on http://0.0.0.0:8000
```

**API Endpoints:**
- `GET /locations` — All campus nodes
- `POST /get-path` — A* pathfinding between two nodes
- `POST /chat` — AI destination resolution (requires Ollama)

### 2. Unity Setup
1. Open project in Unity 2022.3+
2. Run `Tools → Generate UI Icons`
3. Set Backend URL: `CampusApp → Base Url → http://YOUR_LAN_IP:8000`
4. `File → Build Settings → Build`

### 3. Android Device
```bash
adb install Builds/ARCampusNav.apk
# Or use: install_to_device.bat
```

---

## 🏗️ Architecture

### Runtime Flow
```
AppController (singleton, DontDestroyOnLoad)
  └── CampusRuntimeInstaller
        ├── ARFoundationBootstrap (XR Origin, ARSession, Plane Detection)
        ├── CampusRuntimeUI (all UI built at runtime)
        ├── QRScanner → QRScannerUI (XRCpuImage + ZXing)
        ├── QRLocationManager (single source of truth for user position)
        ├── NavigationFlowController (pathfinding orchestration)
        ├── PathVisualizer (AR arrow rendering)
        ├── ChatManager (AI chat integration)
        └── ModeManager (navigation / scanner / chat mode switching)
```

### Key Design Decisions
- **No scene-based UI**: All UI is created at runtime to avoid legacy conflicts
- **QR-only initialization**: Navigation requires a QR scan to establish position
- **XRCpuImage for QR**: Avoids WebCamTexture to prevent ARCore camera hardware deadlock
- **Offline-first**: Falls back to local Dijkstra if backend is unreachable

---

## 🛠️ Technical Details

| Property | Value |
|----------|-------|
| Unity Version | 2022.3.62f3 |
| Android Min SDK | API 24 (Android 7.0) |
| Scripting Backend | IL2CPP |
| Architecture | ARM64 |
| Backend | Python 3.8+ / FastAPI / Uvicorn |
| Package Name | com.srinidhi.arcampusnav |

---

## 🔎 Troubleshooting

- **Phone can't reach backend**: Ensure both devices are on the same Wi-Fi, backend URL uses LAN IP (not `127.0.0.1`), and firewall allows TCP port 8000
- **AR not tracking**: Ensure adequate lighting and textured surfaces for ARCore
- **QR not scanning**: Hold phone steady, ensure QR code is well-lit and within the scanning box
- **Runtime logs**: `adb logcat -s Unity`
