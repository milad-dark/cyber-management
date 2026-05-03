import asyncio
from typing import Dict, Any, List
from loguru import logger

try:
    from pysnmp.hlapi.asyncio import *
    SNMP_AVAILABLE = True
except ImportError:
    SNMP_AVAILABLE = False
    logger.warning("pysnmp not available, using mock SNMP scanner")


class SnmpScanner:
    """SNMP-based asset discovery"""

    COMMON_OIDS = {
        "sysDescr": "1.3.6.1.2.1.1.1.0",
        "sysName": "1.3.6.1.2.1.1.5.0",
        "sysContact": "1.3.6.1.2.1.1.4.0",
        "sysLocation": "1.3.6.1.2.1.1.6.0",
    }

    async def scan(self, target: str, scan_type: str = "full",
                   config: Dict[str, Any] = {}) -> List[Dict[str, Any]]:
        community = config.get("snmp_community", "public")
        port = config.get("snmp_port", 161)

        if not SNMP_AVAILABLE:
            return self._mock_results(target)

        results = []
        import ipaddress
        try:
            network = ipaddress.ip_network(target, strict=False)
            hosts = list(network.hosts())
        except ValueError:
            hosts = [target]

        tasks = [self._probe_host(str(h), community, port) for h in hosts]
        probe_results = await asyncio.gather(*tasks, return_exceptions=True)

        for result in probe_results:
            if isinstance(result, dict) and result:
                results.append(result)

        return results

    async def _probe_host(self, host: str, community: str, port: int) -> Dict[str, Any]:
        if not SNMP_AVAILABLE:
            return {}
        try:
            from pysnmp.hlapi.asyncio import getCmd, SnmpEngine, CommunityData, UdpTransportTarget, ContextData, ObjectType, ObjectIdentity
            engine = SnmpEngine()
            result = {}

            for name, oid in self.COMMON_OIDS.items():
                error_indication, error_status, error_index, var_binds = await getCmd(
                    engine,
                    CommunityData(community),
                    await UdpTransportTarget.create((host, port), timeout=2, retries=1),
                    ContextData(),
                    ObjectType(ObjectIdentity(oid))
                )
                if not error_indication and not error_status:
                    for var_bind in var_binds:
                        result[name] = str(var_bind[1])

            if result:
                return {
                    "ip_address": host,
                    "hostname": result.get("sysName"),
                    "mac_address": None,
                    "os_name": result.get("sysDescr"),
                    "asset_type": "network",
                    "ports": [],
                    "raw_data": result
                }
        except Exception as e:
            logger.debug(f"SNMP probe failed for {host}: {e}")
        return {}

    def _mock_results(self, target: str) -> List[Dict[str, Any]]:
        return [{
            "ip_address": target,
            "hostname": "switch-01",
            "mac_address": "00:11:22:33:44:55",
            "os_name": "Cisco IOS 15.x",
            "asset_type": "network",
            "ports": [{"port": 161, "protocol": "udp", "state": "open", "service": "snmp"}],
            "raw_data": {"mock": True}
        }]
