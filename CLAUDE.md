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

**Persistence** — `JsonDataService` is the single source of truth. `Data` (an `AppData` aggregate of `Groups`, `PlannedEntries`, `Goals`) lives in memory. File is `%LOCALAPPDATA%\ActivityTracker\data.json`. Mutations call `NotifyChanged()`, which debounces a save by 300 ms. Load is async at startup; saves are fire-and-forget.

**Calendar rendering** — Views in `Views/` (DayView, WeekView, MonthView) use `Canvas` with 1 px = 1 minute (1440 px tall, scrolled to 7:00). Entry blocks are positioned in `Canvas_Loaded` code-behind (not via bindings) by reading `StartTime`/`EndTime` on each `CalendarEntryItem`. Drag-to-create (Day/Week views) opens `PlannedEntryEditorDialog` via `IDialogService`.

**Entry merging** — `CalendarService.GetEntriesForRange(start, end)` is the single entry point used by every calendar VM. It expands `PlannedEntry` records (via `RecurrenceService` when recurring) into a flat `List<CalendarEntryItem>`, keyed by activity + group for display.

**Recurrence** — `RecurrenceService.ExpandOccurrences(pattern, rangeStart, rangeEnd)` yields `DateOnly` values. Only one recurrence per pattern; branches on `RecurrenceType.{Daily,Weekly,Monthly}`. `Exceptions` is a set of skipped dates.

**Dialogs** — `IDialogService` wraps modal `Window` dialogs in `Views/Dialogs/` (Group, Activity, PlannedEntry, Goal editors). These are **not** MVVM — each dialog has a code-behind `Save_Click` that validates inputs, writes to a `Result` property, and sets `DialogResult = true`. Call sites mutate `dataService.Data` and call `NotifyChanged()`.

## Theming — `App.xaml` + `Themes/`

Dark "Midnight Studio" theme. `App.xaml` is a thin shell that merges the dictionaries under `Themes/`:

- `Themes/Palette.xaml` — colors and semantic brushes
- `Themes/Typography.xaml` — global `TextBlock` defaults, `SectionHeader`, `CardHeader`
- `Themes/Buttons.xaml` — `PrimaryButtonStyle`, `SecondaryButtonStyle`, `DangerButtonStyle`, `NavButtonStyle`, `ViewModeButtonStyle`
- `Themes/Inputs.xaml` — `TextBox` (default + `DarkTextBoxStyle`), `ComboBox`, `DatePicker`, `CheckBox`, `RadioButton`
- `Themes/Containers.xaml` — `CardStyle`, `TreeView`, `ListView`, `GridViewColumnHeader`, `ScrollViewer`
- `Themes/Menus.xaml` — `ContextMenu`, `MenuItem`, `Separator`, `ToolTip`
- `Themes/Windows.xaml` — `DialogWindowStyle` (custom title bar + `shell:WindowChrome` for dialogs), `CaptionButtonStyle`, `CloseCaptionButtonStyle`

Ordering matters: `Palette.xaml` must be merged first — everything else references its brushes via `StaticResource`.

Semantic brushes (use these, don't hardcode hex):

- Surfaces: `BgDeep`, `BgBase`, `BgSurface`, `BgCard`, `BgHover`
- Borders: `BorderSubtle`, `BorderMedium`
- Text: `TextPrimary`, `TextSecondary`, `TextMuted`
- Accents: `AccentAmber`, `AccentAmberHover`, `AccentAmberSubtle`, `AccentBlue`, `DangerRed`, `DangerRedHover`

Most controls (`Button`, `TextBox`, `ComboBox`, `CheckBox`, `RadioButton`, `TreeView`, `ListView`, `ContextMenu`, `MenuItem`, `ToolTip`) are retemplated to this palette. Date input is done via plain `TextBox` in `YYYY-MM-DD` format (the stock WPF `Calendar` popup cannot be cleanly dark-themed). **Do not set `Application.ThemeMode` / `Window.ThemeMode`** — it conflicts with the custom control templates (notably breaks `ComboBox`).

**Every new UI surface must honor the dark theme.** Before introducing a new control, popup, menu, tooltip, or dialog, verify it doesn't fall back to Windows' default chrome (light backgrounds, system fonts). In particular:

- **Never call `MessageBox.Show`.** Use `Views/Dialogs/MessageDialog` (`MessageDialog.ShowInfo` / `ShowConfirm`) — a themed modal that matches the rest of the app.
- **New control types need a retemplate.** If you introduce a `ContextMenu`/`MenuItem`/`ToolTip`/`Popup`/`Expander`/etc. that isn't already styled under `Themes/`, add a style there. Confirm in a running build — stock WPF defaults are light and won't visually surface as broken until the feature is tested.
- **No hardcoded hex.** Use the semantic brushes above. If a new role is needed, add it to `Palette.xaml` rather than inlining a color.

## Model invariants

- `PlannedEntry` uses `DateOnly` for `Date` and `TimeOnly` for `StartTime`/`EndTime`. JSON serialization uses camelCase with `JsonStringEnumConverter`.
- Entries reference an `Activity` by `Guid`; the activity lives inside an `ActivityGroup`. The flat activity lookup is built on demand in `CalendarService` via `SelectMany`.
- `PlannedEntry.Recurrence` is optional. If null, the entry occurs only on `Date`. If set, `Date`/`StartDate` is the series anchor.
