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
        DateOnly newDate,
        TimeOnly newStart,
        TimeOnly newEnd,
        RecurrenceEditScope scope)
    {
        var isMidnightEnd = newEnd == TimeOnly.MinValue && newStart > TimeOnly.MinValue;
        if (!isMidnightEnd && newEnd <= newStart) return false;

        var isRecurring = entry.Recurrence != null;
        var unchanged = newDate == occurrenceDate
                        && newStart == entry.StartTime
                        && newEnd == entry.EndTime;
        if (unchanged) return false;

        if (!isRecurring)
        {
            var oldDate = entry.Date;
            var oldStart = entry.StartTime;
            var oldEnd = entry.EndTime;
            entry.Date = newDate;
            entry.StartTime = newStart;
            entry.EndTime = newEnd;
            data.NotifyChanged();
            audit?.Log("PlannedEntryRescheduled",
                $"Moved entry from {oldDate:yyyy-MM-dd} {oldStart:HH\\:mm}-{oldEnd:HH\\:mm} to {newDate:yyyy-MM-dd} {newStart:HH\\:mm}-{newEnd:HH\\:mm}",
                new { entryId = entry.Id, oldDate, oldStart, oldEnd, newDate, newStart, newEnd });
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
                    Date = newDate,
                    StartTime = newStart,
                    EndTime = newEnd,
                    Notes = entry.Notes,
                };
                data.Data.PlannedEntries.Add(standalone);
                data.NotifyChanged();
                audit?.Log("PlannedEntryRescheduled",
                    $"Moved occurrence {occurrenceDate:yyyy-MM-dd} of recurring entry to {newDate:yyyy-MM-dd} {newStart:HH\\:mm}-{newEnd:HH\\:mm} (this occurrence only)",
                    new { seriesId = entry.Id, occurrenceDate, newEntryId = standalone.Id, newDate, newStart, newEnd });
                return true;

            case RecurrenceEditScope.WholeSeries:
                var dayDelta = newDate.DayNumber - occurrenceDate.DayNumber;
                var timeDelta = newStart - entry.StartTime;

                if (dayDelta != 0)
                {
                    entry.Date = entry.Date.AddDays(dayDelta);
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

                if (timeDelta != TimeSpan.Zero)
                {
                    entry.StartTime = newStart;
                    entry.EndTime = newEnd;
                }
                else if (newEnd != entry.EndTime)
                {
                    entry.EndTime = newEnd;
                }

                data.NotifyChanged();
                audit?.Log("PlannedEntryRescheduled",
                    $"Shifted whole series (dayDelta={dayDelta}, timeDelta={timeDelta}) from occurrence {occurrenceDate:yyyy-MM-dd}",
                    new { seriesId = entry.Id, dayDelta, timeDelta, newDate, newStart, newEnd });
                return true;

            default:
                return false;
        }
    }
}
