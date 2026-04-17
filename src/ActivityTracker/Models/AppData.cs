namespace ActivityTracker.Models;

public class AppData
{
    public int Version { get; set; } = 1;
    public List<ActivityGroup> Groups { get; set; } = [];
    public List<PlannedEntry> PlannedEntries { get; set; } = [];
    public List<Goal> Goals { get; set; } = [];
}
