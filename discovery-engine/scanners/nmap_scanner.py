import asyncio
from typing import Dict, Any, List
from loguru import logger

try:
    import nmap
    NMAP_AVAILABLE = True
except ImportError:
    NMAP_AVAILABLE = False
    logger.warning("python-nmap not available, using mock scanner")


class NmapScanner:
    """Nmap-based network scanner"""

    SCAN_ARGS = {
        "quick": "-sV -T4 --top-ports 100",
        "full": "-sV -T4 -p 1-65535",
        "deep": "-sV -sC -O -T4",
        "passive": "-sn",
    }

    async def scan(self, target: str, scan_type: str = "full",
                   config: Dict[str, Any] = {}) -> List[Dict[str, Any]]:
        args = config.get("nmap_args") or self.SCAN_ARGS.get(scan_type, self.SCAN_ARGS["full"])
        logger.info(f"Nmap scanning {target} with args: {args}")

        if not NMAP_AVAILABLE:
            return self._mock_results(target)

        loop = asyncio.get_event_loop()
        results = await loop.run_in_executor(None, self._run_nmap, target, args)
        return results

    def _run_nmap(self, target: str, args: str) -> List[Dict[str, Any]]:
        nm = nmap.PortScanner()
        try:
            nm.scan(hosts=target, arguments=args)
        except Exception as e:
            logger.error(f"Nmap scan error: {e}")
            return []

        results = []
        for host in nm.all_hosts():
            if nm[host].state() != "up":
                continue

            host_data: Dict[str, Any] = {
                "ip_address": host,
                "hostname": None,
                "mac_address": None,
                "os_name": None,
                "os_version": None,
                "os_family": None,
                "ports": [],
                "raw_data": {}
            }

            # Hostname
            hostnames = nm[host].hostnames()
            if hostnames:
                host_data["hostname"] = hostnames[0].get("name")

            # MAC address
            if "addresses" in nm[host]:
                host_data["mac_address"] = nm[host]["addresses"].get("mac")

            # OS detection
            if "osmatch" in nm[host] and nm[host]["osmatch"]:
                os_match = nm[host]["osmatch"][0]
                host_data["os_name"] = os_match.get("name", "")
                if "osclass" in os_match and os_match["osclass"]:
                    oc = os_match["osclass"][0]
                    host_data["os_family"] = oc.get("osfamily")
                    host_data["os_version"] = oc.get("osgen")

            # Ports
            for proto in nm[host].all_protocols():
                for port in nm[host][proto].keys():
                    port_info = nm[host][proto][port]
                    host_data["ports"].append({
                        "port": port,
                        "protocol": proto,
                        "state": port_info.get("state", "unknown"),
                        "service": port_info.get("name"),
                        "version": f"{port_info.get('product', '')} {port_info.get('version', '')}".strip() or None,
                        "banner": port_info.get("extrainfo")
                    })

            host_data["raw_data"] = dict(nm[host])
            results.append(host_data)

        return results

    def _mock_results(self, target: str) -> List[Dict[str, Any]]:
        """Return mock results when nmap is not available"""
        import ipaddress
        results = []
        try:
            network = ipaddress.ip_network(target, strict=False)
            hosts = list(network.hosts())[:3]
            for host in hosts:
                results.append({
                    "ip_address": str(host),
                    "hostname": f"host-{str(host).replace('.', '-')}",
                    "mac_address": None,
                    "os_name": "Linux",
                    "os_family": "Linux",
                    "ports": [
                        {"port": 22, "protocol": "tcp", "state": "open", "service": "ssh", "version": "OpenSSH 8.9"},
                        {"port": 80, "protocol": "tcp", "state": "open", "service": "http", "version": "nginx 1.25"},
                    ],
                    "raw_data": {"mock": True}
                })
        except Exception:
            pass
        return results
