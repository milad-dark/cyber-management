using CyberManagement.Api.Data;
using CyberManagement.Api.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public class DataSeeder
{
    private readonly AppDbContext _db;
    private readonly ILogger<DataSeeder> _logger;

    public DataSeeder(AppDbContext db, ILogger<DataSeeder> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            await _db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Migration failed (may already be applied), continuing...");
        }

        await SeedRolesAsync();
        await SeedUsersAsync();
        await SeedCategoriesAsync();
        await SeedLocationsAsync();
        await SeedSampleDataAsync();
    }

    private async Task SeedRolesAsync()
    {
        if (await _db.Roles.AnyAsync()) return;

        var roles = new[]
        {
            new Role { Name = "admin", Description = "مدیر سیستم - دسترسی کامل",
                Permissions = new[] { "*" } },
            new Role { Name = "analyst", Description = "تحلیلگر امنیتی",
                Permissions = new[] { "assets:read", "assets:write", "vulns:read", "risk:read", "threat:read", "threat:write", "discovery:read", "discovery:write", "reports:read", "reports:write" } },
            new Role { Name = "viewer", Description = "مشاهده‌گر",
                Permissions = new[] { "assets:read", "vulns:read", "risk:read", "threat:read", "reports:read" } }
        };

        await _db.Roles.AddRangeAsync(roles);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} roles", roles.Length);
    }

    private async Task SeedUsersAsync()
    {
        if (await _db.Users.AnyAsync()) return;

        var adminRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "admin");
        var analystRole = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "analyst");

        var users = new[]
        {
            new User
            {
                Username = "admin",
                Email = "admin@cyber-mgmt.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@1234"),
                FullName = "مدیر سیستم",
                RoleId = adminRole?.Id,
                IsActive = true
            },
            new User
            {
                Username = "analyst",
                Email = "analyst@cyber-mgmt.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Analyst@1234"),
                FullName = "تحلیلگر امنیتی",
                RoleId = analystRole?.Id,
                IsActive = true
            }
        };

        await _db.Users.AddRangeAsync(users);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Seeded {Count} users", users.Length);
    }

    private async Task SeedCategoriesAsync()
    {
        if (await _db.AssetCategories.AnyAsync()) return;

        var categories = new[]
        {
            new AssetCategory { Name = "Server", NameFa = "سرور", Icon = "🖥️" },
            new AssetCategory { Name = "Workstation", NameFa = "ایستگاه کاری", Icon = "💻" },
            new AssetCategory { Name = "Network Equipment", NameFa = "تجهیزات شبکه", Icon = "🌐" },
            new AssetCategory { Name = "Security Device", NameFa = "تجهیزات امنیتی", Icon = "🔒" },
            new AssetCategory { Name = "IoT Device", NameFa = "دستگاه IoT", Icon = "📡" },
            new AssetCategory { Name = "Mobile Device", NameFa = "دستگاه موبایل", Icon = "📱" },
            new AssetCategory { Name = "Virtual Machine", NameFa = "ماشین مجازی", Icon = "☁️" },
            new AssetCategory { Name = "Database Server", NameFa = "سرور پایگاه داده", Icon = "🗄️" }
        };

        await _db.AssetCategories.AddRangeAsync(categories);
        await _db.SaveChangesAsync();
    }

    private async Task SeedLocationsAsync()
    {
        if (await _db.Locations.AnyAsync()) return;

        var locations = new[]
        {
            new Location { Name = "Data Center", NameFa = "مرکز داده", Building = "ساختمان A" },
            new Location { Name = "Server Room", NameFa = "اتاق سرور", Building = "ساختمان A", Floor = "طبقه اول" },
            new Location { Name = "Network Operations Center", NameFa = "مرکز عملیات شبکه", Building = "ساختمان B" },
            new Location { Name = "Office Floor 1", NameFa = "طبقه اول دفتر", Building = "ساختمان اداری", Floor = "طبقه اول" },
            new Location { Name = "Office Floor 2", NameFa = "طبقه دوم دفتر", Building = "ساختمان اداری", Floor = "طبقه دوم" },
            new Location { Name = "Remote Site", NameFa = "سایت دور", Building = "شعبه" }
        };

        await _db.Locations.AddRangeAsync(locations);
        await _db.SaveChangesAsync();
    }

    private async Task SeedSampleDataAsync()
    {
        if (await _db.Assets.AnyAsync()) return;

        var category = await _db.AssetCategories.FirstAsync();
        var location = await _db.Locations.FirstAsync();

        // Sample assets
        var assets = new[]
        {
            new Asset
            {
                Name = "WebServer-01",
                Hostname = "webserver-01.corp.local",
                IpAddress = "192.168.1.10",
                AssetType = "server",
                CategoryId = category.Id,
                LocationId = location.Id,
                Status = "active",
                Criticality = "critical",
                OsName = "Ubuntu Server 22.04 LTS",
                OsFamily = "Linux",
                Manufacturer = "Dell",
                Department = "IT",
                Description = "وب سرور اصلی"
            },
            new Asset
            {
                Name = "DBServer-01",
                Hostname = "dbserver-01.corp.local",
                IpAddress = "192.168.1.20",
                AssetType = "server",
                CategoryId = category.Id,
                LocationId = location.Id,
                Status = "active",
                Criticality = "critical",
                OsName = "Red Hat Enterprise Linux 9",
                OsFamily = "Linux",
                Manufacturer = "HP",
                Department = "IT",
                Description = "سرور پایگاه داده اصلی"
            },
            new Asset
            {
                Name = "Switch-Core-01",
                Hostname = "switch-core-01.corp.local",
                IpAddress = "192.168.1.1",
                AssetType = "network",
                CategoryId = category.Id,
                LocationId = location.Id,
                Status = "active",
                Criticality = "high",
                OsName = "Cisco IOS 15.2",
                Manufacturer = "Cisco",
                Department = "Network"
            },
            new Asset
            {
                Name = "Firewall-01",
                Hostname = "fw-01.corp.local",
                IpAddress = "192.168.1.254",
                AssetType = "security",
                CategoryId = category.Id,
                LocationId = location.Id,
                Status = "active",
                Criticality = "critical",
                OsName = "Palo Alto PAN-OS",
                Manufacturer = "Palo Alto Networks",
                Department = "Security"
            }
        };

        await _db.Assets.AddRangeAsync(assets);
        await _db.SaveChangesAsync();

        // Sample vulnerability
        var vuln = new Vulnerability
        {
            CveId = "CVE-2024-1234",
            Title = "Remote Code Execution in Apache HTTP Server",
            Description = "یک آسیب‌پذیری بحرانی در Apache HTTP Server که امکان اجرای کد از راه دور را فراهم می‌کند",
            CvssV3Score = 9.8m,
            Severity = "critical",
            ExploitAvailable = true,
            PatchAvailable = true,
            PublishedAt = DateTime.UtcNow.AddDays(-30)
        };

        var vuln2 = new Vulnerability
        {
            CveId = "CVE-2024-5678",
            Title = "SQL Injection Vulnerability",
            Description = "آسیب‌پذیری SQL Injection در نرم‌افزار مدیریت پایگاه داده",
            CvssV3Score = 7.2m,
            Severity = "high",
            ExploitAvailable = false,
            PatchAvailable = true,
            PublishedAt = DateTime.UtcNow.AddDays(-15)
        };

        await _db.Vulnerabilities.AddRangeAsync(vuln, vuln2);
        await _db.SaveChangesAsync();

        // Link vulnerability to asset
        var firstAsset = assets[0];
        _db.AssetVulnerabilities.AddRange(
            new AssetVulnerability { AssetId = firstAsset.Id, VulnerabilityId = vuln.Id, Status = "open" },
            new AssetVulnerability { AssetId = firstAsset.Id, VulnerabilityId = vuln2.Id, Status = "in_progress" }
        );

        // Sample threat intel
        _db.ThreatIntel.AddRange(
            new ThreatIntel
            {
                IocType = "ip",
                IocValue = "185.220.101.45",
                ThreatType = "malware_c2",
                Source = "AlienVault OTX",
                Severity = "high",
                Confidence = 85,
                Description = "آدرس IP C&C باج‌افزار LockBit"
            },
            new ThreatIntel
            {
                IocType = "domain",
                IocValue = "evil-malware.ru",
                ThreatType = "phishing",
                Source = "VirusTotal",
                Severity = "medium",
                Confidence = 70,
                Description = "دامنه فیشینگ"
            }
        );

        await _db.SaveChangesAsync();
        _logger.LogInformation("Sample data seeded successfully");
    }
}
