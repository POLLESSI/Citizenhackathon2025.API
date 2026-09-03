# app/models/requests.py

from pydantic import BaseModel, Field

class GenerationRequest(BaseModel):
    grounded_prompt: str = Field(
        ...,
        min_length=20,
        max_length=100_000,
        description="Prompt grounded with verified OutZen context.",
    )

    response_language: str = Field(
        default="fr-FR",
        min_length=2,
        max_length=32,
    )

    temperature: float | None = Field(
        default=None,
        ge=0.0,
        le=2.0,
    )























































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.