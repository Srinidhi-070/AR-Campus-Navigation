import os
import sys
from pathlib import Path

user_site = (
    Path(os.environ.get("APPDATA", ""))
    / "Python"
    / f"Python{sys.version_info.major}{sys.version_info.minor}"
    / "site-packages"
)
user_site_str = str(user_site)
if user_site_str and user_site_str not in sys.path:
    sys.path.append(user_site_str)

from fastapi import FastAPI, HTTPException
from fastapi.middleware.cors import CORSMiddleware

from schemas import ChatRequest, ChatResponse, LocationsResponse, PathRequest, PathResponse
from services.chat_service import ChatService
from services.graph_service import GraphService

app = FastAPI(title="AR Campus Navigation API")

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

OLLAMA_URL = os.environ.get("OLLAMA_URL", "http://localhost:11434/api/generate")
OLLAMA_MODEL = os.environ.get("OLLAMA_MODEL", "llama3.2")
NODES_PATH = Path(__file__).resolve().parent / "nodes.json"

graph_service = GraphService(NODES_PATH)
try:
    chat_service = ChatService(graph_service, OLLAMA_URL, OLLAMA_MODEL)
    print("Chat service initialized (LLM + semantic matching)")
except Exception as e:
    chat_service = None
    print(f"Chat service disabled (optional deps missing): {e}")


@app.get("/")
def root():
    graph_service.reload_if_needed()
    return {
        "status": "AR Campus Navigation API running",
        "model": OLLAMA_MODEL,
        "nodes_path": str(NODES_PATH),
        "locations": len(graph_service.get_locations()),
    }


@app.get("/locations", response_model=LocationsResponse)
def get_locations() -> LocationsResponse:
    graph_service.reload_if_needed()
    return LocationsResponse(locations=graph_service.get_locations())


@app.get("/health")
async def health():
    graph_service.reload_if_needed()
    return {
        "status": "ok",
        "locations": len(graph_service.get_locations()),
        "ollama_model": OLLAMA_MODEL,
    }


@app.post("/get-path", response_model=PathResponse)
def get_path(req: PathRequest) -> PathResponse:
    try:
        return graph_service.get_path(req.start_node_id, req.destination_node_id)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc)) from exc


@app.post("/chat", response_model=ChatResponse)
async def chat(req: ChatRequest) -> ChatResponse:
    if chat_service is None:
        return ChatResponse(
            answer="AI chat is starting up. Please use the menu to select your destination.",
            destination=None,
            confidence=0.0,
            source="fallback",
        )
    result = await chat_service.resolve_destination(req.query, req.messages)
    return ChatResponse(**result)


@app.post("/ask", response_model=ChatResponse)
async def ask(req: ChatRequest) -> ChatResponse:
    return await chat(req)


if __name__ == "__main__":
    import uvicorn
    print("Starting AR Campus Navigation API...")
    print(f"Server will run on: http://0.0.0.0:8000")
    print(f"Access from this computer: http://localhost:8000")
    print(f"Access from phone: http://192.168.1.4:8000")
    print("Press Ctrl+C to stop")
    uvicorn.run(app, host="0.0.0.0", port=8000)
