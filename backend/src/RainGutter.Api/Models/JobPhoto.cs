namespace RainGutter.Api.Models;

public enum PhotoType { Before, After, Other }

public class JobPhoto
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public Job Job { get; set; } = null!;
    public string Url { get; set; } = "";
    public PhotoType Type { get; set; }
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; }
}
