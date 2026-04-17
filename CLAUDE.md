# CLAUDE.md

This file provides guidance to Claude Code when working with code in this repository.

## Build, run, publish

All commands run from the repo root (`D:\repos\ActivityTracker`).

```
dotnet build src/ActivityTracker/ActivityTracker.csproj
```

```
dotnet run --project src/ActivityTracker/ActivityTracker.csproj
```

**Publish (required after code changes — the user launches from `publish\ActivityTracker.exe`, not from `bin/`):**

```
dotnet publish src/ActivityTracker/ActivityTracker.csproj -c Release -r win-x64 --self-contained false -o publish
```

`-o publish` forces output to the repo-root `publish/` folder. Do not rely on the default nested path.

If the publish is denied because the app is already running, ask the user to turn it off.

There are no tests.

## Architecture

WPF desktop app (`net10.0-windows`). Single project under `src/ActivityTracker/`. MVVM via CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`).

**Startup / DI** — `App.xaml.cs` builds a `ServiceProvider` and stores it in `App.Services` (static). Services are singletons; ViewModels are transient. `LoadAsync()` on `IDataService` runs before `MainWindow` is shown.

**Navigation** — `MainWindow` uses implicit `DataTemplate`s (keyed by ViewModel type) inside a `ContentControl` bound to `MainViewModel.CurrentView`. `NavigationService.NavigateTo<TViewModel>()` resolves the VM from DI and raises an event; `MainViewModel` mirrors it into `CurrentView`. To add a section: register the VM in `App.xaml.cs`, add a `DataTemplate` in `MainWindow.xaml`, add a case in `MainViewModel.NavigateTo`.

**Persistence** — `JsonDataService` is the single source of truth. `Data` (an `AppData` aggregate of `Groups`, `TimeEntries`, `PlannedEntries`, `Goals`) lives in memory. File is `%LOCALAPPDATA%\ActivityTracker\data.json`. Mutations call `NotifyChanged()`, which debounces a save by 300 ms. Load is async at startup; saves are fire-and-forget.

**Calendar rendering** — Views in `Views/` (DayView, WeekView, MonthView) use `Canvas` with 1 px = 1 minute (1440 px tall, scrolled to 7:00). Entry blocks are positioned in `Canvas_Loaded` code-behind (not via bindings) by reading `StartTime`/`EndTime` on each `CalendarEntryItem`. Drag-to-create (Day/Week views) opens `PlannedEntryEditorDialog` via `IDialogService`.

**Entry merging** — `CalendarService.GetEntriesForRange(start, end)` is the single entry point used by every calendar VM. It merges logged `TimeEntry` records with `PlannedEntry` occurrences (expanded via `RecurrenceService`) into a flat `List<CalendarEntryItem>`. `IsPlanned` distinguishes the two in rendering.

**Recurrence** — `RecurrenceService.ExpandOccurrences(pattern, rangeStart, rangeEnd)` yields `DateOnly` values. Only one recurrence per pattern; branches on `RecurrenceType.{Daily,Weekly,Monthly}`. `Exceptions` is a set of skipped dates.

**Dialogs** — `IDialogService` wraps modal `Window` dialogs in `Views/Dialogs/` (Group, Activity, TimeEntry, PlannedEntry, Goal editors). These are **not** MVVM — each dialog has a code-behind `Save_Click` that validates inputs, writes to a `Result` property, and sets `DialogResult = true`. Call sites mutate `dataService.Data` and call `NotifyChanged()`.

## Theming — `App.xaml`

Dark "Midnight Studio" theme. All styling lives in `App.xaml` as `Application.Resources`. Semantic brushes (use these, don't hardcode hex):

- Surfaces: `BgDeep`, `BgBase`, `BgSurface`, `BgCard`, `BgHover`
- Borders: `BorderSubtle`, `BorderMedium`
- Text: `TextPrimary`, `TextSecondary`, `TextMuted`
- Accents: `AccentAmber`, `AccentAmberHover`, `AccentAmberSubtle`, `AccentBlue`, `DangerRed`, `SuccessGreen`

Most controls (`Button`, `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `TreeView`, `ListView`) are retemplated to this palette. Date input is done via plain `TextBox` in `YYYY-MM-DD` format (the stock WPF `Calendar` popup cannot be cleanly dark-themed). **Do not set `Application.ThemeMode` / `Window.ThemeMode`** — it conflicts with the custom control templates (notably breaks `ComboBox`).

## Model invariants

- `TimeEntry` and `PlannedEntry` both use `DateOnly` for `Date` and `TimeOnly` for `StartTime`/`EndTime`. JSON serialization uses camelCase with `JsonStringEnumConverter`.
- Entries reference an `Activity` by `Guid`; the activity lives inside an `ActivityGroup`. The flat activity lookup is built on demand in `CalendarService` via `SelectMany`.
- `PlannedEntry.Recurrence` is optional. If null, the entry occurs only on `Date`. If set, `Date`/`StartDate` is the series anchor.
