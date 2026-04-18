namespace ActivityTracker.Models;

public class DayEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int ReminderDaysBefore { get; set; }
    public RecurrencePattern? Recurrence { get; set; }
    public string? Notes { get; set; }
}
