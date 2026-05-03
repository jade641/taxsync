using backend.Models;

namespace backend.Services;

public interface IGeographyService
{
    Task<List<Region>> GetRegionsAsync();
    Task<List<Province>> GetProvincesAsync(int? regionId = null);
    Task<List<City>> GetCitiesAsync(int? provinceId = null);
    Task<List<Barangay>> GetBarangaysAsync(int? cityId = null);
}
