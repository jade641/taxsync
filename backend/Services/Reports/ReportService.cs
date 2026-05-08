using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Reporting.NETCore;
using System.Data;

namespace backend.Services.Reports;

/// <summary>
/// Production-ready RDLC report service for PDF generation
/// </summary>
public sealed class ReportService : IReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportService> _logger;
    private readonly IWebHostEnvironment _environment;

    public ReportService(
        AppDbContext context,
        ILogger<ReportService> logger,
        IWebHostEnvironment environment)
    {
        _context = context;
        _logger = logger;
        _environment = environment;
    }

    public async Task<byte[]> GeneratePaymentReceiptAsync(int paymentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _context.Payments
                .Include(p => p.Assessment)
                    .ThenInclude(a => a.Property)
                        .ThenInclude(prop => prop!.Owner)
                .Include(p => p.Payer)
                .Include(p => p.Processor)
                .FirstOrDefaultAsync(p => p.PaymentId == paymentId, cancellationToken);

            if (payment == null)
            {
                throw new ArgumentException($"Payment with ID {paymentId} not found");
            }

            // Create DataTable for report
            var dataTable = new DataTable("PaymentReceipt");
            dataTable.Columns.Add("PaymentId", typeof(int));
            dataTable.Columns.Add("PaymentDate", typeof(DateTime));
            dataTable.Columns.Add("Amount", typeof(decimal));
            dataTable.Columns.Add("PaymentMethod", typeof(string));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("PropertyAddress", typeof(string));
            dataTable.Columns.Add("OwnerName", typeof(string));
            dataTable.Columns.Add("ProcessedBy", typeof(string));
            dataTable.Columns.Add("ReferenceNumber", typeof(string));

            dataTable.Rows.Add(
                payment.PaymentId,
                payment.PaymentDate,
                payment.AmountPaid,
                payment.PaymentMethod.ToString(),
                payment.Status.ToString(),
                BuildPropertyAddress(payment.Assessment?.Property),
                BuildUserName(payment.Assessment?.Property?.Owner),
                BuildUserName(payment.Processor),
                payment.PaymentReference
            );

            var reportPath = Path.Combine(_environment.ContentRootPath, "Reports", "PaymentReceipt.rdlc");
            return GeneratePdfReport(reportPath, dataTable, "PaymentReceipt");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating payment receipt for payment ID {PaymentId}", paymentId);
            throw;
        }
    }

    public async Task<byte[]> GeneratePropertyTaxReportAsync(int propertyId, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            var property = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Region)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId, cancellationToken);

            if (property == null)
            {
                throw new ArgumentException($"Property with ID {propertyId} not found");
            }

            var assessments = await _context.TaxAssessments
                .Where(t => t.PropertyId == propertyId && t.TaxYear == year)
                .ToListAsync(cancellationToken);

            var payments = await _context.Payments
                .Include(p => p.Assessment)
                .Where(p => p.Assessment.PropertyId == propertyId && p.PaymentDate.Year == year)
                .ToListAsync(cancellationToken);

            // Create DataTable
            var dataTable = new DataTable("PropertyTaxReport");
            dataTable.Columns.Add("PropertyId", typeof(int));
            dataTable.Columns.Add("PropertyAddress", typeof(string));
            dataTable.Columns.Add("OwnerName", typeof(string));
            dataTable.Columns.Add("PropertyType", typeof(string));
            dataTable.Columns.Add("LandArea", typeof(decimal));
            dataTable.Columns.Add("AssessedValue", typeof(decimal));
            dataTable.Columns.Add("Year", typeof(int));
            dataTable.Columns.Add("TotalTaxDue", typeof(decimal));
            dataTable.Columns.Add("TotalPaid", typeof(decimal));
            dataTable.Columns.Add("Balance", typeof(decimal));
            dataTable.Columns.Add("Region", typeof(string));

            var totalTaxDue = assessments.Sum(a => a.TotalAmount);
            var totalPaid = payments.Sum(p => p.AmountPaid);

            dataTable.Rows.Add(
                property.PropertyId,
                BuildPropertyAddress(property),
                BuildUserName(property.Owner),
                property.PropertyType.ToString(),
                property.LotArea ?? 0,
                property.AssessedValue ?? 0,
                year,
                totalTaxDue,
                totalPaid,
                totalTaxDue - totalPaid,
                property.Region?.RegionName ?? "N/A"
            );

            var reportPath = Path.Combine(_environment.ContentRootPath, "Reports", "PropertyTaxReport.rdlc");
            return GeneratePdfReport(reportPath, dataTable, "PropertyTaxReport");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating property tax report for property ID {PropertyId}", propertyId);
            throw;
        }
    }

    public async Task<byte[]> GenerateCollectionSummaryAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var payments = await _context.Payments
                .Where(p => p.PaymentDate >= startDate && p.PaymentDate <= endDate)
                .ToListAsync(cancellationToken);

            // Create DataTable
            var dataTable = new DataTable("CollectionSummary");
            dataTable.Columns.Add("StartDate", typeof(DateTime));
            dataTable.Columns.Add("EndDate", typeof(DateTime));
            dataTable.Columns.Add("TotalPayments", typeof(int));
            dataTable.Columns.Add("TotalAmount", typeof(decimal));
            dataTable.Columns.Add("CashPayments", typeof(int));
            dataTable.Columns.Add("CashAmount", typeof(decimal));
            dataTable.Columns.Add("OnlinePayments", typeof(int));
            dataTable.Columns.Add("OnlineAmount", typeof(decimal));
            dataTable.Columns.Add("CheckPayments", typeof(int));
            dataTable.Columns.Add("CheckAmount", typeof(decimal));

            var cashPayments = payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).ToList();
            var onlinePayments = payments.Where(p => p.PaymentMethod == PaymentMethod.Online).ToList();
            var checkPayments = payments.Where(p => p.PaymentMethod == PaymentMethod.Check).ToList();

            dataTable.Rows.Add(
                startDate,
                endDate,
                payments.Count,
                payments.Sum(p => p.AmountPaid),
                cashPayments.Count,
                cashPayments.Sum(p => p.AmountPaid),
                onlinePayments.Count,
                onlinePayments.Sum(p => p.AmountPaid),
                checkPayments.Count,
                checkPayments.Sum(p => p.AmountPaid)
            );

            var reportPath = Path.Combine(_environment.ContentRootPath, "Reports", "CollectionSummary.rdlc");
            return GeneratePdfReport(reportPath, dataTable, "CollectionSummary");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating collection summary report");
            throw;
        }
    }

    public async Task<byte[]> GenerateTaxAssessmentReportAsync(int assessmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var assessment = await _context.TaxAssessments
                .Include(t => t.Property)
                    .ThenInclude(p => p!.Owner)
                .Include(t => t.Property)
                    .ThenInclude(p => p!.Region)
                .Include(t => t.Payments)
                .FirstOrDefaultAsync(t => t.AssessmentId == assessmentId, cancellationToken);

            if (assessment == null)
            {
                throw new ArgumentException($"Tax assessment with ID {assessmentId} not found");
            }

            var dataTable = new DataTable("TaxAssessment");
            dataTable.Columns.Add("AssessmentId", typeof(int));
            dataTable.Columns.Add("PropertyAddress", typeof(string));
            dataTable.Columns.Add("OwnerName", typeof(string));
            dataTable.Columns.Add("AssessmentYear", typeof(int));
            dataTable.Columns.Add("TaxAmount", typeof(decimal));
            dataTable.Columns.Add("DueDate", typeof(DateTime));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("AmountPaid", typeof(decimal));
            dataTable.Columns.Add("Balance", typeof(decimal));
            dataTable.Columns.Add("AssessedValue", typeof(decimal));
            dataTable.Columns.Add("TaxRate", typeof(decimal));

            dataTable.Rows.Add(
                assessment.AssessmentId,
                BuildPropertyAddress(assessment.Property),
                BuildUserName(assessment.Property?.Owner),
                assessment.TaxYear,
                assessment.TotalAmount,
                assessment.DueDate,
                assessment.Status.ToString(),
                GetPaidAmount(assessment),
                assessment.TotalAmount - GetPaidAmount(assessment),
                assessment.Property?.AssessedValue ?? 0,
                assessment.TaxRate
            );

            var reportPath = Path.Combine(_environment.ContentRootPath, "Reports", "TaxAssessmentReport.rdlc");
            return GeneratePdfReport(reportPath, dataTable, "TaxAssessment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating tax assessment report");
            throw;
        }
    }

    public async Task<byte[]> GenerateAnnualTaxStatementAsync(int propertyId, int year, CancellationToken cancellationToken = default)
    {
        try
        {
            var property = await _context.Properties
                .Include(p => p.Owner)
                .Include(p => p.Region)
                .FirstOrDefaultAsync(p => p.PropertyId == propertyId, cancellationToken);

            if (property == null)
            {
                throw new ArgumentException($"Property with ID {propertyId} not found");
            }

            var assessments = await _context.TaxAssessments
                .Where(t => t.PropertyId == propertyId && t.TaxYear == year)
                .ToListAsync(cancellationToken);

            var payments = await _context.Payments
                .Include(p => p.Assessment)
                .Where(p => p.Assessment.PropertyId == propertyId && p.PaymentDate.Year == year)
                .OrderBy(p => p.PaymentDate)
                .ToListAsync(cancellationToken);

            var dataTable = new DataTable("AnnualStatement");
            dataTable.Columns.Add("PropertyId", typeof(int));
            dataTable.Columns.Add("PropertyAddress", typeof(string));
            dataTable.Columns.Add("OwnerName", typeof(string));
            dataTable.Columns.Add("Year", typeof(int));
            dataTable.Columns.Add("AssessedValue", typeof(decimal));
            dataTable.Columns.Add("TotalTaxDue", typeof(decimal));
            dataTable.Columns.Add("TotalPayments", typeof(decimal));
            dataTable.Columns.Add("OutstandingBalance", typeof(decimal));
            dataTable.Columns.Add("PaymentCount", typeof(int));
            dataTable.Columns.Add("LastPaymentDate", typeof(DateTime));

            var totalTaxDue = assessments.Sum(a => a.TotalAmount);
            var totalPayments = payments.Sum(p => p.AmountPaid);
            var lastPayment = payments.LastOrDefault();

            dataTable.Rows.Add(
                property.PropertyId,
                BuildPropertyAddress(property),
                BuildUserName(property.Owner),
                year,
                property.AssessedValue ?? 0,
                totalTaxDue,
                totalPayments,
                totalTaxDue - totalPayments,
                payments.Count,
                lastPayment?.PaymentDate ?? DateTime.MinValue
            );

            var reportPath = Path.Combine(_environment.ContentRootPath, "Reports", "AnnualTaxStatement.rdlc");
            return GeneratePdfReport(reportPath, dataTable, "AnnualStatement");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating annual tax statement");
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

    #region Private Helper Methods

    private byte[] GeneratePdfReport(string reportPath, DataTable dataTable, string dataSetName)
    {
        try
        {
            if (!File.Exists(reportPath))
            {
                throw new FileNotFoundException($"Report template not found: {reportPath}");
            }

            using var report = new LocalReport();
            report.ReportPath = reportPath;

            // Add data source
            var dataSource = new ReportDataSource(dataSetName, dataTable);
            report.DataSources.Add(dataSource);

            // Set report parameters
            var parameters = new[]
            {
                new ReportParameter("GeneratedDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new ReportParameter("SystemName", "TaxSync - Property Taxation System")
            };
            report.SetParameters(parameters);

            // Render to PDF
            var pdfBytes = report.Render(
                "PDF",
                deviceInfo: null,
                out string mimeType,
                out string encoding,
                out string fileNameExtension,
                out string[] streams,
                out Warning[] warnings);

            return pdfBytes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF report from {ReportPath}", reportPath);
            throw;
        }
    }

    #endregion
}
