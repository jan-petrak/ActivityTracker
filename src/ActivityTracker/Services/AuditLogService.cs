using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ActivityTracker.Services;

public class AuditLogService : IAuditLogService
{
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ActivityTracker");

    private static readonly string LogFilePath = Path.Combine(DataDirectory, "audit.log.jsonl");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly object _writeLock = new();

    public void Log(string action, string summary, object? data = null)
    {
        var entry = new AuditEntry(
            Ts: DateTimeOffset.Now,
            Action: action,
            Summary: summary,
            Data: data);

        var line = JsonSerializer.Serialize(entry, JsonOptions);

        lock (_writeLock)
        {
            Directory.CreateDirectory(DataDirectory);
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
    }

    private sealed record AuditEntry(
        DateTimeOffset Ts,
        string Action,
        string Summary,
        object? Data);
}
