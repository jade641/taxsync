using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs;

public class PaymentDto
{
    public int PaymentId { get; set; }
    public int AssessmentId { get; set; }
    public string PropertyNumber { get; set; } = string.Empty;
    public string PayerName { get; set; } = string.Empty;
    public string PaymentReference { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal AmountPaid { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? ReceiptNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProcessedByName { get; set; }
}

public class CreatePaymentRequest
{
    [Required]
    public int AssessmentId { get; set; }
    
    [Required]
    public int PayerId { get; set; }
    
    [Required]
    public PaymentMethod PaymentMethod { get; set; }
    
    [Required]
    [Range(0.01, double.MaxValue)]
    public decimal AmountPaid { get; set; }
    
    public string? TransactionId { get; set; }
    public string? BankName { get; set; }
    public string? CheckNumber { get; set; }
    public string? Notes { get; set; }
}

public class PaymentReceiptDto
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; }
    public string PayerName { get; set; } = string.Empty;
    public string PropertyNumber { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public int TaxYear { get; set; }
    public int? Quarter { get; set; }
    public decimal AmountPaid { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ProcessedBy { get; set; } = string.Empty;
}
