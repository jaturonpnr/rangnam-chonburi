namespace RainGutter.Api.Models;

public class Lead
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? LocationDetail { get; set; }
    public int? ServiceZoneId { get; set; }
    public ServiceZone? ServiceZone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
