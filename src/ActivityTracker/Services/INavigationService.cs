using CommunityToolkit.Mvvm.ComponentModel;

namespace ActivityTracker.Services;

public interface INavigationService
{
    ObservableObject CurrentView { get; }
    event Action? CurrentViewChanged;
    void NavigateTo<TViewModel>() where TViewModel : ObservableObject;
}
