using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IPaymentService
{
    Task<PaymentDto> CreatePaymentAsync(CreatePaymentRequest request, int processedBy);
    Task<PaymentDto?> GetPaymentByIdAsync(int paymentId);
    Task<List<PaymentDto>> GetPaymentsAsync(int? assessmentId = null, int? payerId = null, PaymentStatus? status = null, int page = 1, int pageSize = 50);
    Task<PaymentReceiptDto?> GetPaymentReceiptAsync(int paymentId);
    Task<bool> UpdatePaymentStatusAsync(int paymentId, PaymentStatus status, int updatedBy);
    Task<decimal> GetTotalCollectionsAsync(DateTime? from = null, DateTime? to = null);
}
