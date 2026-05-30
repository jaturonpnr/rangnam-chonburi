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
    public Finish? Finish { get; set; }
    public decimal LengthMeters { get; set; }
    public int DownspoutCount { get; set; }
    public int Floors { get; set; }
    public bool RemoveOld { get; set; }

    public decimal EstimatedTotal { get; set; }
    public string BreakdownJson { get; set; } = "[]";
    public QuoteStatus Status { get; set; } = QuoteStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
