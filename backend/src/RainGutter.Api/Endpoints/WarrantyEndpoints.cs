using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class WarrantyEndpoints
{
    public static void MapWarrantyEndpoints(this WebApplication app)
    {
        // GET /api/warranty/{token} — public, no PII
        app.MapGet("/api/warranty/{token}", async (string token, AppDbContext db) =>
        {
            var job = await db.Jobs
                .Include(j => j.Photos.OrderBy(p => p.DisplayOrder))
                .FirstOrDefaultAsync(j => j.PublicToken == token);
            if (job is null) return Results.NotFound();

            var shop = await db.ShopProfiles.FindAsync(1);
            if (shop is null) return Results.StatusCode(500);

            // Privacy: returns only warranty/install info + shop contact. No Lat/Lng, no customer PII.
            return Results.Ok(new WarrantyCardDto(
                job.WarrantyNumber,
                job.InstalledDate?.ToString("yyyy-MM-dd"),
                job.InstalledDate.HasValue && job.WarrantyMonths.HasValue
                    ? job.InstalledDate.Value.AddMonths(job.WarrantyMonths.Value).ToString("yyyy-MM-dd")
                    : null,
                job.Material, job.SizeInches, job.LengthMeters, job.DownspoutCount,
                job.Photos.Select(p => new JobPhotoDto(p.Id, p.Url, p.Type, p.Caption, p.DisplayOrder)).ToList(),
                shop.ShopName, shop.Phone, shop.LineOaLink
            ));
        });

        // POST /api/warranty/{token}/service-request — public
        app.MapPost("/api/warranty/{token}/service-request", async (
            string token, CreateServiceRequestBody body,
            AppDbContext db, ILineNotificationService line) =>
        {
            if (string.IsNullOrWhiteSpace(body.ContactPhone))
                return Results.BadRequest(new { error = "กรุณาระบุเบอร์ติดต่อ" });

            var job = await db.Jobs.FirstOrDefaultAsync(j => j.PublicToken == token);
            if (job is null) return Results.NotFound();

            var sr = new ServiceRequest
            {
                JobId = job.Id,
                ContactPhone = body.ContactPhone,
                CustomerNote = body.CustomerNote,
                Type = body.Type
            };
            db.ServiceRequests.Add(sr);
            await db.SaveChangesAsync();

            _ = line.SendServiceRequestNotificationAsync(sr, job);
            return Results.Ok(new { sr.Id });
        });

        // GET /api/portfolio — public, ApproxLat/Lng only
        app.MapGet("/api/portfolio", async (AppDbContext db) =>
        {
            var jobs = await db.Jobs
                .Include(j => j.Photos.OrderBy(p => p.DisplayOrder))
                .Where(j => j.ShowInPortfolio && j.ApproxLat != null && j.ApproxLng != null)
                .ToListAsync();

            // Privacy: only ApproxLat/ApproxLng returned. No Lat/Lng, no customer name/phone/address.
            var pins = jobs.Select(j => new PortfolioPinDto(
                j.Id, j.ApproxLat!.Value, j.ApproxLng!.Value, j.AreaName,
                j.Material, j.InstalledDate?.ToString("yyyy-MM-dd"),
                j.PhotoConsent
                    ? j.Photos.Select(p => new JobPhotoDto(p.Id, p.Url, p.Type, p.Caption, p.DisplayOrder)).ToList()
                    : new List<JobPhotoDto>()
            )).ToList();
            return Results.Ok(pins);
        });

        // GET /api/portfolio/summary — public
        app.MapGet("/api/portfolio/summary", async (AppDbContext db) =>
        {
            var total = await db.Jobs.CountAsync(j => j.ShowInPortfolio);
            var byArea = await db.Jobs
                .Where(j => j.ShowInPortfolio && j.AreaName != null)
                .GroupBy(j => j.AreaName!)
                .Select(g => new AreaCountDto(g.Key, g.Count()))
                .ToListAsync();
            return Results.Ok(new PortfolioSummaryDto(total, byArea));
        });
    }
}
