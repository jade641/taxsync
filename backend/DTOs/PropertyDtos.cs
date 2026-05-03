using System.ComponentModel.DataAnnotations;
using backend.Models;

namespace backend.DTOs;

public class PropertyDto
{
    public int PropertyId { get; set; }
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string PropertyType { get; set; } = string.Empty;
    public string? PropertyNumber { get; set; }
    public string? TitleNumber { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string RegionName { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public string BarangayName { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public decimal? LotArea { get; set; }
    public decimal? FloorArea { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? AssessedValue { get; set; }
    public int? YearAcquired { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreatePropertyRequest
{
    [Required]
    public int OwnerId { get; set; }
    
    [Required]
    public PropertyType PropertyType { get; set; }
    
    public string? PropertyNumber { get; set; }
    public string? TitleNumber { get; set; }
    
    [Required]
    public string AddressLine1 { get; set; } = string.Empty;
    
    public string? AddressLine2 { get; set; }
    
    [Required]
    public int RegionId { get; set; }
    
    [Required]
    public int ProvinceId { get; set; }
    
    [Required]
    public int CityId { get; set; }
    
    [Required]
    public int BarangayId { get; set; }
    
    public string? PostalCode { get; set; }
    public decimal? LotArea { get; set; }
    public decimal? FloorArea { get; set; }
    
    [Required]
    public decimal MarketValue { get; set; }
    
    [Required]
    public decimal AssessedValue { get; set; }
    
    public int? YearAcquired { get; set; }
}

public class UpdatePropertyRequest
{
    public PropertyType? PropertyType { get; set; }
    public string? TitleNumber { get; set; }
    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public int? BarangayId { get; set; }
    public string? PostalCode { get; set; }
    public decimal? LotArea { get; set; }
    public decimal? FloorArea { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? AssessedValue { get; set; }
    public PropertyStatus? Status { get; set; }
}
