using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class ImportEndpoints
{
    public static void MapImportEndpoints(this WebApplication app)
    {
        // POST /api/admin/portfolio/import/images
        // Upload 1-20 images → creates ImportBatch + one draft Job per photo
        app.MapPost("/api/admin/portfolio/import/images", [Authorize] async (
            HttpRequest request, AppDbContext db, IStorageService storage) =>
        {
            var form = await request.ReadFormAsync();
            var files = form.Files.GetFiles("files");
            if (files.Count == 0)
                return Results.BadRequest(new { error = "ต้องส่งไฟล์อย่างน้อย 1 ไฟล์" });
            if (files.Count > 20)
                return Results.BadRequest(new { error = "อัปโหลดได้สูงสุด 20 ไฟล์ต่อครั้ง" });

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                    return Results.BadRequest(new { error = $"ไม่รองรับไฟล์ {ext}" });
                if (f.Length > 10 * 1024 * 1024)
                    return Results.BadRequest(new { error = $"ไฟล์ {f.FileName} ใหญ่เกิน 10 MB" });
            }

            var batch = new ImportBatch { PhotoCount = files.Count };
            db.ImportBatches.Add(batch);
            await db.SaveChangesAsync();

            var jobs = new List<Job>();
            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName).ToLowerInvariant();
                var fileName = $"portfolio/import/{batch.Id}/{Guid.NewGuid():N}{ext}";
                using var stream = f.OpenReadStream();
                var url = await storage.UploadAsync(fileName, stream, f.ContentType);

                var job = new Job
                {
                    Source = JobSource.FacebookImport,
                    ImportBatchId = batch.Id,
                    Material = Material.Stainless,
                    SizeInches = 6,
                    LengthMeters = 0,
                    DownspoutCount = 0,
                    ShowInPortfolio = false,
                    PhotoConsent = true
                };
                db.Jobs.Add(job);
                await db.SaveChangesAsync();

                var photo = new JobPhoto
                {
                    JobId = job.Id,
                    Url = url,
                    Type = PhotoType.After,
                    DisplayOrder = 1
                };
                db.JobPhotos.Add(photo);
                jobs.Add(job);
            }

            await db.SaveChangesAsync();

            return Results.Ok(new
            {
                batchId = batch.Id,
                jobCount = jobs.Count
            });
        });

        // GET /api/admin/portfolio/imports
        // List all import batches
        app.MapGet("/api/admin/portfolio/imports", [Authorize] async (AppDbContext db) =>
        {
            var batches = await db.ImportBatches
                .Include(b => b.Jobs)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return Results.Ok(batches.Select(b => new ImportBatchSummaryDto(
                b.Id, b.Source, b.PhotoCount, b.Jobs.Count, b.CreatedAt)));
        });

        // GET /api/admin/portfolio/imports/{batchId}/drafts
        // List draft jobs for a batch
        app.MapGet("/api/admin/portfolio/imports/{batchId:int}/drafts", [Authorize] async (
            int batchId, AppDbContext db) =>
        {
            var jobs = await db.Jobs
                .Include(j => j.Photos.OrderBy(p => p.DisplayOrder))
                .Where(j => j.ImportBatchId == batchId)
                .OrderBy(j => j.Id)
                .ToListAsync();

            return Results.Ok(jobs.Select(j => new ImportDraftDto(
                j.Id,
                j.AreaName,
                j.Material, j.SizeInches, j.LengthMeters,
                j.ApproxLat, j.ApproxLng,
                j.ShowInPortfolio, j.PhotoConsent,
                j.Photos.Select(p => new JobPhotoDto(p.Id, p.Url, p.Type, p.Caption, p.DisplayOrder)).ToList(),
                j.ImportBatchId!.Value, j.CreatedAt)));
        });

        // PUT /api/admin/portfolio/imports/drafts/{jobId}
        // Edit metadata for a single draft job
        app.MapPut("/api/admin/portfolio/imports/drafts/{jobId:int}", [Authorize] async (
            int jobId, UpdateImportDraftRequest req, AppDbContext db) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j =>
                j.Id == jobId && j.Source == JobSource.FacebookImport);
            if (job is null) return Results.NotFound();

            job.AreaName = req.AreaName;
            job.Material = req.Material;
            job.SizeInches = req.SizeInches;
            job.LengthMeters = req.LengthMeters;
            job.ShowInPortfolio = req.ShowInPortfolio;
            job.PhotoConsent = req.PhotoConsent;

            if (req.Lat.HasValue && req.Lng.HasValue)
                (job.ApproxLat, job.ApproxLng) = JitterHelper.Jitter(req.Lat.Value, req.Lng.Value);

            await db.SaveChangesAsync();
            return Results.Ok(new { job.Id });
        });

        // POST /api/admin/portfolio/imports/{batchId}/publish
        // Set ShowInPortfolio=true for all drafts in batch that have PhotoConsent=true
        app.MapPost("/api/admin/portfolio/imports/{batchId:int}/publish", [Authorize] async (
            int batchId, AppDbContext db) =>
        {
            var jobs = await db.Jobs
                .Where(j => j.ImportBatchId == batchId && j.PhotoConsent)
                .ToListAsync();

            foreach (var job in jobs)
                job.ShowInPortfolio = true;

            await db.SaveChangesAsync();
            return Results.Ok(new { published = jobs.Count });
        });

        // DELETE /api/admin/portfolio/imports/drafts/{jobId}
        // Delete a single draft job (and its photos from storage)
        app.MapDelete("/api/admin/portfolio/imports/drafts/{jobId:int}", [Authorize] async (
            int jobId, AppDbContext db, IStorageService storage, ILoggerFactory loggerFactory) =>
        {
            var logger = loggerFactory.CreateLogger("ImportEndpoints");
            var job = await db.Jobs
                .Include(j => j.Photos)
                .FirstOrDefaultAsync(j => j.Id == jobId && j.Source == JobSource.FacebookImport);
            if (job is null) return Results.NotFound();

            foreach (var photo in job.Photos)
            {
                try
                {
                    var uri = new Uri(photo.Url);
                    await storage.DeleteAsync(uri.AbsolutePath.TrimStart('/'));
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Storage delete failed for {Url}", photo.Url);
                }
            }

            db.Jobs.Remove(job);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });
    }
}
