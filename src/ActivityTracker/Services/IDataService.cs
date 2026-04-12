using ActivityTracker.Models;

namespace ActivityTracker.Services;

public interface IDataService
{
    AppData Data { get; }
    Task LoadAsync();
    Task SaveAsync();
    void NotifyChanged();
}
