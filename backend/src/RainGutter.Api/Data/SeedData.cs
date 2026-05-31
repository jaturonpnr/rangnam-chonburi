using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;

namespace RainGutter.Api.Data;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        // Products — reseed when count doesn't match expected (schema changed: removed Finish)
        if (db.GutterProducts.Count() != 6)
        {
            db.GutterProducts.RemoveRange(db.GutterProducts);
            db.GutterProducts.AddRange(
                new GutterProduct { Material = Material.Galvanized, SizeInches = 4, PricePerMeter = 350 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 5, PricePerMeter = 450 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 6, PricePerMeter = 550 },
                new GutterProduct { Material = Material.Galvanized, SizeInches = 8, PricePerMeter = 750 },
                new GutterProduct { Material = Material.Stainless, SizeInches = 6, PricePerMeter = 850 },
                new GutterProduct { Material = Material.Stainless, SizeInches = 8, PricePerMeter = 1350 }
            );
        }

        if (!db.BuildingTypes.Any())
        {
            db.BuildingTypes.AddRange(
                new BuildingType { Label = "บ้านพักอาศัย / ทาวน์เฮ้าส์", SizeInches = 6, DisplayOrder = 1 },
                new BuildingType { Label = "อาคารพาณิชย์ / โกดัง", SizeInches = 8, DisplayOrder = 2 }
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

        // Zones — reseed when count doesn't match expected (3 Chonburi zones)
        if (db.ServiceZones.Count() != 3)
        {
            // Null out FK references on leads before deleting zones
            await db.Leads.Where(l => l.ServiceZoneId != null)
                .ExecuteUpdateAsync(s => s.SetProperty(l => l.ServiceZoneId, (int?)null));
            db.ServiceZones.RemoveRange(db.ServiceZones);
            await db.SaveChangesAsync();
            db.ServiceZones.AddRange(
                new ServiceZone { Name = "เมืองชลบุรี", TravelSurcharge = 0 },
                new ServiceZone { Name = "ศรีราชา", TravelSurcharge = 0 },
                new ServiceZone { Name = "พัทยา", TravelSurcharge = 0 }
            );
        }

        if (!db.ShopProfiles.Any())
        {
            db.ShopProfiles.Add(new ShopProfile
            {
                Id = 1,
                ShopName = "ส.จาตุรนต์ รางน้ำ",
                Phone = "0814569272",
                Address = "กรุณาอัปเดตที่อยู่ร้านในหน้า admin",
                LineOaLink = "",
                QuoteValidityDays = 30,
                QuoteFooterNote = "ขอบคุณที่ไว้วางใจใช้บริการของเรา"
            });
        }
        else
        {
            // Always update shop name/phone to latest
            var shop = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (shop != null)
            {
                shop.ShopName = "ส.จาตุรนต์ รางน้ำ";
                shop.Phone = "0814569272";
            }
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
