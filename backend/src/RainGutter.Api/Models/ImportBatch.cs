namespace RainGutter.Api.Models;

public class ImportBatch
{
    public int Id { get; set; }
    public string Source { get; set; } = "Facebook";
    public int PhotoCount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}
