# app.models.responses.py

from pydantic import BaseModel

class RootResponse(BaseModel):
    service: str
    status: str
    version: str

class HealthResponse(BaseModel):
    service: str
    status: str
    ollama: bool
    model: str

class GenerationResponse(BaseModel):
    response: str
    model: str
    provider: str

class GenerationStreamChunkResponse(BaseModel):
    chunk: str = ""
    done: bool = False
    model: str
    provider: str = "Ollama"
    error: str | None = None





















































































































    # Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.