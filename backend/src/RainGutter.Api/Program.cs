using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuestPDF.Infrastructure;
using RainGutter.Api.Data;
using RainGutter.Api.Endpoints;
using RainGutter.Api.Services;

// Load .env for local dev
if (File.Exists(".env"))
{
    foreach (var line in File.ReadAllLines(".env"))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#')) continue;
        var parts = line.Split('=', 2);
        if (parts.Length == 2)
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
    }
}

QuestPDF.Settings.License = LicenseType.Community;

var fontsDir = Path.Combine(AppContext.BaseDirectory, "Assets", "Fonts");
foreach (var f in Directory.EnumerateFiles(fontsDir, "*.ttf"))
    QuestPDF.Drawing.FontManager.RegisterFont(File.OpenRead(f));

var builder = WebApplication.CreateBuilder(args);

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
    ?? builder.Configuration.GetConnectionString("Default")
    ?? "Host=localhost;Database=raingutter;Username=postgres;Password=postgres";

builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(connectionString)
       .UseSnakeCaseNamingConvention());

builder.Services.AddScoped<IPricingService, PricingService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<ILineNotificationService, LineNotificationService>();
builder.Services.AddScoped<IStorageService, StorageService>();
builder.Services.AddScoped<IQrService, QrService>();
builder.Services.AddHttpClient();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? "dev-secret-change-in-production";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });
builder.Services.AddAuthorization();

var allowedOrigins = (Environment.GetEnvironmentVariable("AllowedOrigins") ?? "http://localhost:4200")
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.ConfigureHttpJsonOptions(opt =>
    opt.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(opt =>
    opt.AddDefaultPolicy(policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapPublicEndpoints();
app.MapQuoteEndpoints();
app.MapLineEndpoints();
app.MapAdminEndpoints();
app.MapJobEndpoints();
app.MapImportEndpoints();
app.MapWarrantyEndpoints();

app.Run();

public partial class Program { }
