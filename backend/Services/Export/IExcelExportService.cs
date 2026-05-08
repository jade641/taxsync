namespace backend.Services.Export;

/// <summary>
/// Interface for Excel export operations using ClosedXML
/// </summary>
public interface IExcelExportService
{
    /// <summary>
    /// Export property records to Excel
    /// </summary>
    Task<byte[]> ExportPropertiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export payment history to Excel
    /// </summary>
    Task<byte[]> ExportPaymentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export tax assessments to Excel
    /// </summary>
    Task<byte[]> ExportTaxAssessmentsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Export audit logs to Excel
    /// </summary>
    Task<byte[]> ExportAuditLogsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export filtered properties to Excel
    /// </summary>
    Task<byte[]> ExportPropertiesByRegionAsync(int regionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export payment summary report
    /// </summary>
    Task<byte[]> ExportPaymentSummaryAsync(int year, CancellationToken cancellationToken = default);
}
