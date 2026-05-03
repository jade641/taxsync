using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface ITaxService
{
    Task<TaxComputationResult> ComputeTaxAsync(TaxComputationRequest request);
    Task<TaxAssessmentDto> CreateAssessmentAsync(CreateTaxAssessmentRequest request, int assessedBy);
    Task<TaxAssessmentDto?> GetAssessmentByIdAsync(int assessmentId);
    Task<List<TaxAssessmentDto>> GetAssessmentsByPropertyAsync(int propertyId);
    Task<List<TaxAssessmentDto>> GetAssessmentsAsync(int? propertyId = null, int? taxYear = null, AssessmentStatus? status = null, int page = 1, int pageSize = 50);
    Task<bool> ApproveAssessmentAsync(int assessmentId, int approvedBy);
    Task<bool> UpdateAssessmentStatusAsync(int assessmentId, AssessmentStatus status);
    Task<decimal> CalculatePenaltyAsync(int assessmentId);
    Task<List<TaxRateDto>> GetTaxRatesAsync();
    Task<TaxRateDto> UpdateTaxRateAsync(UpdateTaxRateRequest request, int createdBy);
}
