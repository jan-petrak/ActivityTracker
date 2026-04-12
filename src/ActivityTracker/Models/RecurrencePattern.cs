using System.Text.Json.Serialization;

namespace ActivityTracker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RecurrenceType
{
    Daily,
    Weekly,
    Monthly
}

public class RecurrencePattern
{
    public RecurrenceType Type { get; set; }
    public int Interval { get; set; } = 1;
    public List<DayOfWeek> DaysOfWeek { get; set; } = [];
    public int? DayOfMonth { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public List<DateOnly> Exceptions { get; set; } = [];
}
