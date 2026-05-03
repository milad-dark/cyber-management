from typing import Dict, Any
from models.scan import DiscoveredAsset, PortInfo


def normalize_asset(raw: Dict[str, Any]) -> DiscoveredAsset:
    """Normalize raw scanner output into DiscoveredAsset"""

    ports = []
    for p in raw.get("ports", []):
        ports.append(PortInfo(
            port=int(p.get("port", 0)),
            protocol=p.get("protocol", "tcp"),
            state=p.get("state", "open"),
            service=p.get("service"),
            version=p.get("version"),
            banner=p.get("banner")
        ))

    # Infer asset type from ports/OS
    asset_type = _infer_asset_type(raw)

    # Build CPE if possible
    cpe = _build_cpe(raw)

    return DiscoveredAsset(
        ip_address=raw["ip_address"],
        hostname=raw.get("hostname"),
        mac_address=raw.get("mac_address"),
        os_name=raw.get("os_name"),
        os_version=raw.get("os_version"),
        os_family=raw.get("os_family"),
        manufacturer=raw.get("manufacturer"),
        asset_type=asset_type,
        ports=ports,
        cpe=cpe,
        raw_data=raw.get("raw_data", {})
    )


def _infer_asset_type(raw: Dict[str, Any]) -> str:
    os_name = (raw.get("os_name") or "").lower()
    ports = raw.get("ports", [])
    port_numbers = {int(p.get("port", 0)) for p in ports}

    if any(kw in os_name for kw in ["cisco", "juniper", "arista", "switch", "router"]):
        return "network"
    if any(kw in os_name for kw in ["windows server", "red hat", "ubuntu server", "debian"]):
        return "server"
    if any(kw in os_name for kw in ["windows 10", "windows 11", "macos", "ubuntu desktop"]):
        return "workstation"
    if 161 in port_numbers:
        return "network"
    if 443 in port_numbers or 80 in port_numbers:
        return "server"
    return "server"


def _build_cpe(raw: Dict[str, Any]) -> str | None:
    os_name = raw.get("os_name") or ""
    os_family = raw.get("os_family") or ""
    os_version = raw.get("os_version") or ""

    if "windows" in os_name.lower():
        return f"cpe:/o:microsoft:windows:{os_version}".lower()
    if "linux" in os_family.lower() or "linux" in os_name.lower():
        return f"cpe:/o:linux:linux_kernel".lower()
    if "cisco" in os_name.lower():
        return "cpe:/o:cisco:ios"
    return None
