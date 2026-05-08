using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class TaxService : ITaxService
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private const decimal SEF_RATE = 0.01m; // 1% Special Education Fund

    public TaxService(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<TaxComputationResult> ComputeTaxAsync(TaxComputationRequest request)
    {
        var taxRate = await GetCurrentTaxRateAsync(request.PropertyType, request.TaxYear);
        
        if (taxRate == null)
        {
            throw new InvalidOperationException($"No tax rate found for {request.PropertyType}");
        }

        var basicTax = request.AssessedValue * taxRate.RatePercentage;
        var sefTax = request.AssessedValue * SEF_RATE;
        var totalTax = basicTax + sefTax;

        return new TaxComputationResult
        {
            AssessedValue = request.AssessedValue,
            TaxRate = taxRate.RatePercentage,
            BasicTax = basicTax,
            SefTax = sefTax,
            TotalTax = totalTax
        };
    }

    public async Task<TaxAssessmentDto> CreateAssessmentAsync(CreateTaxAssessmentRequest request, int assessedBy)
    {
        var property = await _context.Properties
            .Include(p => p.Owner)
            .FirstOrDefaultAsync(p => p.PropertyId == request.PropertyId);

        if (property == null)
        {
            throw new InvalidOperationException("Property not found");
        }

        // Check if assessment already exists
        var existing = await _context.TaxAssessments
            .FirstOrDefaultAsync(a => a.PropertyId == request.PropertyId && 
                                     a.TaxYear == request.TaxYear && 
                                     a.Quarter == request.Quarter);

        if (existing != null)
        {
            throw new InvalidOperationException("Assessment already exists for this period");
        }

        // Compute tax
        var computation = await ComputeTaxAsync(new TaxComputationRequest
        {
            PropertyType = property.PropertyType,
            AssessedValue = request.AssessedValue,
            TaxYear = request.TaxYear
        });

        var assessment = new TaxAssessment
        {
            PropertyId = request.PropertyId,
            TaxYear = request.TaxYear,
            Quarter = request.Quarter,
            AssessedValue = request.AssessedValue,
            TaxRate = computation.TaxRate,
            BasicTax = computation.BasicTax,
            SefTax = computation.SefTax,
            Penalties = 0,
            Discounts = 0,
            TotalAmount = computation.TotalTax,
            DueDate = request.DueDate,
            Status = AssessmentStatus.Pending,
            AssessedBy = assessedBy,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.TaxAssessments.Add(assessment);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(assessedBy, "CreateAssessment", "Tax", LogSeverity.Info,
            $"Created tax assessment for property {property.PropertyNumber}");

        return await GetAssessmentByIdAsync(assessment.AssessmentId) 
            ?? throw new InvalidOperationException("Failed to retrieve created assessment");
    }

    public async Task<TaxAssessmentDto?> GetAssessmentByIdAsync(int assessmentId)
    {
        var assessment = await _context.TaxAssessments
            .Include(a => a.Property)
                .ThenInclude(p => p.Owner)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

        if (assessment == null)
        {
            return null;
        }

        return MapToDto(assessment);
    }

    public async Task<List<TaxAssessmentDto>> GetAssessmentsByPropertyAsync(int propertyId)
    {
        var assessments = await _context.TaxAssessments
            .Include(a => a.Property)
                .ThenInclude(p => p.Owner)
            .Include(a => a.Payments)
            .Where(a => a.PropertyId == propertyId)
            .OrderByDescending(a => a.TaxYear)
            .ThenByDescending(a => a.Quarter)
            .ToListAsync();

        return assessments.Select(MapToDto).ToList();
    }

    public async Task<List<TaxAssessmentDto>> GetAssessmentsAsync(int? propertyId = null, int? taxYear = null, 
        AssessmentStatus? status = null, int page = 1, int pageSize = 50)
    {
        var query = _context.TaxAssessments
            .Include(a => a.Property)
                .ThenInclude(p => p.Owner)
            .Include(a => a.Payments)
            .AsQueryable();

        if (propertyId.HasValue)
        {
            query = query.Where(a => a.PropertyId == propertyId.Value);
        }

        if (taxYear.HasValue)
        {
            query = query.Where(a => a.TaxYear == taxYear.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(a => a.Status == status.Value);
        }

        var assessments = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return assessments.Select(MapToDto).ToList();
    }

    public async Task<bool> ApproveAssessmentAsync(int assessmentId, int approvedBy)
    {
        var assessment = await _context.TaxAssessments.FindAsync(assessmentId);
        if (assessment == null)
        {
            return false;
        }

        assessment.Status = AssessmentStatus.Approved;
        assessment.ApprovedBy = approvedBy;
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(approvedBy, "ApproveAssessment", "Tax", LogSeverity.Info,
            $"Approved assessment {assessmentId}");

        return true;
    }

    public async Task<bool> UpdateAssessmentStatusAsync(int assessmentId, AssessmentStatus status)
    {
        var assessment = await _context.TaxAssessments.FindAsync(assessmentId);
        if (assessment == null)
        {
            return false;
        }

        assessment.Status = status;
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<decimal> CalculatePenaltyAsync(int assessmentId)
    {
        var assessment = await _context.TaxAssessments.FindAsync(assessmentId);
        if (assessment == null || assessment.Status == AssessmentStatus.Paid)
        {
            return 0;
        }

        if (DateTime.UtcNow <= assessment.DueDate)
        {
            return 0;
        }

        // Calculate penalty: 2% per month
        var monthsOverdue = (DateTime.UtcNow.Year - assessment.DueDate.Year) * 12 + 
                           DateTime.UtcNow.Month - assessment.DueDate.Month;
        
        if (monthsOverdue < 1)
        {
            monthsOverdue = 1;
        }

        var penaltyRate = 0.02m * monthsOverdue; // 2% per month
        var penalty = assessment.TotalAmount * penaltyRate;

        // Update assessment with penalty
        assessment.Penalties = penalty;
        assessment.TotalAmount = assessment.BasicTax + assessment.SefTax + penalty - assessment.Discounts;
        assessment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return penalty;
    }

    public async Task<List<TaxRateDto>> GetTaxRatesAsync()
    {
        var rates = await _context.TaxRates
            .Where(r => r.EffectiveTo == null || r.EffectiveTo > DateTime.UtcNow)
            .OrderBy(r => r.PropertyType)
            .ToListAsync();

        return rates.Select(r => new TaxRateDto
        {
            RateId = r.RateId,
            PropertyType = r.PropertyType.ToString().ToLower(),
            RatePercentage = r.RatePercentage,
            EffectiveFrom = r.EffectiveFrom,
            EffectiveTo = r.EffectiveTo,
            Description = r.Description
        }).ToList();
    }

    public async Task<TaxRateDto> UpdateTaxRateAsync(UpdateTaxRateRequest request, int createdBy)
    {
        // Expire old rate
        var oldRate = await _context.TaxRates
            .Where(r => r.PropertyType == request.PropertyType && 
                       (r.EffectiveTo == null || r.EffectiveTo > DateTime.UtcNow))
            .FirstOrDefaultAsync();

        if (oldRate != null)
        {
            oldRate.EffectiveTo = request.EffectiveFrom.AddDays(-1);
        }

        // Create new rate
        var newRate = new TaxRate
        {
            PropertyType = request.PropertyType,
            RatePercentage = request.RatePercentage,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Description = request.Description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.TaxRates.Add(newRate);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, "UpdateTaxRate", "Tax", LogSeverity.Info,
            $"Updated tax rate for {request.PropertyType} to {request.RatePercentage:P}");

        return new TaxRateDto
        {
            RateId = newRate.RateId,
            PropertyType = newRate.PropertyType.ToString().ToLower(),
            RatePercentage = newRate.RatePercentage,
            EffectiveFrom = newRate.EffectiveFrom,
            EffectiveTo = newRate.EffectiveTo,
            Description = newRate.Description
        };
    }

    private async Task<TaxRate?> GetCurrentTaxRateAsync(PropertyType propertyType, int taxYear)
    {
        var effectiveDate = new DateTime(taxYear, 1, 1);
        
        return await _context.TaxRates
            .Where(r => r.PropertyType == propertyType &&
                       r.EffectiveFrom <= effectiveDate &&
                       (r.EffectiveTo == null || r.EffectiveTo >= effectiveDate))
            .OrderByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    private TaxAssessmentDto MapToDto(TaxAssessment assessment)
    {
        var amountPaid = assessment.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.AmountPaid);

        return new TaxAssessmentDto
        {
            AssessmentId = assessment.AssessmentId,
            PropertyId = assessment.PropertyId,
            PropertyNumber = assessment.Property.PropertyNumber ?? "N/A",
            OwnerName = $"{assessment.Property.Owner.FirstName} {assessment.Property.Owner.LastName}",
            TaxYear = assessment.TaxYear,
            Quarter = assessment.Quarter,
            AssessedValue = assessment.AssessedValue,
            TaxRate = assessment.TaxRate,
            BasicTax = assessment.BasicTax,
            SefTax = assessment.SefTax,
            Penalties = assessment.Penalties,
            Discounts = assessment.Discounts,
            TotalAmount = assessment.TotalAmount,
            AmountPaid = amountPaid,
            Balance = assessment.TotalAmount - amountPaid,
            DueDate = assessment.DueDate,
            Status = assessment.Status.ToString().ToLower(),
            CreatedAt = assessment.CreatedAt
        };
    }
}
