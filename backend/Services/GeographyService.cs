using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class GeographyService : IGeographyService
{
    private readonly ApplicationDbContext _context;

    public GeographyService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Region>> GetRegionsAsync()
    {
        return await _context.Regions
            .OrderBy(r => r.RegionName)
            .ToListAsync();
    }

    public async Task<List<Province>> GetProvincesAsync(int? regionId = null)
    {
        var query = _context.Provinces.Include(p => p.Region).AsQueryable();

        if (regionId.HasValue)
        {
            query = query.Where(p => p.RegionId == regionId.Value);
        }

        return await query
            .OrderBy(p => p.ProvinceName)
            .ToListAsync();
    }

    public async Task<List<City>> GetCitiesAsync(int? provinceId = null)
    {
        var query = _context.Cities.Include(c => c.Province).AsQueryable();

        if (provinceId.HasValue)
        {
            query = query.Where(c => c.ProvinceId == provinceId.Value);
        }

        return await query
            .OrderBy(c => c.CityName)
            .ToListAsync();
    }

    public async Task<List<Barangay>> GetBarangaysAsync(int? cityId = null)
    {
        var query = _context.Barangays.Include(b => b.City).AsQueryable();

        if (cityId.HasValue)
        {
            query = query.Where(b => b.CityId == cityId.Value);
        }

        return await query
            .OrderBy(b => b.BarangayName)
            .ToListAsync();
    }
}
