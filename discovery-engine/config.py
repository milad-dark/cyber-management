from pydantic_settings import BaseSettings
from typing import Optional


class Settings(BaseSettings):
    postgres_url: str = "postgresql+asyncpg://cyber_admin:StrongPostgresPass!2024@localhost:5432/cyber_management"
    redis_url: str = "redis://:StrongRedisPass!2024@localhost:6379/0"
    discovery_engine_secret: str = "discovery-engine-secret-key"
    backend_callback_url: str = "http://backend:8080/api/discovery/callback"
    max_concurrent_scans: int = 5
    nmap_args_quick: str = "-sV -T4 --top-ports 100"
    nmap_args_full: str = "-sV -T4 -p-"
    nmap_args_deep: str = "-sV -sC -O -T4"

    class Config:
        env_file = ".env"


settings = Settings()
