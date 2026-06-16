# Repository Guidelines

This repository contains the **Trailix** AR Campus Navigation System backend -- a Python FastAPI server providing location data, pathfinding, and AI chat services.

## Project Structure

- **`./ARBackend/`**: Python FastAPI server.
  - **`main.py`**: API entry point with route definitions.
  - **`schemas.py`**: Pydantic request/response models.
  - **`services/`**: Core logic for campus graph management, pathfinding, and AI chat.
  - **`nodes.json`**: Graph data source for campus locations and connections.
  - **`generate_qr.py`**: QR code generation utility for campus anchors.
- **`./Documentation/`**: Guides for setup, deployment, and testing.

## Build, Test, and Development Commands

### Backend Development
- **Run Server**: `python main.py` (within `./ARBackend/`) or use `./ARBackend/start_backend.bat`.
- **Install Dependencies**: `pip install -r ./ARBackend/requirements.txt`.
- **Docker**: `docker-compose up --build` (within `./ARBackend/`).

## Coding Style

- **Python (FastAPI)**: Follow PEP 8 standards. Maintain clear separation between schemas and services.

## Testing Guidelines

- Refer to `./Documentation/INTEGRATION_TESTING_GUIDE.md` for end-to-end validation.
- Focus on pathfinding accuracy during runtime tests.

## Commit Guidelines

Commit messages must be concise and describe the functional change. Use descriptive prefixes:
- `Fix`: Bug fixes
- `Implement`: New features
- `Cleanup`: Code or project organization
- `Major`: Large-scale architectural changes
