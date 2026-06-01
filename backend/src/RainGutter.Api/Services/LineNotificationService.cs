using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using RainGutter.Api.Enums;
using RainGutter.Api.Models;

namespace RainGutter.Api.Services;

public class LineNotificationService(IHttpClientFactory httpClientFactory, ILogger<LineNotificationService> logger) : ILineNotificationService
{
    public async Task SendNewLeadNotificationAsync(QuoteRequest quote, Lead lead)
    {
        var token = Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN");
        var ownerId = Environment.GetEnvironmentVariable("LINE_OWNER_ID");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(ownerId))
        {
            logger.LogWarning("LINE_CHANNEL_ACCESS_TOKEN or LINE_OWNER_ID not set — skipping LINE notification");
            return;
        }

        try
        {
            var materialLabel = quote.Material == Material.Galvanized ? "สังกะสี" : "สแตนเลส";
            var buildingLabel = !string.IsNullOrEmpty(quote.BuildingTypeLabelSnapshot)
                ? $" ({quote.BuildingTypeLabelSnapshot})"
                : "";

            var bodyContents = new List<object>
            {
                new { type = "text", text = $"เลขที่: {quote.QuoteNumber}", size = "sm", color = "#888888" },
                new { type = "separator" },
                Row("ชื่อลูกค้า", lead.CustomerName),
                Row("เบอร์โทร", lead.Phone),
                Row("วัสดุ", $"{materialLabel} {quote.SizeInches}\"{buildingLabel}"),
                Row("จำนวน", $"{quote.LengthMeters} เมตร"),
                Row("ยอดประเมิน", $"{quote.EstimatedTotal:N0} บาท")
            };
            if (!string.IsNullOrEmpty(lead.LocationDetail))
                bodyContents.Add(Row("พื้นที่", lead.LocationDetail));

            var flexMessage = new
            {
                type = "flex",
                altText = $"[Lead ใหม่] {lead.CustomerName} — {quote.QuoteNumber}",
                contents = new
                {
                    type = "bubble",
                    header = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[] { new { type = "text", text = "🔔 Lead ใหม่เข้ามา!", weight = "bold", color = "#1DB446", size = "lg" } },
                        backgroundColor = "#f0fdf4"
                    },
                    body = new
                    {
                        type = "box",
                        layout = "vertical",
                        spacing = "md",
                        contents = bodyContents
                    }
                }
            };

            var payload = new
            {
                to = ownerId,
                messages = new[] { flexMessage }
            };

            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.line.me/v2/bot/message/push", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("LINE push failed: {Status} {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send LINE notification");
        }
    }

    public async Task SendServiceRequestNotificationAsync(ServiceRequest sr, Job job)
    {
        var token = Environment.GetEnvironmentVariable("LINE_CHANNEL_ACCESS_TOKEN");
        var ownerId = Environment.GetEnvironmentVariable("LINE_OWNER_ID");

        if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(ownerId))
        {
            logger.LogWarning("LINE env vars not set — skipping service request notification");
            return;
        }

        try
        {
            var typeLabel = sr.Type switch
            {
                ServiceRequestType.WarrantyClaim => "แจ้งเคลมประกัน",
                ServiceRequestType.Maintenance => "ขอบริการ/ซ่อมบำรุง",
                _ => "อื่นๆ"
            };

            var bodyContents = new List<object>
            {
                new { type = "text", text = $"ใบรับประกัน: {job.WarrantyNumber}", size = "sm", color = "#888888" },
                new { type = "separator" },
                Row("ประเภท", typeLabel),
                Row("เบอร์ติดต่อ", sr.ContactPhone),
            };
            if (!string.IsNullOrEmpty(sr.CustomerNote))
                bodyContents.Add(Row("หมายเหตุ", sr.CustomerNote));
            bodyContents.Add(Row("เวลา", sr.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm")));

            var flexMessage = new
            {
                type = "flex",
                altText = $"[แจ้งเคลม] {job.WarrantyNumber} — {sr.ContactPhone}",
                contents = new
                {
                    type = "bubble",
                    header = new
                    {
                        type = "box",
                        layout = "vertical",
                        contents = new[] { new { type = "text", text = "🔧 แจ้งเคลม/บริการ", weight = "bold", color = "#c2410c", size = "lg" } },
                        backgroundColor = "#fff7ed"
                    },
                    body = new
                    {
                        type = "box",
                        layout = "vertical",
                        spacing = "md",
                        contents = bodyContents
                    }
                }
            };

            var payload = new { to = ownerId, messages = new[] { flexMessage } };
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var content = new System.Net.Http.StringContent(
                System.Text.Json.JsonSerializer.Serialize(payload),
                System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.line.me/v2/bot/message/push", content);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync();
                logger.LogWarning("LINE push failed: {Status} {Body}", response.StatusCode, body);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send LINE service-request notification");
        }
    }

    private static object Row(string label, string value) => new
    {
        type = "box",
        layout = "horizontal",
        contents = new object[]
        {
            new { type = "text", text = label, size = "sm", color = "#555555", flex = 2 },
            new { type = "text", text = value, size = "sm", color = "#111111", flex = 3, wrap = true }
        }
    };
}
