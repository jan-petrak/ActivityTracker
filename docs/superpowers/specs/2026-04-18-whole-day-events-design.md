# Whole-Day Events with Reminders — Design

**Date:** 2026-04-18
**Status:** Approved

## Summary

Add a new first-class entity, `DayEvent`, for whole-day milestones (e.g., "Deadline to pay the rent"). Events are authored via a context menu on Day/Week/Month views, persist alongside timed entries, reuse the existing `RecurrencePattern`, and surface in the Dashboard's "Today's Schedule" card and a new "Upcoming Events" card based on a per-event lead-time reminder.

Out of scope: OS-level notifications / toasts / background reminder service. "Reminder" in this feature means the event appears in the Upcoming Events card within its reminder window.

## Goals

- Capture milestones that have no start/end time and are not hours-logged activities.
- Let users see them prominently as the date approaches (reminder lead-time).
- Consistent recurrence semantics with timed entries.
- No regression to existing timed-entry flows.

## Non-goals

- Live desktop notifications or system-tray alerts.
- Per-reminder multiple lead-times (e.g., "30 days, 7 days, 1 day"). Single integer is enough.
- Linking whole-day events to an `ActivityGroup` or contributing to statistics / weekly hours.
- Extracting a shared recurrence sub-UI between `PlannedEntryEditorDialog` and the new day-event dialog. Each dialog keeps its own copy. If a third consumer arrives, extract then.

## Data model

New model `Models/DayEvent.cs`:

```csharp
public class DayEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public DateOnly Date { get; set; }              // first occurrence / recurrence anchor
    public int ReminderDaysBefore { get; set; }     // 0 = no reminder
    public RecurrencePattern? Recurrence { get; set; }
    public string? Notes { get; set; }
}
```

`AppData` gains one field:

```csharp
public List<DayEvent> DayEvents { get; set; } = [];
```

### Persistence

`JsonDataService` serializes `AppData` wholesale; older JSON files without `dayEvents` deserialize to an empty list (default-initialized). No version bump or migration required.

### Invariants

- `Recurrence.StartDate` = `Date` (same anchor convention as `PlannedEntry`).
- `Recurrence.Exceptions` handles skipped occurrences.
- `ReminderDaysBefore >= 0`. `0` means the event does not appear in Upcoming Events; it still appears in Today's Schedule on its date.
- Title is required and non-empty.

## Service layer

`ICalendarService` gains two methods — one for range expansion (used by Day/Week/Month/Dashboard), one for the reminder-window query (Dashboard "Upcoming").

```csharp
List<DayEventOccurrence> GetDayEventsForRange(DateOnly start, DateOnly end);
List<DayEventOccurrence> GetUpcomingDayEvents(DateOnly today);
```

### `DayEventOccurrence` (new DTO)

```csharp
public class DayEventOccurrence
{
    public Guid SourceId { get; set; }
    public string Title { get; set; } = "";
    public DateOnly Date { get; set; }
    public int ReminderDaysBefore { get; set; }
    public string? Notes { get; set; }
    public int DaysUntil(DateOnly today) => Date.DayNumber - today.DayNumber;
}
```

### `GetDayEventsForRange`

For each `DayEvent`:
- If `Recurrence == null` and `Date ∈ [start, end]`: yield one occurrence.
- Otherwise expand via `IRecurrenceService.ExpandOccurrences(Recurrence, start, end)` and yield each date.

Sorted by `Date` ascending.

### `GetUpcomingDayEvents(today)`

For each `DayEvent` with `ReminderDaysBefore > 0`:
- Find the **next** occurrence whose date is `>= today`. For non-recurring, that's just `Date` if `Date >= today`; for recurring, expand `[today, today + ReminderDaysBefore]` and take the first hit.
- If `DaysUntil(today) in (0, ReminderDaysBefore]`: include it. Exclude `DaysUntil == 0` (today's events live in "Today's Schedule" only).
- Exclude `DaysUntil < 0` (past).

Sorted by `DaysUntil` ascending.

`RecurrenceService` is reused unchanged.

## Editor dialog

New `Views/Dialogs/DayEventEditorDialog.xaml` + `.xaml.cs` following the existing non-MVVM pattern: code-behind `Save_Click` validates, writes `Result`, sets `DialogResult = true`.

### Fields (top to bottom)

1. **Title** — required `TextBox`.
2. **Date (YYYY-MM-DD)** — plain `TextBox` (project convention).
3. **Remind me ___ days before** — integer `TextBox`, default `0`.
4. **Recurring** checkbox — expands into the same recurrence sub-section as `PlannedEntryEditorDialog` (Daily / Weekly / Monthly + interval + weekday checkboxes + optional end date YYYY-MM-DD). The markup is duplicated from the timed dialog; no shared control extracted.
5. **Notes** — optional multi-line `TextBox`.
6. Bottom row: `Cancel` | `Save` (`SecondaryButtonStyle` + `PrimaryButtonStyle`).

### Validation (mirrors `PlannedEntryEditorDialog`)

- Non-empty title.
- Date parseable as `DateOnly` in `YYYY-MM-DD`.
- `ReminderDaysBefore` parseable as non-negative int.
- When Recurring is checked: valid interval (int ≥ 1); end-date parseable if non-empty; weekly type requires at least one day-of-week selected.

Errors surface via `MessageBox.Show`.

### Service registration

`IDialogService` gains:
```csharp
bool ShowDayEventEditor(DayEvent? existing, out DayEvent result);
```
Implementation in `DialogService` mirrors `ShowPlannedEntryEditor`.

## Views — Day & Week

### All-day row placement

The all-day row is pinned to the header and does **not** scroll with the timed grid.

**Day view restructure** (`DayView.xaml`):
```
DockPanel
├── DateHeader           (DockPanel.Dock=Top)
├── AllDayRow            (DockPanel.Dock=Top; new)
└── ScrollViewer         (timed grid)
```

**Week view restructure** (`WeekView.xaml`): today the day-header row is inside the `ScrollViewer`. Move the day-header row and the all-day row outside the `ScrollViewer`. The `ScrollViewer` contains only the hour gutter + per-day timed canvases.

The per-day all-day row is an `ItemsControl` over `DayColumn.DayEvents` (Week) or `DayViewModel.DayEvents` (Day). Fixed height sufficient for ~3 stacked pills; if more, the row scrolls vertically internally (standard `ScrollViewer` on the row with `VerticalScrollBarVisibility=Auto`).

### Pill rendering

Small `Border`, `BgHover` background, `BorderMedium` border, `CornerRadius=4`, `TextPrimary` text. Leading glyph: a small `Path` (pin/flag geometry, ~10 px, `AccentAmber` fill). Title with `TextTrimming=CharacterEllipsis`.

### Context menus

**Add** — attached to each day column `Canvas` (Day) / each day-column `Border` + day header (Week). Single item:
- `Add whole-day event` — opens `DayEventEditorDialog` pre-populated with that column's `Date`. On save: `dataService.Data.DayEvents.Add(result); dataService.NotifyChanged();` and reload the view.

The existing timed drag-to-create on `DragCanvas` uses `MouseLeftButtonDown`; `ContextMenu` activates on `MouseRightButtonUp`. No interaction conflict.

**Edit / Delete (on pill)** — right-click pill:
- `Edit` → open dialog with existing event; on save, replace in `dataService.Data.DayEvents` (matched by `Id`).
- `Delete` → remove from `dataService.Data.DayEvents` by `Id`; `NotifyChanged()`; reload.

### ViewModel additions

- `DayViewModel.DayEvents: ObservableCollection<DayEventOccurrence>` — loaded from `GetDayEventsForRange(Date, Date)`.
- `WeekViewModel.DayColumn.DayEvents: ObservableCollection<DayEventOccurrence>` — per column.

## Views — Month

### Icon + tooltip

Each `MonthDayCell` gains:
```csharp
public List<DayEventOccurrence> DayEvents { get; set; } = [];
public bool HasDayEvents => DayEvents.Count > 0;
public string DayEventsTooltip => string.Join("\n", DayEvents.Select(e => e.Title));
```

`MonthViewModel.Load` makes one extra call: `_calendarService.GetDayEventsForRange(gridStart, gridEnd)`, groups by date, attaches to each cell.

In `MonthView.xaml`, each day-cell `Border` gets a top-right-anchored small `Path` glyph (same pin/flag geometry as Day/Week pills, ~10 px, `AccentAmber`), `Visibility` bound to `HasDayEvents`, `ToolTip` bound to `DayEventsTooltip`.

### Context menus

- **Add (on cell)** — `ContextMenu` on the day-cell `Border`: `Add whole-day event` → opens dialog with cell's `Date`.
- **Edit / Delete (on icon)** — right-click the icon. When multiple events on that day, a flat list is shown, each event rendered as a `MenuItem` header with nested `Edit` / `Delete` sub-items (submenu). When one event, flat `Edit` / `Delete`.

## Dashboard

### "Today's Schedule" card (existing, extended)

Two stacked `ItemsControl`s inside the card:

1. `TodayDayEvents` on top — pin-icon + title, no time range.
2. `TodaySchedule` below — timed entries as today.

A subtle horizontal separator (`Border BorderBrush=BorderSubtle BorderThickness=0,0,0,1`) between them when both are non-empty.

Left-click a whole-day row → open editor dialog. Right-click → Edit / Delete.

### "Upcoming Events" card (new)

New card in the same `WrapPanel` (Width=380, `CardStyle`), placed after "Today's Schedule". Header: `UPCOMING EVENTS`. Empty-state: `"No upcoming events."`.

Rows: pin icon + title + right-aligned relative label (`"Today"`, `"Tomorrow"`, `"in N days"`). Sorted by `DaysUntil` ascending.

Click behaviors match Today's rows (left-click edit, right-click Edit / Delete).

### ViewModel additions

```csharp
[ObservableProperty]
private ObservableCollection<DayEventOccurrence> todayDayEvents = [];

[ObservableProperty]
private ObservableCollection<UpcomingDayEventItem> upcomingDayEvents = [];
```

`UpcomingDayEventItem`:
```csharp
public class UpcomingDayEventItem
{
    public DayEventOccurrence Occurrence { get; init; } = null!;
    public string Title => Occurrence.Title;
    public int DaysUntil { get; init; }
    public string RelativeLabel => DaysUntil switch
    {
        0 => "Today",
        1 => "Tomorrow",
        _ => $"in {DaysUntil} days"
    };
    public DateOnly Date => Occurrence.Date;
}
```

`Refresh()` adds:
```csharp
TodayDayEvents      = new(_calendarService.GetDayEventsForRange(today, today));
UpcomingDayEvents   = new(_calendarService.GetUpcomingDayEvents(today)
                           .Select(o => new UpcomingDayEventItem { Occurrence = o, DaysUntil = o.DaysUntil(today) }));
```

## Files

### New
- `src/ActivityTracker/Models/DayEvent.cs`
- `src/ActivityTracker/Views/Dialogs/DayEventEditorDialog.xaml`
- `src/ActivityTracker/Views/Dialogs/DayEventEditorDialog.xaml.cs`

### Modified
- `src/ActivityTracker/Models/AppData.cs` — add `DayEvents` list.
- `src/ActivityTracker/Services/ICalendarService.cs` — add two methods; co-locate `DayEventOccurrence` DTO here alongside `CalendarEntryItem` (existing convention in this file).
- `src/ActivityTracker/Services/CalendarService.cs` — implement two methods.
- `src/ActivityTracker/Services/IDialogService.cs` — add `ShowDayEventEditor`.
- `src/ActivityTracker/Services/DialogService.cs` — implement.
- `src/ActivityTracker/ViewModels/DayViewModel.cs` — add `DayEvents`.
- `src/ActivityTracker/ViewModels/WeekViewModel.cs` — add `DayEvents` on `DayColumn`.
- `src/ActivityTracker/ViewModels/MonthViewModel.cs` — populate `DayEvents` on `MonthDayCell`.
- `src/ActivityTracker/ViewModels/DashboardViewModel.cs` — add `TodayDayEvents`, `UpcomingDayEvents`, `UpcomingDayEventItem`.
- `src/ActivityTracker/Views/DayView.xaml` + `.xaml.cs` — restructure for pinned all-day row; context menu + pill interactions.
- `src/ActivityTracker/Views/WeekView.xaml` + `.xaml.cs` — restructure header out of ScrollViewer; all-day row; context menu + pill interactions.
- `src/ActivityTracker/Views/MonthView.xaml` + `.xaml.cs` — add icon + tooltip + context menu.
- `src/ActivityTracker/Views/DashboardView.xaml` + `.xaml.cs` — extend Today card; new Upcoming card.

No changes to `JsonDataService`, `RecurrenceService`, `StatisticsService`.

## Build sequence

1. Data model (`DayEvent`, `AppData`).
2. DTO + service interface + implementation.
3. Editor dialog + `IDialogService` wiring.
4. Dashboard (easiest to validate end-to-end with no calendar-view changes yet).
5. Day view (all-day row + context menus).
6. Week view (header restructure + all-day row + context menus).
7. Month view (icon + tooltip + context menus).
8. `dotnet publish ... -o publish` as the final step (user launches from `publish/`).

## Risks

- **Week view header restructure** — moving the day-header `ItemsControl` out of the `ScrollViewer` changes horizontal sizing. Needs to share a width with the scrolling content below (hour gutter column + day columns). Mitigate with a shared `Grid.ColumnDefinitions` structure or a `SharedSizeGroup`. Verify in the UI that columns align.
- **Context-menu conflict on `DragCanvas`** — none expected (left vs. right button), but verify mouse-capture doesn't swallow the right-click.
- **Recurrence dialog duplication** — cosmetic divergence risk over time. Accepted for this round.
