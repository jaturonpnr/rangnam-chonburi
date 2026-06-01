using RainGutter.Api.Enums;

namespace RainGutter.Api.Models;

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
