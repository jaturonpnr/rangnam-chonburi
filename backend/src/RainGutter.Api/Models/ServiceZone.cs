namespace RainGutter.Api.Models;

public class ServiceZone
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal TravelSurcharge { get; set; }
    public bool IsActive { get; set; } = true;
}
