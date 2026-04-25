using System.Text.Json.Serialization;

namespace ActivityTracker.Models;

public class PlannedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    [JsonIgnore] public DateOnly Date => DateOnly.FromDateTime(Start);
    public RecurrencePattern? Recurrence { get; set; }
    public string? Notes { get; set; }
}
