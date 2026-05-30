using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace RainGutter.Api.Endpoints;

public static class LineEndpoints
{
    public static void MapLineEndpoints(this WebApplication app)
    {
        app.MapPost("/api/line/webhook", async (HttpContext ctx, ILogger<Program> logger) =>
        {
            var secret = Environment.GetEnvironmentVariable("LINE_CHANNEL_SECRET");
            var body = await new StreamReader(ctx.Request.Body).ReadToEndAsync();

            if (!string.IsNullOrEmpty(secret))
            {
                var signature = ctx.Request.Headers["x-line-signature"].FirstOrDefault();
                if (!VerifySignature(secret, body, signature))
                {
                    logger.LogWarning("Invalid LINE webhook signature");
                    return Results.Unauthorized();
                }
            }

            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("events", out var events))
                {
                    foreach (var evt in events.EnumerateArray())
                    {
                        if (evt.TryGetProperty("source", out var source))
                        {
                            if (source.TryGetProperty("userId", out var userId))
                                logger.LogInformation("LINE userId: {UserId}", userId.GetString());
                            if (source.TryGetProperty("groupId", out var groupId))
                                logger.LogInformation("LINE groupId: {GroupId}", groupId.GetString());
                        }
                    }
                }
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "Failed to parse LINE webhook body");
            }

            return Results.Ok();
        });
    }

    private static bool VerifySignature(string secret, string body, string? signature)
    {
        if (string.IsNullOrEmpty(signature)) return false;
        var key = Encoding.UTF8.GetBytes(secret);
        var data = Encoding.UTF8.GetBytes(body);
        using var hmac = new HMACSHA256(key);
        var hash = Convert.ToBase64String(hmac.ComputeHash(data));
        return hash == signature;
    }
}
