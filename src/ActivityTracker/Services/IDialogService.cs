using ActivityTracker.Models;

namespace ActivityTracker.Services;

public interface IDialogService
{
    bool ShowGroupEditor(ActivityGroup? existing, out ActivityGroup result);
    bool ShowActivityEditor(Activity? existing, out Activity result);
    bool ShowPlannedEntryEditor(List<ActivityGroup> groups, Guid? defaultActivityId, PlannedEntry? existing, out PlannedEntry result);
    bool ShowGoalEditor(List<ActivityGroup> groups, Goal? existing, out Goal result);
    bool ShowDayEventEditor(DayEvent? existing, out DayEvent result);
}
