namespace RainGutter.Api.Models;

public class BuildingType
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int SizeInches { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
