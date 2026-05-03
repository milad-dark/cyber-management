// =============================================================
// Cyber Asset Management Platform — Neo4j Graph Schema
// =============================================================

// Constraints
CREATE CONSTRAINT asset_id IF NOT EXISTS FOR (a:Asset) REQUIRE a.id IS UNIQUE;
CREATE CONSTRAINT vuln_id IF NOT EXISTS FOR (v:Vulnerability) REQUIRE v.id IS UNIQUE;
CREATE CONSTRAINT user_id IF NOT EXISTS FOR (u:User) REQUIRE u.id IS UNIQUE;
CREATE CONSTRAINT ioc_id IF NOT EXISTS FOR (i:IOC) REQUIRE i.id IS UNIQUE;

// Indexes
CREATE INDEX asset_ip IF NOT EXISTS FOR (a:Asset) ON (a.ipAddress);
CREATE INDEX asset_hostname IF NOT EXISTS FOR (a:Asset) ON (a.hostname);
CREATE INDEX vuln_cve IF NOT EXISTS FOR (v:Vulnerability) ON (v.cveId);
CREATE INDEX ioc_value IF NOT EXISTS FOR (i:IOC) ON (i.value);

// ─── Sample Graph Structure ────────────────────────────────
// Assets communicate with each other (network topology)
// MERGE (:Asset {id: 'asset-1', name: 'Web Server', ipAddress: '10.0.0.1', type: 'server'})
// MERGE (:Asset {id: 'asset-2', name: 'DB Server',  ipAddress: '10.0.0.2', type: 'server'})
// MATCH (a:Asset {id:'asset-1'}), (b:Asset {id:'asset-2'})
// MERGE (a)-[:COMMUNICATES_WITH {port: 5432, protocol: 'tcp', direction: 'outbound'}]->(b)

// Assets have vulnerabilities
// MERGE (:Vulnerability {id:'vuln-1', cveId:'CVE-2024-1234', severity:'critical', cvssScore:9.8})
// MATCH (a:Asset {id:'asset-1'}), (v:Vulnerability {id:'vuln-1'})
// MERGE (a)-[:HAS_VULNERABILITY {detectedAt: datetime(), status: 'open'}]->(v)

// Assets match IOCs
// MERGE (:IOC {id:'ioc-1', type:'ip', value:'192.168.1.100', threatType:'botnet'})
// MATCH (a:Asset {id:'asset-1'}), (i:IOC {id:'ioc-1'})
// MERGE (a)-[:MATCHES_IOC {matchedAt: datetime()}]->(i)

// Users own assets
// MERGE (:User {id:'user-1', username:'admin'})
// MATCH (u:User {id:'user-1'}), (a:Asset {id:'asset-1'})
// MERGE (u)-[:OWNS]->(a)
