namespace backend.Services.Reports;

/// <summary>
/// Interface for RDLC report generation
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Generate payment receipt PDF report
    /// </summary>
    Task<byte[]> GeneratePaymentReceiptAsync(int paymentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate property tax report PDF
    /// </summary>
    Task<byte[]> GeneratePropertyTaxReportAsync(int propertyId, int year, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate collection summary report PDF
    /// </summary>
    Task<byte[]> GenerateCollectionSummaryAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate tax assessment report PDF
    /// </summary>
    Task<byte[]> GenerateTaxAssessmentReportAsync(int assessmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate annual property tax statement PDF
    /// </summary>
    Task<byte[]> GenerateAnnualTaxStatementAsync(int propertyId, int year, CancellationToken cancellationToken = default);
}
