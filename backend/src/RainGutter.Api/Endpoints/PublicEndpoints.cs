using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class PublicEndpoints
{
    public static void MapPublicEndpoints(this WebApplication app)
    {
        app.MapGet("/api/products", async (AppDbContext db) =>
        {
            var products = await db.GutterProducts
                .Where(p => p.IsActive)
                .OrderBy(p => p.Material)
                .ThenBy(p => p.SizeInches)
                .ToListAsync();
            return Results.Ok(products);
        });

        app.MapGet("/api/building-types", async (AppDbContext db) =>
        {
            var types = await db.BuildingTypes
                .Where(b => b.IsActive)
                .OrderBy(b => b.DisplayOrder)
                .ToListAsync();
            return Results.Ok(types);
        });

        app.MapGet("/api/zones", async (AppDbContext db) =>
        {
            var zones = await db.ServiceZones
                .Where(z => z.IsActive)
                .OrderBy(z => z.Name)
                .ToListAsync();
            return Results.Ok(zones);
        });

        app.MapGet("/api/shop-profile", async (AppDbContext db) =>
        {
            var profile = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (profile is null) return Results.NotFound();
            return Results.Ok(new { profile.ShopName, profile.Phone, profile.LineOaLink });
        });

        app.MapPost("/api/estimate", async (EstimateBody req, AppDbContext db, IPricingService pricing) =>
        {
            if (req.LengthMeters <= 0)
                return Results.BadRequest(new { error = "จำนวนเมตรต้องมากกว่า 0" });
            if (req.DownspoutCount < 0)
                return Results.BadRequest(new { error = "จำนวนท่อน้ำลงต้องไม่ติดลบ" });
            if (req.Floors < 1)
                return Results.BadRequest(new { error = "จำนวนชั้นต้องมากกว่า 0" });

            var buildingType = await db.BuildingTypes.FirstOrDefaultAsync(b => b.Id == req.BuildingTypeId && b.IsActive);
            if (buildingType is null)
                return Results.BadRequest(new { error = "ไม่พบประเภทอาคารที่ระบุ" });

            var product = await db.GutterProducts.FirstOrDefaultAsync(p =>
                p.IsActive &&
                p.Material == req.Material &&
                p.SizeInches == buildingType.SizeInches);

            if (product is null)
                return Results.BadRequest(new { error = "ไม่พบสินค้าที่ตรงกับเงื่อนไข" });

            var config = await db.PricingConfigs.FirstOrDefaultAsync(c => c.Id == 1);
            if (config is null)
                return Results.StatusCode(500);

            var zone = req.ServiceZoneId.HasValue
                ? await db.ServiceZones.FirstOrDefaultAsync(z => z.Id == req.ServiceZoneId && z.IsActive)
                : null;

            var estimateReq = new EstimateRequest(req.LengthMeters, req.DownspoutCount, req.Floors, req.RemoveOld, req.ServiceZoneId);
            var result = pricing.Calculate(estimateReq, config, product, zone);
            return Results.Ok(result);
        });
    }
}
