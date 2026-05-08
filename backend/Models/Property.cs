namespace backend.Models;

public class Property
{
    public int PropertyId { get; set; }
    public int OwnerId { get; set; }
    public PropertyType PropertyType { get; set; }
    public string? PropertyNumber { get; set; }
    public string? TitleNumber { get; set; }
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public int RegionId { get; set; }
    public int ProvinceId { get; set; }
    public int CityId { get; set; }
    public int BarangayId { get; set; }
    public string? PostalCode { get; set; }
    public decimal? LotArea { get; set; }
    public decimal? FloorArea { get; set; }
    public decimal? MarketValue { get; set; }
    public decimal? AssessedValue { get; set; }
    public int? YearAcquired { get; set; }
    public DateTime? RegistrationDate { get; set; }
    public PropertyStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public ApplicationUser Owner { get; set; } = null!;
    public Region Region { get; set; } = null!;
    public Province Province { get; set; } = null!;
    public City City { get; set; } = null!;
    public Barangay Barangay { get; set; } = null!;
    public ICollection<PropertyDocument> Documents { get; set; } = new List<PropertyDocument>();
    public ICollection<TaxAssessment> TaxAssessments { get; set; } = new List<TaxAssessment>();
}

public class PropertyDocument
{
    public int DocumentId { get; set; }
    public int PropertyId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int? FileSize { get; set; }
    public int? UploadedBy { get; set; }
    public DateTime UploadedAt { get; set; }
    
    public Property Property { get; set; } = null!;
    public ApplicationUser? Uploader { get; set; }
}

public enum PropertyType
{
    Residential,
    Commercial,
    Industrial,
    Agricultural,
    MixedUse
}

public enum PropertyStatus
{
    Active,
    Inactive,
    Pending,
    Archived
}

public enum DocumentType
{
    Title,
    TaxDeclaration,
    DeedOfSale,
    SurveyPlan,
    Other
}
