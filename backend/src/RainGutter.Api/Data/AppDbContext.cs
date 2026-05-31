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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PricingConfig>().HasKey(c => c.Id);
        modelBuilder.Entity<ShopProfile>().HasKey(s => s.Id);

        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.BreakdownJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.MeasuredGeoJson)
            .HasColumnType("jsonb");

        modelBuilder.Entity<Lead>()
            .HasOne(l => l.ServiceZone)
            .WithMany()
            .HasForeignKey(l => l.ServiceZoneId)
            .IsRequired(false);

        modelBuilder.Entity<QuoteRequest>()
            .HasOne(q => q.Lead)
            .WithMany()
            .HasForeignKey(q => q.LeadId);

        // Store enums as strings for readability
        modelBuilder.Entity<GutterProduct>()
            .Property(p => p.Material).HasConversion<string>();
        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.Material).HasConversion<string>();
        modelBuilder.Entity<QuoteRequest>()
            .Property(q => q.Status).HasConversion<string>();

        modelBuilder.Entity<QuoteRequest>()
            .HasOne<BuildingType>()
            .WithMany()
            .HasForeignKey(q => q.BuildingTypeId)
            .IsRequired(false);

        // Unique index on QuoteNumber
        modelBuilder.Entity<QuoteRequest>()
            .HasIndex(q => q.QuoteNumber)
            .IsUnique();
    }
}
