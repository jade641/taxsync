using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs;

public class TaxAssessmentDto
{
    public int AssessmentId { get; set; }
    public int PropertyId { get; set; }
    public string PropertyNumber { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public int? Quarter { get; set; }
    public decimal AssessedValue { get; set; }
    public decimal TaxRate { get; set; }
    public decimal BasicTax { get; set; }
    public decimal SefTax { get; set; }
    public decimal Penalties { get; set; }
    public decimal Discounts { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public DateTime DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateTaxAssessmentRequest
{
    [Required]
    public int PropertyId { get; set; }
    
    [Required]
    public int TaxYear { get; set; }
    
    public int? Quarter { get; set; }
    
    [Required]
    public decimal AssessedValue { get; set; }
    
    [Required]
    public DateTime DueDate { get; set; }
    
    public string? Notes { get; set; }
}

public class TaxComputationRequest
{
    [Required]
    public PropertyType PropertyType { get; set; }
    
    [Required]
    public decimal AssessedValue { get; set; }
    
    public int TaxYear { get; set; }
}

public class TaxComputationResult
{
    public decimal AssessedValue { get; set; }
    public decimal TaxRate { get; set; }
    public decimal BasicTax { get; set; }
    public decimal SefTax { get; set; }
    public decimal TotalTax { get; set; }
}

public class TaxRateDto
{
    public int RateId { get; set; }
    public string PropertyType { get; set; } = string.Empty;
    public decimal RatePercentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
}

public class UpdateTaxRateRequest
{
    [Required]
    public PropertyType PropertyType { get; set; }
    
    [Required]
    [Range(0, 1)]
    public decimal RatePercentage { get; set; }
    
    [Required]
    public DateTime EffectiveFrom { get; set; }
    
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
}
