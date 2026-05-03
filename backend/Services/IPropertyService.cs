using backend.DTOs;
using backend.Models;

namespace backend.Services;

public interface IPropertyService
{
    Task<PropertyDto> CreatePropertyAsync(CreatePropertyRequest request, int createdBy);
    Task<PropertyDto?> GetPropertyByIdAsync(int propertyId);
    Task<List<PropertyDto>> GetPropertiesAsync(int? ownerId = null, PropertyType? propertyType = null, PropertyStatus? status = null, int page = 1, int pageSize = 50);
    Task<PropertyDto?> UpdatePropertyAsync(int propertyId, UpdatePropertyRequest request, int updatedBy);
    Task<bool> DeletePropertyAsync(int propertyId, int deletedBy);
    Task<bool> UpdatePropertyStatusAsync(int propertyId, PropertyStatus status, int updatedBy);
    Task<int> GetPropertiesCountAsync(int? ownerId = null, PropertyType? propertyType = null, PropertyStatus? status = null);
}
