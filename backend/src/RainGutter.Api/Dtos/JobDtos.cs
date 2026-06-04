using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

// ── Shared ───────────────────────────────────────────────────────────────────
public record JobPhotoDto(int Id, string Url, PhotoType Type, string? Caption, int DisplayOrder);

public record ServiceRequestDto(
    int Id, string ContactPhone, string? CustomerNote,
    ServiceRequestType Type, ServiceRequestStatus Status, DateTime CreatedAt);

// ── Admin ─────────────────────────────────────────────────────────────────────
public record CompleteJobRequest(
    DateTime InstalledDate, int WarrantyMonths,
    double? Lat, double? Lng, string? AreaName);

public record EditJobRequest(
    int? WarrantyMonths, DateTime? InstalledDate, string? AreaName,
    double? Lat, double? Lng, bool ShowInPortfolio, bool PhotoConsent);

public record JobSummaryDto(
    int Id, string? WarrantyNumber, string? QuoteNumber,
    string? InstalledDate, string? WarrantyExpiry,
    Material Material, int SizeInches, decimal LengthMeters,
    bool ShowInPortfolio, bool PhotoConsent, int ServiceRequestCount,
    JobSource Source);

public record JobDetailDto(
    int Id, int? QuoteRequestId, string? QuoteNumber,
    string? WarrantyNumber, string? PublicToken,
    string? InstalledDate, int? WarrantyMonths, string? WarrantyExpiry,
    Material Material, int SizeInches, decimal LengthMeters, int DownspoutCount,
    double? Lat, double? Lng, double? ApproxLat, double? ApproxLng, string? AreaName,
    bool ShowInPortfolio, bool PhotoConsent,
    JobSource Source, int? ImportBatchId,
    List<JobPhotoDto> Photos, List<ServiceRequestDto> ServiceRequests);

public record UpdateServiceRequestStatusRequest(ServiceRequestStatus Status);

// ── Import ────────────────────────────────────────────────────────────────────
public record ImportDraftDto(
    int JobId,
    string? AreaName,
    Material Material, int SizeInches, decimal LengthMeters,
    double? ApproxLat, double? ApproxLng,
    bool ShowInPortfolio, bool PhotoConsent,
    List<JobPhotoDto> Photos,
    int ImportBatchId, DateTime CreatedAt);

public record UpdateImportDraftRequest(
    string? AreaName,
    Material Material, int SizeInches, decimal LengthMeters,
    double? Lat, double? Lng,
    bool ShowInPortfolio, bool PhotoConsent);

public record ImportBatchSummaryDto(
    int Id, string Source, int PhotoCount, int JobCount, DateTime CreatedAt);

public record BulkUpdateDraftRequest(
    List<int> JobIds,
    string? AreaName,
    bool? ShowInPortfolio,
    bool? PhotoConsent);

public record FbImportResultDto(
    int BatchId,
    int Paired,
    int Unpaired,
    int Skipped);

// ── Public ────────────────────────────────────────────────────────────────────
public record WarrantyCardDto(
    string? WarrantyNumber,
    string? InstalledDate,
    string? WarrantyExpiry,
    Material Material, int SizeInches, decimal LengthMeters, int DownspoutCount,
    List<JobPhotoDto> Photos,
    string ShopName, string ShopPhone, string LineOaLink);

public record CreateServiceRequestBody(
    string ContactPhone, string? CustomerNote, ServiceRequestType Type);

public record PortfolioPinDto(
    int JobId, double ApproxLat, double ApproxLng, string? AreaName,
    Material Material, string? InstalledDate,
    List<JobPhotoDto> ConsentedPhotos);

public record PortfolioSummaryDto(int Total, List<AreaCountDto> ByArea);
public record AreaCountDto(string Name, int Count);
