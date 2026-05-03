import asyncio
from typing import List
from loguru import logger

from models.scan import DiscoveredAsset

try:
    import asyncpg
    DB_AVAILABLE = True
except ImportError:
    DB_AVAILABLE = False

from config import settings


async def write_assets_to_db(job_id: str, assets: List[DiscoveredAsset]) -> None:
    """Write discovered assets to PostgreSQL"""
    if not DB_AVAILABLE or not assets:
        logger.warning(f"DB not available or no assets to write for job {job_id}")
        return

    try:
        conn = await asyncpg.connect(settings.postgres_url.replace("+asyncpg", ""))

        for asset in assets:
            # Check if asset already exists
            existing = await conn.fetchrow(
                "SELECT id FROM assets WHERE ip_address = $1",
                asset.ip_address
            )

            if existing:
                # Update last_seen
                await conn.execute(
                    """UPDATE assets SET
                        hostname = COALESCE($2, hostname),
                        mac_address = COALESCE($3::macaddr, mac_address),
                        os_name = COALESCE($4, os_name),
                        os_version = COALESCE($5, os_version),
                        os_family = COALESCE($6, os_family),
                        cpe = COALESCE($7, cpe),
                        last_seen = NOW(),
                        updated_at = NOW()
                    WHERE ip_address = $1""",
                    asset.ip_address,
                    asset.hostname,
                    asset.mac_address,
                    asset.os_name,
                    asset.os_version,
                    asset.os_family,
                    asset.cpe
                )
                asset_id = existing["id"]
            else:
                # Insert new asset
                asset_id = await conn.fetchval(
                    """INSERT INTO assets
                        (name, hostname, ip_address, mac_address, asset_type,
                         os_name, os_version, os_family, cpe, status, criticality,
                         first_seen, last_seen, created_at, updated_at)
                       VALUES
                        ($1, $2, $3::inet, $4::macaddr, $5, $6, $7, $8, $9,
                         'active', 'medium', NOW(), NOW(), NOW(), NOW())
                       RETURNING id""",
                    asset.hostname or asset.ip_address,
                    asset.hostname,
                    asset.ip_address,
                    asset.mac_address,
                    asset.asset_type,
                    asset.os_name,
                    asset.os_version,
                    asset.os_family,
                    asset.cpe
                )

            # Write ports
            for port in asset.ports:
                await conn.execute(
                    """INSERT INTO asset_ports
                        (asset_id, port, protocol, state, service, version, last_seen)
                       VALUES ($1, $2, $3, $4, $5, $6, NOW())
                       ON CONFLICT (asset_id, port, protocol)
                       DO UPDATE SET state=$4, service=$5, version=$6, last_seen=NOW()""",
                    asset_id, port.port, port.protocol,
                    port.state, port.service, port.version
                )

        await conn.close()
        logger.info(f"Wrote {len(assets)} assets to DB for job {job_id}")

    except Exception as e:
        logger.error(f"DB write error for job {job_id}: {e}")
