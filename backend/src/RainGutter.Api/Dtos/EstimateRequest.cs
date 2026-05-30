using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

public record EstimateRequest(
    Material Material,
    int SizeInches,
    Finish? Finish,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId
);
