using RainGutter.Api.Enums;

namespace RainGutter.Api.Models;

public class Job
{
    public int Id { get; set; }

    public int? QuoteRequestId { get; set; }
    public QuoteRequest? QuoteRequest { get; set; }

    public JobSource Source { get; set; } = JobSource.Quote;

    public int? ImportBatchId { get; set; }
    public ImportBatch? ImportBatch { get; set; }

    public string? WarrantyNumber { get; set; }
    public string? PublicToken { get; set; }

    public DateTime? InstalledDate { get; set; }
    public int? WarrantyMonths { get; set; }

    public Material Material { get; set; }
    public int SizeInches { get; set; }
    public decimal LengthMeters { get; set; }
    public int DownspoutCount { get; set; }

    public double? Lat { get; set; }
    public double? Lng { get; set; }
    public double? ApproxLat { get; set; }
    public double? ApproxLng { get; set; }
    public string? AreaName { get; set; }

    public bool ShowInPortfolio { get; set; } = false;
    public bool PhotoConsent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<JobPhoto> Photos { get; set; } = new List<JobPhoto>();
    public ICollection<ServiceRequest> ServiceRequests { get; set; } = new List<ServiceRequest>();
}
