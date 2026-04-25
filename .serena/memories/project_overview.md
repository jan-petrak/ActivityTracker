# Project Overview

WPF desktop activity/time-tracking app targeting net10.0-windows, single project under src/ActivityTracker/.

## Tech Stack
- C# / .NET 10 / WPF
- CommunityToolkit.Mvvm ([ObservableProperty], [RelayCommand])
- MVVM architecture, DI via Microsoft.Extensions.DependencyInjection (App.Services static)
- JSON persistence via JsonDataService (%LOCALAPPDATA%\ActivityTracker\data.json)

## Key Architecture
- Navigation: MainViewModel.CurrentView + implicit DataTemplates in MainWindow.xaml
- Calendar: Canvas-based (1px = 1min, 1440px tall). Entry blocks positioned in code-behind.
- Drag-to-create: DayView/WeekView code-behind handle mouse events.
- Dialogs: Not MVVM — code-behind Save_Click writes to Result property, sets DialogResult=true.
- Recurrence: RecurrenceService.ExpandOccurrences yields DateOnly values.
- CalendarService.GetEntriesForRange is the single entry point for all calendar VMs.

## Midnight convention
EndTime = TimeOnly(0,0) with StartTime > TimeOnly(0,0) means "end of day / midnight".
PositionEntryBlocks/PositionWeekEntries handle this: endMin = EndTime == TimeOnly.MinValue ? 24*60 : normal.
PlannedEntryRescheduler and PlannedEntryEditorDialog both allow this special case.
