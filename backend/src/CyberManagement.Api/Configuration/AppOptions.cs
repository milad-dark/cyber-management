namespace CyberManagement.Api.Configuration;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = "CyberManagement";
    public string Audience { get; set; } = "CyberManagementUsers";
    public int ExpiryHours { get; set; } = 8;
}

public class DiscoveryEngineOptions
{
    public string BaseUrl { get; set; } = "http://discovery-engine:8001";
    public string Secret { get; set; } = string.Empty;
}

public class GlpiOptions
{
    public string Url { get; set; } = string.Empty;
    public string AppToken { get; set; } = string.Empty;
    public string UserToken { get; set; } = string.Empty;
}

public class SiemOptions
{
    public string SyslogHost { get; set; } = string.Empty;
    public int SyslogPort { get; set; } = 514;
    public string WebhookUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = false;
}
