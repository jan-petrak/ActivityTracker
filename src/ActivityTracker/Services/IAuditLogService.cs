namespace ActivityTracker.Services;

public interface IAuditLogService
{
    void Log(string action, string summary, object? data = null);
}
