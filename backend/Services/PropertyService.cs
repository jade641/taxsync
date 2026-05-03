using backend.Data;
using backend.DTOs;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class PropertyService : IPropertyService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditService _auditService;

    public PropertyService(ApplicationDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    public async Task<PropertyDto> CreatePropertyAsync(CreatePropertyRequest request, int createdBy)
    {
        // Validate owner exists
        var owner = await _context.Users.FindAsync(request.OwnerId);
        if (owner == null)
        {
            throw new InvalidOperationException("Owner not found");
        }

        // Validate geographic hierarchy
        var barangay = await _context.Barangays
            .Include(b => b.City)
                .ThenInclude(c => c.Province)
                    .ThenInclude(p => p.Region)
            .FirstOrDefaultAsync(b => b.BarangayId == request.BarangayId);

        if (barangay == null)
        {
            throw new InvalidOperationException("Invalid barangay");
        }

        // Generate property number if not provided
        var propertyNumber = request.PropertyNumber ?? await GeneratePropertyNumberAsync();

        var property = new Property
        {
            OwnerId = request.OwnerId,
            PropertyType = request.PropertyType,
            PropertyNumber = propertyNumber,
            TitleNumber = request.TitleNumber,
            AddressLine1 = request.AddressLine1,
            AddressLine2 = request.AddressLine2,
            RegionId = request.RegionId,
            ProvinceId = request.ProvinceId,
            CityId = request.CityId,
            BarangayId = request.BarangayId,
            PostalCode = request.PostalCode,
            LotArea = request.LotArea,
            FloorArea = request.FloorArea,
            MarketValue = request.MarketValue,
            AssessedValue = request.AssessedValue,
            YearAcquired = request.YearAcquired,
            RegistrationDate = DateTime.UtcNow,
            Status = PropertyStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(createdBy, "CreateProperty", "Property", LogSeverity.Info,
            $"Created property {propertyNumber} for owner {owner.Username}");

        return await GetPropertyByIdAsync(property.PropertyId) 
            ?? throw new InvalidOperationException("Failed to retrieve created property");
    }

    public async Task<PropertyDto?> GetPropertyByIdAsync(int propertyId)
    {
        var property = await _context.Properties
            .Include(p => p.Owner)
            .Include(p => p.Region)
            .Include(p => p.Province)
            .Include(p => p.City)
            .Include(p => p.Barangay)
            .FirstOrDefaultAsync(p => p.PropertyId == propertyId);

        if (property == null)
        {
            return null;
        }

        return MapToDto(property);
    }

    public async Task<List<PropertyDto>> GetPropertiesAsync(int? ownerId = null, PropertyType? propertyType = null, 
        PropertyStatus? status = null, int page = 1, int pageSize = 50)
    {
        var query = _context.Properties
            .Include(p => p.Owner)
            .Include(p => p.Region)
            .Include(p => p.Province)
            .Include(p => p.City)
            .Include(p => p.Barangay)
            .AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(p => p.OwnerId == ownerId.Value);
        }

        if (propertyType.HasValue)
        {
            query = query.Where(p => p.PropertyType == propertyType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        var properties = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return properties.Select(MapToDto).ToList();
    }

    public async Task<PropertyDto?> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request, int updatedBy)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null)
        {
            return null;
        }

        if (request.PropertyType.HasValue)
        {
            property.PropertyType = request.PropertyType.Value;
        }

        if (!string.IsNullOrEmpty(request.TitleNumber))
        {
            property.TitleNumber = request.TitleNumber;
        }

        if (!string.IsNullOrEmpty(request.AddressLine1))
        {
            property.AddressLine1 = request.AddressLine1;
        }

        if (request.AddressLine2 != null)
        {
            property.AddressLine2 = request.AddressLine2;
        }

        if (request.BarangayId.HasValue)
        {
            property.BarangayId = request.BarangayId.Value;
        }

        if (request.PostalCode != null)
        {
            property.PostalCode = request.PostalCode;
        }

        if (request.LotArea.HasValue)
        {
            property.LotArea = request.LotArea.Value;
        }

        if (request.FloorArea.HasValue)
        {
            property.FloorArea = request.FloorArea.Value;
        }

        if (request.MarketValue.HasValue)
        {
            property.MarketValue = request.MarketValue.Value;
        }

        if (request.AssessedValue.HasValue)
        {
            property.AssessedValue = request.AssessedValue.Value;
        }

        if (request.Status.HasValue)
        {
            property.Status = request.Status.Value;
        }

        property.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(updatedBy, "UpdateProperty", "Property", LogSeverity.Info,
            $"Updated property {property.PropertyNumber}");

        return await GetPropertyByIdAsync(propertyId);
    }

    public async Task<bool> DeletePropertyAsync(int propertyId, int deletedBy)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null)
        {
            return false;
        }

        _context.Properties.Remove(property);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(deletedBy, "DeleteProperty", "Property", LogSeverity.Warning,
            $"Deleted property {property.PropertyNumber}");

        return true;
    }

    public async Task<bool> UpdatePropertyStatusAsync(int propertyId, PropertyStatus status, int updatedBy)
    {
        var property = await _context.Properties.FindAsync(propertyId);
        if (property == null)
        {
            return false;
        }

        property.Status = status;
        property.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(updatedBy, "UpdatePropertyStatus", "Property", LogSeverity.Info,
            $"Updated property {property.PropertyNumber} status to {status}");

        return true;
    }

    public async Task<int> GetPropertiesCountAsync(int? ownerId = null, PropertyType? propertyType = null, 
        PropertyStatus? status = null)
    {
        var query = _context.Properties.AsQueryable();

        if (ownerId.HasValue)
        {
            query = query.Where(p => p.OwnerId == ownerId.Value);
        }

        if (propertyType.HasValue)
        {
            query = query.Where(p => p.PropertyType == propertyType.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(p => p.Status == status.Value);
        }

        return await query.CountAsync();
    }

    private async Task<string> GeneratePropertyNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Properties.CountAsync(p => p.CreatedAt.Year == year);
        return $"PROP-{year}-{(count + 1):D6}";
    }

    private PropertyDto MapToDto(Property property)
    {
        return new PropertyDto
        {
            PropertyId = property.PropertyId,
            OwnerId = property.OwnerId,
            OwnerName = $"{property.Owner.FirstName} {property.Owner.LastName}",
            PropertyType = property.PropertyType.ToString().ToLower(),
            PropertyNumber = property.PropertyNumber,
            TitleNumber = property.TitleNumber,
            AddressLine1 = property.AddressLine1,
            AddressLine2 = property.AddressLine2,
            RegionName = property.Region.RegionName,
            ProvinceName = property.Province.ProvinceName,
            CityName = property.City.CityName,
            BarangayName = property.Barangay.BarangayName,
            PostalCode = property.PostalCode,
            LotArea = property.LotArea,
            FloorArea = property.FloorArea,
            MarketValue = property.MarketValue,
            AssessedValue = property.AssessedValue,
            YearAcquired = property.YearAcquired,
            RegistrationDate = property.RegistrationDate,
            Status = property.Status.ToString().ToLower(),
            CreatedAt = property.CreatedAt
        };
    }
}
