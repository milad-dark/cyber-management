using CyberManagement.Api.DTOs;
using CyberManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CyberManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboard;

    public DashboardController(IDashboardService dashboard) => _dashboard = dashboard;

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        var stats = await _dashboard.GetStatsAsync();
        return Ok(new ApiResponse<DashboardStatsDto>(true, stats));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RiskController : ControllerBase
{
    private readonly IRiskService _risk;

    public RiskController(IRiskService risk) => _risk = risk;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<RiskScoreDto>>>> GetRiskScores(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _risk.GetRiskScoresAsync(page, pageSize);
        return Ok(new ApiResponse<PagedResult<RiskScoreDto>>(true, result));
    }

    [HttpPost("recalculate")]
    [Authorize(Roles = "admin,analyst")]
    public async Task<ActionResult<ApiResponse<object>>> RecalculateAll()
    {
        _ = Task.Run(() => _risk.RecalculateAllRisksAsync());
        return Ok(new ApiResponse<object>(true, null, "محاسبه ریسک در حال انجام است"));
    }

    [HttpPost("recalculate/{assetId:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> RecalculateAsset(Guid assetId)
    {
        await _risk.RecalculateRiskForAsset(assetId);
        return Ok(new ApiResponse<object>(true, null, "ریسک دارایی محاسبه شد"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ThreatIntelController : ControllerBase
{
    private readonly IThreatIntelService _threats;

    public ThreatIntelController(IThreatIntelService threats) => _threats = threats;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ThreatIntelDto>>>> GetThreats(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? iocType = null)
    {
        var result = await _threats.GetThreatsAsync(page, pageSize, search, iocType);
        return Ok(new ApiResponse<PagedResult<ThreatIntelDto>>(true, result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ThreatIntelDto>>> GetThreat(Guid id)
    {
        var t = await _threats.GetByIdAsync(id);
        return t == null
            ? NotFound(new ApiResponse<ThreatIntelDto>(false, null, "تهدید یافت نشد"))
            : Ok(new ApiResponse<ThreatIntelDto>(true, t));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ThreatIntelDto>>> CreateThreat([FromBody] CreateThreatIntelRequest request)
    {
        var t = await _threats.CreateAsync(request);
        return CreatedAtAction(nameof(GetThreat), new { id = t.Id }, new ApiResponse<ThreatIntelDto>(true, t));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteThreat(Guid id)
    {
        var deleted = await _threats.DeleteAsync(id);
        return deleted
            ? Ok(new ApiResponse<object>(true, null, "تهدید حذف شد"))
            : NotFound(new ApiResponse<object>(false, null, "تهدید یافت نشد"));
    }

    [HttpPost("match")]
    [Authorize(Roles = "admin,analyst")]
    public async Task<ActionResult<ApiResponse<object>>> MatchToAssets()
    {
        await _threats.MatchThreatsToAssetsAsync();
        return Ok(new ApiResponse<object>(true, null, "تطابق‌یابی انجام شد"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SiemController : ControllerBase
{
    private readonly ISiemService _siem;

    public SiemController(ISiemService siem) => _siem = siem;

    [HttpGet("events")]
    public async Task<ActionResult<ApiResponse<PagedResult<SiemEventDto>>>> GetEvents(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? severity = null,
        [FromQuery] string? eventType = null)
    {
        var result = await _siem.GetEventsAsync(page, pageSize, severity, eventType);
        return Ok(new ApiResponse<PagedResult<SiemEventDto>>(true, result));
    }

    [HttpPost("forward")]
    [Authorize(Roles = "admin")]
    public async Task<ActionResult<ApiResponse<object>>> ForwardPending()
    {
        await _siem.ForwardPendingEventsAsync();
        return Ok(new ApiResponse<object>(true, null, "رویدادهای معلق ارسال شدند"));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit) => _audit = audit;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AuditLogDto>>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? action = null,
        [FromQuery] string? username = null)
    {
        var result = await _audit.GetLogsAsync(page, pageSize, action, username);
        return Ok(new ApiResponse<PagedResult<AuditLogDto>>(true, result));
    }
}

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;

    public ReportsController(IReportService reports) => _reports = reports;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ReportDto>>>> GetReports(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _reports.GetReportsAsync(page, pageSize);
        return Ok(new ApiResponse<PagedResult<ReportDto>>(true, result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ReportDto>>> CreateReport([FromBody] CreateReportRequest request,
        [FromServices] IHttpContextAccessor httpCtx)
    {
        var userIdClaim = httpCtx.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var userId = userIdClaim != null ? Guid.Parse(userIdClaim) : (Guid?)null;
        var report = await _reports.CreateReportAsync(request, userId);
        return Ok(new ApiResponse<ReportDto>(true, report, "گزارش در حال تولید است"));
    }

    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> DownloadReport(Guid id)
    {
        var result = await _reports.DownloadReportAsync(id);
        if (result == null)
            return NotFound(new ApiResponse<object>(false, null, "گزارش آماده نیست"));
        return File(result.Value.Data, result.Value.ContentType, result.Value.FileName);
    }
}
