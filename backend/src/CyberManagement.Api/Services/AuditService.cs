using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IAuditService
{
    Task LogAsync(Guid? userId, string? username, string action, string? resourceType = null,
        Guid? resourceId = null, string? description = null, string? ipAddress = null,
        string? userAgent = null, int? responseCode = null);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(int page, int pageSize, string? action, string? username);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _db;

    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(Guid? userId, string? username, string action, string? resourceType = null,
        Guid? resourceId = null, string? description = null, string? ipAddress = null,
        string? userAgent = null, int? responseCode = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Username = username,
            Action = action,
            ResourceType = resourceType,
            ResourceId = resourceId,
            Description = description,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ResponseCode = responseCode
        };
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(int page, int pageSize, string? action, string? username)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);

        if (!string.IsNullOrWhiteSpace(username))
            query = query.Where(l => l.Username != null && l.Username.Contains(username));

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto
            {
                Id = l.Id,
                Username = l.Username,
                Action = l.Action,
                ResourceType = l.ResourceType,
                ResourceId = l.ResourceId,
                Description = l.Description,
                IpAddress = l.IpAddress,
                ResponseCode = l.ResponseCode,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<AuditLogDto>(items, total, page, pageSize);
    }
}
