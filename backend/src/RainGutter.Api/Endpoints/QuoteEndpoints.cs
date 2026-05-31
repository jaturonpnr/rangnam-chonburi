using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using RainGutter.Api.Data;
using RainGutter.Api.Dtos;
using RainGutter.Api.Models;
using RainGutter.Api.Services;

namespace RainGutter.Api.Endpoints;

public static class QuoteEndpoints
{
    public static void MapQuoteEndpoints(this WebApplication app)
    {
        app.MapPost("/api/quote-requests", async (
            CreateQuoteRequest req,
            AppDbContext db,
            IPricingService pricing,
            ILineNotificationService line) =>
        {
            if (string.IsNullOrWhiteSpace(req.CustomerName))
                return Results.BadRequest(new { error = "กรุณาระบุชื่อลูกค้า" });
            if (!System.Text.RegularExpressions.Regex.IsMatch(req.Phone, @"^\d{9,10}$"))
                return Results.BadRequest(new { error = "เบอร์โทรต้องเป็นตัวเลข 9-10 หลัก" });
            if (req.LengthMeters <= 0)
                return Results.BadRequest(new { error = "จำนวนเมตรต้องมากกว่า 0" });

            var buildingType = await db.BuildingTypes.FirstOrDefaultAsync(b => b.Id == req.BuildingTypeId && b.IsActive);
            if (buildingType is null)
                return Results.BadRequest(new { error = "ไม่พบประเภทอาคารที่ระบุ" });

            var product = await db.GutterProducts.FirstOrDefaultAsync(p =>
                p.IsActive && p.Material == req.Material && p.SizeInches == buildingType.SizeInches);
            if (product is null)
                return Results.BadRequest(new { error = "ไม่พบสินค้าที่ตรงกับเงื่อนไข" });

            var config = await db.PricingConfigs.FirstOrDefaultAsync(c => c.Id == 1);
            if (config is null) return Results.StatusCode(500);

            var zone = req.ServiceZoneId.HasValue
                ? await db.ServiceZones.FirstOrDefaultAsync(z => z.Id == req.ServiceZoneId && z.IsActive)
                : null;

            var estimateReq = new EstimateRequest(req.LengthMeters, req.DownspoutCount, req.Floors, req.RemoveOld, req.ServiceZoneId);
            var estimate = pricing.Calculate(estimateReq, config, product, zone);

            await using var tx = await db.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.Serializable);

            var year = DateTime.UtcNow.Year;
            var maxSeq = await db.QuoteRequests
                .Where(q => q.QuoteNumber.StartsWith($"QT-{year}-"))
                .Select(q => q.QuoteNumber)
                .ToListAsync();
            var nextSeq = maxSeq.Count > 0
                ? maxSeq.Select(n => int.TryParse(n.Split('-').LastOrDefault(), out var v) ? v : 0).Max() + 1
                : 1;
            var quoteNumber = $"QT-{year}-{nextSeq:D4}";

            var lead = new Lead
            {
                CustomerName = req.CustomerName,
                Phone = req.Phone,
                Address = req.Address,
                LocationDetail = req.LocationDetail,
                ServiceZoneId = req.ServiceZoneId
            };
            db.Leads.Add(lead);
            await db.SaveChangesAsync();

            var quoteRequest = new QuoteRequest
            {
                QuoteNumber = quoteNumber,
                LeadId = lead.Id,
                Material = req.Material,
                SizeInches = buildingType.SizeInches,
                BuildingTypeId = buildingType.Id,
                BuildingTypeLabelSnapshot = buildingType.Label,
                LengthMeters = req.LengthMeters,
                DownspoutCount = req.DownspoutCount,
                Floors = req.Floors,
                RemoveOld = req.RemoveOld,
                EstimatedTotal = estimate.Total,
                BreakdownJson = JsonSerializer.Serialize(estimate.Breakdown)
            };
            db.QuoteRequests.Add(quoteRequest);
            await db.SaveChangesAsync();
            await tx.CommitAsync();

            // Fire-and-forget LINE notification
            _ = line.SendNewLeadNotificationAsync(quoteRequest, lead);

            return Results.Ok(new CreateQuoteResponse(quoteNumber, quoteRequest.Id));
        });

        app.MapGet("/api/quote-requests/{id:int}/pdf", async (
            int id, AppDbContext db, IPdfService pdf) =>
        {
            var quote = await db.QuoteRequests
                .Include(q => q.Lead)
                .FirstOrDefaultAsync(q => q.Id == id);
            if (quote is null) return Results.NotFound();

            var shop = await db.ShopProfiles.FirstOrDefaultAsync(s => s.Id == 1);
            if (shop is null) return Results.StatusCode(500);

            var pdfBytes = pdf.GenerateQuotePdf(quote, quote.Lead, shop);
            return Results.File(pdfBytes, "application/pdf", $"{quote.QuoteNumber}.pdf");
        });
    }
}
