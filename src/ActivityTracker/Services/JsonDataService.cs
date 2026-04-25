using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using ActivityTracker.Models;

namespace ActivityTracker.Services;

public class JsonDataService : IDataService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ActivityTracker");

    private static readonly string DataFilePath = Path.Combine(DataDirectory, "data.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private CancellationTokenSource? _debounceCts;

    public AppData Data { get; private set; } = new();

    public async Task LoadAsync()
    {
        if (!File.Exists(DataFilePath))
        {
            Data = new AppData();
            return;
        }

        var json = await File.ReadAllTextAsync(DataFilePath);

        // Migrate old format (date + startTime + endTime) to new (start + end DateTime)
        if (json.Contains("\"startTime\""))
        {
            json = MigrateV1ToV2(json);
            await File.WriteAllTextAsync(DataFilePath, json);
        }

        Data = JsonSerializer.Deserialize<AppData>(json, JsonOptions) ?? new AppData();
    }

    public async Task SaveAsync()
    {
        Directory.CreateDirectory(DataDirectory);
        await using var stream = File.Create(DataFilePath);
        await JsonSerializer.SerializeAsync(stream, Data, JsonOptions);
    }

    public void NotifyChanged()
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await SaveAsync();
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    private static string MigrateV1ToV2(string json)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(json)!.AsObject();
        var entries = root["plannedEntries"]?.AsArray();
        if (entries == null) return json;

        foreach (var node in entries)
        {
            var entry = node!.AsObject();
            if (entry.ContainsKey("start")) continue; // already new format

            var dateStr = entry["date"]?.GetValue<string>();
            var startStr = entry["startTime"]?.GetValue<string>();
            var endStr = entry["endTime"]?.GetValue<string>();
            if (dateStr == null || startStr == null || endStr == null) continue;

            var date = DateOnly.Parse(dateStr);
            var startTime = TimeOnly.Parse(startStr);
            var endTime = TimeOnly.Parse(endStr);

            var start = date.ToDateTime(startTime);
            // Treat TimeOnly.MinValue (00:00) with startTime > 00:00 as midnight end (old sentinel)
            var endDate = endTime == TimeOnly.MinValue && startTime > TimeOnly.MinValue
                ? date.AddDays(1)
                : (endTime < startTime ? date.AddDays(1) : date);
            var end = endDate.ToDateTime(endTime);

            entry["start"] = start.ToString("o");
            entry["end"] = end.ToString("o");
            entry.Remove("date");
            entry.Remove("startTime");
            entry.Remove("endTime");
        }

        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }
}
