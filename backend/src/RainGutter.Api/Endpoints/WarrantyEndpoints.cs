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

    }
}
