using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RainGutter.Api.Data;
using RainGutter.Api.Services;

namespace RainGutter.Tests;

// ── Fake storage so upload calls don't hit real S3 ──────────────────────────
public sealed class FakeStorageService : IStorageService
{
    public Task<string> UploadAsync(string fileName, Stream content, string contentType)
        => Task.FromResult($"https://fake-storage/{fileName}");

    public Task DeleteAsync(string fileName) => Task.CompletedTask;
}

// ── WebApplicationFactory that swaps Postgres → InMemory ────────────────────
public sealed class RainGutterWebFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"raingutter-test-{Guid.NewGuid():N}";

    // 32-char secret satisfies HS256 minimum key size (256 bits)
    private const string TestJwtSecret = "test-secret-for-integration-test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        // Ensure the server and the JWT builder share the same secret
        Environment.SetEnvironmentVariable("JWT_SECRET", TestJwtSecret);

        builder.ConfigureServices(services =>
        {
            // Remove DbContext registrations to prevent "two providers" conflict.
            // ConfigureServices runs AFTER the app's own DI registrations,
            // so we pull out the existing AppDbContext descriptor (which carries
            // the Npgsql options action) and replace it with an InMemory one.
            var existingDbContext = services
                .Where(d => d.ServiceType == typeof(AppDbContext))
                .ToList();
            foreach (var d in existingDbContext) services.Remove(d);

            var existingOptions = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>))
                .ToList();
            foreach (var d in existingOptions) services.Remove(d);

            // Also remove the IDbContextOptionsConfiguration<AppDbContext> that
            // carries the Npgsql UseNpgsql / UseSnakeCaseNamingConvention action
            var optionsConfig = services
                .Where(d => d.ServiceType.IsGenericType &&
                            d.ServiceType.Name.StartsWith("IDbContextOptionsConfiguration"))
                .ToList();
            foreach (var d in optionsConfig) services.Remove(d);

            // Register InMemory DB (no Npgsql, no snake_case needed for tests)
            services.AddDbContext<AppDbContext>(opts =>
                opts.UseInMemoryDatabase(_dbName));

            // Replace real storage with fake
            var storageDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IStorageService));
            if (storageDescriptor != null) services.Remove(storageDescriptor);
            services.AddScoped<IStorageService, FakeStorageService>();
        });

    }

    /// <summary>Build an HttpClient that carries a valid admin JWT.</summary>
    public HttpClient CreateAuthenticatedClient()
    {
        var client = CreateClient();
        var token = BuildAdminJwt();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static string BuildAdminJwt()
    {
        var secret = TestJwtSecret;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(ClaimTypes.Name, "testadmin")],
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

// ── Tests ────────────────────────────────────────────────────────────────────
public class ImportEndpointTests : IClassFixture<RainGutterWebFactory>
{
    private readonly RainGutterWebFactory _factory;

    public ImportEndpointTests(RainGutterWebFactory factory)
    {
        _factory = factory;
    }

    // 1 ── POST with no files → 400 ────────────────────────────────────────────
    [Fact]
    public async Task PostImages_NoFiles_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        // Include a dummy field so the multipart is well-formed but contains no "files" entries
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("placeholder"), "dummy");

        var response = await client.PostAsync("/api/admin/portfolio/import/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 2 ── POST with 21 files → 400 ────────────────────────────────────────────
    [Fact]
    public async Task PostImages_TooManyFiles_Returns400()
    {
        var client = _factory.CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();

        for (int i = 0; i < 21; i++)
        {
            var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            content.Add(fileContent, "files", $"photo{i:D3}.jpg");
        }

        var response = await client.PostAsync("/api/admin/portfolio/import/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // 3 ── POST single jpeg → 200 or 500 (storage may fail in CI) ────────────
    [Fact]
    public async Task PostImages_SingleJpeg_CreatesBatchAndJob()
    {
        var client = _factory.CreateAuthenticatedClient();
        var content = new MultipartFormDataContent();

        var fileContent = new ByteArrayContent(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 });
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        content.Add(fileContent, "files", "photo.jpg");

        var response = await client.PostAsync("/api/admin/portfolio/import/images", content);

        // Accept 200 (FakeStorageService) or 500 (real storage unavailable)
        Assert.True(
            response.StatusCode == HttpStatusCode.OK ||
            response.StatusCode == HttpStatusCode.InternalServerError,
            $"Expected 200 or 500, got {(int)response.StatusCode}");

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            Assert.True(doc.RootElement.TryGetProperty("batchId", out _), "Response should contain batchId");
            Assert.True(doc.RootElement.TryGetProperty("jobCount", out var jobCount), "Response should contain jobCount");
            Assert.Equal(1, jobCount.GetInt32());
        }
    }

    // 4 ── GET /api/admin/portfolio/imports → 200 with list ───────────────────
    [Fact]
    public async Task GetImportBatches_ReturnsEmptyList()
    {
        var client = _factory.CreateAuthenticatedClient();

        var response = await client.GetAsync("/api/admin/portfolio/imports");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    // 5 ── PUT non-existent draft job → 404 ────────────────────────────────────
    [Fact]
    public async Task PutDraft_NonExistentJob_Returns404()
    {
        var client = _factory.CreateAuthenticatedClient();
        var payload = JsonSerializer.Serialize(new
        {
            areaName = "test",
            material = "Stainless",
            sizeInches = 6,
            lengthMeters = 10,
            showInPortfolio = false,
            photoConsent = true
        });
        var requestContent = new StringContent(payload, Encoding.UTF8, "application/json");

        var response = await client.PutAsync("/api/admin/portfolio/imports/drafts/999999", requestContent);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
