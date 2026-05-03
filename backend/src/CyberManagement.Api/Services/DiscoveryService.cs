using CyberManagement.Api.Configuration;
using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace CyberManagement.Api.Services;

public interface IDiscoveryService
{
    Task<PagedResult<DiscoveryJobDto>> GetJobsAsync(int page, int pageSize);
    Task<DiscoveryJobDto?> GetJobByIdAsync(Guid id);
    Task<DiscoveryJobDto> CreateJobAsync(CreateDiscoveryJobRequest request, Guid? userId);
    Task<bool> StartJobAsync(Guid id);
    Task<bool> CancelJobAsync(Guid id);
    Task UpdateJobFromEngineAsync(Guid jobId, string status, int assetsFound, string? error);
}

public class DiscoveryService : IDiscoveryService
{
    private readonly AppDbContext _db;
    private readonly HttpClient _http;
    private readonly DiscoveryEngineOptions _opts;
    private readonly ILogger<DiscoveryService> _logger;

    public DiscoveryService(AppDbContext db, HttpClient http,
        IOptions<DiscoveryEngineOptions> opts, ILogger<DiscoveryService> logger)
    {
        _db = db;
        _http = http;
        _opts = opts.Value;
        _logger = logger;
        _http.BaseAddress = new Uri(_opts.BaseUrl);
        _http.DefaultRequestHeaders.Add("X-Engine-Secret", _opts.Secret);
    }

    public async Task<PagedResult<DiscoveryJobDto>> GetJobsAsync(int page, int pageSize)
    {
        var total = await _db.DiscoveryJobs.CountAsync();
        var items = await _db.DiscoveryJobs
            .Include(j => j.CreatedBy)
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => MapToDto(j))
            .ToListAsync();
        return new PagedResult<DiscoveryJobDto>(items, total, page, pageSize);
    }

    public async Task<DiscoveryJobDto?> GetJobByIdAsync(Guid id)
    {
        var job = await _db.DiscoveryJobs.Include(j => j.CreatedBy).FirstOrDefaultAsync(j => j.Id == id);
        return job == null ? null : MapToDto(job);
    }

    public async Task<DiscoveryJobDto> CreateJobAsync(CreateDiscoveryJobRequest request, Guid? userId)
    {
        var job = new DiscoveryJob
        {
            Name = request.Name,
            ScanType = request.ScanType,
            Target = request.Target,
            Scanner = request.Scanner,
            Schedule = request.Schedule,
            Config = request.Config,
            CreatedById = userId
        };
        _db.DiscoveryJobs.Add(job);
        await _db.SaveChangesAsync();
        return MapToDto(job);
    }

    public async Task<bool> StartJobAsync(Guid id)
    {
        var job = await _db.DiscoveryJobs.FindAsync(id);
        if (job == null || job.Status == "running") return false;

        job.Status = "running";
        job.StartedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        try
        {
            var payload = new
            {
                job_id = job.Id.ToString(),
                scan_type = job.ScanType,
                target = job.Target,
                scanner = job.Scanner ?? "nmap",
                config = job.Config
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _http.PostAsync("/scan/start", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to notify discovery engine for job {JobId}", id);
        }

        return true;
    }

    public async Task<bool> CancelJobAsync(Guid id)
    {
        var job = await _db.DiscoveryJobs.FindAsync(id);
        if (job == null) return false;
        job.Status = "cancelled";
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task UpdateJobFromEngineAsync(Guid jobId, string status, int assetsFound, string? error)
    {
        var job = await _db.DiscoveryJobs.FindAsync(jobId);
        if (job == null) return;
        job.Status = status;
        job.AssetsFound = assetsFound;
        job.ErrorMessage = error;
        if (status == "completed" || status == "failed")
            job.CompletedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
    }

    private static DiscoveryJobDto MapToDto(DiscoveryJob j) => new()
    {
        Id = j.Id,
        Name = j.Name,
        ScanType = j.ScanType,
        Target = j.Target,
        Status = j.Status,
        Scanner = j.Scanner,
        StartedAt = j.StartedAt,
        CompletedAt = j.CompletedAt,
        AssetsFound = j.AssetsFound,
        ErrorMessage = j.ErrorMessage,
        CreatedAt = j.CreatedAt
    };
}
