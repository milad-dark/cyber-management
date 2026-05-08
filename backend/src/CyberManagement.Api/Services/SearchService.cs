using CyberManagement.Api.DTOs;

namespace CyberManagement.Api.Services;

public interface ISearchService
{
    /// <summary>
    /// Performs advanced/federated search across internal DB and optionally GLPI.
    /// Results are merged and deduplicated by IP address + hostname.
    /// </summary>
    Task<PagedResult<UnifiedAssetDto>> SearchAsync(AdvancedAssetSearchRequest request);
}

public class SearchService : ISearchService
{
    private readonly IAssetService _assets;
    private readonly IGlpiService _glpi;
    private readonly ILogger<SearchService> _logger;

    public SearchService(IAssetService assets, IGlpiService glpi, ILogger<SearchService> logger)
    {
        _assets = assets;
        _glpi = glpi;
        _logger = logger;
    }

    public async Task<PagedResult<UnifiedAssetDto>> SearchAsync(AdvancedAssetSearchRequest request)
    {
        // 1. Fetch ALL matching internal DB items (unpaged) then merge, then paginate
        //    To keep it efficient we only do unpaged fetch when GLPI is enabled.
        if (!request.IncludeGlpi || string.IsNullOrWhiteSpace(request.Keyword))
        {
            // No GLPI — return internal result directly (already paginated)
            return await _assets.AdvancedSearchAsync(request);
        }

        // 2. Fetch all matching internal items (bounded) for merge with GLPI results.
        //    Cap at 1,000 to bound memory usage; caller should use tighter filters for very large datasets.
        const int internalFetchCap = 1_000;
        var allPagesRequest = new AdvancedAssetSearchRequest
        {
            Keyword = request.Keyword,
            Hostname = request.Hostname,
            IpAddress = request.IpAddress,
            MacAddress = request.MacAddress,
            AssetType = request.AssetType,
            OsName = request.OsName,
            Owner = request.Owner,
            Status = request.Status,
            Criticality = request.Criticality,
            RiskLevel = request.RiskLevel,
            DiscoveredFrom = request.DiscoveredFrom,
            DiscoveredTo = request.DiscoveredTo,
            Cpe = request.Cpe,
            SoftwareName = request.SoftwareName,
            SoftwareVersion = request.SoftwareVersion,
            SoftwareVendor = request.SoftwareVendor,
            IncludeGlpi = false,
            Page = 1,
            PageSize = internalFetchCap,
            SortBy = request.SortBy,
            SortDir = request.SortDir
        };

        var internalResult = await _assets.AdvancedSearchAsync(allPagesRequest);
        var merged = internalResult.Items.ToList();

        // 3. Fetch GLPI results and merge/dedup
        try
        {
            var glpiItems = (await _glpi.SearchAssetsAsync(
                request.Keyword,
                request.AssetType,
                limit: request.PageSize * 2)).ToList();

            // Dedup: skip GLPI items whose IP or hostname already exist in internal results
            var internalIps = merged
                .Where(a => !string.IsNullOrWhiteSpace(a.IpAddress))
                .Select(a => a.IpAddress!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var internalHostnames = merged
                .Where(a => !string.IsNullOrWhiteSpace(a.Hostname))
                .Select(a => a.Hostname!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var item in glpiItems)
            {
                var isDup = (!string.IsNullOrWhiteSpace(item.IpAddress) && internalIps.Contains(item.IpAddress!)) ||
                            (!string.IsNullOrWhiteSpace(item.Hostname) && internalHostnames.Contains(item.Hostname!));

                if (!isDup)
                    merged.Add(item);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GLPI federated search failed — returning internal results only");
        }

        // 4. Apply pagination to the fully merged list
        int total = merged.Count;
        var pageItems = merged
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new PagedResult<UnifiedAssetDto>(pageItems, total, request.Page, request.PageSize);
    }
}
