namespace ActivityTracker.Models;

public class Activity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
