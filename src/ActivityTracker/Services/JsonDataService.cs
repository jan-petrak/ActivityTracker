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

        await using var stream = File.OpenRead(DataFilePath);
        Data = await JsonSerializer.DeserializeAsync<AppData>(stream, JsonOptions) ?? new AppData();
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
}
