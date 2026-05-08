using CyberManagement.Api.DTOs;
using CyberManagement.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CyberManagement.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssetsController : ControllerBase
{
    private readonly IAssetService _assets;
    private readonly IAuditService _audit;
    private readonly IGraphService _graph;
    private readonly ISearchService _search;

    public AssetsController(IAssetService assets, IAuditService audit, IGraphService graph, ISearchService search)
    {
        _assets = assets;
        _audit = audit;
        _graph = graph;
        _search = search;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetDto>>>> GetAssets([FromQuery] AssetFilterRequest filter)
    {
        var result = await _assets.GetAssetsAsync(filter);
        return Ok(new ApiResponse<PagedResult<AssetDto>>(true, result));
    }

    /// <summary>
    /// Advanced / federated search — supports keyword, specific field filters, GLPI integration, and pagination.
    /// Every call is audit-logged (who searched, what terms, when).
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<ApiResponse<PagedResult<UnifiedAssetDto>>>> SearchAssets(
        [FromQuery] AdvancedAssetSearchRequest request)
    {
        var userId = GetUserId();
        var searchDesc = BuildSearchDescription(request);
        await _audit.LogAsync(userId, GetUsername(), "SEARCH", "Asset", null,
            searchDesc, GetIpAddress());

        var result = await _search.SearchAsync(request);
        return Ok(new ApiResponse<PagedResult<UnifiedAssetDto>>(true, result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDetailDto>>> GetAsset(Guid id)
    {
        var asset = await _assets.GetAssetByIdAsync(id);
        if (asset == null)
            return NotFound(new ApiResponse<AssetDetailDto>(false, null, "دارایی یافت نشد"));
        return Ok(new ApiResponse<AssetDetailDto>(true, asset));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AssetDto>>> CreateAsset([FromBody] CreateAssetRequest request)
    {
        var userId = GetUserId();
        var asset = await _assets.CreateAssetAsync(request, userId);
        await _audit.LogAsync(userId, GetUsername(), "CREATE", "Asset", asset.Id,
            $"دارایی جدید: {asset.Name}", GetIpAddress(), responseCode: 201);
        await _graph.SyncAssetToGraphAsync(asset.Id, asset.Name, asset.IpAddress, asset.AssetType);
        return CreatedAtAction(nameof(GetAsset), new { id = asset.Id },
            new ApiResponse<AssetDto>(true, asset, "دارایی با موفقیت ایجاد شد"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AssetDto>>> UpdateAsset(Guid id, [FromBody] UpdateAssetRequest request)
    {
        var asset = await _assets.UpdateAssetAsync(id, request);
        if (asset == null)
            return NotFound(new ApiResponse<AssetDto>(false, null, "دارایی یافت نشد"));
        await _audit.LogAsync(GetUserId(), GetUsername(), "UPDATE", "Asset", id,
            $"بروزرسانی دارایی: {asset.Name}", GetIpAddress());
        return Ok(new ApiResponse<AssetDto>(true, asset, "دارایی با موفقیت بروزرسانی شد"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteAsset(Guid id)
    {
        var deleted = await _assets.DeleteAssetAsync(id);
        if (!deleted)
            return NotFound(new ApiResponse<object>(false, null, "دارایی یافت نشد"));
        await _audit.LogAsync(GetUserId(), GetUsername(), "DELETE", "Asset", id,
            "حذف دارایی", GetIpAddress());
        return Ok(new ApiResponse<object>(true, null, "دارایی با موفقیت حذف شد"));
    }

    [HttpGet("{id:guid}/graph")]
    public async Task<ActionResult<ApiResponse<object>>> GetAssetGraph(Guid id, [FromQuery] int depth = 2)
    {
        var graph = await _graph.GetAssetNetworkAsync(id, depth);
        return Ok(new ApiResponse<object>(true, graph));
    }

    private static string BuildSearchDescription(AdvancedAssetSearchRequest r)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(r.Keyword))     parts.Add($"کلیدواژه:{Sanitize(r.Keyword)}");
        if (!string.IsNullOrWhiteSpace(r.Hostname))    parts.Add($"hostname:{Sanitize(r.Hostname)}");
        if (!string.IsNullOrWhiteSpace(r.IpAddress))   parts.Add($"ip:{Sanitize(r.IpAddress)}");
        if (!string.IsNullOrWhiteSpace(r.MacAddress))  parts.Add($"mac:{Sanitize(r.MacAddress)}");
        if (!string.IsNullOrWhiteSpace(r.AssetType))   parts.Add($"نوع:{Sanitize(r.AssetType)}");
        if (!string.IsNullOrWhiteSpace(r.OsName))      parts.Add($"os:{Sanitize(r.OsName)}");
        if (!string.IsNullOrWhiteSpace(r.Owner))       parts.Add($"مالک:{Sanitize(r.Owner)}");
        if (!string.IsNullOrWhiteSpace(r.Status))      parts.Add($"وضعیت:{Sanitize(r.Status)}");
        if (!string.IsNullOrWhiteSpace(r.RiskLevel))   parts.Add($"ریسک:{Sanitize(r.RiskLevel)}");
        if (!string.IsNullOrWhiteSpace(r.Cpe))         parts.Add($"cpe:{Sanitize(r.Cpe)}");
        if (!string.IsNullOrWhiteSpace(r.SoftwareName))parts.Add($"نرم‌افزار:{Sanitize(r.SoftwareName)}");
        if (r.DiscoveredFrom.HasValue)                 parts.Add($"از:{r.DiscoveredFrom:yyyy-MM-dd}");
        if (r.DiscoveredTo.HasValue)                   parts.Add($"تا:{r.DiscoveredTo:yyyy-MM-dd}");
        if (r.IncludeGlpi)                             parts.Add("شامل-GLPI");
        return parts.Count > 0 ? $"جستجوی پیشرفته دارایی‌ها [{string.Join(", ", parts)}]" : "جستجوی پیشرفته دارایی‌ها";
    }

    /// <summary>Removes newlines and control characters to prevent audit log injection.</summary>
    private static string Sanitize(string? value) =>
        value?.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ") ?? string.Empty;

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetUsername() => User.FindFirstValue(ClaimTypes.Name);
    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
