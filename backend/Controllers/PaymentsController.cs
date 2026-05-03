using backend.DTOs;
using backend.Models;
using backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace backend.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;

    public PaymentsController(IPaymentService paymentService)
    {
        _paymentService = paymentService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPayments([FromQuery] int? assessmentId = null, [FromQuery] int? payerId = null, 
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            PaymentStatus? statusEnum = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<PaymentStatus>(status, true, out var s))
            {
                statusEnum = s;
            }

            var payments = await _paymentService.GetPaymentsAsync(assessmentId, payerId, statusEnum, page, pageSize);
            return Ok(new { payments, page, pageSize });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetPayment(int id)
    {
        try
        {
            var payment = await _paymentService.GetPaymentByIdAsync(id);
            if (payment == null)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Accountant,Staff")]
    [HttpPost]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var payment = await _paymentService.CreatePaymentAsync(request, currentUserId);
            return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}/receipt")]
    public async Task<IActionResult> GetPaymentReceipt(int id)
    {
        try
        {
            var receipt = await _paymentService.GetPaymentReceiptAsync(id);
            if (receipt == null)
            {
                return NotFound(new { message = "Receipt not found or payment not completed" });
            }

            return Ok(receipt);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Accountant")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] UpdatePaymentStatusRequest request)
    {
        try
        {
            var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var success = await _paymentService.UpdatePaymentStatusAsync(id, request.Status, currentUserId);

            if (!success)
            {
                return NotFound(new { message = "Payment not found" });
            }

            return Ok(new { message = "Payment status updated successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize(Roles = "Admin,Accountant,Auditor")]
    [HttpGet("collections/total")]
    public async Task<IActionResult> GetTotalCollections([FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
    {
        try
        {
            var total = await _paymentService.GetTotalCollectionsAsync(from, to);
            return Ok(new { total, from, to });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class UpdatePaymentStatusRequest
{
    public PaymentStatus Status { get; set; }
}
