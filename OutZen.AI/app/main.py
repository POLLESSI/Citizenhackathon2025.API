# app/main.py

from fastapi import FastAPI

from app.api.generation import router as generation_router
from app.api.health import router as health_router
from app.core.config import get_settings
from app.models.responses import RootResponse


settings = get_settings()


app = FastAPI(
    title="OutZen AI",
    description="AI microservice for the OutZen platform.",
    version=settings.app_version,
)


app.include_router(
    health_router,
)


app.include_router(
    generation_router,
    prefix="/api/v1",
)


@app.get(
    "/",
    response_model=RootResponse,
    tags=["Service"],
)
async def root() -> RootResponse:
    return RootResponse(
        service=settings.app_name,
        status="running",
        version=settings.app_version,
    )










































































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.