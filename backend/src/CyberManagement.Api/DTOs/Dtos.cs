namespace CyberManagement.Api.DTOs;

// ─── Auth ──────────────────────────────────────────────────
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string TokenType, int ExpiresIn, UserDto User);
public record UserDto(Guid Id, string Username, string Email, string? FullName, string? Role, bool IsActive, DateTime? LastLogin);

// ─── Common ────────────────────────────────────────────────
public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
public record ApiResponse<T>(bool Success, T? Data, string? Message = null, object? Errors = null);

// ─── Asset ────────────────────────────────────────────────
public class AssetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? CategoryName { get; set; }
    public string? LocationName { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Criticality { get; set; } = string.Empty;
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public int VulnerabilityCount { get; set; }
    public int CriticalVulnCount { get; set; }
    public decimal? RiskScore { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssetDetailDto : AssetDto
{
    public string? SerialNumber { get; set; }
    public string? FirmwareVersion { get; set; }
    public string? Cpe { get; set; }
    public int? GlpiId { get; set; }
    public string? OwnerName { get; set; }
    public Dictionary<string, object?> CustomFields { get; set; } = new();
    public List<PortDto> Ports { get; set; } = new();
    public List<AssetVulnDto> Vulnerabilities { get; set; } = new();
}

public class CreateAssetRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Hostname { get; set; }
    public string? IpAddress { get; set; }
    public string? MacAddress { get; set; }
    public string AssetType { get; set; } = "server";
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public string Status { get; set; } = "active";
    public string Criticality { get; set; } = "medium";
    public string? OsName { get; set; }
    public string? OsVersion { get; set; }
    public string? Manufacturer { get; set; }
    public string? Model { get; set; }
    public string? SerialNumber { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UpdateAssetRequest : CreateAssetRequest { }

public class AssetFilterRequest
{
    public string? Search { get; set; }
    public string? AssetType { get; set; }
    public string? Status { get; set; }
    public string? Criticality { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string SortBy { get; set; } = "createdAt";
    public string SortDir { get; set; } = "desc";
}

// ─── Port ─────────────────────────────────────────────────
public class PortDto
{
    public int Port { get; set; }
    public string Protocol { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string? Service { get; set; }
    public string? Version { get; set; }
}

// ─── Vulnerability ────────────────────────────────────────
public class VulnerabilityDto
{
    public Guid Id { get; set; }
    public string? CveId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? CvssV3Score { get; set; }
    public string? CvssV3Vector { get; set; }
    public string Severity { get; set; } = string.Empty;
    public bool ExploitAvailable { get; set; }
    public bool PatchAvailable { get; set; }
    public int AffectedAssetsCount { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AssetVulnDto
{
    public Guid VulnerabilityId { get; set; }
    public string? CveId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public decimal? CvssV3Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DetectedAt { get; set; }
    public bool ExploitAvailable { get; set; }
    public bool PatchAvailable { get; set; }
}

public class VulnerabilityFilterRequest
{
    public string? Search { get; set; }
    public string? Severity { get; set; }
    public string? Status { get; set; }
    public bool? ExploitAvailable { get; set; }
    public Guid? AssetId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// ─── Discovery ────────────────────────────────────────────
public class DiscoveryJobDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Scanner { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int AssetsFound { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDiscoveryJobRequest
{
    public string Name { get; set; } = string.Empty;
    public string ScanType { get; set; } = "full";
    public string Target { get; set; } = string.Empty;
    public string? Scanner { get; set; }
    public string? Schedule { get; set; }
    public Dictionary<string, object?> Config { get; set; } = new();
}

// ─── Risk ─────────────────────────────────────────────────
public class RiskScoreDto
{
    public Guid AssetId { get; set; }
    public string AssetName { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public decimal OverallScore { get; set; }
    public decimal VulnerabilityScore { get; set; }
    public decimal ExposureScore { get; set; }
    public decimal CriticalityScore { get; set; }
    public string RiskLevel { get; set; } = string.Empty;
    public DateTime CalculatedAt { get; set; }
}

// ─── Threat Intel ─────────────────────────────────────────
public class ThreatIntelDto
{
    public Guid Id { get; set; }
    public string IocType { get; set; } = string.Empty;
    public string IocValue { get; set; } = string.Empty;
    public string? ThreatType { get; set; }
    public string? Source { get; set; }
    public string Severity { get; set; } = string.Empty;
    public int Confidence { get; set; }
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime FirstSeen { get; set; }
    public DateTime LastSeen { get; set; }
}

public class CreateThreatIntelRequest
{
    public string IocType { get; set; } = string.Empty;
    public string IocValue { get; set; } = string.Empty;
    public string? ThreatType { get; set; }
    public string? Source { get; set; }
    public string Severity { get; set; } = "medium";
    public int Confidence { get; set; } = 50;
    public string? Description { get; set; }
    public List<string> Tags { get; set; } = new();
}

// ─── SIEM ─────────────────────────────────────────────────
public class SiemEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? AssetName { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Forwarded { get; set; }
    public DateTime OccurredAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Audit Log ────────────────────────────────────────────
public class AuditLogDto
{
    public Guid Id { get; set; }
    public string? Username { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? ResourceType { get; set; }
    public Guid? ResourceId { get; set; }
    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public int? ResponseCode { get; set; }
    public DateTime CreatedAt { get; set; }
}

// ─── Dashboard ────────────────────────────────────────────
public class DashboardStatsDto
{
    public int TotalAssets { get; set; }
    public int ActiveAssets { get; set; }
    public int TotalVulnerabilities { get; set; }
    public int CriticalVulnerabilities { get; set; }
    public int HighVulnerabilities { get; set; }
    public int OpenDiscoveryJobs { get; set; }
    public int ActiveThreats { get; set; }
    public decimal AverageRiskScore { get; set; }
    public Dictionary<string, int> AssetsByType { get; set; } = new();
    public Dictionary<string, int> VulnsBySeverity { get; set; } = new();
    public Dictionary<string, int> AssetsByStatus { get; set; } = new();
    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<TopRiskAssetDto> TopRiskAssets { get; set; } = new();
}

public record RecentActivityDto(string Action, string Description, string? Username, DateTime At);
public record TopRiskAssetDto(Guid AssetId, string AssetName, string? IpAddress, decimal RiskScore, string Criticality);

// ─── Report ───────────────────────────────────────────────
public class ReportDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Format { get; set; } = string.Empty;
    public long? FileSize { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string ReportType { get; set; } = "summary";
    public string Format { get; set; } = "xlsx";
    public Dictionary<string, object?> Filters { get; set; } = new();
}
