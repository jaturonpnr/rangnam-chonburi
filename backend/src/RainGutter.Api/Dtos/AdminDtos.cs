using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token);

public record UpdateStatusRequest(QuoteStatus Status);

public record QuoteRequestSummary(
    int Id,
    string QuoteNumber,
    string CustomerName,
    string Phone,
    decimal EstimatedTotal,
    QuoteStatus Status,
    DateTime CreatedAt
);

public record QuoteRequestDetail(
    int Id,
    string QuoteNumber,
    string CustomerName,
    string Phone,
    string? Address,
    string? LocationDetail,
    string? ServiceZoneName,
    string? BuildingTypeLabel,
    Material Material,
    int SizeInches,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    decimal EstimatedTotal,
    string BreakdownJson,
    QuoteStatus Status,
    DateTime CreatedAt,
    string MeasureSource,
    decimal? MeasuredLengthMeters,
    string? MeasuredGeoJson,
    double? MapCenterLat,
    double? MapCenterLng,
    int? MapZoom
);

public record StatsResponse(
    int TotalLeads,
    int LeadsThisMonth,
    int LeadsThisWeek,
    decimal TotalEstimatedValue,
    decimal AverageQuoteValue,
    IReadOnlyList<StatusCount> ByStatus,
    IReadOnlyList<ZoneCount> ByZone,
    IReadOnlyList<WeeklyCount> WeeklySeries,
    IReadOnlyList<TopProduct> TopProducts
);

public record StatusCount(string Status, int Count);
public record ZoneCount(string Zone, int Count);
public record WeeklyCount(string Week, int Count);
public record TopProduct(string Label, int Count);

public record UpsertProductRequest(
    Material Material,
    int SizeInches,
    decimal PricePerMeter,
    bool IsActive
);

public record UpsertBuildingTypeRequest(
    string Label,
    int SizeInches,
    int DisplayOrder,
    bool IsActive
);

public record UpsertZoneRequest(string Name, decimal TravelSurcharge, bool IsActive);

public record UpdateConfigRequest(
    decimal MinimumMeters,
    decimal DownspoutPricePerPoint,
    decimal HeightSurchargePercent,
    decimal RemovalPricePerMeter,
    decimal SurveyFee
);

public record UpdateShopProfileRequest(
    string ShopName,
    string Phone,
    string Address,
    string? LogoUrl,
    string LineOaLink,
    int QuoteValidityDays,
    string QuoteFooterNote
);
