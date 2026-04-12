using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;

namespace ActivityTracker.Services;

public class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ObservableObject CurrentView { get; private set; } = null!;
    public event Action? CurrentViewChanged;

    public void NavigateTo<TViewModel>() where TViewModel : ObservableObject
    {
        CurrentView = _serviceProvider.GetRequiredService<TViewModel>();
        CurrentViewChanged?.Invoke();
    }
}
