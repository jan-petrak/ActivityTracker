namespace ActivityTracker.Models;

public class PlannedEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActivityId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public RecurrencePattern? Recurrence { get; set; }
    public string? Notes { get; set; }
}
