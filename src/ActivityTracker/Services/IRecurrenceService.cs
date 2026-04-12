using ActivityTracker.Models;

namespace ActivityTracker.Services;

public interface IRecurrenceService
{
    IEnumerable<DateOnly> ExpandOccurrences(RecurrencePattern pattern, DateOnly rangeStart, DateOnly rangeEnd);
}
