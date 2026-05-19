from typing import Dict, List, Optional, Tuple

import httpx

try:
    import numpy as np
except ImportError:  # pragma: no cover - optional dependency
    np = None

try:
    from sentence_transformers import SentenceTransformer
except ImportError:  # pragma: no cover - optional dependency
    SentenceTransformer = None

from schemas import ChatMessage


class ChatService:
    def __init__(self, graph_service, ollama_url: str, ollama_model: str):
        self._graph_service = graph_service
        self._ollama_url = ollama_url
        self._ollama_model = ollama_model
        self._encoder = None
        self._indexed_ids: List[str] = []
        self._indexed_text: List[str] = []
        self._embeddings = None

        if SentenceTransformer is not None and np is not None:
            try:
                self._encoder = SentenceTransformer("all-MiniLM-L6-v2")
            except Exception:
                self._encoder = None

    def refresh_index(self) -> None:
        locations = self._graph_service.get_locations()
        new_ids = [location.id for location in locations]
        if new_ids == self._indexed_ids:
            return

        self._indexed_ids = new_ids
        self._indexed_text = [
            f"{location.displayName}. {location.description} Building: {location.building}. Floor: {location.floor}."
            for location in locations
        ]
        if self._indexed_text and self._encoder is not None and np is not None:
            self._embeddings = self._encoder.encode(self._indexed_text, convert_to_numpy=True).astype(np.float32)
        else:
            self._embeddings = None

    async def resolve_destination(self, query: str, history: List[ChatMessage]) -> Dict[str, Optional[str]]:
        self.refresh_index()
        locations = self._graph_service.get_locations()
        if not locations:
            return {
                "answer": "No campus map has been exported yet. Create the floor map and export nodes first.",
                "destination": None,
                "confidence": 0.0,
                "source": "empty_graph",
            }

        llm_text = await self._ask_ollama(query, history, locations)
        if llm_text:
            parsed = self._parse_llm_response(llm_text)
            if parsed["destination"] and self._graph_service.get_node(parsed["destination"]):
                return {
                    "answer": parsed["answer"],
                    "destination": parsed["destination"],
                    "confidence": 0.95,
                    "source": "llm",
                }
            if parsed["answer"]:
                return {
                    "answer": parsed["answer"],
                    "destination": None,
                    "confidence": 0.0,
                    "source": "llm",
                }

        fallback = self._semantic_match(query)
        if fallback is None:
            return {
                "answer": "I could not match that destination. Try the room or office name exactly.",
                "destination": None,
                "confidence": 0.0,
                "source": "semantic",
            }

        destination, confidence = fallback
        location = self._graph_service.get_node(destination)
        answer = f"I found {location.displayName}. I'll prepare navigation to that location."
        return {
            "answer": answer,
            "destination": destination,
            "confidence": confidence,
            "source": "semantic",
        }

    def _semantic_match(self, query: str) -> Optional[Tuple[str, float]]:
        if not self._indexed_ids:
            return None

        if self._embeddings is None or np is None or self._encoder is None:
            return self._keyword_match(query)

        query_embedding = self._encoder.encode([query], convert_to_numpy=True).astype(np.float32)[0]
        norms = np.linalg.norm(self._embeddings, axis=1) * np.linalg.norm(query_embedding)
        norms = np.where(norms == 0, 1e-6, norms)
        scores = np.dot(self._embeddings, query_embedding) / norms

        best_index = int(np.argmax(scores))
        best_score = float(scores[best_index])
        if best_score < 0.30:
            return None

        confidence = max(0.0, min(1.0, (best_score + 1.0) / 2.0))
        return self._indexed_ids[best_index], round(confidence, 4)

    def _keyword_match(self, query: str) -> Optional[Tuple[str, float]]:
        tokens = {token for token in self._normalize(query) if token}
        if not tokens:
            return None

        best_id = None
        best_score = 0.0

        for location_id, text in zip(self._indexed_ids, self._indexed_text):
            location_tokens = {token for token in self._normalize(text) if token}
            if not location_tokens:
                continue

            overlap = len(tokens.intersection(location_tokens))
            if overlap == 0:
                continue

            score = overlap / max(len(tokens), 1)
            if score > best_score:
                best_score = score
                best_id = location_id

        if best_id is None or best_score < 0.34:
            return None

        return best_id, round(min(0.89, max(0.35, best_score)), 4)

    async def _ask_ollama(self, query: str, history: List[ChatMessage], locations) -> Optional[str]:
        history_text = "\n".join(
            f"{'User' if message.role == 'user' else 'Assistant'}: {message.content}"
            for message in history[-6:]
        )
        location_lines = "\n".join(
            f"- {location.id}: {location.displayName} ({location.building}, floor {location.floor})"
            for location in locations
        )

        prompt = (
            "You are a smart campus navigation assistant.\n"
            "Your only job is to map the user request to exactly one destination node id from the list.\n\n"
            f"Available locations:\n{location_lines}\n\n"
            f"Conversation:\n{history_text}\n\n"
            f"User: {query}\n\n"
            "Respond in this exact format:\n"
            "DESTINATION: <NODE_ID or NONE>\n"
            "ANSWER: <one short helpful sentence>\n"
        )

        try:
            async with httpx.AsyncClient(timeout=30.0) as client:
                response = await client.post(
                    self._ollama_url,
                    json={"model": self._ollama_model, "prompt": prompt, "stream": False},
                )
            if response.status_code != 200:
                return None
            return response.json().get("response", "")
        except Exception:
            return None

    def _parse_llm_response(self, text: str) -> Dict[str, Optional[str]]:
        destination = None
        answer = ""

        for raw_line in text.splitlines():
            line = raw_line.strip()
            if line.upper().startswith("DESTINATION:"):
                value = line.split(":", 1)[1].strip().upper()
                if value and value != "NONE":
                    destination = value
            elif line.upper().startswith("ANSWER:"):
                answer = line.split(":", 1)[1].strip()

        if not destination:
            upper_text = text.upper()
            for node_id in self._indexed_ids:
                if node_id in upper_text:
                    destination = node_id
                    break

        return {"destination": destination, "answer": answer or text.strip()}

    def _normalize(self, text: str) -> List[str]:
        normalized = []
        current = []

        for char in (text or "").lower():
            if char.isalnum():
                current.append(char)
                continue

            if current:
                normalized.append("".join(current))
                current = []

        if current:
            normalized.append("".join(current))

        return normalized
