using CyberManagement.Api.Configuration;
using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace CyberManagement.Api.Services;

public interface ISiemService
{
    Task<PagedResult<SiemEventDto>> GetEventsAsync(int page, int pageSize, string? severity, string? eventType);
    Task<SiemEvent> CreateEventAsync(string eventType, string severity, string title, string? description, Guid? assetId, Dictionary<string, object?>? rawEvent = null);
    Task ForwardPendingEventsAsync();
}

public class SiemService : ISiemService
{
    private readonly AppDbContext _db;
    private readonly SiemOptions _opts;
    private readonly ILogger<SiemService> _logger;

    public SiemService(AppDbContext db, IOptions<SiemOptions> opts, ILogger<SiemService> logger)
    {
        _db = db;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<PagedResult<SiemEventDto>> GetEventsAsync(int page, int pageSize, string? severity, string? eventType)
    {
        var query = _db.SiemEvents.Include(e => e.Asset).AsQueryable();

        if (!string.IsNullOrWhiteSpace(severity))
            query = query.Where(e => e.Severity == severity);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new SiemEventDto
            {
                Id = e.Id,
                EventType = e.EventType,
                Severity = e.Severity,
                Source = e.Source,
                AssetName = e.Asset != null ? e.Asset.Name : null,
                Title = e.Title,
                Description = e.Description,
                Forwarded = e.Forwarded,
                OccurredAt = e.OccurredAt,
                CreatedAt = e.CreatedAt
            })
            .ToListAsync();

        return new PagedResult<SiemEventDto>(items, total, page, pageSize);
    }

    public async Task<SiemEvent> CreateEventAsync(string eventType, string severity, string title,
        string? description, Guid? assetId, Dictionary<string, object?>? rawEvent = null)
    {
        var evt = new SiemEvent
        {
            EventType = eventType,
            Severity = severity,
            Title = title,
            Description = description,
            AssetId = assetId,
            RawEvent = rawEvent,
            Source = "CyberManagement"
        };
        _db.SiemEvents.Add(evt);
        await _db.SaveChangesAsync();

        if (_opts.Enabled)
            _ = Task.Run(() => ForwardEvent(evt));

        return evt;
    }

    public async Task ForwardPendingEventsAsync()
    {
        var pending = await _db.SiemEvents
            .Where(e => !e.Forwarded)
            .OrderBy(e => e.OccurredAt)
            .Take(100)
            .ToListAsync();

        foreach (var evt in pending)
            await ForwardEvent(evt);
    }

    private async Task ForwardEvent(SiemEvent evt)
    {
        if (!_opts.Enabled) return;
        try
        {
            if (!string.IsNullOrWhiteSpace(_opts.SyslogHost))
                await SendSyslog(evt);

            if (!string.IsNullOrWhiteSpace(_opts.WebhookUrl))
                await SendWebhook(evt);

            evt.Forwarded = true;
            evt.ForwardedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to forward SIEM event {Id}", evt.Id);
        }
    }

    private async Task SendSyslog(SiemEvent evt)
    {
        var priority = evt.Severity switch { "critical" => 2, "high" => 3, "medium" => 5, _ => 6 };
        var msg = $"<{priority}>CyberMgmt: [{evt.EventType}] {evt.Title}";
        using var udp = new UdpClient();
        var bytes = Encoding.UTF8.GetBytes(msg);
        await udp.SendAsync(bytes, bytes.Length, _opts.SyslogHost, _opts.SyslogPort);
    }

    private async Task SendWebhook(SiemEvent evt)
    {
        using var http = new HttpClient();
        var payload = JsonSerializer.Serialize(new
        {
            id = evt.Id,
            event_type = evt.EventType,
            severity = evt.Severity,
            title = evt.Title,
            description = evt.Description,
            occurred_at = evt.OccurredAt
        });
        await http.PostAsync(_opts.WebhookUrl,
            new StringContent(payload, Encoding.UTF8, "application/json"));
    }
}
