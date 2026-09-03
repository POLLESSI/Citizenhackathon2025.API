# app.core.security.py

import secrets

from typing import Annotated

from fastapi import (
    HTTPException,
    Security,
    status,
)

from fastapi.security import APIKeyHeader

from app.core.config import get_settings


INTERNAL_API_KEY_HEADER = "X-OutZen-Internal-Key"


api_key_header = APIKeyHeader(
    name=INTERNAL_API_KEY_HEADER,
    auto_error=False,
)


async def require_internal_api_key(
    supplied_key: Annotated[
        str | None,
        Security(api_key_header),
    ],
) -> None:

    settings = get_settings()

    if not supplied_key:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing internal API key.",
        )

    if not secrets.compare_digest(
        supplied_key,
        settings.internal_api_key,
    ):
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Invalid internal API key.",
        )























































































    # Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.