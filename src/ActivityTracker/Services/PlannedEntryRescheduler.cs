using ActivityTracker.Models;
using ActivityTracker.Views.Dialogs;

namespace ActivityTracker.Services;

public static class PlannedEntryRescheduler
{
    public static bool TryReschedule(
        IDataService data,
        IAuditLogService? audit,
        PlannedEntry entry,
        DateOnly occurrenceDate,
        DateTime newStart,
        DateTime newEnd,
        RecurrenceEditScope scope)
    {
        if (newEnd <= newStart) return false;

        var newDate = DateOnly.FromDateTime(newStart);
        var isRecurring = entry.Recurrence != null;
        var unchanged = newDate == occurrenceDate
                        && newStart.TimeOfDay == entry.Start.TimeOfDay
                        && newEnd.TimeOfDay == entry.End.TimeOfDay
                        && (newEnd - newStart) == (entry.End - entry.Start);
        if (unchanged) return false;

        if (!isRecurring)
        {
            var oldStart = entry.Start;
            var oldEnd = entry.End;
            entry.Start = newStart;
            entry.End = newEnd;
            data.NotifyChanged();
            audit?.Log("PlannedEntryRescheduled",
                $"Moved entry from {oldStart:yyyy-MM-dd HH\\:mm} to {newStart:yyyy-MM-dd HH\\:mm}-{newEnd:HH\\:mm}",
                new { entryId = entry.Id, oldStart, oldEnd, newStart, newEnd });
            return true;
        }

        switch (scope)
        {
            case RecurrenceEditScope.Cancel:
                return false;

            case RecurrenceEditScope.ThisOccurrence:
                if (!entry.Recurrence!.Exceptions.Contains(occurrenceDate))
                    entry.Recurrence.Exceptions.Add(occurrenceDate);
                var standalone = new PlannedEntry
                {
                    ActivityId = entry.ActivityId,
                    Start = newStart,
                    End = newEnd,
                    Notes = entry.Notes,
                };
                data.Data.PlannedEntries.Add(standalone);
                data.NotifyChanged();
                audit?.Log("PlannedEntryRescheduled",
                    $"Moved occurrence {occurrenceDate:yyyy-MM-dd} of recurring entry to {newStart:yyyy-MM-dd HH\\:mm}-{newEnd:HH\\:mm} (this occurrence only)",
                    new { seriesId = entry.Id, occurrenceDate, newEntryId = standalone.Id, newStart, newEnd });
                return true;

            case RecurrenceEditScope.WholeSeries:
                var dayDelta = newDate.DayNumber - occurrenceDate.DayNumber;
                var timeDelta = newStart.TimeOfDay - entry.Start.TimeOfDay;

                if (dayDelta != 0)
                {
                    entry.Start = entry.Start.AddDays(dayDelta);
                    entry.End = entry.End.AddDays(dayDelta);
                    entry.Recurrence!.StartDate = entry.Recurrence.StartDate.AddDays(dayDelta);
                    if (entry.Recurrence.Type == RecurrenceType.Weekly && entry.Recurrence.DaysOfWeek.Count > 0)
                    {
                        var offset = ((dayDelta % 7) + 7) % 7;
                        entry.Recurrence.DaysOfWeek = entry.Recurrence.DaysOfWeek
                            .Select(d => (DayOfWeek)(((int)d + offset) % 7))
                            .Distinct()
                            .ToList();
                    }
                }

                var newDuration = newEnd - newStart;
                if (timeDelta != TimeSpan.Zero || newDuration != (entry.End - entry.Start))
                {
                    var anchorDate = DateOnly.FromDateTime(entry.Start);
                    entry.Start = anchorDate.ToDateTime(TimeOnly.FromTimeSpan(newStart.TimeOfDay));
                    entry.End = entry.Start + newDuration;
                }

                data.NotifyChanged();
                audit?.Log("PlannedEntryRescheduled",
                    $"Shifted whole series (dayDelta={dayDelta}, timeDelta={timeDelta}) from occurrence {occurrenceDate:yyyy-MM-dd}",
                    new { seriesId = entry.Id, dayDelta, timeDelta, newStart, newEnd });
                return true;

            default:
                return false;
        }
    }
}
