using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var totalAssets = await _db.Assets.CountAsync();
        var activeAssets = await _db.Assets.CountAsync(a => a.Status == "active");
        var totalVulns = await _db.Vulnerabilities.CountAsync();
        var criticalVulns = await _db.Vulnerabilities.CountAsync(v => v.Severity == "critical");
        var highVulns = await _db.Vulnerabilities.CountAsync(v => v.Severity == "high");
        var openJobs = await _db.DiscoveryJobs.CountAsync(j => j.Status == "running" || j.Status == "pending");
        var activeThreats = await _db.ThreatIntel.CountAsync(t => t.IsActive);
        var avgRisk = await _db.RiskScores.AnyAsync()
            ? await _db.RiskScores.AverageAsync(r => r.OverallScore)
            : 0m;

        var assetsByType = await _db.Assets
            .GroupBy(a => a.AssetType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Type, x => x.Count);

        var vulnsBySeverity = await _db.AssetVulnerabilities
            .Where(av => av.Status == "open")
            .Include(av => av.Vulnerability)
            .GroupBy(av => av.Vulnerability.Severity)
            .Select(g => new { Sev = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Sev, x => x.Count);

        var assetsByStatus = await _db.Assets
            .GroupBy(a => a.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);

        var recentAudit = await _db.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new RecentActivityDto(l.Action, l.Description ?? "", l.Username, l.CreatedAt))
            .ToListAsync();

        var topRisk = await _db.RiskScores
            .Include(r => r.Asset)
            .OrderByDescending(r => r.OverallScore)
            .Take(5)
            .Select(r => new TopRiskAssetDto(r.AssetId, r.Asset.Name, r.Asset.IpAddress, r.OverallScore, r.Asset.Criticality))
            .ToListAsync();

        return new DashboardStatsDto
        {
            TotalAssets = totalAssets,
            ActiveAssets = activeAssets,
            TotalVulnerabilities = totalVulns,
            CriticalVulnerabilities = criticalVulns,
            HighVulnerabilities = highVulns,
            OpenDiscoveryJobs = openJobs,
            ActiveThreats = activeThreats,
            AverageRiskScore = Math.Round(avgRisk, 2),
            AssetsByType = assetsByType,
            VulnsBySeverity = vulnsBySeverity,
            AssetsByStatus = assetsByStatus,
            RecentActivities = recentAudit,
            TopRiskAssets = topRisk
        };
    }
}
