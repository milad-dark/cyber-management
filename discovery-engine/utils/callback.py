import httpx
from loguru import logger
from config import settings


async def notify_backend(job_id: str, status: str, assets_found: int, error: str | None = None) -> None:
    """Notify the .NET backend about job completion"""
    try:
        async with httpx.AsyncClient(timeout=10) as client:
            response = await client.post(
                settings.backend_callback_url,
                json={
                    "jobId": job_id,
                    "status": status,
                    "assetsFound": assets_found,
                    "error": error
                },
                headers={"X-Engine-Secret": settings.discovery_engine_secret}
            )
            if response.status_code != 200:
                logger.warning(f"Backend callback returned {response.status_code} for job {job_id}")
    except Exception as e:
        logger.warning(f"Failed to notify backend for job {job_id}: {e}")
