using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;
using Singulink.UI.Navigation.MvvmToolkit;
using Singulink.UI.Tasks;

namespace Playground.ViewModels;

public partial class LoginViewModel : RoutedObservableViewModel, IProvideTaskRunner
{
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    public INavigator Navigator { get => field ?? throw new InvalidOperationException("Navigator not set."); set; }

    public ITaskRunner TaskRunner { get => field ?? throw new InvalidOperationException("Task runner not set."); set; }

    public override async ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        Navigator = navigator;
        await Task.Delay(500); // Simulate async loading
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        // Simulate a login process (email and password would be checked here)

        await TaskRunner.RunAsBusyAsync(async () => await Task.Delay(2000));

        // Navigate to the main view after successful login
        await Navigator.NavigateAsync(Routes.MainRoute);
        Navigator.ClearHistory();
    }
}
