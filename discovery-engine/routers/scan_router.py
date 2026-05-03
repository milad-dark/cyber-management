from fastapi import APIRouter, HTTPException, Header, BackgroundTasks, Depends
from typing import Optional
import asyncio
from loguru import logger

from models.scan import ScanRequest, ScanResult, ScanStatus
from scanners.nmap_scanner import NmapScanner
from scanners.snmp_scanner import SnmpScanner
from utils.normalizer import normalize_asset
from utils.db_writer import write_assets_to_db
from utils.callback import notify_backend
from config import settings

router = APIRouter()

# In-memory job status tracking
_job_status: dict[str, ScanStatus] = {}


def verify_secret(x_engine_secret: Optional[str] = Header(default=None)):
    if x_engine_secret != settings.discovery_engine_secret:
        raise HTTPException(status_code=401, detail="Invalid engine secret")
    return x_engine_secret


@router.post("/start", dependencies=[Depends(verify_secret)])
async def start_scan(request: ScanRequest, background_tasks: BackgroundTasks):
    job_id = request.job_id
    _job_status[job_id] = ScanStatus(job_id=job_id, status="running", message="شروع اسکن")
    background_tasks.add_task(run_scan, request)
    return {"job_id": job_id, "status": "started"}


@router.get("/status/{job_id}", dependencies=[Depends(verify_secret)])
async def get_status(job_id: str):
    status = _job_status.get(job_id)
    if not status:
        raise HTTPException(status_code=404, detail="Job not found")
    return status


@router.post("/test")
async def test_scan(request: ScanRequest, background_tasks: BackgroundTasks,
                    x_engine_secret: Optional[str] = Header(default=None)):
    """Test endpoint for quick validation without secret"""
    job_id = request.job_id
    _job_status[job_id] = ScanStatus(job_id=job_id, status="running", message="تست اسکن")
    background_tasks.add_task(run_scan, request)
    return {"job_id": job_id, "status": "started"}


async def run_scan(request: ScanRequest):
    job_id = request.job_id
    try:
        logger.info(f"Starting scan job {job_id} - type={request.scan_type} target={request.target}")

        if request.scanner == "snmp":
            scanner = SnmpScanner()
        else:
            scanner = NmapScanner()

        raw_results = await scanner.scan(
            target=request.target,
            scan_type=request.scan_type.value,
            config=request.config
        )

        assets = [normalize_asset(r) for r in raw_results]
        await write_assets_to_db(job_id, assets)

        _job_status[job_id] = ScanStatus(
            job_id=job_id,
            status="completed",
            progress=100,
            assets_found=len(assets),
            message=f"{len(assets)} دارایی یافت شد"
        )

        await notify_backend(job_id, "completed", len(assets))
        logger.info(f"Scan job {job_id} completed: {len(assets)} assets")

    except Exception as e:
        logger.error(f"Scan job {job_id} failed: {e}")
        _job_status[job_id] = ScanStatus(
            job_id=job_id,
            status="failed",
            message=str(e)
        )
        await notify_backend(job_id, "failed", 0, str(e))
