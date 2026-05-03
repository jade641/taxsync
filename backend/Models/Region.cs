namespace backend.Models;

public class Region
{
    public int RegionId { get; set; }
    public string RegionCode { get; set; } = string.Empty;
    public string RegionName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public ICollection<Province> Provinces { get; set; } = new List<Province>();
}

public class Province
{
    public int ProvinceId { get; set; }
    public int RegionId { get; set; }
    public string ProvinceCode { get; set; } = string.Empty;
    public string ProvinceName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public Region Region { get; set; } = null!;
    public ICollection<City> Cities { get; set; } = new List<City>();
}

public class City
{
    public int CityId { get; set; }
    public int ProvinceId { get; set; }
    public string CityCode { get; set; } = string.Empty;
    public string CityName { get; set; } = string.Empty;
    public CityType CityType { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Province Province { get; set; } = null!;
    public ICollection<Barangay> Barangays { get; set; } = new List<Barangay>();
}

public class Barangay
{
    public int BarangayId { get; set; }
    public int CityId { get; set; }
    public string BarangayCode { get; set; } = string.Empty;
    public string BarangayName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    
    public City City { get; set; } = null!;
}

public enum CityType
{
    City,
    Municipality
}
