using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CyberManagement.Api.Models;

public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

public class Role : BaseEntity
{
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> Permissions { get; set; } = new();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class User : BaseEntity
{
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? FullName { get; set; }
    public Guid? RoleId { get; set; }
    [ForeignKey(nameof(RoleId))]
    public Role? Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastLogin { get; set; }
}

public class AssetCategory : BaseEntity
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(100)]
    public string NameFa { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    [ForeignKey(nameof(ParentId))]
    public AssetCategory? Parent { get; set; }
    public string? Icon { get; set; }
}

public class Location : BaseEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(200)]
    public string? NameFa { get; set; }
    public string? Building { get; set; }
    public string? Floor { get; set; }
    public string? Room { get; set; }
    public Guid? ParentId { get; set; }
    [ForeignKey(nameof(ParentId))]
    public Location? Parent { get; set; }
}

public class Asset : BaseEntity
{
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(255)]
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    [MaxLength(50)]
    public string AssetType { get; set; } = "server";
    public Guid? CategoryId { get; set; }
    [ForeignKey(nameof(CategoryId))]
    public AssetCategory? Category { get; set; }
    public Guid? LocationId { get; set; }
    [ForeignKey(nameof(LocationId))]
    public Location? Location { get; set; }
    [MaxLength(30)]
    public string Status { get; set; } = "active";
    [MaxLength(20)]
    public string Criticality { get; set; } = "medium";
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? OsFamily { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Cpe { get; set; }
    public int? GlpiId { get; set; }
    public Guid? OwnerId { get; set; }
    [ForeignKey(nameof(OwnerId))]
    public User? Owner { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public Dictionary<string, object?> CustomFields { get; set; } = new();
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public ICollection<AssetPort> Ports { get; set; } = new List<AssetPort>();
    public ICollection<AssetVulnerability> AssetVulnerabilities { get; set; } = new List<AssetVulnerability>();
    public RiskScore? RiskScore { get; set; }
}

public class AssetPort : BaseEntity
{
    public Guid AssetId { get; set; }
    [ForeignKey(nameof(AssetId))]
    public Asset Asset { get; set; } = null!;
    public int Port { get; set; }
    [MaxLength(10)]
    public string Protocol { get; set; } = "tcp";
    [MaxLength(20)]
    public string State { get; set; } = "open";
    public string? Service { get; set; }
    public string? Version { get; set; }
    public string? Banner { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

public class Vulnerability : BaseEntity
{
    [MaxLength(20)]
    public string? CveId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? CvssV3Score { get; set; }
    public string? CvssV3Vector { get; set; }
    public decimal? CvssV2Score { get; set; }
    [MaxLength(20)]
    public string Severity { get; set; } = "medium";
    public List<string> CpeMatches { get; set; } = new();
    public List<string> References { get; set; } = new();
    public DateTime? PublishedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool ExploitAvailable { get; set; }
    public bool PatchAvailable { get; set; }
    public ICollection<AssetVulnerability> AssetVulnerabilities { get; set; } = new List<AssetVulnerability>();
}

public class AssetVulnerability : BaseEntity
{
    public Guid AssetId { get; set; }
    [ForeignKey(nameof(AssetId))]
    public Asset Asset { get; set; } = null!;
    public Guid VulnerabilityId { get; set; }
    [ForeignKey(nameof(VulnerabilityId))]
    public Vulnerability Vulnerability { get; set; } = null!;
    [MaxLength(30)]
    public string Status { get; set; } = "open";
    public decimal? RiskScore { get; set; }
    public DateTime DetectedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
    public string? Notes { get; set; }
}

public class DiscoveryJob : BaseEntity
{
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(30)]
    public string ScanType { get; set; } = "full";
    public string Target { get; set; } = string.Empty;
    [MaxLength(30)]
    public string Status { get; set; } = "pending";
    public string? Scanner { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AssetsFound { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Schedule { get; set; }
    public Dictionary<string, object?> Config { get; set; } = new();
    public Guid? CreatedById { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }
}

public class RiskScore : BaseEntity
{
    public Guid AssetId { get; set; }
    [ForeignKey(nameof(AssetId))]
    public Asset Asset { get; set; } = null!;
    public decimal OverallScore { get; set; }
    public decimal VulnerabilityScore { get; set; }
    public decimal ExposureScore { get; set; }
    public decimal CriticalityScore { get; set; }
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

public class ThreatIntel : BaseEntity
{
    [MaxLength(30)]
    public string IocType { get; set; } = string.Empty;
    public string IocValue { get; set; } = string.Empty;
    public string? ThreatType { get; set; }
    public string? Source { get; set; }
    [MaxLength(20)]
    public string Severity { get; set; } = "medium";
    public int Confidence { get; set; } = 50;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public DateTime FirstSeen { get; set; } = DateTime.UtcNow;
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
}

public class AssetIocMatch : BaseEntity
{
    public Guid AssetId { get; set; }
    [ForeignKey(nameof(AssetId))]
    public Asset Asset { get; set; } = null!;
    public Guid ThreatId { get; set; }
    [ForeignKey(nameof(ThreatId))]
    public ThreatIntel Threat { get; set; } = null!;
    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
    public string? MatchField { get; set; }
}

public class SiemEvent : BaseEntity
{
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;
    [MaxLength(20)]
    public string Severity { get; set; } = "info";
    public string? Source { get; set; }
    public Guid? AssetId { get; set; }
    [ForeignKey(nameof(AssetId))]
    public Asset? Asset { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Dictionary<string, object?>? RawEvent { get; set; }
    public bool Forwarded { get; set; }
    public DateTime? ForwardedAt { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
    [MaxLength(100)]
    public string? Username { get; set; }
    [MaxLength(50)]
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public Dictionary<string, object?>? RequestData { get; set; }
    public int? ResponseCode { get; set; }
}

public class Report : BaseEntity
{
    [MaxLength(300)]
    public string Title { get; set; } = string.Empty;
    [MaxLength(50)]
    public string ReportType { get; set; } = string.Empty;
    [MaxLength(30)]
    public string Status { get; set; } = "pending";
    [MaxLength(10)]
    public string Format { get; set; } = "pdf";
    public Dictionary<string, object?> Filters { get; set; } = new();
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public Guid? CreatedById { get; set; }
    [ForeignKey(nameof(CreatedById))]
    public User? CreatedBy { get; set; }
    public DateTime? CompletedAt { get; set; }
}
