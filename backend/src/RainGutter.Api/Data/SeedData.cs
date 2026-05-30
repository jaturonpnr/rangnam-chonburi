using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;

namespace RainGutter.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!db.GutterProducts.Any())
        {
            db.GutterProducts.AddRange(
                // Galvanized
                new GutterProduct { Material = Material.Galvanized, SizeInches = 4, PricePerMeter = 400 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 5, PricePerMeter = 450 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 6, PricePerMeter = 500 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 8, PricePerMeter = 600 },
                // Stainless
                new GutterProduct { Material = Material.Stainless, SizeInches = 6, Finish = Finish.Glossy, PricePerMeter = 850 },
                new GutterProduct { Material = Material.Stainless, SizeInches = 6, Finish = Finish.Matte, PricePerMeter = 800 },
                new GutterProduct { Material = Material.Stainless, SizeInches = 8, Finish = Finish.Glossy, PricePerMeter = 1350 },
                new GutterProduct { Material = Material.Stainless, SizeInches = 8, Finish = Finish.Matte, PricePerMeter = 1300 }
            );
        }

        if (!db.PricingConfigs.Any())
        {
            db.PricingConfigs.Add(new PricingConfig
            {
                Id = 1,
                MinimumMeters = 10,
                DownspoutPricePerPoint = 500,
                HeightSurchargePercent = 20,
                RemovalPricePerMeter = 60,
                SurveyFee = 1000
            });
        }

        if (!db.ServiceZones.Any())
        {
            db.ServiceZones.Add(new ServiceZone { Name = "พื้นที่หลัก", TravelSurcharge = 0 });
        }

        if (!db.ShopProfiles.Any())
        {
            db.ShopProfiles.Add(new ShopProfile
            {
                Id = 1,
                ShopName = "ร้านติดตั้งรางน้ำฝน",
                Phone = "000-000-0000",
                Address = "กรุณาอัปเดตที่อยู่ร้านในหน้า admin",
                LineOaLink = "",
                QuoteValidityDays = 30,
                QuoteFooterNote = "ขอบคุณที่ไว้วางใจใช้บริการของเรา"
            });
        }

        if (!db.AdminUsers.Any())
        {
            var username = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? "admin";
            var password = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "changeme123";
            db.AdminUsers.Add(new AdminUser
            {
                Username = username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password)
            });
        }

        await db.SaveChangesAsync();
    }
}
