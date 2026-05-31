using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

public record CreateQuoteRequest(
    Material Material,
    int BuildingTypeId,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId,
    string CustomerName,
    string Phone,
    string? Address,
    string? LocationDetail,
    string MeasureSource = "Manual",
    decimal? MeasuredLengthMeters = null,
    string? MeasuredGeoJson = null,
    double? MapCenterLat = null,
    double? MapCenterLng = null,
    int? MapZoom = null
);

public record CreateQuoteResponse(string QuoteNumber, int QuoteRequestId);
