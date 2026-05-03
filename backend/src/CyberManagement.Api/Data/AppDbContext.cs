using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CyberManagement.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<AssetPort> AssetPorts => Set<AssetPort>();
    public DbSet<Vulnerability> Vulnerabilities => Set<Vulnerability>();
    public DbSet<AssetVulnerability> AssetVulnerabilities => Set<AssetVulnerability>();
    public DbSet<DiscoveryJob> DiscoveryJobs => Set<DiscoveryJob>();
    public DbSet<RiskScore> RiskScores => Set<RiskScore>();
    public DbSet<ThreatIntel> ThreatIntel => Set<ThreatIntel>();
    public DbSet<AssetIocMatch> AssetIocMatches => Set<AssetIocMatch>();
    public DbSet<SiemEvent> SiemEvents => Set<SiemEvent>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Report> Reports => Set<Report>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Table naming
        modelBuilder.Entity<Role>().ToTable("roles");
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<AssetCategory>().ToTable("asset_categories");
        modelBuilder.Entity<Location>().ToTable("locations");
        modelBuilder.Entity<Asset>().ToTable("assets");
        modelBuilder.Entity<AssetPort>().ToTable("asset_ports");
        modelBuilder.Entity<Vulnerability>().ToTable("vulnerabilities");
        modelBuilder.Entity<AssetVulnerability>().ToTable("asset_vulnerabilities");
        modelBuilder.Entity<DiscoveryJob>().ToTable("discovery_jobs");
        modelBuilder.Entity<RiskScore>().ToTable("risk_scores");
        modelBuilder.Entity<ThreatIntel>().ToTable("threat_intel");
        modelBuilder.Entity<AssetIocMatch>().ToTable("asset_ioc_matches");
        modelBuilder.Entity<SiemEvent>().ToTable("siem_events");
        modelBuilder.Entity<AuditLog>().ToTable("audit_logs");
        modelBuilder.Entity<Report>().ToTable("reports");

        // JSON column conversions
        var jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        modelBuilder.Entity<Role>()
            .Property(e => e.Permissions)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new());

        modelBuilder.Entity<Asset>()
            .Property(e => e.Tags)
            .HasColumnType("text[]");

        modelBuilder.Entity<Asset>()
            .Property(e => e.CustomFields)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, jsonOptions) ?? new());

        modelBuilder.Entity<Vulnerability>()
            .Property(e => e.CpeMatches)
            .HasColumnType("text[]");

        modelBuilder.Entity<Vulnerability>()
            .Property(e => e.References)
            .HasColumnType("text[]");

        modelBuilder.Entity<ThreatIntel>()
            .Property(e => e.Tags)
            .HasColumnType("text[]");

        modelBuilder.Entity<DiscoveryJob>()
            .Property(e => e.Config)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, jsonOptions) ?? new());

        modelBuilder.Entity<SiemEvent>()
            .Property(e => e.RawEvent)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, jsonOptions));

        modelBuilder.Entity<AuditLog>()
            .Property(e => e.RequestData)
            .HasColumnType("jsonb")
            .HasConversion(
                v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
                v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, jsonOptions));

        modelBuilder.Entity<Report>()
            .Property(e => e.Filters)
            .HasColumnType("jsonb")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object?>>(v, jsonOptions) ?? new());

        // Unique constraints
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Vulnerability>().HasIndex(v => v.CveId).IsUnique();
        modelBuilder.Entity<AssetVulnerability>().HasIndex(av => new { av.AssetId, av.VulnerabilityId }).IsUnique();
        modelBuilder.Entity<AssetPort>().HasIndex(p => new { p.AssetId, p.Port, p.Protocol }).IsUnique();
        modelBuilder.Entity<ThreatIntel>().HasIndex(t => new { t.IocType, t.IocValue }).IsUnique();
        modelBuilder.Entity<AssetIocMatch>().HasIndex(m => new { m.AssetId, m.ThreatId }).IsUnique();
        modelBuilder.Entity<RiskScore>().HasIndex(r => r.AssetId).IsUnique();

        // Column name overrides
        modelBuilder.Entity<User>().Property(u => u.PasswordHash).HasColumnName("password_hash");
        modelBuilder.Entity<User>().Property(u => u.FullName).HasColumnName("full_name");
        modelBuilder.Entity<User>().Property(u => u.IsActive).HasColumnName("is_active");
        modelBuilder.Entity<User>().Property(u => u.LastLogin).HasColumnName("last_login");
        modelBuilder.Entity<User>().Property(u => u.RoleId).HasColumnName("role_id");
        modelBuilder.Entity<User>().Property(u => u.CreatedAt).HasColumnName("created_at");
        modelBuilder.Entity<User>().Property(u => u.UpdatedAt).HasColumnName("updated_at");

        // UpdatedAt auto
        foreach (var entity in modelBuilder.Model.GetEntityTypes())
        {
            var prop = entity.FindProperty("UpdatedAt");
            if (prop != null)
                prop.SetValueGeneratedOnAddOrUpdate();
        }
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Modified);
        foreach (var entry in entries)
            entry.Entity.UpdatedAt = DateTime.UtcNow;
    }
}
