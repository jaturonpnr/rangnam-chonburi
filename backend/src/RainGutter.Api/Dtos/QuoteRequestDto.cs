using RainGutter.Api.Enums;

namespace RainGutter.Api.Dtos;

public record CreateQuoteRequest(
    Material Material,
    int SizeInches,
    Finish? Finish,
    decimal LengthMeters,
    int DownspoutCount,
    int Floors,
    bool RemoveOld,
    int? ServiceZoneId,
    string CustomerName,
    string Phone,
    string? Address
);

public record CreateQuoteResponse(string QuoteNumber, int QuoteRequestId);
