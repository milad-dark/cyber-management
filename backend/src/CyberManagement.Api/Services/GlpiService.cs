using CyberManagement.Api.Configuration;
using CyberManagement.Api.DTOs;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace CyberManagement.Api.Services;

public interface IGlpiService
{
    Task<int?> SyncAssetToGlpiAsync(Guid assetId, string name, string? ipAddress, string assetType);
    Task<bool> IsAvailableAsync();
    Task<IEnumerable<UnifiedAssetDto>> SearchAssetsAsync(string keyword, string? assetType, int limit);
}

public class GlpiService : IGlpiService
{
    private readonly HttpClient _http;
    private readonly GlpiOptions _opts;
    private readonly ILogger<GlpiService> _logger;

    public GlpiService(HttpClient http, IOptions<GlpiOptions> opts, ILogger<GlpiService> logger)
    {
        _http = http;
        _opts = opts.Value;
        _logger = logger;
    }

    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrWhiteSpace(_opts.Url)) return false;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, $"{_opts.Url}/initSession");
            request.Headers.Add("App-Token", _opts.AppToken);
            request.Headers.Add("Authorization", $"user_token {_opts.UserToken}");
            var response = await _http.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<int?> SyncAssetToGlpiAsync(Guid assetId, string name, string? ipAddress, string assetType)
    {
        if (string.IsNullOrWhiteSpace(_opts.Url)) return null;
        try
        {
            var session = await InitSessionAsync();
            if (string.IsNullOrEmpty(session)) return null;

            var computer = new { input = new { name, comment = $"Synced from CyberManagement - {assetId}" } };
            var content = new StringContent(JsonSerializer.Serialize(computer), Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, $"{_opts.Url}/Computer");
            request.Headers.Add("App-Token", _opts.AppToken);
            request.Headers.Add("Session-Token", session);
            request.Content = content;

            var response = await _http.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                var json = JsonSerializer.Deserialize<JsonElement>(body);
                return json.TryGetProperty("id", out var idProp) ? idProp.GetInt32() : null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GLPI sync failed for asset {AssetId}", assetId);
        }
        return null;
    }

    private async Task<string?> InitSessionAsync()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{_opts.Url}/initSession");
        request.Headers.Add("App-Token", _opts.AppToken);
        request.Headers.Add("Authorization", $"user_token {_opts.UserToken}");
        var response = await _http.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        return json.TryGetProperty("session_token", out var t) ? t.GetString() : null;
    }

    public async Task<IEnumerable<UnifiedAssetDto>> SearchAssetsAsync(string keyword, string? assetType, int limit)
    {
        var results = new List<UnifiedAssetDto>();
        if (string.IsNullOrWhiteSpace(_opts.Url)) return results;
        try
        {
            var session = await InitSessionAsync();
            if (string.IsNullOrEmpty(session)) return results;

            // Use GLPI search API: GET /Computer?searchText[name]=keyword&range=0-limit
            var kw = Uri.EscapeDataString(keyword);
            var url = $"{_opts.Url}/Computer?searchText[name]={kw}&range=0-{limit - 1}";
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("App-Token", _opts.AppToken);
            request.Headers.Add("Session-Token", session);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode) return results;

            var body = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(body);

            if (json.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in json.EnumerateArray())
                {
                    var dto = new UnifiedAssetDto
                    {
                        Id = Guid.NewGuid(),
                        Name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "",
                        Hostname = item.TryGetProperty("name", out var hn) ? hn.GetString() : null,
                        IpAddress = item.TryGetProperty("ip", out var ip) ? ip.GetString() : null,
                        AssetType = assetType ?? "computer",
                        Status = "active",
                        Criticality = "medium",
                        Source = "glpi",
                        Tags = new List<string>()
                    };
                    // Map GLPI id into GlpiId field via AssetDetailDto pattern
                    if (item.TryGetProperty("id", out var glpiIdProp) && glpiIdProp.TryGetInt32(out var glpiId))
                    {
                        // Store GLPI id in description for now since UnifiedAssetDto doesn't have GlpiId
                        dto.Description = $"GLPI ID: {glpiId}";
                    }
                    results.Add(dto);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GLPI search failed for keyword {Keyword}", SanitizeForLog(keyword));
        }
        return results;
    }

    private static string SanitizeForLog(string input)
    {
        // Remove newlines and control characters to prevent log injection
        return input.Replace("\n", " ").Replace("\r", " ").Replace("\t", " ");
    }
}
