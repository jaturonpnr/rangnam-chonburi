namespace RainGutter.Api.Models;

public enum ServiceRequestType { WarrantyClaim, Maintenance, Other }
public enum ServiceRequestStatus { New, Contacted, Done }

public class ServiceRequest
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string ContactPhone { get; set; } = "";
    public string? CustomerNote { get; set; }
    public ServiceRequestType Type { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.New;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
