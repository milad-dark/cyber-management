-- =============================================================
-- Cyber Asset Management Platform — PostgreSQL Init Schema
-- =============================================================

-- Enable extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- =============================================================
-- USERS & ROLES
-- =============================================================
CREATE TABLE IF NOT EXISTS roles (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(50) NOT NULL UNIQUE,
    description TEXT,
    permissions JSONB NOT NULL DEFAULT '[]',
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS users (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    username      VARCHAR(100) NOT NULL UNIQUE,
    email         VARCHAR(255) NOT NULL UNIQUE,
    password_hash TEXT NOT NULL,
    full_name     VARCHAR(200),
    role_id       UUID REFERENCES roles(id),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    last_login    TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================
-- ASSET CATEGORIES & LOCATIONS
-- =============================================================
CREATE TABLE IF NOT EXISTS asset_categories (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(100) NOT NULL,
    name_fa     VARCHAR(100) NOT NULL,
    parent_id   UUID REFERENCES asset_categories(id),
    icon        VARCHAR(50),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE IF NOT EXISTS locations (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        VARCHAR(200) NOT NULL,
    name_fa     VARCHAR(200),
    building    VARCHAR(100),
    floor       VARCHAR(50),
    room        VARCHAR(100),
    parent_id   UUID REFERENCES locations(id),
    created_at  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- =============================================================
-- ASSETS
-- =============================================================
CREATE TABLE IF NOT EXISTS assets (
    id                UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name              VARCHAR(255) NOT NULL,
    hostname          VARCHAR(255),
    ip_address        INET,
    mac_address       MACADDR,
    asset_type        VARCHAR(50) NOT NULL,  -- server, workstation, network, iot, mobile
    category_id       UUID REFERENCES asset_categories(id),
    location_id       UUID REFERENCES locations(id),
    status            VARCHAR(30) NOT NULL DEFAULT 'active', -- active, inactive, maintenance, decommissioned
    criticality       VARCHAR(20) NOT NULL DEFAULT 'medium', -- critical, high, medium, low
    os_name           VARCHAR(100),
    os_version        VARCHAR(100),
    os_family         VARCHAR(50),
    manufacturer      VARCHAR(100),
    model             VARCHAR(100),
    serial_number     VARCHAR(100),
    firmware_version  VARCHAR(100),
    cpe               TEXT,                  -- Common Platform Enumeration
    glpi_id           INTEGER,               -- GLPI asset ID
    owner_id          UUID REFERENCES users(id),
    department        VARCHAR(100),
    description       TEXT,
    tags              TEXT[] DEFAULT '{}',
    custom_fields     JSONB DEFAULT '{}',
    first_seen        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_seen         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at        TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_assets_ip ON assets(ip_address);
CREATE INDEX IF NOT EXISTS idx_assets_hostname ON assets(hostname);
CREATE INDEX IF NOT EXISTS idx_assets_status ON assets(status);
CREATE INDEX IF NOT EXISTS idx_assets_type ON assets(asset_type);
CREATE INDEX IF NOT EXISTS idx_assets_criticality ON assets(criticality);
CREATE INDEX IF NOT EXISTS idx_assets_tags ON assets USING GIN(tags);

-- =============================================================
-- ASSET PORTS & SERVICES
-- =============================================================
CREATE TABLE IF NOT EXISTS asset_ports (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id    UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    port        INTEGER NOT NULL,
    protocol    VARCHAR(10) NOT NULL DEFAULT 'tcp',
    state       VARCHAR(20) NOT NULL DEFAULT 'open',
    service     VARCHAR(100),
    version     VARCHAR(200),
    banner      TEXT,
    last_seen   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(asset_id, port, protocol)
);

CREATE INDEX IF NOT EXISTS idx_ports_asset ON asset_ports(asset_id);

-- =============================================================
-- VULNERABILITIES
-- =============================================================
CREATE TABLE IF NOT EXISTS vulnerabilities (
    id               UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    cve_id           VARCHAR(20) UNIQUE,
    title            TEXT NOT NULL,
    description      TEXT,
    cvss_v3_score    NUMERIC(3,1),
    cvss_v3_vector   VARCHAR(100),
    cvss_v2_score    NUMERIC(3,1),
    severity         VARCHAR(20) NOT NULL DEFAULT 'medium', -- critical, high, medium, low, info
    cpe_matches      TEXT[] DEFAULT '{}',
    references       TEXT[] DEFAULT '{}',
    published_at     TIMESTAMPTZ,
    modified_at      TIMESTAMPTZ,
    exploit_available BOOLEAN DEFAULT FALSE,
    patch_available   BOOLEAN DEFAULT FALSE,
    created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at       TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_vuln_cve ON vulnerabilities(cve_id);
CREATE INDEX IF NOT EXISTS idx_vuln_severity ON vulnerabilities(severity);

-- =============================================================
-- ASSET VULNERABILITIES (M:M)
-- =============================================================
CREATE TABLE IF NOT EXISTS asset_vulnerabilities (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id        UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    vulnerability_id UUID NOT NULL REFERENCES vulnerabilities(id) ON DELETE CASCADE,
    status          VARCHAR(30) NOT NULL DEFAULT 'open', -- open, in_progress, mitigated, false_positive, accepted
    risk_score      NUMERIC(5,2),
    detected_at     TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resolved_at     TIMESTAMPTZ,
    notes           TEXT,
    UNIQUE(asset_id, vulnerability_id)
);

CREATE INDEX IF NOT EXISTS idx_av_asset ON asset_vulnerabilities(asset_id);
CREATE INDEX IF NOT EXISTS idx_av_status ON asset_vulnerabilities(status);

-- =============================================================
-- DISCOVERY JOBS
-- =============================================================
CREATE TABLE IF NOT EXISTS discovery_jobs (
    id             UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    name           VARCHAR(200) NOT NULL,
    scan_type      VARCHAR(30) NOT NULL DEFAULT 'full', -- quick, full, deep, passive
    target         TEXT NOT NULL,                       -- IP, CIDR, range
    status         VARCHAR(30) NOT NULL DEFAULT 'pending', -- pending, running, completed, failed
    scanner        VARCHAR(50),                         -- nmap, masscan, snmp, arp
    started_at     TIMESTAMPTZ,
    completed_at   TIMESTAMPTZ,
    assets_found   INTEGER DEFAULT 0,
    error_message  TEXT,
    schedule       VARCHAR(100),                        -- cron expression
    config         JSONB DEFAULT '{}',
    created_by     UUID REFERENCES users(id),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_jobs_status ON discovery_jobs(status);
CREATE INDEX IF NOT EXISTS idx_jobs_created ON discovery_jobs(created_at DESC);

-- =============================================================
-- RISK SCORES
-- =============================================================
CREATE TABLE IF NOT EXISTS risk_scores (
    id              UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id        UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    overall_score   NUMERIC(5,2) NOT NULL DEFAULT 0,
    vulnerability_score NUMERIC(5,2) DEFAULT 0,
    exposure_score  NUMERIC(5,2) DEFAULT 0,
    criticality_score NUMERIC(5,2) DEFAULT 0,
    calculated_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(asset_id)
);

CREATE INDEX IF NOT EXISTS idx_risk_asset ON risk_scores(asset_id);
CREATE INDEX IF NOT EXISTS idx_risk_score ON risk_scores(overall_score DESC);

-- =============================================================
-- THREAT INTELLIGENCE
-- =============================================================
CREATE TABLE IF NOT EXISTS threat_intel (
    id          UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    ioc_type    VARCHAR(30) NOT NULL, -- ip, domain, hash_md5, hash_sha1, hash_sha256, url
    ioc_value   TEXT NOT NULL,
    threat_type VARCHAR(50),          -- malware, botnet, ransomware, apt, phishing
    source      VARCHAR(100),
    severity    VARCHAR(20) NOT NULL DEFAULT 'medium',
    confidence  INTEGER DEFAULT 50,   -- 0-100
    description TEXT,
    tags        TEXT[] DEFAULT '{}',
    first_seen  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    last_seen   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at  TIMESTAMPTZ,
    is_active   BOOLEAN DEFAULT TRUE,
    UNIQUE(ioc_type, ioc_value)
);

CREATE INDEX IF NOT EXISTS idx_ioc_type ON threat_intel(ioc_type);
CREATE INDEX IF NOT EXISTS idx_ioc_value ON threat_intel(ioc_value);
CREATE INDEX IF NOT EXISTS idx_ioc_active ON threat_intel(is_active);

-- Asset IOC matches
CREATE TABLE IF NOT EXISTS asset_ioc_matches (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    asset_id     UUID NOT NULL REFERENCES assets(id) ON DELETE CASCADE,
    threat_id    UUID NOT NULL REFERENCES threat_intel(id) ON DELETE CASCADE,
    matched_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    match_field  VARCHAR(50),
    UNIQUE(asset_id, threat_id)
);

-- =============================================================
-- SIEM EVENTS
-- =============================================================
CREATE TABLE IF NOT EXISTS siem_events (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    event_type   VARCHAR(50) NOT NULL,
    severity     VARCHAR(20) NOT NULL DEFAULT 'info',
    source       VARCHAR(100),
    asset_id     UUID REFERENCES assets(id),
    title        TEXT NOT NULL,
    description  TEXT,
    raw_event    JSONB,
    forwarded    BOOLEAN DEFAULT FALSE,
    forwarded_at TIMESTAMPTZ,
    occurred_at  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_siem_type ON siem_events(event_type);
CREATE INDEX IF NOT EXISTS idx_siem_severity ON siem_events(severity);
CREATE INDEX IF NOT EXISTS idx_siem_occurred ON siem_events(occurred_at DESC);
CREATE INDEX IF NOT EXISTS idx_siem_asset ON siem_events(asset_id);

-- =============================================================
-- AUDIT LOGS
-- =============================================================
CREATE TABLE IF NOT EXISTS audit_logs (
    id            UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    user_id       UUID REFERENCES users(id),
    username      VARCHAR(100),
    action        VARCHAR(50) NOT NULL,  -- CREATE, READ, UPDATE, DELETE, LOGIN, LOGOUT, SCAN, EXPORT
    resource_type VARCHAR(50),
    resource_id   UUID,
    description   TEXT,
    ip_address    INET,
    user_agent    TEXT,
    request_data  JSONB,
    response_code INTEGER,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IF NOT EXISTS idx_audit_user ON audit_logs(user_id);
CREATE INDEX IF NOT EXISTS idx_audit_action ON audit_logs(action);
CREATE INDEX IF NOT EXISTS idx_audit_resource ON audit_logs(resource_type, resource_id);
CREATE INDEX IF NOT EXISTS idx_audit_created ON audit_logs(created_at DESC);

-- =============================================================
-- REPORTS
-- =============================================================
CREATE TABLE IF NOT EXISTS reports (
    id           UUID PRIMARY KEY DEFAULT uuid_generate_v4(),
    title        VARCHAR(300) NOT NULL,
    report_type  VARCHAR(50) NOT NULL,  -- summary, vulnerability, risk, inventory, compliance
    status       VARCHAR(30) NOT NULL DEFAULT 'pending',
    format       VARCHAR(10) NOT NULL DEFAULT 'pdf',  -- pdf, xlsx, csv
    filters      JSONB DEFAULT '{}',
    file_path    TEXT,
    file_size    INTEGER,
    created_by   UUID REFERENCES users(id),
    created_at   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at TIMESTAMPTZ
);

-- =============================================================
-- SEED DATA
-- =============================================================

-- Default roles
INSERT INTO roles (id, name, description, permissions) VALUES
    ('00000000-0000-0000-0000-000000000001', 'admin',
     'مدیر سیستم', '["*"]'),
    ('00000000-0000-0000-0000-000000000002', 'analyst',
     'تحلیلگر امنیتی', '["assets:read","assets:write","vulnerabilities:read","vulnerabilities:write","reports:read","reports:write","discovery:read","discovery:write"]'),
    ('00000000-0000-0000-0000-000000000003', 'viewer',
     'مشاهده‌گر', '["assets:read","vulnerabilities:read","reports:read"]')
ON CONFLICT DO NOTHING;

-- Default admin user (password: Admin@1234)
INSERT INTO users (id, username, email, password_hash, full_name, role_id, is_active) VALUES
    ('00000000-0000-0000-0000-000000000001', 'admin', 'admin@cyberplatform.local',
     '$2a$11$rBnFLpPCFNJQ9R7K.tHXTOa/Y8RZJz1K2KCf5t7UBl9lHBMBxZGdW',
     'مدیر سیستم', '00000000-0000-0000-0000-000000000001', TRUE)
ON CONFLICT DO NOTHING;

-- Asset categories
INSERT INTO asset_categories (id, name, name_fa) VALUES
    ('10000000-0000-0000-0000-000000000001', 'Server', 'سرور'),
    ('10000000-0000-0000-0000-000000000002', 'Workstation', 'ایستگاه کاری'),
    ('10000000-0000-0000-0000-000000000003', 'Network Device', 'تجهیزات شبکه'),
    ('10000000-0000-0000-0000-000000000004', 'IoT Device', 'دستگاه IoT'),
    ('10000000-0000-0000-0000-000000000005', 'Mobile Device', 'دستگاه موبایل'),
    ('10000000-0000-0000-0000-000000000006', 'Security Device', 'تجهیزات امنیتی'),
    ('10000000-0000-0000-0000-000000000007', 'Storage', 'ذخیره‌سازی'),
    ('10000000-0000-0000-0000-000000000008', 'Virtual Machine', 'ماشین مجازی')
ON CONFLICT DO NOTHING;

-- Default location
INSERT INTO locations (id, name, name_fa, building) VALUES
    ('20000000-0000-0000-0000-000000000001', 'Main Data Center', 'مرکز داده اصلی', 'ساختمان A')
ON CONFLICT DO NOTHING;
