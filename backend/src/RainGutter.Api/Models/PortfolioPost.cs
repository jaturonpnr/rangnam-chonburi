namespace RainGutter.Api.Models;

public class PortfolioPost
{
    public int Id { get; set; }
    public string FbPostUrl { get; set; } = "";      // unique, used as embed source
    public string? Title { get; set; }               // from CSV "ชื่อ" (first 100 chars, no hashtags)
    public string? AreaName { get; set; }            // admin-assigned
    public double? ApproxLat { get; set; }           // offset coords only
    public double? ApproxLng { get; set; }
    public DateTime? PostedDate { get; set; }        // from CSV
    public int? Reach { get; set; }                  // from CSV (internal only, not exposed publicly)
    public bool IsPublished { get; set; } = false;   // admin must review before publishing
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
