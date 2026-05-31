using RainGutter.Api.Enums;

namespace RainGutter.Api.Models;

public class QuoteRequest
{
    public int Id { get; set; }
    public string QuoteNumber { get; set; } = string.Empty;
    public int LeadId { get; set; }
    public Lead Lead { get; set; } = null!;

    // Input snapshot
    public Material Material { get; set; }
    public int SizeInches { get; set; }
    public int? BuildingTypeId { get; set; }
    public string? BuildingTypeLabelSnapshot { get; set; }
    public decimal LengthMeters { get; set; }
    public int DownspoutCount { get; set; }
    public int Floors { get; set; }
    public bool RemoveOld { get; set; }
    public string MeasureSource { get; set; } = "Manual";
    public decimal? MeasuredLengthMeters { get; set; }
    public string? MeasuredGeoJson { get; set; }
    public double? MapCenterLat { get; set; }
    public double? MapCenterLng { get; set; }
    public int? MapZoom { get; set; }

    public decimal EstimatedTotal { get; set; }
    public string BreakdownJson { get; set; } = "[]";
    public QuoteStatus Status { get; set; } = QuoteStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
