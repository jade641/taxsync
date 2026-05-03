namespace backend.Models;

public class TaxRate
{
    public int RateId { get; set; }
    public PropertyType PropertyType { get; set; }
    public decimal RatePercentage { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Description { get; set; }
    public int? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User? Creator { get; set; }
}

public class TaxAssessment
{
    public int AssessmentId { get; set; }
    public int PropertyId { get; set; }
    public int TaxYear { get; set; }
    public int? Quarter { get; set; }
    public decimal AssessedValue { get; set; }
    public decimal TaxRate { get; set; }
    public decimal BasicTax { get; set; }
    public decimal SefTax { get; set; }
    public decimal Penalties { get; set; }
    public decimal Discounts { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public AssessmentStatus Status { get; set; }
    public int? AssessedBy { get; set; }
    public int? ApprovedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public Property Property { get; set; } = null!;
    public User? Assessor { get; set; }
    public User? Approver { get; set; }
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

public enum AssessmentStatus
{
    Pending,
    Approved,
    Paid,
    Overdue,
    Cancelled
}
