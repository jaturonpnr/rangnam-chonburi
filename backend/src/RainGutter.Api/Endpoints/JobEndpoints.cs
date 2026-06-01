using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class JobEndpoints
{
    public static void MapJobEndpoints(this WebApplication app)
    {
        // POST /api/admin/quote-requests/{id}/complete
        app.MapPost("/api/admin/quote-requests/{id}/complete", [Authorize] async (
            int id, CompleteJobRequest req, AppDbContext db) =>
        {
            var quote = await db.QuoteRequests
                .Include(q => q.Lead)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quote is null) return Results.NotFound();
            if (quote.Status != QuoteStatus.Won)
                return Results.BadRequest(new { error = "QuoteRequest ต้องมีสถานะ Won" });
            if (await db.Jobs.AnyAsync(j => j.QuoteRequestId == id))
                return Results.Conflict(new { error = "มี Job สำหรับ QuoteRequest นี้แล้ว" });

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var year = DateTime.UtcNow.Year;
            var existing = await db.Jobs
                .Where(j => j.WarrantyNumber.StartsWith($"WR-{year}-"))
                .Select(j => j.WarrantyNumber)
                .ToListAsync();
            var nextSeq = existing.Count > 0
                ? existing.Select(n => int.TryParse(n.Split('-').LastOrDefault(), out var v) ? v : 0).Max() + 1
                : 1;
            var warrantyNumber = $"WR-{year}-{nextSeq:D4}";

            double? lat = req.Lat ?? quote.MapCenterLat;
            double? lng = req.Lng ?? quote.MapCenterLng;

            double? approxLat = null, approxLng = null;
            if (lat.HasValue && lng.HasValue)
                (approxLat, approxLng) = JitterHelper.Jitter(lat.Value, lng.Value);

            var job = new Job
            {
                QuoteRequestId = id,
                WarrantyNumber = warrantyNumber,
                PublicToken = Guid.NewGuid().ToString("N"),
                InstalledDate = req.InstalledDate.ToUniversalTime(),
                WarrantyMonths = req.WarrantyMonths,
                Material = quote.Material,
                SizeInches = quote.SizeInches,
                LengthMeters = quote.LengthMeters,
                DownspoutCount = quote.DownspoutCount,
                Lat = lat,
                Lng = lng,
                ApproxLat = approxLat,
                ApproxLng = approxLng,
                AreaName = req.AreaName
            };
            db.Jobs.Add(job);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            return Results.Ok(MapJobDetail(job, quote.QuoteNumber));
        });

        // GET /api/admin/jobs
        app.MapGet("/api/admin/jobs", [Authorize] async (
            AppDbContext db, int page = 1, int pageSize = 20) =>
        {
            var total = await db.Jobs.CountAsync();
            var items = await db.Jobs
                .Include(j => j.QuoteRequest)
                .Include(j => j.ServiceRequests)
                .OrderByDescending(j => j.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Results.Ok(new
            {
                total,
                page,
                pageSize,
                items = items.Select(j => new JobSummaryDto(
                    j.Id, j.WarrantyNumber, j.QuoteRequest.QuoteNumber,
                    j.InstalledDate.ToString("yyyy-MM-dd"),
                    j.InstalledDate.AddMonths(j.WarrantyMonths).ToString("yyyy-MM-dd"),
                    j.Material, j.SizeInches, j.LengthMeters,
                    j.ShowInPortfolio, j.PhotoConsent,
                    j.ServiceRequests.Count))
            });
        });

        // GET /api/admin/jobs/{id}
        app.MapGet("/api/admin/jobs/{id}", [Authorize] async (int id, AppDbContext db) =>
        {
            var job = await db.Jobs
                .Include(j => j.QuoteRequest)
                .Include(j => j.Photos.OrderBy(p => p.DisplayOrder))
                .Include(j => j.ServiceRequests.OrderByDescending(s => s.CreatedAt))
                .FirstOrDefaultAsync(j => j.Id == id);
            if (job is null) return Results.NotFound();
            return Results.Ok(MapJobDetail(job, job.QuoteRequest.QuoteNumber));
        });

        // PUT /api/admin/jobs/{id}
        app.MapPut("/api/admin/jobs/{id}", [Authorize] async (
            int id, EditJobRequest req, AppDbContext db) =>
        {
            var job = await db.Jobs.FindAsync(id);
            if (job is null) return Results.NotFound();

            job.WarrantyMonths = req.WarrantyMonths;
            job.InstalledDate = req.InstalledDate.ToUniversalTime();
            job.AreaName = req.AreaName;
            job.ShowInPortfolio = req.ShowInPortfolio;
            job.PhotoConsent = req.PhotoConsent;

            if (req.Lat.HasValue && req.Lng.HasValue &&
                (req.Lat != job.Lat || req.Lng != job.Lng))
            {
                job.Lat = req.Lat;
                job.Lng = req.Lng;
                (job.ApproxLat, job.ApproxLng) = JitterHelper.Jitter(req.Lat.Value, req.Lng.Value);
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { job.Id });
        });

        // POST /api/admin/jobs/{id}/photos
        app.MapPost("/api/admin/jobs/{id}/photos", [Authorize] async (
            int id, HttpRequest request, AppDbContext db, IStorageService storage) =>
        {
            var job = await db.Jobs.FindAsync(id);
            if (job is null) return Results.NotFound();

            var form = await request.ReadFormAsync();
            var file = form.Files["file"];
            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "No file uploaded" });
            if (!Enum.TryParse<PhotoType>(form["type"], true, out var photoType))
                return Results.BadRequest(new { error = "Invalid photo type (Before/After/Other)" });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!new[] { ".jpg", ".jpeg", ".png", ".webp" }.Contains(ext))
                return Results.BadRequest(new { error = "Unsupported file type" });

            var fileName = $"jobs/{id}/{Guid.NewGuid():N}{ext}";
            using var stream = file.OpenReadStream();
            var url = await storage.UploadAsync(fileName, stream, file.ContentType);

            var maxOrder = await db.JobPhotos
                .Where(p => p.JobId == id)
                .Select(p => (int?)p.DisplayOrder)
                .MaxAsync() ?? 0;

            var photo = new JobPhoto
            {
                JobId = id,
                Url = url,
                Type = photoType,
                Caption = string.IsNullOrEmpty(form["caption"]) ? null : form["caption"].ToString(),
                DisplayOrder = maxOrder + 1
            };
            db.JobPhotos.Add(photo);
            await db.SaveChangesAsync();
            return Results.Ok(new JobPhotoDto(photo.Id, photo.Url, photo.Type, photo.Caption, photo.DisplayOrder));
        });

        // DELETE /api/admin/jobs/{id}/photos/{photoId}
        app.MapDelete("/api/admin/jobs/{id}/photos/{photoId}", [Authorize] async (
            int id, int photoId, AppDbContext db, IStorageService storage) =>
        {
            var photo = await db.JobPhotos.FirstOrDefaultAsync(p => p.Id == photoId && p.JobId == id);
            if (photo is null) return Results.NotFound();

            try
            {
                var uri = new Uri(photo.Url);
                var fileName = uri.AbsolutePath.TrimStart('/');
                await storage.DeleteAsync(fileName);
            }
            catch { /* storage delete failure should not block DB cleanup */ }

            db.JobPhotos.Remove(photo);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // GET /api/admin/jobs/{id}/qr
        app.MapGet("/api/admin/jobs/{id}/qr", [Authorize] async (
            int id, AppDbContext db, IQrService qr) =>
        {
            var job = await db.Jobs.FindAsync(id);
            if (job is null) return Results.NotFound();
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_PUBLIC_URL") ?? "http://localhost:4200";
            var png = qr.GenerateQrPng($"{frontendUrl}/w/{job.PublicToken}");
            return Results.File(png, "image/png", $"qr-{job.WarrantyNumber}.png");
        });

        // GET /api/admin/jobs/{id}/warranty-pdf
        app.MapGet("/api/admin/jobs/{id}/warranty-pdf", [Authorize] async (
            int id, AppDbContext db, IPdfService pdf, IQrService qr) =>
        {
            var job = await db.Jobs.FindAsync(id);
            if (job is null) return Results.NotFound();
            var shop = await db.ShopProfiles.FindAsync(1);
            if (shop is null) return Results.StatusCode(500);
            var frontendUrl = Environment.GetEnvironmentVariable("FRONTEND_PUBLIC_URL") ?? "http://localhost:4200";
            var qrPng = qr.GenerateQrPng($"{frontendUrl}/w/{job.PublicToken}");
            var pdfBytes = pdf.GenerateWarrantyPdf(job, shop, qrPng);
            return Results.File(pdfBytes, "application/pdf", $"warranty-{job.WarrantyNumber}.pdf");
        });

        // GET /api/admin/service-requests
        app.MapGet("/api/admin/service-requests", [Authorize] async (
            AppDbContext db, string? status) =>
        {
            var query = db.ServiceRequests.Include(s => s.Job).AsQueryable();
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ServiceRequestStatus>(status, out var st))
                query = query.Where(s => s.Status == st);
            var items = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
            return Results.Ok(items.Select(s => new
            {
                s.Id, s.ContactPhone, s.CustomerNote, s.Type, s.Status, s.CreatedAt,
                JobId = s.JobId, WarrantyNumber = s.Job.WarrantyNumber
            }));
        });

        // PUT /api/admin/service-requests/{id}/status
        app.MapPut("/api/admin/service-requests/{id}/status", [Authorize] async (
            int id, UpdateServiceRequestStatusRequest req, AppDbContext db) =>
        {
            var sr = await db.ServiceRequests.FindAsync(id);
            if (sr is null) return Results.NotFound();
            sr.Status = req.Status;
            await db.SaveChangesAsync();
            return Results.Ok(new { sr.Id, sr.Status });
        });
    }

    private static JobDetailDto MapJobDetail(Job job, string quoteNumber)
    {
        var expiry = job.InstalledDate.AddMonths(job.WarrantyMonths);
        return new JobDetailDto(
            job.Id, job.QuoteRequestId, quoteNumber,
            job.WarrantyNumber, job.PublicToken,
            job.InstalledDate.ToString("yyyy-MM-dd"), job.WarrantyMonths,
            expiry.ToString("yyyy-MM-dd"),
            job.Material, job.SizeInches, job.LengthMeters, job.DownspoutCount,
            job.Lat, job.Lng, job.ApproxLat, job.ApproxLng, job.AreaName,
            job.ShowInPortfolio, job.PhotoConsent,
            job.Photos.OrderBy(p => p.DisplayOrder)
                .Select(p => new JobPhotoDto(p.Id, p.Url, p.Type, p.Caption, p.DisplayOrder)).ToList(),
            job.ServiceRequests.OrderByDescending(s => s.CreatedAt)
                .Select(s => new ServiceRequestDto(s.Id, s.ContactPhone, s.CustomerNote, s.Type, s.Status, s.CreatedAt)).ToList()
        );
    }
}
