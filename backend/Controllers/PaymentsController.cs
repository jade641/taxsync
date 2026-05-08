using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PaymentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Payment>>> GetPayments()
    {
        var payments = await _context.Payments.AsNoTracking().ToListAsync();
        return Ok(payments);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Payment>> GetPayment(int id)
    {
        var payment = await _context.Payments.FindAsync(id);
        if (payment == null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> CreatePayment([FromBody] Payment payment)
    {
        if (payment.PaymentDate == default)
        {
            payment.PaymentDate = DateTime.UtcNow;
        }

        if (payment.CreatedAt == default)
        {
            payment.CreatedAt = DateTime.UtcNow;
        }

        payment.UpdatedAt = DateTime.UtcNow;

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPayment), new { id = payment.PaymentId }, payment);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePayment(int id, [FromBody] Payment payment)
    {
        if (id != payment.PaymentId)
        {
            return BadRequest();
        }

        var existing = await _context.Payments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.AssessmentId = payment.AssessmentId;
        existing.PayerId = payment.PayerId;
        existing.PaymentReference = payment.PaymentReference;
        existing.PaymentMethod = payment.PaymentMethod;
        existing.AmountPaid = payment.AmountPaid;
        existing.PaymentDate = payment.PaymentDate == default ? existing.PaymentDate : payment.PaymentDate;
        existing.TransactionId = payment.TransactionId;
        existing.BankName = payment.BankName;
        existing.CheckNumber = payment.CheckNumber;
        existing.Status = payment.Status;
        existing.ReceiptNumber = payment.ReceiptNumber;
        existing.ProcessedBy = payment.ProcessedBy;
        existing.Notes = payment.Notes;
        existing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePayment(int id)
    {
        var existing = await _context.Payments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.Payments.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
