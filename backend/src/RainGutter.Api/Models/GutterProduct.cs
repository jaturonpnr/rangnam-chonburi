using RainGutter.Api.Enums;

namespace RainGutter.Api.Models;

public class GutterProduct
{
    public int Id { get; set; }
    public Material Material { get; set; }
    public int SizeInches { get; set; }
    public decimal PricePerMeter { get; set; }
    public bool IsActive { get; set; } = true;
}
