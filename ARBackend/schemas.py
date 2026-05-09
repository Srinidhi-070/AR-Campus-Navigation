from typing import List, Optional

from pydantic import BaseModel, Field


class LocationNode(BaseModel):
    id: str
    displayName: str
    type: str
    building: str
    floor: int
    x: float
    y: float
    z: float
    rotation_y: float = 0.0
    qr_point: bool = False
    description: str = ""
    neighbors: List[str] = Field(default_factory=list)


class LocationsResponse(BaseModel):
    locations: List[LocationNode] = Field(default_factory=list)


class ChatMessage(BaseModel):
    role: str
    content: str


class ChatRequest(BaseModel):
    messages: List[ChatMessage] = Field(default_factory=list)
    query: str


class ChatResponse(BaseModel):
    answer: str
    destination: Optional[str] = None
    confidence: Optional[float] = None
    source: Optional[str] = None


class PathRequest(BaseModel):
    start_node_id: str
    destination_node_id: str


class PathPoint(BaseModel):
    id: str
    x: float
    y: float
    z: float
    rotation_y: float = 0.0
    building: str
    floor: int


class PathResponse(BaseModel):
    path: List[PathPoint] = Field(default_factory=list)
    directions: List[str] = Field(default_factory=list)
