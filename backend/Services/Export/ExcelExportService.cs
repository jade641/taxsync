using backend.Data;
using backend.Models;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;

namespace backend.Services.Export;

/// <summary>
/// Production-ready Excel export service using ClosedXML
/// </summary>
public sealed class ExcelExportService : IExcelExportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ExcelExportService> _logger;

    public ExcelExportService(AppDbContext context, ILogger<ExcelExportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<byte[]> ExportPropertiesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var properties = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Region)
                .OrderBy(p => p.PropertyId)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Properties");

            // Set title
            worksheet.Cell(1, 1).Value = "Property Records Export";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 8).Merge();

            // Set export timestamp
            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 8).Merge();

            // Headers
            var headerRow = 4;
            var headers = new[]
            {
                "Property ID",
                "Owner Name",
                "Address",
                "Region",
                "Property Type",
                "Area (sqm)",
                "Assessed Value",
                "Registration Date"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = headerRow + 1;
            foreach (var property in properties)
            {
                worksheet.Cell(row, 1).Value = property.PropertyId;
                worksheet.Cell(row, 2).Value = BuildUserName(property.Owner);
                worksheet.Cell(row, 3).Value = BuildPropertyAddress(property);
                worksheet.Cell(row, 4).Value = property.Region?.RegionName ?? "N/A";
                worksheet.Cell(row, 5).Value = property.PropertyType.ToString();
                worksheet.Cell(row, 6).Value = property.LotArea ?? 0;
                worksheet.Cell(row, 7).Value = property.AssessedValue ?? 0;
                worksheet.Cell(row, 8).Value = property.CreatedAt;

                // Format currency
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "₱#,##0.00";
                
                // Format date
                worksheet.Cell(row, 8).Style.DateFormat.Format = "yyyy-MM-dd";

                // Add borders
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            // Auto-fit columns
            worksheet.Columns().AdjustToContents();

            // Add summary
            var summaryRow = row + 2;
            worksheet.Cell(summaryRow, 1).Value = "Total Properties:";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 2).Value = properties.Count;

            worksheet.Cell(summaryRow + 1, 1).Value = "Total Assessed Value:";
            worksheet.Cell(summaryRow + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow + 1, 2).Value = properties.Sum(p => p.AssessedValue ?? 0);
            worksheet.Cell(summaryRow + 1, 2).Style.NumberFormat.Format = "₱#,##0.00";

            // Convert to byte array
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting properties to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportPaymentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _context.Payments
                .Include(p => p.Assessment)
                    .ThenInclude(a => a.Property)
                .Include(p => p.Payer)
                .Include(p => p.Processor)
                .OrderByDescending(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Payments");

            // Title
            worksheet.Cell(1, 1).Value = "Payment History Export";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 7).Merge();

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 7).Merge();

            // Headers
            var headerRow = 4;
            var headers = new[]
            {
                "Payment ID",
                "Property Address",
                "Amount",
                "Payment Date",
                "Payment Method",
                "Status",
                "Processed By"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#16a34a");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = headerRow + 1;
            foreach (var payment in payments)
            {
                worksheet.Cell(row, 1).Value = payment.PaymentId;
                worksheet.Cell(row, 2).Value = BuildPropertyAddress(payment.Assessment?.Property);
                worksheet.Cell(row, 3).Value = payment.AmountPaid;
                worksheet.Cell(row, 4).Value = payment.PaymentDate;
                worksheet.Cell(row, 5).Value = payment.PaymentMethod.ToString();
                worksheet.Cell(row, 6).Value = payment.Status.ToString();
                worksheet.Cell(row, 7).Value = BuildUserName(payment.Processor);

                // Format currency
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "₱#,##0.00";
                
                // Format date
                worksheet.Cell(row, 4).Style.DateFormat.Format = "yyyy-MM-dd HH:mm";

                // Color code status
                var statusCell = worksheet.Cell(row, 6);
                if (payment.Status == PaymentStatus.Completed)
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#d1fae5");
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#065f46");
                }
                else if (payment.Status == PaymentStatus.Pending)
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#fef3c7");
                    statusCell.Style.Font.FontColor = XLColor.FromHtml("#92400e");
                }

                // Add borders
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            // Summary
            var summaryRow = row + 2;
            worksheet.Cell(summaryRow, 1).Value = "Total Payments:";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 2).Value = payments.Count;

            worksheet.Cell(summaryRow + 1, 1).Value = "Total Amount:";
            worksheet.Cell(summaryRow + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow + 1, 2).Value = payments.Sum(p => p.AmountPaid);
            worksheet.Cell(summaryRow + 1, 2).Style.NumberFormat.Format = "₱#,##0.00";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payments to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportTaxAssessmentsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var assessments = await _context.TaxAssessments
                .Include(t => t.Property)
                .Include(t => t.Payments)
                .OrderByDescending(t => t.TaxYear)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Tax Assessments");

            // Title
            worksheet.Cell(1, 1).Value = "Tax Assessments Export";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 8).Merge();

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 8).Merge();

            // Headers
            var headerRow = 4;
            var headers = new[]
            {
                "Assessment ID",
                "Property Address",
                "Assessment Year",
                "Tax Amount",
                "Due Date",
                "Status",
                "Amount Paid",
                "Balance"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#7c3aed");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = headerRow + 1;
            foreach (var assessment in assessments)
            {
                worksheet.Cell(row, 1).Value = assessment.AssessmentId;
                var amountPaid = GetPaidAmount(assessment);

                worksheet.Cell(row, 2).Value = BuildPropertyAddress(assessment.Property);
                worksheet.Cell(row, 3).Value = assessment.TaxYear;
                worksheet.Cell(row, 4).Value = assessment.TotalAmount;
                worksheet.Cell(row, 5).Value = assessment.DueDate;
                worksheet.Cell(row, 6).Value = assessment.Status.ToString();
                worksheet.Cell(row, 7).Value = amountPaid;
                worksheet.Cell(row, 8).Value = assessment.TotalAmount - amountPaid;

                // Format currency
                worksheet.Cell(row, 4).Style.NumberFormat.Format = "₱#,##0.00";
                worksheet.Cell(row, 7).Style.NumberFormat.Format = "₱#,##0.00";
                worksheet.Cell(row, 8).Style.NumberFormat.Format = "₱#,##0.00";
                
                // Format date
                worksheet.Cell(row, 5).Style.DateFormat.Format = "yyyy-MM-dd";

                // Color code status
                var statusCell = worksheet.Cell(row, 6);
                if (assessment.Status == AssessmentStatus.Paid)
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#d1fae5");
                }
                else if (assessment.Status == AssessmentStatus.Overdue)
                {
                    statusCell.Style.Fill.BackgroundColor = XLColor.FromHtml("#fee2e2");
                }

                // Add borders
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            // Summary
            var summaryRow = row + 2;
            worksheet.Cell(summaryRow, 1).Value = "Total Assessments:";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 2).Value = assessments.Count;

            worksheet.Cell(summaryRow + 1, 1).Value = "Total Tax Amount:";
            worksheet.Cell(summaryRow + 1, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow + 1, 2).Value = assessments.Sum(a => a.TotalAmount);
            worksheet.Cell(summaryRow + 1, 2).Style.NumberFormat.Format = "₱#,##0.00";

            worksheet.Cell(summaryRow + 2, 1).Value = "Total Collected:";
            worksheet.Cell(summaryRow + 2, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow + 2, 2).Value = assessments.Sum(GetPaidAmount);
            worksheet.Cell(summaryRow + 2, 2).Style.NumberFormat.Format = "₱#,##0.00";

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting tax assessments to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportAuditLogsAsync(
        DateTime? startDate = null,
        DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.AuditLogs
                .Include(a => a.User)
                .AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Audit Logs");

            // Title
            worksheet.Cell(1, 1).Value = "Audit Logs Export";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 6).Merge();

            var dateRange = startDate.HasValue || endDate.HasValue
                ? $"Period: {startDate?.ToString("yyyy-MM-dd") ?? "Start"} to {endDate?.ToString("yyyy-MM-dd") ?? "End"}"
                : "All Records";

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss} | {dateRange}";
            worksheet.Cell(2, 1).Style.Font.Italic = true;
            worksheet.Range(2, 1, 2, 6).Merge();

            // Headers
            var headerRow = 4;
            var headers = new[]
            {
                "Log ID",
                "User",
                "Action",
                "Module",
                "Description",
                "Timestamp"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#dc2626");
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            int row = headerRow + 1;
            foreach (var log in logs)
            {
                worksheet.Cell(row, 1).Value = log.LogId;
                worksheet.Cell(row, 2).Value = BuildUserName(log.User);
                worksheet.Cell(row, 3).Value = log.Action;
                worksheet.Cell(row, 4).Value = log.Module;
                worksheet.Cell(row, 5).Value = log.Description ?? string.Empty;
                worksheet.Cell(row, 6).Value = log.CreatedAt;

                // Format date
                worksheet.Cell(row, 6).Style.DateFormat.Format = "yyyy-MM-dd HH:mm:ss";

                // Add borders
                for (int col = 1; col <= headers.Length; col++)
                {
                    worksheet.Cell(row, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                }

                row++;
            }

            worksheet.Columns().AdjustToContents();

            // Summary
            var summaryRow = row + 2;
            worksheet.Cell(summaryRow, 1).Value = "Total Logs:";
            worksheet.Cell(summaryRow, 1).Style.Font.Bold = true;
            worksheet.Cell(summaryRow, 2).Value = logs.Count;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting audit logs to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportPropertiesByRegionAsync(int regionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var properties = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Region)
                .Where(p => p.RegionId == regionId)
                .OrderBy(p => p.PropertyId)
                .ToListAsync(cancellationToken);

            var regionName = properties.FirstOrDefault()?.Region?.RegionName ?? "Unknown Region";

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Properties");

            worksheet.Cell(1, 1).Value = $"Properties in {regionName}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;
            worksheet.Range(1, 1, 1, 7).Merge();

            worksheet.Cell(2, 1).Value = $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            worksheet.Range(2, 1, 2, 7).Merge();

            var headerRow = 4;
            var headers = new[] { "Property ID", "Owner", "Address", "Type", "Area", "Value", "Status" };

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = worksheet.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#2563eb");
                cell.Style.Font.FontColor = XLColor.White;
            }

            int row = headerRow + 1;
            foreach (var property in properties)
            {
                worksheet.Cell(row, 1).Value = property.PropertyId;
                worksheet.Cell(row, 2).Value = BuildUserName(property.Owner);
                worksheet.Cell(row, 3).Value = BuildPropertyAddress(property);
                worksheet.Cell(row, 4).Value = property.PropertyType.ToString();
                worksheet.Cell(row, 5).Value = property.LotArea ?? 0;
                worksheet.Cell(row, 6).Value = property.AssessedValue ?? 0;
                worksheet.Cell(row, 7).Value = property.Status.ToString();

                worksheet.Cell(row, 6).Style.NumberFormat.Format = "₱#,##0.00";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting properties by region to Excel");
            throw;
        }
    }

    public async Task<byte[]> ExportPaymentSummaryAsync(int year, CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentDate.Year == year)
                .ToListAsync(cancellationToken);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add($"Summary {year}");

            worksheet.Cell(1, 1).Value = $"Payment Summary Report - {year}";
            worksheet.Cell(1, 1).Style.Font.Bold = true;
            worksheet.Cell(1, 1).Style.Font.FontSize = 16;

            // Monthly summary
            var monthlySummary = payments
                .GroupBy(p => p.PaymentDate.Month)
                .Select(g => new
                {
                    Month = g.Key,
                    Count = g.Count(),
                    Total = g.Sum(p => p.AmountPaid)
                })
                .OrderBy(x => x.Month)
                .ToList();

            var headerRow = 3;
            worksheet.Cell(headerRow, 1).Value = "Month";
            worksheet.Cell(headerRow, 2).Value = "Payment Count";
            worksheet.Cell(headerRow, 3).Value = "Total Amount";

            for (int i = 1; i <= 3; i++)
            {
                worksheet.Cell(headerRow, i).Style.Font.Bold = true;
                worksheet.Cell(headerRow, i).Style.Fill.BackgroundColor = XLColor.FromHtml("#16a34a");
                worksheet.Cell(headerRow, i).Style.Font.FontColor = XLColor.White;
            }

            int row = headerRow + 1;
            foreach (var month in monthlySummary)
            {
                worksheet.Cell(row, 1).Value = new DateTime(year, month.Month, 1).ToString("MMMM");
                worksheet.Cell(row, 2).Value = month.Count;
                worksheet.Cell(row, 3).Value = month.Total;
                worksheet.Cell(row, 3).Style.NumberFormat.Format = "₱#,##0.00";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting payment summary to Excel");
            throw;
        }
    }

    private static string BuildUserName(ApplicationUser? user)
    {
        if (user == null) return "N/A";
        var fullName = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrWhiteSpace(fullName) ? (user.UserName ?? "N/A") : fullName;
    }

    private static string BuildPropertyAddress(Property? property)
    {
        if (property == null) return "N/A";
        var address = property.AddressLine1?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(property.AddressLine2))
        {
            address = string.IsNullOrWhiteSpace(address)
                ? property.AddressLine2.Trim()
                : $"{address}, {property.AddressLine2.Trim()}";
        }

        return string.IsNullOrWhiteSpace(address) ? "N/A" : address;
    }

    private static decimal GetPaidAmount(TaxAssessment assessment)
    {
        return assessment.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.AmountPaid);
    }
}
