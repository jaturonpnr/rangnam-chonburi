using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Models;

namespace RainGutter.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<GutterProduct> GutterProducts => Set<GutterProduct>();
    public DbSet<BuildingType> BuildingTypes => Set<BuildingType>();
    public DbSet<PricingConfig> PricingConfigs => Set<PricingConfig>();
    public DbSet<ServiceZone> ServiceZones => Set<ServiceZone>();
    public DbSet<ShopProfile> ShopProfiles => Set<ShopProfile>();
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<QuoteRequest> QuoteRequests => Set<QuoteRequest>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobPhoto> JobPhotos => Set<JobPhoto>();
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<ImportBatch> ImportBatches => Set<ImportBatch>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PricingConfig>().HasKey(c => c.Id);
        modelBuilder.Entity<ShopProfile>().HasKey(s => s.Id);

        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.BreakdownJson).HasColumnType("jsonb");
        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.MeasuredGeoJson).HasColumnType("jsonb");

        modelBuilder.Entity<Lead>()
            .HasOne(l => l.ServiceZone).WithMany()
            .HasForeignKey(l => l.ServiceZoneId).IsRequired(false);

        modelBuilder.Entity<QuoteRequest>()
            .HasOne(q => q.Lead).WithMany()
            .HasForeignKey(q => q.LeadId);

        modelBuilder.Entity<GutterProduct>()
            .Property(p => p.Material).HasConversion<string>();
        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.Material).HasConversion<string>();
        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.Status).HasConversion<string>();

        modelBuilder.Entity<QuoteRequest>()
            .HasOne<BuildingType>().WithMany()
            .HasForeignKey(q => q.BuildingTypeId).IsRequired(false);
        modelBuilder.Entity<QuoteRequest>()
            .HasIndex(q => q.QuoteNumber).IsUnique();

        // Job relationships
        modelBuilder.Entity<Job>()
            .HasOne(j => j.QuoteRequest).WithMany()
            .HasForeignKey(j => j.QuoteRequestId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Job>()
            .HasOne(j => j.ImportBatch).WithMany(b => b.Jobs)
            .HasForeignKey(j => j.ImportBatchId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<Job>()
            .HasIndex(j => j.WarrantyNumber).IsUnique();
        modelBuilder.Entity<Job>()
            .HasIndex(j => j.PublicToken).IsUnique();

        modelBuilder.Entity<JobPhoto>()
            .HasOne(p => p.Job).WithMany(j => j.Photos)
            .HasForeignKey(p => p.JobId);

        modelBuilder.Entity<ServiceRequest>()
            .HasOne(s => s.Job).WithMany(j => j.ServiceRequests)
            .HasForeignKey(s => s.JobId);

        // Enum conversions
        modelBuilder.Entity<Job>()
            .Property(j => j.Material).HasConversion<string>();
        modelBuilder.Entity<Job>()
            .Property(j => j.Source).HasConversion<string>();
        modelBuilder.Entity<JobPhoto>()
            .Property(p => p.Type).HasConversion<string>();
        modelBuilder.Entity<ServiceRequest>()
            .Property(s => s.Type).HasConversion<string>();
        modelBuilder.Entity<ServiceRequest>()
            .Property(s => s.Status).HasConversion<string>();
    }
}
