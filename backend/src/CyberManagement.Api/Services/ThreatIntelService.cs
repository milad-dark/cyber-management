using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IThreatIntelService
{
    Task<PagedResult<ThreatIntelDto>> GetThreatsAsync(int page, int pageSize, string? search, string? iocType);
    Task<ThreatIntelDto?> GetByIdAsync(Guid id);
    Task<ThreatIntelDto> CreateAsync(CreateThreatIntelRequest request);
    Task<bool> DeleteAsync(Guid id);
    Task MatchThreatsToAssetsAsync();
}

public class ThreatIntelService : IThreatIntelService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ThreatIntelService> _logger;

    public ThreatIntelService(AppDbContext db, ILogger<ThreatIntelService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<ThreatIntelDto>> GetThreatsAsync(int page, int pageSize, string? search, string? iocType)
    {
        var query = _db.ThreatIntel.Where(t => t.IsActive).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.IocValue.Contains(search) || (t.Description != null && t.Description.Contains(search)));

        if (!string.IsNullOrWhiteSpace(iocType))
            query = query.Where(t => t.IocType == iocType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.LastSeen)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => MapToDto(t))
            .ToListAsync();

        return new PagedResult<ThreatIntelDto>(items, total, page, pageSize);
    }

    public async Task<ThreatIntelDto?> GetByIdAsync(Guid id)
    {
        var t = await _db.ThreatIntel.FindAsync(id);
        return t == null ? null : MapToDto(t);
    }

    public async Task<ThreatIntelDto> CreateAsync(CreateThreatIntelRequest request)
    {
        var threat = new ThreatIntel
        {
            IocType = request.IocType,
            IocValue = request.IocValue,
            ThreatType = request.ThreatType,
            Source = request.Source,
            Severity = request.Severity,
            Confidence = request.Confidence,
            Description = request.Description,
            Tags = request.Tags
        };
        _db.ThreatIntel.Add(threat);
        await _db.SaveChangesAsync();
        return MapToDto(threat);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var t = await _db.ThreatIntel.FindAsync(id);
        if (t == null) return false;
        t.IsActive = false;
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task MatchThreatsToAssetsAsync()
    {
        var threats = await _db.ThreatIntel.Where(t => t.IsActive && t.IocType == "ip").ToListAsync();
        var assets = await _db.Assets.ToListAsync();

        foreach (var threat in threats)
        {
            var matchedAssets = assets.Where(a => a.IpAddress == threat.IocValue).ToList();
            foreach (var asset in matchedAssets)
            {
                var existing = await _db.AssetIocMatches
                    .AnyAsync(m => m.AssetId == asset.Id && m.ThreatId == threat.Id);
                if (!existing)
                {
                    _db.AssetIocMatches.Add(new AssetIocMatch
                    {
                        AssetId = asset.Id,
                        ThreatId = threat.Id,
                        MatchField = "ip_address"
                    });
                }
            }
        }
        await _db.SaveChangesAsync();
    }

    private static ThreatIntelDto MapToDto(ThreatIntel t) => new()
    {
        Id = t.Id,
        IocType = t.IocType,
        IocValue = t.IocValue,
        ThreatType = t.ThreatType,
        Source = t.Source,
        Severity = t.Severity,
        Confidence = t.Confidence,
        Description = t.Description,
        Tags = t.Tags,
        IsActive = t.IsActive,
        FirstSeen = t.FirstSeen,
        LastSeen = t.LastSeen
    };
}
