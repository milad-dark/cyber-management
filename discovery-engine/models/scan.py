from pydantic import BaseModel
from typing import Optional, List, Dict, Any
from enum import Enum
from datetime import datetime


class ScanType(str, Enum):
    quick = "quick"
    full = "full"
    deep = "deep"
    passive = "passive"


class ScannerType(str, Enum):
    nmap = "nmap"
    masscan = "masscan"
    snmp = "snmp"
    arp = "arp"


class ScanRequest(BaseModel):
    job_id: str
    scan_type: ScanType = ScanType.full
    target: str          # IP, CIDR, or range (e.g., 192.168.1.0/24)
    scanner: ScannerType = ScannerType.nmap
    config: Dict[str, Any] = {}


class PortInfo(BaseModel):
    port: int
    protocol: str = "tcp"
    state: str = "open"
    service: Optional[str] = None
    version: Optional[str] = None
    banner: Optional[str] = None


class DiscoveredAsset(BaseModel):
    ip_address: str
    hostname: Optional[str] = None
    mac_address: Optional[str] = None
    os_name: Optional[str] = None
    os_version: Optional[str] = None
    os_family: Optional[str] = None
    manufacturer: Optional[str] = None
    asset_type: str = "server"
    ports: List[PortInfo] = []
    cpe: Optional[str] = None
    raw_data: Dict[str, Any] = {}


class ScanResult(BaseModel):
    job_id: str
    status: str
    assets_found: int
    assets: List[DiscoveredAsset] = []
    error: Optional[str] = None
    started_at: Optional[datetime] = None
    completed_at: Optional[datetime] = None


class ScanStatus(BaseModel):
    job_id: str
    status: str
    progress: int = 0
    assets_found: int = 0
    message: Optional[str] = None
