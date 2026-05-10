# Repository Guidelines

This repository contains the **AR Campus Navigation System**, a hybrid project consisting of a Unity-based AR client and a Python FastAPI backend.

## Project Structure & Module Organization

- **`.\ARBackend\`**: Python FastAPI server providing location data and pathfinding services.
  - **`main.py`**: API entry point.
  - **`services\`**: Core logic for campus graph management and spatial services.
  - **`nodes.json`**: Data source for campus locations and connections.
- **`.\ARSpatialClient\`**: Unity project (Version: **2022.3.62f3**) targeting Android (ARCore).
  - **`Assets\ProjectCore\`**: Primary source code and assets.
  - **`Assets\ProjectCore\Scripts\`**: C# logic for AR navigation and UI.
- **`.\Documentation\`**: Centralized guides for builds, deployment, and testing.
- **`.\Builds\`**: Stores generated Android APKs (`ARCampusNav.apk`).

## Build, Test, and Development Commands

### Backend Development
- **Run Server**: `python main.py` (within `.\ARBackend\`) or use `.\ARBackend\start_backend.bat`.
- **Install Dependencies**: `pip install -r .\ARBackend\requirements.txt`.

### Unity & Android Development
- **Install to Device**: `.\install_to_device.bat` (Requires ADB and prior Unity build).
- **View Runtime Logs**: `adb logcat -s Unity`.
- **Generate Icons**: Unity Editor → `Tools → Generate UI Icons`.

## Coding Style & Naming Conventions

- **C# (Unity)**: Use standard Unity PascalCase for classes and methods. Avoid legacy scene components; prefer runtime installation via `CampusRuntimeInstaller`.
- **Python (FastAPI)**: Follow PEP 8 standards. Maintain clear separation between schemas and services.

## Testing Guidelines

- Refer to `.\Documentation\INTEGRATION_TESTING_GUIDE.md` for end-to-end validation.
- Focus on QR detection and pathfinding accuracy during runtime tests.

## Commit & Pull Request Guidelines

Commit messages must be concise and describe the functional change. Use descriptive prefixes:
- `Fix`: Bug fixes (e.g., `Fix arrow rendering`)
- `Implement`: New features (e.g., `Implement Node Rotation Offset`)
- `Cleanup`: Code or project organization (e.g., `Phase 1 Cleanup`)
- `Major`: Large-scale architectural changes or snapshots

## Current Project Status

The project is undergoing a systematic cleanup. Prioritize removing legacy scripts and scene components over adding new features. Refer to the "Required Cleanup" section in `.\README.md` for immediate priorities.
