using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class AdminEndpoints
{
    public static void MapAdminEndpoints(this WebApplication app)
    {
        // Login
        app.MapPost("/api/admin/login", async (LoginRequest req, AppDbContext db) =>
        {
            var user = await db.AdminUsers.FirstOrDefaultAsync(u => u.Username == req.Username);
            if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
                return Results.Unauthorized();

            var secret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev-secret-change-in-production";
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                claims: [new Claim(ClaimTypes.Name, user.Username)],
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds);
            return Results.Ok(new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token)));
        });

        // Stats
        app.MapGet("/api/admin/stats", [Authorize] async (AppDbContext db) =>
        {
            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek).Date.ToUniversalTime();

            var quotes = await db.QuoteRequests
                .Include(q => q.Lead).ThenInclude(l => l.ServiceZone)
                .ToListAsync();

            var byStatus = quotes
                .GroupBy(q => q.Status.ToString())
                .Select(g => new StatusCount(g.Key, g.Count()))
                .ToList();

            var byZone = quotes
                .GroupBy(q => q.Lead.ServiceZone?.Name ?? "ไม่ระบุ")
                .Select(g => new ZoneCount(g.Key, g.Count()))
                .ToList();

            var weeklySeries = Enumerable.Range(0, 12)
                .Select(i =>
                {
                    var weekStart = startOfWeek.AddDays(-7 * (11 - i));
                    var weekEnd = weekStart.AddDays(7);
                    var count = quotes.Count(q => q.CreatedAt >= weekStart && q.CreatedAt < weekEnd);
                    return new WeeklyCount(weekStart.ToString("dd/MM"), count);
                }).ToList();

            var topProducts = quotes
                .GroupBy(q => $"{(q.Material == Enums.Material.Galvanized ? "สังกะสี" : "สแตนเลส")} {q.SizeInches}\"")
                .OrderByDescending(g => g.Count())
                .Take(5)
                .Select(g => new TopProduct(g.Key, g.Count()))
                .ToList();

            var totalValue = quotes.Sum(q => q.EstimatedTotal);
            return Results.Ok(new StatsResponse(
                quotes.Count,
                quotes.Count(q => q.CreatedAt >= startOfMonth),
                quotes.Count(q => q.CreatedAt >= startOfWeek),
                totalValue,
                quotes.Count > 0 ? totalValue / quotes.Count : 0,
                byStatus, byZone, weeklySeries, topProducts
            ));
        });

        // Quote request list
        app.MapGet("/api/admin/quote-requests", [Authorize] async (
            AppDbContext db, string? status, DateTime? from, DateTime? to,
            int page = 1, int pageSize = 20) =>
        {
            var query = db.QuoteRequests.Include(q => q.Lead).AsQueryable();

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<Enums.QuoteStatus>(status, out var s))
                query = query.Where(q => q.Status == s);
            if (from.HasValue)
                query = query.Where(q => q.CreatedAt >= from.Value.ToUniversalTime());
            if (to.HasValue)
                query = query.Where(q => q.CreatedAt <= to.Value.ToUniversalTime());

            var total = await query.CountAsync();
            var items = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new QuoteRequestSummary(
                    q.Id, q.QuoteNumber, q.Lead.CustomerName, q.Lead.Phone,
                    q.EstimatedTotal, q.Status, q.CreatedAt))
                .ToListAsync();

            return Results.Ok(new { total, page, pageSize, items });
        });

        // Quote request detail
        app.MapGet("/api/admin/quote-requests/{id:int}", [Authorize] async (int id, AppDbContext db) =>
        {
            var q = await db.QuoteRequests
                .Include(x => x.Lead).ThenInclude(l => l.ServiceZone)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (q is null) return Results.NotFound();

            return Results.Ok(new QuoteRequestDetail(
                q.Id, q.QuoteNumber, q.Lead.CustomerName, q.Lead.Phone, q.Lead.Address,
                q.Lead.LocationDetail, q.Lead.ServiceZone?.Name, q.BuildingTypeLabelSnapshot,
                q.Material, q.SizeInches, q.LengthMeters, q.DownspoutCount, q.Floors, q.RemoveOld,
                q.EstimatedTotal, q.BreakdownJson, q.Status, q.CreatedAt,
                q.MeasureSource,
                q.MeasuredLengthMeters,
                q.MeasuredGeoJson,
                q.MapCenterLat,
                q.MapCenterLng,
                q.MapZoom));
        });

        // Update status
        app.MapPut("/api/admin/quote-requests/{id:int}/status", [Authorize] async (
            int id, UpdateStatusRequest req, AppDbContext db) =>
        {
            var quote = await db.QuoteRequests.FindAsync(id);
            if (quote is null) return Results.NotFound();
            quote.Status = req.Status;
            await db.SaveChangesAsync();
            return Results.Ok();
        });

        // PDF (admin)
        app.MapGet("/api/admin/quote-requests/{id:int}/pdf", [Authorize] async (
            int id, AppDbContext db, IPdfService pdf) =>
        {
            var quote = await db.QuoteRequests.Include(q => q.Lead).FirstOrDefaultAsync(q => q.Id == id);
            if (quote is null) return Results.NotFound();
            var shop = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (shop is null) return Results.StatusCode(500);
            var pdfBytes = pdf.GenerateQuotePdf(quote, quote.Lead, shop);
            return Results.File(pdfBytes, "application/pdf", $"{quote.QuoteNumber}.pdf");
        });

        // Products CRUD
        app.MapGet("/api/admin/products", [Authorize] async (AppDbContext db) =>
            Results.Ok(await db.GutterProducts.OrderBy(p => p.Material).ThenBy(p => p.SizeInches).ToListAsync()));

        app.MapPost("/api/admin/products", [Authorize] async (UpsertProductRequest req, AppDbContext db) =>
        {
            var product = new GutterProduct
            {
                Material = req.Material, SizeInches = req.SizeInches,
                PricePerMeter = req.PricePerMeter, IsActive = req.IsActive
            };
            db.GutterProducts.Add(product);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/products/{product.Id}", product);
        });

        app.MapPut("/api/admin/products/{id:int}", [Authorize] async (int id, UpsertProductRequest req, AppDbContext db) =>
        {
            var product = await db.GutterProducts.FindAsync(id);
            if (product is null) return Results.NotFound();
            product.Material = req.Material; product.SizeInches = req.SizeInches;
            product.PricePerMeter = req.PricePerMeter; product.IsActive = req.IsActive;
            await db.SaveChangesAsync();
            return Results.Ok(product);
        });

        app.MapDelete("/api/admin/products/{id:int}", [Authorize] async (int id, AppDbContext db) =>
        {
            var product = await db.GutterProducts.FindAsync(id);
            if (product is null) return Results.NotFound();
            db.GutterProducts.Remove(product);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Pricing config
        app.MapGet("/api/admin/config", [Authorize] async (AppDbContext db) =>
            Results.Ok(await db.PricingConfigs.FirstOrDefaultAsync(c => c.Id == 1)));

        app.MapPut("/api/admin/config", [Authorize] async (UpdateConfigRequest req, AppDbContext db) =>
        {
            var config = await db.PricingConfigs.FirstOrDefaultAsync(c => c.Id == 1);
            if (config is null) return Results.NotFound();
            config.MinimumMeters = req.MinimumMeters;
            config.DownspoutPricePerPoint = req.DownspoutPricePerPoint;
            config.HeightSurchargePercent = req.HeightSurchargePercent;
            config.RemovalPricePerMeter = req.RemovalPricePerMeter;
            config.SurveyFee = req.SurveyFee;
            await db.SaveChangesAsync();
            return Results.Ok(config);
        });

        // Zones CRUD
        app.MapGet("/api/admin/zones", [Authorize] async (AppDbContext db) =>
            Results.Ok(await db.ServiceZones.ToListAsync()));

        app.MapPost("/api/admin/zones", [Authorize] async (UpsertZoneRequest req, AppDbContext db) =>
        {
            var zone = new ServiceZone { Name = req.Name, TravelSurcharge = req.TravelSurcharge, IsActive = req.IsActive };
            db.ServiceZones.Add(zone);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/zones/{zone.Id}", zone);
        });

        app.MapPut("/api/admin/zones/{id:int}", [Authorize] async (int id, UpsertZoneRequest req, AppDbContext db) =>
        {
            var zone = await db.ServiceZones.FindAsync(id);
            if (zone is null) return Results.NotFound();
            zone.Name = req.Name; zone.TravelSurcharge = req.TravelSurcharge; zone.IsActive = req.IsActive;
            await db.SaveChangesAsync();
            return Results.Ok(zone);
        });

        app.MapDelete("/api/admin/zones/{id:int}", [Authorize] async (int id, AppDbContext db) =>
        {
            var zone = await db.ServiceZones.FindAsync(id);
            if (zone is null) return Results.NotFound();
            db.ServiceZones.Remove(zone);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // Shop profile
        app.MapGet("/api/admin/shop-profile", [Authorize] async (AppDbContext db) =>
            Results.Ok(await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1)));

        app.MapPut("/api/admin/shop-profile", [Authorize] async (UpdateShopProfileRequest req, AppDbContext db) =>
        {
            var profile = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (profile is null) return Results.NotFound();
            profile.ShopName = req.ShopName; profile.Phone = req.Phone; profile.Address = req.Address;
            profile.LogoUrl = req.LogoUrl; profile.LineOaLink = req.LineOaLink;
            profile.QuoteValidityDays = req.QuoteValidityDays; profile.QuoteFooterNote = req.QuoteFooterNote;
            await db.SaveChangesAsync();
            return Results.Ok(profile);
        });

        // Building types CRUD
        app.MapGet("/api/admin/building-types", [Authorize] async (AppDbContext db) =>
            Results.Ok(await db.BuildingTypes.OrderBy(b => b.DisplayOrder).ToListAsync()));

        app.MapPost("/api/admin/building-types", [Authorize] async (UpsertBuildingTypeRequest req, AppDbContext db) =>
        {
            var bt = new Models.BuildingType
            {
                Label = req.Label, SizeInches = req.SizeInches,
                DisplayOrder = req.DisplayOrder, IsActive = req.IsActive
            };
            db.BuildingTypes.Add(bt);
            await db.SaveChangesAsync();
            return Results.Created($"/api/admin/building-types/{bt.Id}", bt);
        });

        app.MapPut("/api/admin/building-types/{id:int}", [Authorize] async (int id, UpsertBuildingTypeRequest req, AppDbContext db) =>
        {
            var bt = await db.BuildingTypes.FindAsync(id);
            if (bt is null) return Results.NotFound();
            bt.Label = req.Label; bt.SizeInches = req.SizeInches;
            bt.DisplayOrder = req.DisplayOrder; bt.IsActive = req.IsActive;
            await db.SaveChangesAsync();
            return Results.Ok(bt);
        });

        app.MapDelete("/api/admin/building-types/{id:int}", [Authorize] async (int id, AppDbContext db) =>
        {
            var bt = await db.BuildingTypes.FindAsync(id);
            if (bt is null) return Results.NotFound();
            db.BuildingTypes.Remove(bt);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
