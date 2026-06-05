using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class PortfolioPostEndpoints
{
    public static void MapPortfolioPostEndpoints(this WebApplication app)
    {
        // --- Public endpoints ---

        // GET /api/portfolio/posts — public map pins (published only, ApproxLat/Lng only)
        app.MapGet("/api/portfolio/posts", async (AppDbContext db) =>
        {
            var posts = await db.PortfolioPosts
                .Where(p => p.IsPublished && p.ApproxLat != null && p.ApproxLng != null)
                .OrderBy(p => p.DisplayOrder).ThenByDescending(p => p.PostedDate)
                .Select(p => new PortfolioPostPinDto(
                    p.Id, p.ApproxLat!.Value, p.ApproxLng!.Value,
                    p.AreaName, p.FbPostUrl, p.Title, p.PostedDate))
                .ToListAsync();
            return Results.Ok(posts);
        });

        // GET /api/portfolio/summary — public summary counts (repointed to PortfolioPost)
        app.MapGet("/api/portfolio/summary", async (AppDbContext db) =>
        {
            var total = await db.PortfolioPosts.CountAsync(p => p.IsPublished);
            var byArea = await db.PortfolioPosts
                .Where(p => p.IsPublished && p.AreaName != null)
                .GroupBy(p => p.AreaName!)
                .Select(g => new AreaCountDto(g.Key, g.Count()))
                .ToListAsync();
            return Results.Ok(new PortfolioSummaryDto(total, byArea));
        });

        // --- Admin endpoints (require JWT) ---

        // GET /api/admin/portfolio/posts
        app.MapGet("/api/admin/portfolio/posts", [Authorize] async (AppDbContext db) =>
        {
            var posts = await db.PortfolioPosts
                .OrderByDescending(p => p.PostedDate).ThenByDescending(p => p.CreatedAt)
                .Select(p => new PortfolioPostAdminDto(
                    p.Id, p.FbPostUrl, p.Title, p.AreaName,
                    p.ApproxLat, p.ApproxLng, p.PostedDate, p.Reach,
                    p.IsPublished, p.DisplayOrder, p.CreatedAt))
                .ToListAsync();
            return Results.Ok(posts);
        });

        // POST /api/admin/portfolio/posts
        app.MapPost("/api/admin/portfolio/posts", [Authorize] async (
            SavePortfolioPostRequest req, AppDbContext db) =>
        {
            if (string.IsNullOrWhiteSpace(req.FbPostUrl))
                return Results.BadRequest(new { error = "FbPostUrl is required" });

            var exists = await db.PortfolioPosts.AnyAsync(p => p.FbPostUrl == req.FbPostUrl);
            if (exists) return Results.Conflict(new { error = "URL already exists" });

            var post = new PortfolioPost
            {
                FbPostUrl = req.FbPostUrl, Title = req.Title, AreaName = req.AreaName,
                ApproxLat = req.ApproxLat, ApproxLng = req.ApproxLng,
                PostedDate = req.PostedDate, IsPublished = req.IsPublished,
                DisplayOrder = req.DisplayOrder
            };
            db.PortfolioPosts.Add(post);
            await db.SaveChangesAsync();
            return Results.Ok(new { post.Id });
        });

        // PUT /api/admin/portfolio/posts/{id}
        app.MapPut("/api/admin/portfolio/posts/{id:int}", [Authorize] async (
            int id, SavePortfolioPostRequest req, AppDbContext db) =>
        {
            var post = await db.PortfolioPosts.FindAsync(id);
            if (post is null) return Results.NotFound();

            // Check uniqueness when URL is changing
            if (post.FbPostUrl != req.FbPostUrl)
            {
                var urlTaken = await db.PortfolioPosts.AnyAsync(p => p.FbPostUrl == req.FbPostUrl && p.Id != id);
                if (urlTaken) return Results.Conflict(new { error = "URL already exists" });
            }

            post.FbPostUrl = req.FbPostUrl; post.Title = req.Title;
            post.AreaName = req.AreaName; post.ApproxLat = req.ApproxLat;
            post.ApproxLng = req.ApproxLng; post.PostedDate = req.PostedDate;
            post.IsPublished = req.IsPublished; post.DisplayOrder = req.DisplayOrder;
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // DELETE /api/admin/portfolio/posts/{id}
        app.MapDelete("/api/admin/portfolio/posts/{id:int}", [Authorize] async (
            int id, AppDbContext db) =>
        {
            var post = await db.PortfolioPosts.FindAsync(id);
            if (post is null) return Results.NotFound();
            db.PortfolioPosts.Remove(post);
            await db.SaveChangesAsync();
            return Results.NoContent();
        });

        // POST /api/admin/portfolio/import-csv
        app.MapPost("/api/admin/portfolio/import-csv", [Authorize] async (
            HttpRequest request, AppDbContext db) =>
        {
            IFormFile? file;
            try
            {
                var form = await request.ReadFormAsync();
                file = form.Files.GetFile("file");
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { error = "Invalid multipart body" });
            }

            if (file is null || file.Length == 0)
                return Results.BadRequest(new { error = "กรุณาแนบไฟล์ CSV" });
            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest(new { error = "รองรับเฉพาะไฟล์ .csv" });

            using var stream = file.OpenReadStream();
            var parsed = PortfolioCsvParser.Parse(stream);

            // Batch lookup to avoid N+1 queries
            var urls = parsed.Entries.Select(e => e.FbPostUrl).ToList();
            var existingByUrl = await db.PortfolioPosts
                .Where(p => urls.Contains(p.FbPostUrl))
                .ToDictionaryAsync(p => p.FbPostUrl);

            int imported = 0, updated = 0;
            foreach (var entry in parsed.Entries)
            {
                if (existingByUrl.TryGetValue(entry.FbPostUrl, out var existing))
                {
                    // Update metadata but preserve admin-set fields (AreaName, ApproxLat/Lng, IsPublished)
                    existing.Title ??= entry.Title;
                    existing.PostedDate ??= entry.PostedDate;
                    existing.Reach = entry.Reach;
                    updated++;
                }
                else
                {
                    db.PortfolioPosts.Add(new PortfolioPost
                    {
                        FbPostUrl = entry.FbPostUrl, Title = entry.Title,
                        PostedDate = entry.PostedDate, Reach = entry.Reach,
                        IsPublished = false  // admin must review
                    });
                    imported++;
                }
            }
            await db.SaveChangesAsync();

            return Results.Ok(new PortfolioCsvImportResultDto(
                imported, parsed.Skipped, updated, parsed.Errors));
        });

        // POST /api/admin/portfolio/posts/bulk-publish
        app.MapPost("/api/admin/portfolio/posts/bulk-publish", [Authorize] async (
            BulkPortfolioPublishRequest req, AppDbContext db) =>
        {
            if (req.Ids.Count == 0)
                return Results.BadRequest(new { error = "กรุณาเลือกรายการ" });

            var posts = await db.PortfolioPosts
                .Where(p => req.Ids.Contains(p.Id))
                .ToListAsync();
            foreach (var p in posts) p.IsPublished = req.IsPublished;
            await db.SaveChangesAsync();
            return Results.Ok(new { Updated = posts.Count });
        });
    }
}
