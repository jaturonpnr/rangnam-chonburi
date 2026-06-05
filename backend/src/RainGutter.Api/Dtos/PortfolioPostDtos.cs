namespace RainGutter.Api.Dtos;

// Public portfolio map pin (no PII, no Reach, ApproxLat/Lng only)
public record PortfolioPostPinDto(
    int Id,
    double ApproxLat,
    double ApproxLng,
    string? AreaName,
    string FbPostUrl,
    string? Title,
    DateTime? PostedDate
);

// Admin list/edit view (includes Reach for internal use)
public record PortfolioPostAdminDto(
    int Id,
    string FbPostUrl,
    string? Title,
    string? AreaName,
    double? ApproxLat,
    double? ApproxLng,
    DateTime? PostedDate,
    int? Reach,
    bool IsPublished,
    int DisplayOrder,
    DateTime CreatedAt
);

// Create/update request (admin only)
public record SavePortfolioPostRequest(
    string FbPostUrl,
    string? Title,
    string? AreaName,
    double? ApproxLat,
    double? ApproxLng,
    DateTime? PostedDate,
    bool IsPublished,
    int DisplayOrder
);

// Bulk publish request
public record BulkPortfolioPublishRequest(List<int> Ids, bool IsPublished);

// CSV import result
public record PortfolioCsvImportResultDto(int Imported, int Skipped, int Updated, List<string> Errors);
