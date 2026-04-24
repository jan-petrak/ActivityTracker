# Drag-and-drop rescheduling for Day and Month views

**Date:** 2026-04-24
**Status:** Approved for implementation planning

## Goal

Let the user reschedule planned entries by dragging them in DayView and MonthView, with appropriate handling for recurring series (prompt for "this occurrence" vs "whole series").

## Scope

In scope:
- **DayView**: drag an entry block to move (preserving duration) or drag its top/bottom edge to resize. Snap to 15 minutes.
- **MonthView**: drag a timed-entry row from one day cell to another, changing the entry's date (time of day unchanged).
- Recurring-entry prompt on every successful drop that would mutate a recurring series.
- Cancel via `Escape` during an active drag.

Out of scope:
- Dragging whole-day events (their icon is a summary marker; reschedule via the editor).
- WeekView (existing drag-to-create stays as is; rescheduling can be a later pass).
- Cross-day drags inside DayView (DayView shows one day only).
- Schema changes to `PlannedEntry` / `Recurrence`.

## Shared pieces

### `RecurrencePromptDialog` — new themed modal

Lives in `Views/Dialogs/`. Matches `MessageDialog` in style (dark theme, `DialogWindowStyle`). Three outcomes exposed as `RecurrenceEditScope`:

```csharp
public enum RecurrenceEditScope
{
    Cancel,
    ThisOccurrence,
    WholeSeries,
}
```

Static helper (mirrors `MessageDialog.ShowConfirm`):

```csharp
public static RecurrenceEditScope Show(string title, string message);
```

### `PlannedEntryRescheduler` — static helper

New static class in `Services/`:

```csharp
public static bool TryReschedule(
    IDataService data,
    IAuditLogService audit,
    PlannedEntry entry,
    DateOnly occurrenceDate,
    DateOnly newDate,
    TimeOnly newStart,
    TimeOnly newEnd,
    RecurrenceEditScope scope);
```

Behavior:

- **Non-recurring entry** (caller skips the prompt; `scope` is ignored): mutate `Date`, `StartTime`, `EndTime` in place. Log `PlannedEntryRescheduled`.
- **Recurring + `WholeSeries`**:
  - Compute `dayDelta = newDate.DayNumber - occurrenceDate.DayNumber`, `timeDelta` from `newStart - entry.StartTime`.
  - Shift `entry.Date` and `entry.Recurrence.StartDate` by `dayDelta`.
  - For weekly recurrence with `DaysOfWeek`: shift each day by `dayDelta % 7` (mod, positive).
  - Shift `StartTime`/`EndTime` by `timeDelta`.
  - `Exceptions` left as-is (users should expect to re-confirm skips if they shift a series).
- **Recurring + `ThisOccurrence`**:
  - Add `occurrenceDate` to `entry.Recurrence.Exceptions`.
  - Create a new non-recurring `PlannedEntry` with the same `ActivityId`, `Date = newDate`, `StartTime = newStart`, `EndTime = newEnd`. Append to `data.Data.PlannedEntries`.
- **`Cancel`**: no-op, return `false`.

After any successful mutation: `data.NotifyChanged()` and `audit.Log(...)`. Return `true`.

## DayView drag

**Hit zones on each entry block** (the `Border` currently tagged with `SourceId`):

- Top 6 px → `ResizeTop`
- Bottom 6 px → `ResizeBottom`
- Rest → `Move`

### State machine

1. `MouseLeftButtonDown` on block: capture cursor point and hit-zone, capture mouse on the block, but do **not** commit to drag yet.
2. `MouseMove` (captured): if `|dy|` ≥ 4 px, enter active drag — show a ghost preview (same styling as existing `DragPreview`), fade the source block to `Opacity = 0.4`, set `Cursor` to `SizeNS` (resize) or `SizeAll` (move).
3. `MouseMove` during active drag: snap `e.GetPosition(DragCanvas).Y` to 15 minutes.
   - `Move`: shift both endpoints by the same delta; clamp so neither crosses 0 or 1440.
   - `ResizeTop`: move start; clamp so duration ≥ 15.
   - `ResizeBottom`: move end; clamp so duration ≥ 15.
4. `MouseLeftButtonUp`:
   - If never entered active drag → treat as click → `vm.EditEntry(id)` (current behavior).
   - If active drag → compute `newStart` / `newEnd`. If unchanged, no-op. Otherwise:
     - If entry is recurring → `RecurrencePromptDialog.Show(...)`; pipe result into `PlannedEntryRescheduler.TryReschedule(...)`.
     - Else → call rescheduler directly with `RecurrenceEditScope.WholeSeries` (harmless for non-recurring).
     - `newDate` is always `vm.Date` in DayView.
   - Reload VM.
5. `PreviewKeyDown` on the view with `Key == Escape` while capture is active → cancel, hide ghost, restore source opacity, release capture.

### Cursor hinting without active drag

Handle `MouseMove` on the block (no button down) to update `Cursor` based on hit-zone, so the user sees `SizeNS` near the edges before they click.

### Interaction with existing drag-to-create

Entry blocks set `e.Handled = true` on `MouseLeftButtonDown` today — the canvas handler doesn't fire. That remains true; entry-drag replaces the pure edit-on-click path but still stops propagation.

## MonthView drag

### Timed-entry rows

Day cells already have `Tag="{Binding Date}"` on the outer `Border`. Entry rows have `Tag="{Binding SourceId}"`.

1. `MouseLeftButtonDown` on row: record point (in `UserControl` coords), source `SourceId` + source `Date` (walk up visual tree to find parent cell, read its `Tag`), capture mouse on the row.
2. `MouseMove` (captured): if moved ≥ 4 px, enter active drag.
   - Create ghost: a small `Border` with the row's summary text. Host it in an overlay `Canvas` added to the `UserControl`'s root `DockPanel` (as its own layer) with `Panel.ZIndex` high. Position via `Canvas.Left` / `Canvas.Top`.
3. `MouseMove` during active drag:
   - Move ghost to cursor.
   - `VisualTreeHelper.HitTest` at cursor; walk up until a `Border` whose `Tag is DateOnly` is found.
   - Clear highlight on previously highlighted cell (reset `BorderBrush` / `BorderThickness` to defaults). Highlight new target (`BorderBrush = AccentAmber`, `BorderThickness = 1.5`).
4. `MouseLeftButtonUp`:
   - No active drag → existing `EntryRow_MouseLeftButtonUp` path (edit).
   - Active drag, no target → cancel.
   - Active drag, target found:
     - If `targetDate == sourceDate` → cancel.
     - Else: if recurring → prompt; call `PlannedEntryRescheduler.TryReschedule` with `newDate = targetDate`, `newStart = entry.StartTime`, `newEnd = entry.EndTime`.
     - Reload VM.
5. `PreviewKeyDown` with `Escape` → cancel, clear ghost and highlight, release capture.

### Adjacent-month cells

Cells outside the current month are rendered at `Opacity = 0.5` but still have `Tag = Date`. They are valid drop targets — the reschedule writes to that date; the user stays on the current month view (no navigation). If they want to see the moved entry, they can navigate manually.

### Whole-day event icon

Not draggable in this pass (out of scope). Existing click / right-click behavior is unchanged.

## Error handling

- If `IDialogService` / `IDataService` / `IAuditLogService` resolution fails in the drag handler, silently drop the drag (matches the pattern in the existing drag-to-create handler). This only happens if DI is misconfigured.
- If the rescheduler encounters a recurring entry whose `Recurrence` field is somehow null while in "WholeSeries" recurring path, treat as non-recurring.
- Drops onto valid dates that would produce no change (same date/time) are no-ops, no prompt, no audit entry.

## Testing

No automated tests exist in this project. Manual verification steps:

1. DayView: drag a non-recurring entry vertically; verify time changes, duration preserved.
2. DayView: drag top edge; verify start changes, end fixed, min 15-min duration respected.
3. DayView: drag bottom edge; verify end changes, start fixed.
4. DayView: click without moving; verify editor opens (click behavior intact).
5. DayView: Escape mid-drag; verify entry snaps back, no mutation.
6. DayView: drag a recurring entry; verify prompt, "This occurrence" adds exception + spawns standalone, "Whole series" shifts anchor time.
7. MonthView: drag a timed entry to another day; verify date change.
8. MonthView: drag a recurring entry; verify prompt, both branches.
9. MonthView: drag to an adjacent-month greyed cell; verify date change applies.
10. MonthView: Escape mid-drag; verify no mutation.
11. Both views: drop in same position; verify no-op, no audit log.
12. Confirm whole-day event icon in MonthView still opens / context-menus normally (not accidentally grabbed by drag code).

Per project `CLAUDE.md`, after code changes:

```
dotnet publish src/ActivityTracker/ActivityTracker.csproj -c Release -r win-x64 --self-contained false -o publish
```

## Implementation order suggestion

1. `RecurrenceEditScope` enum + `RecurrencePromptDialog` (+ XAML) — no callers yet.
2. `PlannedEntryRescheduler` static helper — unit-of-logic, no UI.
3. DayView drag — move + resize + prompt wiring.
4. MonthView drag — row drag + ghost overlay + prompt wiring.
5. Manual verification sweep, then publish.
