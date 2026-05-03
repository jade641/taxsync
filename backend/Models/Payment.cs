namespace backend.Models;

public class Payment
{
    public int PaymentId { get; set; }
    public int AssessmentId { get; set; }
    public int PayerId { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? TransactionId { get; set; }
    public string? BankName { get; set; }
    public string? CheckNumber { get; set; }
    public PaymentStatus Status { get; set; }
    public string? ReceiptNumber { get; set; }
    public int? ProcessedBy { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public TaxAssessment Assessment { get; set; } = null!;
    public User Payer { get; set; } = null!;
    public User? Processor { get; set; }
}

public enum PaymentMethod
{
    Cash,
    Check,
    BankTransfer,
    CreditCard,
    DebitCard,
    Gcash,
    Paymaya,
    Online
}

public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded,
    Cancelled
}
