using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Region> Regions { get; set; }
    public DbSet<Province> Provinces { get; set; }
    public DbSet<City> Cities { get; set; }
    public DbSet<Barangay> Barangays { get; set; }
    public DbSet<Property> Properties { get; set; }
    public DbSet<PropertyDocument> PropertyDocuments { get; set; }
    public DbSet<TaxRate> TaxRates { get; set; }
    public DbSet<TaxAssessment> TaxAssessments { get; set; }
    public DbSet<Payment> Payments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.UserId);
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(20);
            entity.Property(e => e.Role).HasColumnName("role").HasConversion<string>();
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.EmailVerified).HasColumnName("email_verified");
            entity.Property(e => e.ProfileImage).HasColumnName("profile_image");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.LastLogin).HasColumnName("last_login");
        });

        // Region configuration
        modelBuilder.Entity<Region>(entity =>
        {
            entity.ToTable("regions");
            entity.HasKey(e => e.RegionId);
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.RegionCode).HasColumnName("region_code").HasMaxLength(20);
            entity.Property(e => e.RegionName).HasColumnName("region_name").HasMaxLength(100);
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        // Province configuration
        modelBuilder.Entity<Province>(entity =>
        {
            entity.ToTable("provinces");
            entity.HasKey(e => e.ProvinceId);
            entity.Property(e => e.ProvinceId).HasColumnName("province_id");
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.ProvinceCode).HasColumnName("province_code").HasMaxLength(20);
            entity.Property(e => e.ProvinceName).HasColumnName("province_name").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            entity.HasOne(e => e.Region)
                .WithMany(r => r.Provinces)
                .HasForeignKey(e => e.RegionId);
        });

        // City configuration
        modelBuilder.Entity<City>(entity =>
        {
            entity.ToTable("cities");
            entity.HasKey(e => e.CityId);
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.ProvinceId).HasColumnName("province_id");
            entity.Property(e => e.CityCode).HasColumnName("city_code").HasMaxLength(20);
            entity.Property(e => e.CityName).HasColumnName("city_name").HasMaxLength(100);
            entity.Property(e => e.CityType).HasColumnName("city_type").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            entity.HasOne(e => e.Province)
                .WithMany(p => p.Cities)
                .HasForeignKey(e => e.ProvinceId);
        });

        // Barangay configuration
        modelBuilder.Entity<Barangay>(entity =>
        {
            entity.ToTable("barangays");
            entity.HasKey(e => e.BarangayId);
            entity.Property(e => e.BarangayId).HasColumnName("barangay_id");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.BarangayCode).HasColumnName("barangay_code").HasMaxLength(20);
            entity.Property(e => e.BarangayName).HasColumnName("barangay_name").HasMaxLength(100);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            entity.HasOne(e => e.City)
                .WithMany(c => c.Barangays)
                .HasForeignKey(e => e.CityId);
        });

        // Property configuration
        modelBuilder.Entity<Property>(entity =>
        {
            entity.ToTable("properties");
            entity.HasKey(e => e.PropertyId);
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");
            entity.Property(e => e.PropertyType).HasColumnName("property_type").HasConversion<string>();
            entity.Property(e => e.PropertyNumber).HasColumnName("property_number").HasMaxLength(50);
            entity.Property(e => e.TitleNumber).HasColumnName("title_number").HasMaxLength(100);
            entity.Property(e => e.AddressLine1).HasColumnName("address_line1").HasMaxLength(255);
            entity.Property(e => e.AddressLine2).HasColumnName("address_line2").HasMaxLength(255);
            entity.Property(e => e.RegionId).HasColumnName("region_id");
            entity.Property(e => e.ProvinceId).HasColumnName("province_id");
            entity.Property(e => e.CityId).HasColumnName("city_id");
            entity.Property(e => e.BarangayId).HasColumnName("barangay_id");
            entity.Property(e => e.PostalCode).HasColumnName("postal_code").HasMaxLength(10);
            entity.Property(e => e.LotArea).HasColumnName("lot_area").HasPrecision(12, 2);
            entity.Property(e => e.FloorArea).HasColumnName("floor_area").HasPrecision(12, 2);
            entity.Property(e => e.MarketValue).HasColumnName("market_value").HasPrecision(15, 2);
            entity.Property(e => e.AssessedValue).HasColumnName("assessed_value").HasPrecision(15, 2);
            entity.Property(e => e.YearAcquired).HasColumnName("year_acquired");
            entity.Property(e => e.RegistrationDate).HasColumnName("registration_date");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Owner)
                .WithMany()
                .HasForeignKey(e => e.OwnerId);
            
            entity.HasOne(e => e.Region)
                .WithMany()
                .HasForeignKey(e => e.RegionId);
            
            entity.HasOne(e => e.Province)
                .WithMany()
                .HasForeignKey(e => e.ProvinceId);
            
            entity.HasOne(e => e.City)
                .WithMany()
                .HasForeignKey(e => e.CityId);
            
            entity.HasOne(e => e.Barangay)
                .WithMany()
                .HasForeignKey(e => e.BarangayId);
        });

        // PropertyDocument configuration
        modelBuilder.Entity<PropertyDocument>(entity =>
        {
            entity.ToTable("property_documents");
            entity.HasKey(e => e.DocumentId);
            entity.Property(e => e.DocumentId).HasColumnName("document_id");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.DocumentType).HasColumnName("document_type").HasConversion<string>();
            entity.Property(e => e.DocumentName).HasColumnName("document_name").HasMaxLength(255);
            entity.Property(e => e.FilePath).HasColumnName("file_path").HasMaxLength(255);
            entity.Property(e => e.FileSize).HasColumnName("file_size");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");
            entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at");
            
            entity.HasOne(e => e.Property)
                .WithMany(p => p.Documents)
                .HasForeignKey(e => e.PropertyId);
            
            entity.HasOne(e => e.Uploader)
                .WithMany()
                .HasForeignKey(e => e.UploadedBy);
        });

        // TaxRate configuration
        modelBuilder.Entity<TaxRate>(entity =>
        {
            entity.ToTable("tax_rates");
            entity.HasKey(e => e.RateId);
            entity.Property(e => e.RateId).HasColumnName("rate_id");
            entity.Property(e => e.PropertyType).HasColumnName("property_type").HasConversion<string>();
            entity.Property(e => e.RatePercentage).HasColumnName("rate_percentage").HasPrecision(5, 4);
            entity.Property(e => e.EffectiveFrom).HasColumnName("effective_from");
            entity.Property(e => e.EffectiveTo).HasColumnName("effective_to");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy);
        });

        // TaxAssessment configuration
        modelBuilder.Entity<TaxAssessment>(entity =>
        {
            entity.ToTable("tax_assessments");
            entity.HasKey(e => e.AssessmentId);
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.PropertyId).HasColumnName("property_id");
            entity.Property(e => e.TaxYear).HasColumnName("tax_year");
            entity.Property(e => e.Quarter).HasColumnName("quarter");
            entity.Property(e => e.AssessedValue).HasColumnName("assessed_value").HasPrecision(15, 2);
            entity.Property(e => e.TaxRate).HasColumnName("tax_rate").HasPrecision(5, 4);
            entity.Property(e => e.BasicTax).HasColumnName("basic_tax").HasPrecision(15, 2);
            entity.Property(e => e.SefTax).HasColumnName("sef_tax").HasPrecision(15, 2);
            entity.Property(e => e.Penalties).HasColumnName("penalties").HasPrecision(15, 2);
            entity.Property(e => e.Discounts).HasColumnName("discounts").HasPrecision(15, 2);
            entity.Property(e => e.TotalAmount).HasColumnName("total_amount").HasPrecision(15, 2);
            entity.Property(e => e.DueDate).HasColumnName("due_date");
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.AssessedBy).HasColumnName("assessed_by");
            entity.Property(e => e.ApprovedBy).HasColumnName("approved_by");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Property)
                .WithMany(p => p.TaxAssessments)
                .HasForeignKey(e => e.PropertyId);
            
            entity.HasOne(e => e.Assessor)
                .WithMany()
                .HasForeignKey(e => e.AssessedBy);
            
            entity.HasOne(e => e.Approver)
                .WithMany()
                .HasForeignKey(e => e.ApprovedBy);
        });

        // Payment configuration
        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(e => e.PaymentId);
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.AssessmentId).HasColumnName("assessment_id");
            entity.Property(e => e.PayerId).HasColumnName("payer_id");
            entity.Property(e => e.PaymentReference).HasColumnName("payment_reference").HasMaxLength(100);
            entity.Property(e => e.PaymentMethod).HasColumnName("payment_method").HasConversion<string>();
            entity.Property(e => e.AmountPaid).HasColumnName("amount_paid").HasPrecision(15, 2);
            entity.Property(e => e.PaymentDate).HasColumnName("payment_date");
            entity.Property(e => e.TransactionId).HasColumnName("transaction_id").HasMaxLength(100);
            entity.Property(e => e.BankName).HasColumnName("bank_name").HasMaxLength(100);
            entity.Property(e => e.CheckNumber).HasColumnName("check_number").HasMaxLength(50);
            entity.Property(e => e.Status).HasColumnName("status").HasConversion<string>();
            entity.Property(e => e.ReceiptNumber).HasColumnName("receipt_number").HasMaxLength(100);
            entity.Property(e => e.ProcessedBy).HasColumnName("processed_by");
            entity.Property(e => e.Notes).HasColumnName("notes");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            
            entity.HasOne(e => e.Assessment)
                .WithMany(a => a.Payments)
                .HasForeignKey(e => e.AssessmentId);
            
            entity.HasOne(e => e.Payer)
                .WithMany()
                .HasForeignKey(e => e.PayerId);
            
            entity.HasOne(e => e.Processor)
                .WithMany()
                .HasForeignKey(e => e.ProcessedBy);
        });

        // AuditLog configuration
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("activity_logs");
            entity.HasKey(e => e.LogId);
            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Action).HasColumnName("action").HasMaxLength(100);
            entity.Property(e => e.Module).HasColumnName("module").HasMaxLength(50);
            entity.Property(e => e.Severity).HasColumnName("severity").HasConversion<string>();
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasColumnName("user_agent");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId);
        });
    }
}
