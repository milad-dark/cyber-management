using CyberManagement.Api.DTOs;
using CyberManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CyberManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DiscoveryController : ControllerBase
{
    private readonly IDiscoveryService _discovery;
    private readonly IAuditService _audit;

    public DiscoveryController(IDiscoveryService discovery, IAuditService audit)
    {
        _discovery = discovery;
        _audit = audit;
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<ApiResponse<PagedResult<DiscoveryJobDto>>>> GetJobs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _discovery.GetJobsAsync(page, pageSize);
        return Ok(new ApiResponse<PagedResult<DiscoveryJobDto>>(true, result));
    }

    [HttpGet("jobs/{id:guid}")]
    public async Task<ActionResult<ApiResponse<DiscoveryJobDto>>> GetJob(Guid id)
    {
        var job = await _discovery.GetJobByIdAsync(id);
        return job == null
            ? NotFound(new ApiResponse<DiscoveryJobDto>(false, null, "کار یافت نشد"))
            : Ok(new ApiResponse<DiscoveryJobDto>(true, job));
    }

    [HttpPost("jobs")]
    public async Task<ActionResult<ApiResponse<DiscoveryJobDto>>> CreateJob([FromBody] CreateDiscoveryJobRequest request)
    {
        var userId = GetUserId();
        var job = await _discovery.CreateJobAsync(request, userId);
        await _audit.LogAsync(userId, GetUsername(), "SCAN_CREATE", "DiscoveryJob", job.Id,
            $"کار کشف جدید: {job.Name}", GetIpAddress());
        return CreatedAtAction(nameof(GetJob), new { id = job.Id },
            new ApiResponse<DiscoveryJobDto>(true, job, "کار کشف ایجاد شد"));
    }

    [HttpPost("jobs/{id:guid}/start")]
    public async Task<ActionResult<ApiResponse<object>>> StartJob(Guid id)
    {
        var started = await _discovery.StartJobAsync(id);
        if (!started)
            return BadRequest(new ApiResponse<object>(false, null, "شروع کار ممکن نیست"));
        await _audit.LogAsync(GetUserId(), GetUsername(), "SCAN_START", "DiscoveryJob", id,
            "شروع کار کشف", GetIpAddress());
        return Ok(new ApiResponse<object>(true, null, "کار کشف آغاز شد"));
    }

    [HttpPost("jobs/{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<object>>> CancelJob(Guid id)
    {
        var cancelled = await _discovery.CancelJobAsync(id);
        return cancelled
            ? Ok(new ApiResponse<object>(true, null, "کار لغو شد"))
            : NotFound(new ApiResponse<object>(false, null, "کار یافت نشد"));
    }

    [HttpPost("callback")]
    [AllowAnonymous]
    public async Task<ActionResult> EngineCallback([FromBody] DiscoveryCallbackRequest request,
        [FromHeader(Name = "X-Engine-Secret")] string? secret,
        [FromServices] IConfiguration config)
    {
        var expectedSecret = config["DiscoveryEngine:Secret"];
        if (secret != expectedSecret)
            return Unauthorized();

        await _discovery.UpdateJobFromEngineAsync(request.JobId, request.Status, request.AssetsFound, request.Error);
        return Ok();
    }

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetUsername() => User.FindFirstValue(ClaimTypes.Name);
    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

public record DiscoveryCallbackRequest(Guid JobId, string Status, int AssetsFound, string? Error);
