# app/core/config.py

from functools import lru_cache

from pydantic import Field
from pydantic_settings import (
    BaseSettings,
    SettingsConfigDict,
)

class Settings(BaseSettings):
    app_name: str = "OutZen.AI"
    app_version: str = "1.0.0"

    ollama_base_url: str = "http://127.0.0.1:11434"
    ollama_model: str = "mistral:7b"

    generation_temperature: float = 0.3

    internal_api_key: str = Field(
        ...,
        min_length=32,
        validation_alias="OUTZEN_INTERNAL_API_KEY",
    )

    model_config = SettingsConfigDict(
        env_file=".env",
        env_file_encoding="utf-8",
        extra="ignore",
    )

@lru_cache
def get_settings() -> Settings:
    return Settings()





















































































































# Copyrigtht (c) 2025 Citizen Hackathon https://github.com/POLLESSI/Citizenhackathon2025.API. All rights reserved.