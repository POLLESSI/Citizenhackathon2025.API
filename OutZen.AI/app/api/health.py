# app/api/health.py

import httpx

from fastapi import APIRouter

from app.core.config import get_settings
from app.models.responses import HealthResponse


router = APIRouter(
    tags=["Health"],
)

@router.get(
    "/health",
    response_model=HealthResponse,
)
async def health() -> HealthResponse:
    settings = get_settings()

    ollama_available = False

    try:
        timeout = httpx.Timeout(
            connect=2.0,
            read=2.0,
            write=2.0,
            pool=2.0,
        )

        async with httpx.AsyncClient(
            base_url=settings.ollama_base_url,
            timeout=3.0
        ) as client: 

            response = await client.get(
                "/api/tags"
            )

            ollama_available = (
                response.is_success    
            )

    except httpx.HTTPError:
        ollama_available = False

    return HealthResponse(
        service=settings.app_name,
        status=(
            "healthy"
            if ollama_available
            else "degraded"
        ),
        ollama=ollama_available,
        model=settings.ollama_model,
    )























































































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.