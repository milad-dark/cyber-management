using CyberManagement.Api.DTOs;
using CyberManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CyberManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VulnerabilitiesController : ControllerBase
{
    private readonly IVulnerabilityService _vulns;

    public VulnerabilitiesController(IVulnerabilityService vulns) => _vulns = vulns;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<VulnerabilityDto>>>> GetVulnerabilities(
        [FromQuery] VulnerabilityFilterRequest filter)
    {
        var result = await _vulns.GetVulnerabilitiesAsync(filter);
        return Ok(new ApiResponse<PagedResult<VulnerabilityDto>>(true, result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<VulnerabilityDto>>> GetVulnerability(Guid id)
    {
        var vuln = await _vulns.GetVulnerabilityByIdAsync(id);
        return vuln == null
            ? NotFound(new ApiResponse<VulnerabilityDto>(false, null, "آسیب‌پذیری یافت نشد"))
            : Ok(new ApiResponse<VulnerabilityDto>(true, vuln));
    }

    [HttpPatch("{vulnId:guid}/assets/{assetId:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> UpdateStatus(
        Guid vulnId, Guid assetId, [FromBody] UpdateVulnStatusRequest request)
    {
        await _vulns.UpdateAssetVulnStatusAsync(assetId, vulnId, request.Status, request.Notes);
        return Ok(new ApiResponse<object>(true, null, "وضعیت آسیب‌پذیری بروزرسانی شد"));
    }
}

public record UpdateVulnStatusRequest(string Status, string? Notes);
