namespace ActivityTracker.Models;

public class ActivityGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#4A90D9";
    public int SortOrder { get; set; }
    public List<Activity> Activities { get; set; } = [];
}
