using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IRiskService
{
    Task<PagedResult<RiskScoreDto>> GetRiskScoresAsync(int page, int pageSize);
    Task RecalculateRiskForAsset(Guid assetId);
    Task RecalculateAllRisksAsync();
}

public class RiskService : IRiskService
{
    private readonly AppDbContext _db;
    private readonly ILogger<RiskService> _logger;

    // Risk score weighting factors (must sum to 1.0)
    private const decimal VulnerabilityWeight = 0.5m;
    private const decimal ExposureWeight = 0.2m;
    private const decimal CriticalityWeight = 0.3m;

    public RiskService(AppDbContext db, ILogger<RiskService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<RiskScoreDto>> GetRiskScoresAsync(int page, int pageSize)
    {
        var total = await _db.RiskScores.CountAsync();
        var items = await _db.RiskScores
            .Include(r => r.Asset)
            .OrderByDescending(r => r.OverallScore)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RiskScoreDto
            {
                AssetId = r.AssetId,
                AssetName = r.Asset.Name,
                IpAddress = r.Asset.IpAddress,
                OverallScore = r.OverallScore,
                VulnerabilityScore = r.VulnerabilityScore,
                ExposureScore = r.ExposureScore,
                CriticalityScore = r.CriticalityScore,
                RiskLevel = GetRiskLevel(r.OverallScore),
                CalculatedAt = r.CalculatedAt
            })
            .ToListAsync();

        return new PagedResult<RiskScoreDto>(items, total, page, pageSize);
    }

    public async Task RecalculateRiskForAsset(Guid assetId)
    {
        var asset = await _db.Assets
            .Include(a => a.AssetVulnerabilities)
                .ThenInclude(av => av.Vulnerability)
            .Include(a => a.Ports)
            .FirstOrDefaultAsync(a => a.Id == assetId);

        if (asset == null) return;

        var openVulns = asset.AssetVulnerabilities
            .Where(av => av.Status == "open" || av.Status == "in_progress")
            .ToList();

        // Vulnerability score (0-100)
        var vulnScore = 0m;
        if (openVulns.Any())
        {
            var criticalCount = openVulns.Count(av => av.Vulnerability.Severity == "critical");
            var highCount = openVulns.Count(av => av.Vulnerability.Severity == "high");
            var mediumCount = openVulns.Count(av => av.Vulnerability.Severity == "medium");
            vulnScore = Math.Min(100, criticalCount * 25m + highCount * 10m + mediumCount * 5m);
        }

        // Exposure score based on open ports
        var exposureScore = Math.Min(100, asset.Ports.Count(p => p.State == "open") * 5m);

        // Criticality score
        var criticalityScore = asset.Criticality switch
        {
            "critical" => 100m,
            "high" => 75m,
            "medium" => 50m,
            "low" => 25m,
            _ => 50m
        };

        var overallScore = Math.Round((vulnScore * VulnerabilityWeight + exposureScore * ExposureWeight + criticalityScore * CriticalityWeight), 2);

        var existing = await _db.RiskScores.FirstOrDefaultAsync(r => r.AssetId == assetId);
        if (existing == null)
        {
            _db.RiskScores.Add(new Models.RiskScore
            {
                AssetId = assetId,
                OverallScore = overallScore,
                VulnerabilityScore = vulnScore,
                ExposureScore = exposureScore,
                CriticalityScore = criticalityScore,
                CalculatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.OverallScore = overallScore;
            existing.VulnerabilityScore = vulnScore;
            existing.ExposureScore = exposureScore;
            existing.CriticalityScore = criticalityScore;
            existing.CalculatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
    }

    public async Task RecalculateAllRisksAsync()
    {
        var assetIds = await _db.Assets.Select(a => a.Id).ToListAsync();
        _logger.LogInformation("Recalculating risk for {Count} assets", assetIds.Count);
        foreach (var id in assetIds)
            await RecalculateRiskForAsset(id);
    }

    private static string GetRiskLevel(decimal score) => score switch
    {
        >= 75 => "critical",
        >= 50 => "high",
        >= 25 => "medium",
        _ => "low"
    };
}
