using ActivityTracker.Models;

namespace ActivityTracker.Services;

// Deep-copy helpers used to freeze "before" state for the audit log
// before invoking editor dialogs that may mutate the passed reference.
internal static class AuditSnapshots
{
    public static PlannedEntry Clone(PlannedEntry e) => new()
    {
        Id = e.Id,
        ActivityId = e.ActivityId,
        Start = e.Start,
        End = e.End,
        Recurrence = e.Recurrence,
        Notes = e.Notes
    };

    public static DayEvent Clone(DayEvent e) => new()
    {
        Id = e.Id,
        Title = e.Title,
        Date = e.Date,
        ReminderDaysBefore = e.ReminderDaysBefore,
        Recurrence = e.Recurrence,
        Notes = e.Notes
    };
}
