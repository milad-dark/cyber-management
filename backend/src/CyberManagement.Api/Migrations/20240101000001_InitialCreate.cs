using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CyberManagement.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    permissions = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table => table.PrimaryKey("pk_roles", x => x.id));

            migrationBuilder.CreateTable(
                name: "asset_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name_fa = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    icon = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asset_categories", x => x.id);
                    table.ForeignKey("fk_categories_parent", x => x.parent_id, "asset_categories", "id");
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    name_fa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    building = table.Column<string>(type: "text", nullable: true),
                    floor = table.Column<string>(type: "text", nullable: true),
                    room = table.Column<string>(type: "text", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_locations", x => x.id);
                    table.ForeignKey("fk_locations_parent", x => x.parent_id, "locations", "id");
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    role_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    last_login = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey("fk_users_roles_role_id", x => x.role_id, "roles", "id");
                });

            migrationBuilder.CreateTable(
                name: "assets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    hostname = table.Column<string>(type: "character varying(255)", nullable: true),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    mac_address = table.Column<string>(type: "text", nullable: true),
                    asset_type = table.Column<string>(type: "character varying(50)", nullable: false, defaultValue: "server"),
                    category_id = table.Column<Guid>(type: "uuid", nullable: true),
                    location_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", nullable: false, defaultValue: "active"),
                    criticality = table.Column<string>(type: "character varying(20)", nullable: false, defaultValue: "medium"),
                    os_name = table.Column<string>(type: "text", nullable: true),
                    os_version = table.Column<string>(type: "text", nullable: true),
                    os_family = table.Column<string>(type: "text", nullable: true),
                    manufacturer = table.Column<string>(type: "text", nullable: true),
                    model = table.Column<string>(type: "text", nullable: true),
                    serial_number = table.Column<string>(type: "text", nullable: true),
                    firmware_version = table.Column<string>(type: "text", nullable: true),
                    cpe = table.Column<string>(type: "text", nullable: true),
                    glpi_id = table.Column<int>(type: "integer", nullable: true),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: true),
                    department = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    tags = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                    custom_fields = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    first_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    last_seen = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_assets", x => x.id);
                    table.ForeignKey("fk_assets_categories", x => x.category_id, "asset_categories", "id");
                    table.ForeignKey("fk_assets_locations", x => x.location_id, "locations", "id");
                    table.ForeignKey("fk_assets_owner", x => x.owner_id, "users", "id");
                });

            // Additional tables
            migrationBuilder.CreateTable("asset_ports", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                asset_id = table.Column<Guid>("uuid", nullable: false),
                port = table.Column<int>("integer", nullable: false),
                protocol = table.Column<string>("character varying(10)", nullable: false, defaultValue: "tcp"),
                state = table.Column<string>("character varying(20)", nullable: false, defaultValue: "open"),
                service = table.Column<string>("text", nullable: true),
                version = table.Column<string>("text", nullable: true),
                banner = table.Column<string>("text", nullable: true),
                last_seen = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_asset_ports", x => x.id);
                c.ForeignKey("fk_asset_ports_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
            });

            migrationBuilder.CreateTable("vulnerabilities", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                cve_id = table.Column<string>("character varying(20)", nullable: true),
                title = table.Column<string>("text", nullable: false),
                description = table.Column<string>("text", nullable: true),
                cvss_v3_score = table.Column<decimal>("numeric(4,1)", nullable: true),
                cvss_v3_vector = table.Column<string>("text", nullable: true),
                cvss_v2_score = table.Column<decimal>("numeric(4,1)", nullable: true),
                severity = table.Column<string>("character varying(20)", nullable: false, defaultValue: "medium"),
                cpe_matches = table.Column<string[]>("text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                references = table.Column<string[]>("text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                published_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                modified_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                exploit_available = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                patch_available = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c => c.PrimaryKey("pk_vulnerabilities", x => x.id));

            migrationBuilder.CreateTable("asset_vulnerabilities", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                asset_id = table.Column<Guid>("uuid", nullable: false),
                vulnerability_id = table.Column<Guid>("uuid", nullable: false),
                status = table.Column<string>("character varying(30)", nullable: false, defaultValue: "open"),
                risk_score = table.Column<decimal>("numeric(5,2)", nullable: true),
                detected_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                resolved_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                notes = table.Column<string>("text", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_asset_vulnerabilities", x => x.id);
                c.ForeignKey("fk_av_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
                c.ForeignKey("fk_av_vulns", x => x.vulnerability_id, "vulnerabilities", "id", onDelete: ReferentialAction.Cascade);
            });

            migrationBuilder.CreateTable("discovery_jobs", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                name = table.Column<string>("character varying(200)", nullable: false),
                scan_type = table.Column<string>("character varying(30)", nullable: false, defaultValue: "full"),
                target = table.Column<string>("text", nullable: false),
                status = table.Column<string>("character varying(30)", nullable: false, defaultValue: "pending"),
                scanner = table.Column<string>("text", nullable: true),
                started_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                assets_found = table.Column<int>("integer", nullable: false, defaultValue: 0),
                error_message = table.Column<string>("text", nullable: true),
                schedule = table.Column<string>("text", nullable: true),
                config = table.Column<string>("jsonb", nullable: false, defaultValue: "{}"),
                created_by_id = table.Column<Guid>("uuid", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_discovery_jobs", x => x.id);
                c.ForeignKey("fk_jobs_users", x => x.created_by_id, "users", "id");
            });

            migrationBuilder.CreateTable("risk_scores", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                asset_id = table.Column<Guid>("uuid", nullable: false),
                overall_score = table.Column<decimal>("numeric(5,2)", nullable: false),
                vulnerability_score = table.Column<decimal>("numeric(5,2)", nullable: false),
                exposure_score = table.Column<decimal>("numeric(5,2)", nullable: false),
                criticality_score = table.Column<decimal>("numeric(5,2)", nullable: false),
                calculated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_risk_scores", x => x.id);
                c.ForeignKey("fk_risk_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
            });

            migrationBuilder.CreateTable("threat_intel", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                ioc_type = table.Column<string>("character varying(30)", nullable: false),
                ioc_value = table.Column<string>("text", nullable: false),
                threat_type = table.Column<string>("text", nullable: true),
                source = table.Column<string>("text", nullable: true),
                severity = table.Column<string>("character varying(20)", nullable: false, defaultValue: "medium"),
                confidence = table.Column<int>("integer", nullable: false, defaultValue: 50),
                description = table.Column<string>("text", nullable: true),
                tags = table.Column<string[]>("text[]", nullable: false, defaultValueSql: "'{}'::text[]"),
                first_seen = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                last_seen = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                expires_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                is_active = table.Column<bool>("boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c => c.PrimaryKey("pk_threat_intel", x => x.id));

            migrationBuilder.CreateTable("asset_ioc_matches", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                asset_id = table.Column<Guid>("uuid", nullable: false),
                threat_id = table.Column<Guid>("uuid", nullable: false),
                matched_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                match_field = table.Column<string>("text", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_asset_ioc_matches", x => x.id);
                c.ForeignKey("fk_ioc_match_assets", x => x.asset_id, "assets", "id", onDelete: ReferentialAction.Cascade);
                c.ForeignKey("fk_ioc_match_threats", x => x.threat_id, "threat_intel", "id", onDelete: ReferentialAction.Cascade);
            });

            migrationBuilder.CreateTable("siem_events", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                event_type = table.Column<string>("character varying(50)", nullable: false),
                severity = table.Column<string>("character varying(20)", nullable: false, defaultValue: "info"),
                source = table.Column<string>("text", nullable: true),
                asset_id = table.Column<Guid>("uuid", nullable: true),
                title = table.Column<string>("text", nullable: false),
                description = table.Column<string>("text", nullable: true),
                raw_event = table.Column<string>("jsonb", nullable: true),
                forwarded = table.Column<bool>("boolean", nullable: false, defaultValue: false),
                forwarded_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                occurred_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_siem_events", x => x.id);
                c.ForeignKey("fk_siem_events_assets", x => x.asset_id, "assets", "id");
            });

            migrationBuilder.CreateTable("audit_logs", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                user_id = table.Column<Guid>("uuid", nullable: true),
                username = table.Column<string>("character varying(100)", nullable: true),
                action = table.Column<string>("character varying(50)", nullable: false),
                resource_type = table.Column<string>("text", nullable: true),
                resource_id = table.Column<Guid>("uuid", nullable: true),
                description = table.Column<string>("text", nullable: true),
                ip_address = table.Column<string>("text", nullable: true),
                user_agent = table.Column<string>("text", nullable: true),
                request_data = table.Column<string>("jsonb", nullable: true),
                response_code = table.Column<int>("integer", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_audit_logs", x => x.id);
                c.ForeignKey("fk_audit_users", x => x.user_id, "users", "id");
            });

            migrationBuilder.CreateTable("reports", table => new
            {
                id = table.Column<Guid>("uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                title = table.Column<string>("character varying(300)", nullable: false),
                report_type = table.Column<string>("character varying(50)", nullable: false),
                status = table.Column<string>("character varying(30)", nullable: false, defaultValue: "pending"),
                format = table.Column<string>("character varying(10)", nullable: false, defaultValue: "xlsx"),
                filters = table.Column<string>("jsonb", nullable: false, defaultValue: "{}"),
                file_path = table.Column<string>("text", nullable: true),
                file_size = table.Column<long>("bigint", nullable: true),
                created_by_id = table.Column<Guid>("uuid", nullable: true),
                completed_at = table.Column<DateTime>("timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                updated_at = table.Column<DateTime>("timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
            }, c =>
            {
                c.PrimaryKey("pk_reports", x => x.id);
                c.ForeignKey("fk_reports_users", x => x.created_by_id, "users", "id");
            });

            // Indexes
            migrationBuilder.CreateIndex("ix_users_username", "users", "username", unique: true);
            migrationBuilder.CreateIndex("ix_users_email", "users", "email", unique: true);
            migrationBuilder.CreateIndex("ix_asset_ports_unique", "asset_ports", new[] { "asset_id", "port", "protocol" }, unique: true);
            migrationBuilder.CreateIndex("ix_av_unique", "asset_vulnerabilities", new[] { "asset_id", "vulnerability_id" }, unique: true);
            migrationBuilder.CreateIndex("ix_risk_scores_asset_id", "risk_scores", "asset_id", unique: true);
            migrationBuilder.CreateIndex("ix_threat_intel_unique", "threat_intel", new[] { "ioc_type", "ioc_value" }, unique: true);
            migrationBuilder.CreateIndex("ix_ioc_match_unique", "asset_ioc_matches", new[] { "asset_id", "threat_id" }, unique: true);
            migrationBuilder.CreateIndex("ix_assets_ip", "assets", "ip_address");
            migrationBuilder.CreateIndex("ix_audit_logs_created", "audit_logs", "created_at");
            migrationBuilder.CreateIndex("ix_siem_events_occurred", "siem_events", "occurred_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("reports");
            migrationBuilder.DropTable("audit_logs");
            migrationBuilder.DropTable("siem_events");
            migrationBuilder.DropTable("asset_ioc_matches");
            migrationBuilder.DropTable("threat_intel");
            migrationBuilder.DropTable("risk_scores");
            migrationBuilder.DropTable("discovery_jobs");
            migrationBuilder.DropTable("asset_vulnerabilities");
            migrationBuilder.DropTable("vulnerabilities");
            migrationBuilder.DropTable("asset_ports");
            migrationBuilder.DropTable("assets");
            migrationBuilder.DropTable("locations");
            migrationBuilder.DropTable("asset_categories");
            migrationBuilder.DropTable("users");
            migrationBuilder.DropTable("roles");
        }
    }
}
