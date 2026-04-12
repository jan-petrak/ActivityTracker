using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ActivityTracker.Services;

namespace ActivityTracker.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly INavigationService _navigationService;

    [ObservableProperty]
    private ObservableObject? currentView;

    [ObservableProperty]
    private string currentSection = "Dashboard";

    public MainViewModel(
        INavigationService navigationService,
        DashboardViewModel dashboardViewModel)
    {
        _navigationService = navigationService;
        _navigationService.CurrentViewChanged += () =>
        {
            CurrentView = _navigationService.CurrentView;
        };

        // Start on Dashboard
        CurrentView = dashboardViewModel;
    }

    [RelayCommand]
    private void NavigateTo(string section)
    {
        CurrentSection = section;
        switch (section)
        {
            case "Dashboard":
                _navigationService.NavigateTo<DashboardViewModel>();
                break;
            case "Calendar":
                _navigationService.NavigateTo<CalendarViewModel>();
                break;
            case "Activities":
                _navigationService.NavigateTo<ActivitiesViewModel>();
                break;
            case "Statistics":
                _navigationService.NavigateTo<StatisticsViewModel>();
                break;
        }
    }
}
