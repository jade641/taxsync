using backend.Data;
using backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PropertyDocumentsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PropertyDocumentsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<PropertyDocument>>> GetPropertyDocuments()
    {
        var documents = await _context.PropertyDocuments.AsNoTracking().ToListAsync();
        return Ok(documents);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PropertyDocument>> GetPropertyDocument(int id)
    {
        var document = await _context.PropertyDocuments.FindAsync(id);
        if (document == null)
        {
            return NotFound();
        }

        return Ok(document);
    }

    [HttpPost]
    public async Task<ActionResult<PropertyDocument>> CreatePropertyDocument([FromBody] PropertyDocument document)
    {
        if (document.UploadedAt == default)
        {
            document.UploadedAt = DateTime.UtcNow;
        }

        _context.PropertyDocuments.Add(document);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPropertyDocument), new { id = document.DocumentId }, document);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdatePropertyDocument(int id, [FromBody] PropertyDocument document)
    {
        if (id != document.DocumentId)
        {
            return BadRequest();
        }

        var existing = await _context.PropertyDocuments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.PropertyId = document.PropertyId;
        existing.DocumentType = document.DocumentType;
        existing.DocumentName = document.DocumentName;
        existing.FilePath = document.FilePath;
        existing.FileSize = document.FileSize;
        existing.UploadedBy = document.UploadedBy;

        await _context.SaveChangesAsync();

        return Ok(existing);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeletePropertyDocument(int id)
    {
        var existing = await _context.PropertyDocuments.FindAsync(id);
        if (existing == null)
        {
            return NotFound();
        }

        _context.PropertyDocuments.Remove(existing);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
