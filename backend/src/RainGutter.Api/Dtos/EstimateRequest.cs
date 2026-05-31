using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

// Used internally by PricingService
public record EstimateRequest(
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId
);

// HTTP body for POST /api/estimate
public record EstimateBody(
    Material Material,
    int BuildingTypeId,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId
);
