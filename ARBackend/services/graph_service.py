import json
import math
from heapq import heappop, heappush
from pathlib import Path
from typing import Dict, List, Optional, Tuple

from schemas import LocationNode, PathPoint, PathResponse


class GraphService:
    def __init__(self, nodes_path: Path):
        self._nodes_path = Path(nodes_path)
        self._mtime: Optional[float] = None
        self._locations: List[LocationNode] = []
        self._node_map: Dict[str, LocationNode] = {}
        self._adjacency: Dict[str, List[str]] = {}
        self.reload_if_needed(force=True)

    def reload_if_needed(self, force: bool = False) -> None:
        exists = self._nodes_path.exists()
        mtime = self._nodes_path.stat().st_mtime if exists else None

        if not force and mtime == self._mtime:
            return

        self._mtime = mtime
        self._locations = []
        self._node_map = {}
        self._adjacency = {}

        if not exists:
            return

        payload = json.loads(self._nodes_path.read_text(encoding="utf-8"))
        raw_nodes = payload.get("nodes", []) if isinstance(payload, dict) else []

        seen = set()
        for raw in raw_nodes:
            node = LocationNode(**raw)
            node_id = node.id.upper()
            if node_id in seen:
                raise ValueError(f"Duplicate node id in nodes.json: {node_id}")
            seen.add(node_id)
            node.id = node_id
            node.neighbors = [neighbor.upper() for neighbor in node.neighbors]
            self._locations.append(node)
            self._node_map[node_id] = node

        for node in self._locations:
            valid_neighbors = []
            for neighbor_id in node.neighbors:
                if neighbor_id in self._node_map:
                    valid_neighbors.append(neighbor_id)
            self._adjacency[node.id] = valid_neighbors

    def get_locations(self) -> List[LocationNode]:
        self.reload_if_needed()
        return list(self._locations)

    def get_node(self, node_id: str) -> Optional[LocationNode]:
        self.reload_if_needed()
        if not node_id:
            return None
        return self._node_map.get(node_id.upper())

    def get_path(self, start_node_id: str, destination_node_id: str) -> PathResponse:
        self.reload_if_needed()

        start = self.get_node(start_node_id)
        goal = self.get_node(destination_node_id)
        if start is None:
            raise ValueError(f"Unknown start node: {start_node_id}")
        if goal is None:
            raise ValueError(f"Unknown destination node: {destination_node_id}")

        node_path = self._a_star(start.id, goal.id)
        if not node_path:
            raise ValueError(f"No path found from {start.id} to {goal.id}")

        return PathResponse(
            path=[
                PathPoint(
                    id=node.id,
                    x=node.x,
                    y=node.y,
                    z=node.z,
                    rotation_y=getattr(node, 'rotation_y', 0.0),
                    building=node.building,
                    floor=node.floor,
                )
                for node in node_path
            ],
            directions=self._build_directions(node_path),
        )

    def _a_star(self, start_id: str, goal_id: str) -> List[LocationNode]:
        open_heap: List[Tuple[float, int, str]] = []
        heappush(open_heap, (0.0, 0, start_id))

        came_from: Dict[str, str] = {}
        g_score: Dict[str, float] = {start_id: 0.0}
        sequence = 0
        closed = set()

        while open_heap:
            _, _, current_id = heappop(open_heap)
            if current_id in closed:
                continue

            if current_id == goal_id:
                return self._reconstruct_path(came_from, current_id)

            closed.add(current_id)
            current = self._node_map[current_id]

            for neighbor_id in self._adjacency.get(current_id, []):
                if neighbor_id in closed:
                    continue

                neighbor = self._node_map[neighbor_id]
                tentative_g = g_score[current_id] + self._distance(current, neighbor)
                if tentative_g >= g_score.get(neighbor_id, float("inf")):
                    continue

                came_from[neighbor_id] = current_id
                g_score[neighbor_id] = tentative_g
                sequence += 1
                f_score = tentative_g + self._distance(neighbor, self._node_map[goal_id])
                heappush(open_heap, (f_score, sequence, neighbor_id))

        return []

    def _reconstruct_path(self, came_from: Dict[str, str], current_id: str) -> List[LocationNode]:
        ordered = [self._node_map[current_id]]
        while current_id in came_from:
            current_id = came_from[current_id]
            ordered.append(self._node_map[current_id])
        ordered.reverse()
        return ordered

    def _distance(self, a: LocationNode, b: LocationNode) -> float:
        return math.sqrt((a.x - b.x) ** 2 + (a.y - b.y) ** 2 + (a.z - b.z) ** 2)

    def _build_directions(self, path: List[LocationNode]) -> List[str]:
        if not path:
            return []
        if len(path) == 1:
            return ["Destination Reached"]

        directions = ["Start"]
        for index in range(1, len(path) - 1):
            prev_node = path[index - 1]
            current = path[index]
            next_node = path[index + 1]

            if current.floor != next_node.floor:
                verb = "Take the lift" if "lift" in {current.type, next_node.type} else "Take the stairs"
                directions.append(f"{verb} to floor {next_node.floor}")
                continue

            directions.append(self._turn_instruction(prev_node, current, next_node))

        directions.append("Destination Reached")
        return directions

    def _turn_instruction(self, prev_node: LocationNode, current: LocationNode, next_node: LocationNode) -> str:
        v1 = (current.x - prev_node.x, current.z - prev_node.z)
        v2 = (next_node.x - current.x, next_node.z - current.z)

        mag1 = math.sqrt((v1[0] ** 2) + (v1[1] ** 2))
        mag2 = math.sqrt((v2[0] ** 2) + (v2[1] ** 2))
        if mag1 == 0 or mag2 == 0:
            return "Go Straight"

        dot = max(-1.0, min(1.0, ((v1[0] * v2[0]) + (v1[1] * v2[1])) / (mag1 * mag2)))
        angle = math.degrees(math.acos(dot))
        cross = (v1[0] * v2[1]) - (v1[1] * v2[0])

        if angle < 20.0:
            return "Go Straight"
        if cross < 0:
            return "Turn Right"
        return "Turn Left"
