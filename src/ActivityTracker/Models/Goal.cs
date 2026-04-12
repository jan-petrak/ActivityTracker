using System.Text.Json.Serialization;

namespace ActivityTracker.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GoalPeriod
{
    Weekly,
    Monthly
}

public class Goal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid GroupId { get; set; }
    public Guid? ActivityId { get; set; }
    public GoalPeriod Period { get; set; }
    public double TargetHours { get; set; }
    public bool IsActive { get; set; } = true;
}
