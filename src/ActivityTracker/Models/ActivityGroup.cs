using System.Collections.ObjectModel;

namespace ActivityTracker.Models;

public class ActivityGroup
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#6B8FD6";
    public int SortOrder { get; set; }
    public ObservableCollection<Activity> Activities { get; set; } = [];
}
