# Suggested Commands

## Build
```
dotnet build src/ActivityTracker/ActivityTracker.csproj
```

## Run (dev)
```
dotnet run --project src/ActivityTracker/ActivityTracker.csproj
```

## Publish (required after code changes — user launches from publish\ActivityTracker.exe)
```
dotnet publish src/ActivityTracker/ActivityTracker.csproj -c Release -r win-x64 --self-contained false -o publish
```

No tests, no linting step. Publish is the final step after every code change.
