using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IAssetService
{
    Task<PagedResult<AssetDto>> GetAssetsAsync(AssetFilterRequest filter);
    Task<AssetDetailDto?> GetAssetByIdAsync(Guid id);
    Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, Guid? userId);
    Task<AssetDto?> UpdateAssetAsync(Guid id, UpdateAssetRequest request);
    Task<bool> DeleteAssetAsync(Guid id);
    Task<int> GetTotalCountAsync();
}

public class AssetService : IAssetService
{
    private readonly AppDbContext _db;

    public AssetService(AppDbContext db) => _db = db;

    public async Task<PagedResult<AssetDto>> GetAssetsAsync(AssetFilterRequest filter)
    {
        var query = _db.Assets
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.RiskScore)
            .Include(a => a.AssetVulnerabilities)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.ToLower();
            query = query.Where(a =>
                a.Name.ToLower().Contains(s) ||
                (a.Hostname != null && a.Hostname.ToLower().Contains(s)) ||
                (a.IpAddress != null && a.IpAddress.Contains(s)));
        }

        if (!string.IsNullOrWhiteSpace(filter.AssetType))
            query = query.Where(a => a.AssetType == filter.AssetType);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(a => a.Status == filter.Status);

        if (!string.IsNullOrWhiteSpace(filter.Criticality))
            query = query.Where(a => a.Criticality == filter.Criticality);

        if (filter.CategoryId.HasValue)
            query = query.Where(a => a.CategoryId == filter.CategoryId);

        if (filter.LocationId.HasValue)
            query = query.Where(a => a.LocationId == filter.LocationId);

        var total = await query.CountAsync();

        query = filter.SortBy switch
        {
            "name" => filter.SortDir == "asc" ? query.OrderBy(a => a.Name) : query.OrderByDescending(a => a.Name),
            "ipAddress" => filter.SortDir == "asc" ? query.OrderBy(a => a.IpAddress) : query.OrderByDescending(a => a.IpAddress),
            "riskScore" => filter.SortDir == "asc"
                ? query.OrderBy(a => a.RiskScore!.OverallScore)
                : query.OrderByDescending(a => a.RiskScore!.OverallScore),
            _ => filter.SortDir == "asc" ? query.OrderBy(a => a.CreatedAt) : query.OrderByDescending(a => a.CreatedAt)
        };

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => MapToDto(a))
            .ToListAsync();

        return new PagedResult<AssetDto>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<AssetDetailDto?> GetAssetByIdAsync(Guid id)
    {
        var asset = await _db.Assets
            .Include(a => a.Category)
            .Include(a => a.Location)
            .Include(a => a.Owner)
            .Include(a => a.Ports)
            .Include(a => a.RiskScore)
            .Include(a => a.AssetVulnerabilities)
                .ThenInclude(av => av.Vulnerability)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (asset == null) return null;

        var dto = new AssetDetailDto
        {
            Id = asset.Id,
            Name = asset.Name,
            Hostname = asset.Hostname,
            IpAddress = asset.IpAddress,
            MacAddress = asset.MacAddress,
            AssetType = asset.AssetType,
            CategoryName = asset.Category?.NameFa ?? asset.Category?.Name,
            LocationName = asset.Location?.NameFa ?? asset.Location?.Name,
            Status = asset.Status,
            Criticality = asset.Criticality,
            OsName = asset.OsName,
            OsVersion = asset.OsVersion,
            Manufacturer = asset.Manufacturer,
            Model = asset.Model,
            SerialNumber = asset.SerialNumber,
            FirmwareVersion = asset.FirmwareVersion,
            Cpe = asset.Cpe,
            GlpiId = asset.GlpiId,
            OwnerName = asset.Owner?.FullName ?? asset.Owner?.Username,
            Department = asset.Department,
            Description = asset.Description,
            Tags = asset.Tags,
            CustomFields = asset.CustomFields,
            VulnerabilityCount = asset.AssetVulnerabilities.Count,
            CriticalVulnCount = asset.AssetVulnerabilities.Count(av => av.Vulnerability.Severity == "critical"),
            RiskScore = asset.RiskScore?.OverallScore,
            FirstSeen = asset.FirstSeen,
            LastSeen = asset.LastSeen,
            CreatedAt = asset.CreatedAt,
            Ports = asset.Ports.Select(p => new PortDto
            {
                Port = p.Port,
                Protocol = p.Protocol,
                State = p.State,
                Service = p.Service,
                Version = p.Version
            }).ToList(),
            Vulnerabilities = asset.AssetVulnerabilities.Select(av => new AssetVulnDto
            {
                VulnerabilityId = av.VulnerabilityId,
                CveId = av.Vulnerability.CveId,
                Title = av.Vulnerability.Title,
                Severity = av.Vulnerability.Severity,
                CvssV3Score = av.Vulnerability.CvssV3Score,
                Status = av.Status,
                DetectedAt = av.DetectedAt,
                ExploitAvailable = av.Vulnerability.ExploitAvailable,
                PatchAvailable = av.Vulnerability.PatchAvailable
            }).ToList()
        };

        return dto;
    }

    public async Task<AssetDto> CreateAssetAsync(CreateAssetRequest request, Guid? userId)
    {
        var asset = new Asset
        {
            Name = request.Name,
            Hostname = request.Hostname,
            IpAddress = request.IpAddress,
            MacAddress = request.MacAddress,
            AssetType = request.AssetType,
            CategoryId = request.CategoryId,
            LocationId = request.LocationId,
            Status = request.Status,
            Criticality = request.Criticality,
            OsName = request.OsName,
            OsVersion = request.OsVersion,
            Manufacturer = request.Manufacturer,
            Model = request.Model,
            SerialNumber = request.SerialNumber,
            Department = request.Department,
            Description = request.Description,
            Tags = request.Tags,
            OwnerId = userId
        };

        _db.Assets.Add(asset);
        await _db.SaveChangesAsync();

        return MapToDto(asset);
    }

    public async Task<AssetDto?> UpdateAssetAsync(Guid id, UpdateAssetRequest request)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset == null) return null;

        asset.Name = request.Name;
        asset.Hostname = request.Hostname;
        asset.IpAddress = request.IpAddress;
        asset.MacAddress = request.MacAddress;
        asset.AssetType = request.AssetType;
        asset.CategoryId = request.CategoryId;
        asset.LocationId = request.LocationId;
        asset.Status = request.Status;
        asset.Criticality = request.Criticality;
        asset.OsName = request.OsName;
        asset.OsVersion = request.OsVersion;
        asset.Manufacturer = request.Manufacturer;
        asset.Model = request.Model;
        asset.SerialNumber = request.SerialNumber;
        asset.Department = request.Department;
        asset.Description = request.Description;
        asset.Tags = request.Tags;
        asset.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return MapToDto(asset);
    }

    public async Task<bool> DeleteAssetAsync(Guid id)
    {
        var asset = await _db.Assets.FindAsync(id);
        if (asset == null) return false;
        _db.Assets.Remove(asset);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetTotalCountAsync() => await _db.Assets.CountAsync();

    private static AssetDto MapToDto(Asset a) => new()
    {
        Id = a.Id,
        Name = a.Name,
        Hostname = a.Hostname,
        IpAddress = a.IpAddress,
        MacAddress = a.MacAddress,
        AssetType = a.AssetType,
        CategoryName = a.Category?.NameFa ?? a.Category?.Name,
        LocationName = a.Location?.NameFa ?? a.Location?.Name,
        Status = a.Status,
        Criticality = a.Criticality,
        OsName = a.OsName,
        OsVersion = a.OsVersion,
        Manufacturer = a.Manufacturer,
        Model = a.Model,
        Department = a.Department,
        Description = a.Description,
        Tags = a.Tags,
        VulnerabilityCount = a.AssetVulnerabilities?.Count ?? 0,
        CriticalVulnCount = a.AssetVulnerabilities?.Count(av => av.Vulnerability?.Severity == "critical") ?? 0,
        RiskScore = a.RiskScore?.OverallScore,
        FirstSeen = a.FirstSeen,
        LastSeen = a.LastSeen,
        CreatedAt = a.CreatedAt
    };
}
