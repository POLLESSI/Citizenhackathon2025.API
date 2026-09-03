# app/api/generation.py

import httpx

from fastapi import (APIRouter, Depends, HTTPException, status,)
from fastapi.responses import StreamingResponse
from app.core.security import require_internal_api_key
from app.models.requests import GenerationRequest
from app.models.responses import (GenerationResponse, GenerationStreamChunkResponse,)

from app.services.ollama_service import OllamaService


router = APIRouter(tags=["AI Generation"], dependencies=[Depends(require_internal_api_key)],)


# ============================================================
# Standard generation
# ============================================================

@router.post("/generate", response_model=GenerationResponse, status_code=status.HTTP_200_OK,)
async def generate(request: GenerationRequest, ) -> GenerationResponse:

    service = OllamaService()

    try:
        return await service.generate(request)

    except httpx.ConnectError as exc:
        raise HTTPException(
            status_code=status.HTTP_503_SERVICE_UNAVAILABLE,
            detail="Ollama is unavailable.",
        ) from exc

    except httpx.TimeoutException as exc:
        raise HTTPException(
            status_code=status.HTTP_504_GATEWAY_TIMEOUT,
            detail="Ollama generation timed out.",
        ) from exc

    except httpx.HTTPStatusError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=(
                "Ollama returned an invalid HTTP response."
            ),
        ) from exc

    except RuntimeError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=str(exc),
        ) from exc


# ============================================================
# Streaming generation
# ============================================================

@router.post(
    "/generate/stream",
    response_class=StreamingResponse,
    status_code=status.HTTP_200_OK,
)
async def generate_stream(
    request: GenerationRequest,
) -> StreamingResponse:

    service = OllamaService()

    async def stream_response():

        try:
            async for item in service.stream_generate(
                request
            ):
                yield (
                    item.model_dump_json()
                    + "\n"
                )

        except httpx.ConnectError:
            item = GenerationStreamChunkResponse(
                chunk="",
                done=True,
                model="",
                provider="Ollama",
                error="Ollama is unavailable.",
            )

            yield item.model_dump_json() + "\n"

        except httpx.TimeoutException:
            item = GenerationStreamChunkResponse(
                chunk="",
                done=True,
                model="",
                provider="Ollama",
                error="Ollama generation timed out.",
            )

            yield item.model_dump_json() + "\n"

        except httpx.HTTPStatusError as exc:
            item = GenerationStreamChunkResponse(
                chunk="",
                done=True,
                model="",
                provider="Ollama",
                error=(
                    "Ollama returned HTTP "
                    f"{exc.response.status_code}."
                ),
            )

            yield item.model_dump_json() + "\n"

        except RuntimeError as exc:
            item = GenerationStreamChunkResponse(
                chunk="",
                done=True,
                model="",
                provider="Ollama",
                error=str(exc),
            )

            yield item.model_dump_json() + "\n"

    return StreamingResponse(
        stream_response(),
        media_type="application/x-ndjson",
        headers={
            "Cache-Control": "no-cache",
            "X-Accel-Buffering": "no",
        },
    )
















































































































































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.