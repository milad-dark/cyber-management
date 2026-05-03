from fastapi import FastAPI, HTTPException, Header, BackgroundTasks
from fastapi.middleware.cors import CORSMiddleware
from pydantic import BaseModel
from typing import Optional, Dict, Any
import asyncio
from loguru import logger

from config import settings
from routers import scan_router
from models.scan import ScanRequest, ScanStatus

app = FastAPI(
    title="Cyber Asset Discovery Engine",
    description="Network asset discovery microservice for Cyber Management Platform",
    version="1.0.0"
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_methods=["*"],
    allow_headers=["*"],
)

app.include_router(scan_router.router, prefix="/scan", tags=["Scan"])


@app.get("/health")
async def health():
    return {"status": "ok", "service": "discovery-engine"}


@app.get("/")
async def root():
    return {"message": "Cyber Asset Discovery Engine", "version": "1.0.0"}
