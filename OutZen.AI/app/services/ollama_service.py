# app/services/ollama_service.py

import json
from collections.abc import AsyncIterator

import httpx

from app.core.config import get_settings
from app.models.requests import GenerationRequest
from app.models.responses import (
    GenerationResponse,
    GenerationStreamChunkResponse,
)


class OllamaService:
    def __init__(self) -> None:
        self._settings = get_settings()

    def _build_payload(
        self,
        request: GenerationRequest,
        *,
        stream: bool,
    ) -> dict:
        temperature = (
            request.temperature
            if request.temperature is not None
            else self._settings.generation_temperature
        )

        return {
            "model": self._settings.ollama_model,

            "messages": [
                {
                    "role": "system",
                    "content": (
                        "You are the OutZen AI generation engine. "
                        "Use only information supplied in the "
                        "grounded prompt. "
                        "Do not invent places, distances, events, "
                        "alerts, crowd levels, weather conditions "
                        "or traffic data."
                    ),
                },
                {
                    "role": "user",
                    "content": (
                        f"Response language: "
                        f"{request.response_language}\n\n"
                        f"{request.grounded_prompt}"
                    ),
                },
            ],

            "stream": stream,

            "keep_alive": "30m",

           "options": {
                "temperature": temperature,
                "num_predict": 220,
                "num_ctx": 3072,
            },
        }

    def _timeout(self) -> httpx.Timeout:
        return httpx.Timeout(
            connect=15.0,
            read=900.0,
            write=60.0,
            pool=15.0,
        )

    # ============================================================
    # Non-streaming generation
    # ============================================================

    async def generate(self, request: GenerationRequest,) -> GenerationResponse:

        payload = self._build_payload(
            request,
            stream=False,
        )

        async with httpx.AsyncClient(
            base_url=self._settings.ollama_base_url,
            timeout=self._timeout(),
        ) as client:

            response = await client.post(
                "/api/chat",
                json=payload,
            )

            response.raise_for_status()

            data = response.json()

        text = (
            data
            .get("message", {})
            .get("content", "")
            .strip()
        )

        if not text:
            raise RuntimeError(
                "Ollama returned an empty response."
            )

        return GenerationResponse(
            response=text,
            model=self._settings.ollama_model,
            provider="Ollama",
        )

    # ============================================================
    # Streaming generation
    # ============================================================

    async def stream_generate(
        self,
        request: GenerationRequest,
    ) -> AsyncIterator[GenerationStreamChunkResponse]:

        payload = self._build_payload(
            request,
            stream=True,
        )

        async with httpx.AsyncClient(
            base_url=self._settings.ollama_base_url,
            timeout=self._timeout(),
        ) as client:

            async with client.stream(
                "POST",
                "/api/chat",
                json=payload,
            ) as response:

                response.raise_for_status()

                async for line in response.aiter_lines():

                    if not line:
                        continue

                    try:
                        data = json.loads(line)

                    except json.JSONDecodeError as exc:
                        raise RuntimeError(
                            "Ollama returned invalid NDJSON."
                        ) from exc

                    message = (
                        data.get("message")
                        or {}
                    )

                    chunk = message.get(
                        "content",
                        "",
                    )

                    if chunk:
                        yield GenerationStreamChunkResponse(
                            chunk=chunk,
                            done=False,
                            model=self._settings.ollama_model,
                            provider="Ollama",
                        )

                    if data.get("done") is True:
                        yield GenerationStreamChunkResponse(
                            chunk="",
                            done=True,
                            model=self._settings.ollama_model,
                            provider="Ollama",
                        )

                        return




































































































































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.