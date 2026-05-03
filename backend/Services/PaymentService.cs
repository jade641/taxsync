using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PaymentService : IPaymentService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public PaymentService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, int processedBy)
    {
        var assessment = await _context.TaxAssessments
            .Include(a => a.Property)
            .Include(a => a.Payments)
            .FirstOrDefaultAsync(a => a.AssessmentId == request.AssessmentId);

        if (assessment == null)
        {
            throw new InvalidOperationException("Assessment not found");
        }

        var payer = await _context.Users.FindAsync(request.PayerId);
        if (payer == null)
        {
            throw new InvalidOperationException("Payer not found");
        }

        // Calculate remaining balance
        var totalPaid = assessment.Payments
            .Where(p => p.Status == PaymentStatus.Completed)
            .Sum(p => p.AmountPaid);
        
        var balance = assessment.TotalAmount - totalPaid;

        if (request.AmountPaid > balance)
        {
            throw new InvalidOperationException($"Payment amount exceeds balance. Balance: {balance:C}");
        }

        // Generate payment reference and receipt number
        var paymentReference = await GeneratePaymentReferenceAsync();
        var receiptNumber = await GenerateReceiptNumberAsync();

        var payment = new Payment
        {
            AssessmentId = request.AssessmentId,
            PayerId = request.PayerId,
            PaymentReference = paymentReference,
            PaymentMethod = request.PaymentMethod,
            AmountPaid = request.AmountPaid,
            PaymentDate = DateTime.UtcNow,
            TransactionId = request.TransactionId,
            BankName = request.BankName,
            CheckNumber = request.CheckNumber,
            Status = PaymentStatus.Completed,
            ReceiptNumber = receiptNumber,
            ProcessedBy = processedBy,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Payments.Add(payment);

        // Update assessment status if fully paid
        var newTotalPaid = totalPaid + request.AmountPaid;
        if (newTotalPaid >= assessment.TotalAmount)
        {
            assessment.Status = AssessmentStatus.Paid;
            assessment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(processedBy, "CreatePayment", "Payment", LogSeverity.Info,
            $"Processed payment {paymentReference} for {request.AmountPaid:C}");

        return await GetPaymentByIdAsync(payment.PaymentId) 
            ?? throw new InvalidOperationException("Failed to retrieve created payment");
    }

    public async Task<PaymentDto?> GetPaymentByIdAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Assessment)
                .ThenInclude(a => a.Property)
            .Include(p => p.Payer)
            .Include(p => p.Processor)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null)
        {
            return null;
        }

        return MapToDto(payment);
    }

    public async Task<List<PaymentDto>> GetPaymentsAsync(int? assessmentId = null, int? payerId = null, 
        PaymentStatus? status = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Payments
            .Include(p => p.Assessment)
                .ThenInclude(a => a.Property)
            .Include(p => p.Payer)
            .Include(p => p.Processor)
            .AsQueryable();

        if (assessmentId.HasValue)
        {
            query = query.Where(p => p.AssessmentId == assessmentId.Value);
        }

        if (payerId.HasValue)
        {
            query = query.Where(p => p.PayerId == payerId.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var payments = await query
            .OrderByDescending(p => p.PaymentDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return payments.Select(MapToDto).ToList();
    }

    public async Task<PaymentReceiptDto?> GetPaymentReceiptAsync(int paymentId)
    {
        var payment = await _context.Payments
            .Include(p => p.Assessment)
                .ThenInclude(a => a.Property)
            .Include(p => p.Payer)
            .Include(p => p.Processor)
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

        if (payment == null || payment.Status != PaymentStatus.Completed)
        {
            return null;
        }

        var property = payment.Assessment.Property;
        var address = $"{property.AddressLine1}, {property.AddressLine2}".TrimEnd(',', ' ');

        return new PaymentReceiptDto
        {
            ReceiptNumber = payment.ReceiptNumber ?? "N/A",
            PaymentDate = payment.PaymentDate,
            PayerName = $"{payment.Payer.FirstName} {payment.Payer.LastName}",
            PropertyNumber = property.PropertyNumber ?? "N/A",
            PropertyAddress = address,
            TaxYear = payment.Assessment.TaxYear,
            Quarter = payment.Assessment.Quarter,
            AmountPaid = payment.AmountPaid,
            PaymentMethod = payment.PaymentMethod.ToString(),
            ProcessedBy = payment.Processor != null 
                ? $"{payment.Processor.FirstName} {payment.Processor.LastName}" 
                : "System"
        };
    }

    public async Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status, int updatedBy)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment == null)
        {
            return false;
        }

        payment.Status = status;
        payment.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(updatedBy, "UpdatePaymentStatus", "Payment", LogSeverity.Info,
            $"Updated payment {payment.PaymentReference} status to {status}");

        return true;
    }

    public async Task<decimal> GetTotalCollectionsAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Payments
            .Where(p => p.Status == PaymentStatus.Completed);

        if (from.HasValue)
        {
            query = query.Where(p => p.PaymentDate >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(p => p.PaymentDate <= to.Value);
        }

        return await query.SumAsync(p => p.AmountPaid);
    }

    private async Task<string> GeneratePaymentReferenceAsync()
    {
        var date = DateTime.UtcNow;
        var count = await _context.Payments.CountAsync(p => p.CreatedAt.Date == date.Date);
        return $"PAY-{date:yyyyMMdd}-{(count + 1):D4}";
    }

    private async Task<string> GenerateReceiptNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Payments.CountAsync(p => p.CreatedAt.Year == year);
        return $"OR-{year}-{(count + 1):D6}";
    }

    private PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto
        {
            PaymentId = payment.PaymentId,
            AssessmentId = payment.AssessmentId,
            PropertyNumber = payment.Assessment.Property.PropertyNumber ?? "N/A",
            PayerName = $"{payment.Payer.FirstName} {payment.Payer.LastName}",
            PaymentReference = payment.PaymentReference,
            PaymentMethod = payment.PaymentMethod.ToString().ToLower(),
            AmountPaid = payment.AmountPaid,
            PaymentDate = payment.PaymentDate,
            ReceiptNumber = payment.ReceiptNumber,
            Status = payment.Status.ToString().ToLower(),
            ProcessedByName = payment.Processor != null 
                ? $"{payment.Processor.FirstName} {payment.Processor.LastName}" 
                : null
        };
    }
}
