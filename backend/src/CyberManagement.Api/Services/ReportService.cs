using CyberManagement.Api.Data;
using CyberManagement.Api.DTOs;
using CyberManagement.Api.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace CyberManagement.Api.Services;

public interface IReportService
{
    Task<PagedResult<ReportDto>> GetReportsAsync(int page, int pageSize);
    Task<ReportDto> CreateReportAsync(CreateReportRequest request, Guid? userId);
    Task<(byte[] Data, string ContentType, string FileName)?> DownloadReportAsync(Guid id);
}

public class ReportService : IReportService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ReportService> _logger;
    private readonly string _reportsPath;

    public ReportService(AppDbContext db, ILogger<ReportService> logger, IWebHostEnvironment env)
    {
        _db = db;
        _logger = logger;
        _reportsPath = Path.Combine(env.ContentRootPath, "reports");
        Directory.CreateDirectory(_reportsPath);
    }

    public async Task<PagedResult<ReportDto>> GetReportsAsync(int page, int pageSize)
    {
        var total = await _db.Reports.CountAsync();
        var items = await _db.Reports
            .Include(r => r.CreatedBy)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ReportDto
            {
                Id = r.Id,
                Title = r.Title,
                ReportType = r.ReportType,
                Status = r.Status,
                Format = r.Format,
                FileSize = r.FileSize,
                CreatedByName = r.CreatedBy != null ? r.CreatedBy.FullName ?? r.CreatedBy.Username : null,
                CreatedAt = r.CreatedAt,
                CompletedAt = r.CompletedAt
            })
            .ToListAsync();
        return new PagedResult<ReportDto>(items, total, page, pageSize);
    }

    public async Task<ReportDto> CreateReportAsync(CreateReportRequest request, Guid? userId)
    {
        var report = new Report
        {
            Title = request.Title,
            ReportType = request.ReportType,
            Format = request.Format,
            Filters = request.Filters,
            Status = "pending",
            CreatedById = userId
        };
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        _ = Task.Run(() => GenerateReportAsync(report.Id));

        return new ReportDto
        {
            Id = report.Id,
            Title = report.Title,
            ReportType = report.ReportType,
            Status = report.Status,
            Format = report.Format,
            CreatedAt = report.CreatedAt
        };
    }

    public async Task<(byte[] Data, string ContentType, string FileName)?> DownloadReportAsync(Guid id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null || report.Status != "completed" || string.IsNullOrEmpty(report.FilePath))
            return null;

        if (!File.Exists(report.FilePath)) return null;

        var data = await File.ReadAllBytesAsync(report.FilePath);
        var contentType = report.Format == "xlsx"
            ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
            : "text/csv";
        var fileName = Path.GetFileName(report.FilePath);
        return (data, contentType, fileName);
    }

    private async Task GenerateReportAsync(Guid reportId)
    {
        var report = await _db.Reports.FindAsync(reportId);
        if (report == null) return;

        try
        {
            report.Status = "generating";
            await _db.SaveChangesAsync();

            var fileName = $"report_{reportId:N}.xlsx";
            var filePath = Path.Combine(_reportsPath, fileName);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("گزارش");

            switch (report.ReportType)
            {
                case "inventory":
                    await GenerateInventorySheet(ws);
                    break;
                case "vulnerability":
                    await GenerateVulnerabilitySheet(ws);
                    break;
                case "risk":
                    await GenerateRiskSheet(ws);
                    break;
                default:
                    await GenerateSummarySheet(ws);
                    break;
            }

            wb.SaveAs(filePath);
            report.FilePath = filePath;
            report.FileSize = new FileInfo(filePath).Length;
            report.Status = "completed";
            report.CompletedAt = DateTime.UtcNow;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Report generation failed for {Id}", reportId);
            report!.Status = "failed";
        }
        finally
        {
            await _db.SaveChangesAsync();
        }
    }

    private async Task GenerateInventorySheet(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "شناسه";
        ws.Cell(1, 2).Value = "نام";
        ws.Cell(1, 3).Value = "نوع";
        ws.Cell(1, 4).Value = "آدرس IP";
        ws.Cell(1, 5).Value = "وضعیت";
        ws.Cell(1, 6).Value = "اهمیت";
        ws.Cell(1, 7).Value = "سیستم‌عامل";
        ws.Cell(1, 8).Value = "آخرین مشاهده";

        var assets = await _db.Assets.OrderBy(a => a.Name).ToListAsync();
        for (int i = 0; i < assets.Count; i++)
        {
            var a = assets[i];
            ws.Cell(i + 2, 1).Value = a.Id.ToString();
            ws.Cell(i + 2, 2).Value = a.Name;
            ws.Cell(i + 2, 3).Value = a.AssetType;
            ws.Cell(i + 2, 4).Value = a.IpAddress ?? "";
            ws.Cell(i + 2, 5).Value = a.Status;
            ws.Cell(i + 2, 6).Value = a.Criticality;
            ws.Cell(i + 2, 7).Value = a.OsName ?? "";
            ws.Cell(i + 2, 8).Value = a.LastSeen.ToString("yyyy-MM-dd HH:mm");
        }
    }

    private async Task GenerateVulnerabilitySheet(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "CVE";
        ws.Cell(1, 2).Value = "عنوان";
        ws.Cell(1, 3).Value = "شدت";
        ws.Cell(1, 4).Value = "امتیاز CVSS";
        ws.Cell(1, 5).Value = "تعداد دارایی‌های آسیب‌دیده";
        ws.Cell(1, 6).Value = "اکسپلویت موجود";

        var vulns = await _db.Vulnerabilities.Include(v => v.AssetVulnerabilities).OrderByDescending(v => v.CvssV3Score).ToListAsync();
        for (int i = 0; i < vulns.Count; i++)
        {
            var v = vulns[i];
            ws.Cell(i + 2, 1).Value = v.CveId ?? "";
            ws.Cell(i + 2, 2).Value = v.Title;
            ws.Cell(i + 2, 3).Value = v.Severity;
            ws.Cell(i + 2, 4).Value = (double)(v.CvssV3Score ?? 0);
            ws.Cell(i + 2, 5).Value = v.AssetVulnerabilities.Count;
            ws.Cell(i + 2, 6).Value = v.ExploitAvailable ? "بله" : "خیر";
        }
    }

    private async Task GenerateRiskSheet(IXLWorksheet ws)
    {
        ws.Cell(1, 1).Value = "نام دارایی";
        ws.Cell(1, 2).Value = "آدرس IP";
        ws.Cell(1, 3).Value = "امتیاز کلی ریسک";
        ws.Cell(1, 4).Value = "امتیاز آسیب‌پذیری";
        ws.Cell(1, 5).Value = "امتیاز نمایش";
        ws.Cell(1, 6).Value = "سطح ریسک";

        var risks = await _db.RiskScores.Include(r => r.Asset).OrderByDescending(r => r.OverallScore).ToListAsync();
        for (int i = 0; i < risks.Count; i++)
        {
            var r = risks[i];
            ws.Cell(i + 2, 1).Value = r.Asset.Name;
            ws.Cell(i + 2, 2).Value = r.Asset.IpAddress ?? "";
            ws.Cell(i + 2, 3).Value = (double)r.OverallScore;
            ws.Cell(i + 2, 4).Value = (double)r.VulnerabilityScore;
            ws.Cell(i + 2, 5).Value = (double)r.ExposureScore;
            ws.Cell(i + 2, 6).Value = r.OverallScore >= 75 ? "بحرانی" : r.OverallScore >= 50 ? "بالا" : r.OverallScore >= 25 ? "متوسط" : "پایین";
        }
    }

    private async Task GenerateSummarySheet(IXLWorksheet ws)
    {
        var total = await _db.Assets.CountAsync();
        var vulns = await _db.Vulnerabilities.CountAsync();
        var critical = await _db.Vulnerabilities.CountAsync(v => v.Severity == "critical");

        ws.Cell(1, 1).Value = "معیار";
        ws.Cell(1, 2).Value = "مقدار";
        ws.Cell(2, 1).Value = "تعداد کل دارایی‌ها";
        ws.Cell(2, 2).Value = total;
        ws.Cell(3, 1).Value = "تعداد آسیب‌پذیری‌ها";
        ws.Cell(3, 2).Value = vulns;
        ws.Cell(4, 1).Value = "آسیب‌پذیری‌های بحرانی";
        ws.Cell(4, 2).Value = critical;
        ws.Cell(5, 1).Value = "تاریخ گزارش";
        ws.Cell(5, 2).Value = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm");
    }
}
