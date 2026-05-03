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

    public AssetsController(IAssetService assets, IAuditService audit, IGraphService graph)
    {
        _assets = assets;
        _audit = audit;
        _graph = graph;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AssetDto>>>> GetAssets([FromQuery] AssetFilterRequest filter)
    {
        var result = await _assets.GetAssetsAsync(filter);
        return Ok(new ApiResponse<PagedResult<AssetDto>>(true, result));
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

    private Guid? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim) : null;
    }

    private string? GetUsername() => User.FindFirstValue(ClaimTypes.Name);
    private string GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
