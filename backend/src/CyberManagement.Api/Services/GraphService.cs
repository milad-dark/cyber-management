using Neo4j.Driver;

namespace CyberManagement.Api.Services;

public interface IGraphService
{
    Task SyncAssetToGraphAsync(Guid assetId, string name, string? ipAddress, string assetType);
    Task AddVulnerabilityRelationshipAsync(Guid assetId, Guid vulnId, string cveId, string severity);
    Task AddCommunicationRelationshipAsync(Guid fromAssetId, Guid toAssetId, int port, string protocol);
    Task<List<Dictionary<string, object?>>> GetAssetNetworkAsync(Guid assetId, int depth = 2);
}

public class GraphService : IGraphService
{
    private readonly IDriver _driver;
    private readonly ILogger<GraphService> _logger;

    public GraphService(IDriver driver, ILogger<GraphService> logger)
    {
        _driver = driver;
        _logger = logger;
    }

    public async Task SyncAssetToGraphAsync(Guid assetId, string name, string? ipAddress, string assetType)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(
            "MERGE (a:Asset {id: $id}) SET a.name = $name, a.ipAddress = $ip, a.type = $type, a.updatedAt = datetime()",
            new { id = assetId.ToString(), name, ip = ipAddress ?? "", type = assetType });
    }

    public async Task AddVulnerabilityRelationshipAsync(Guid assetId, Guid vulnId, string cveId, string severity)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(
            @"MERGE (a:Asset {id: $assetId})
              MERGE (v:Vulnerability {id: $vulnId}) SET v.cveId = $cveId, v.severity = $severity
              MERGE (a)-[:HAS_VULNERABILITY {detectedAt: datetime()}]->(v)",
            new { assetId = assetId.ToString(), vulnId = vulnId.ToString(), cveId, severity });
    }

    public async Task AddCommunicationRelationshipAsync(Guid fromAssetId, Guid toAssetId, int port, string protocol)
    {
        await using var session = _driver.AsyncSession();
        await session.RunAsync(
            @"MATCH (a:Asset {id: $fromId}), (b:Asset {id: $toId})
              MERGE (a)-[r:COMMUNICATES_WITH {port: $port, protocol: $protocol}]->(b)
              SET r.lastSeen = datetime()",
            new { fromId = fromAssetId.ToString(), toId = toAssetId.ToString(), port, protocol });
    }

    public async Task<List<Dictionary<string, object?>>> GetAssetNetworkAsync(Guid assetId, int depth = 2)
    {
        try
        {
            await using var session = _driver.AsyncSession();
            var result = await session.RunAsync(
                @"MATCH path = (a:Asset {id: $id})-[*0.." + depth + @"]->(b)
                  RETURN path LIMIT 100",
                new { id = assetId.ToString() });

            var records = await result.ToListAsync();
            return records.Select(r => new Dictionary<string, object?> { ["path"] = r["path"].ToString() }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Neo4j graph query failed for asset {AssetId}", assetId);
            return new List<Dictionary<string, object?>>();
        }
    }
}
