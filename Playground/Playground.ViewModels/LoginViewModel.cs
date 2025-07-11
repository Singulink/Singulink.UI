using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Singulink.UI.Navigation;

namespace Playground.ViewModels;

public partial class LoginViewModel : ObservableObject, IRoutedViewModel
{
    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Password { get; set; } = string.Empty;

    public INavigator Navigator => this.GetNavigator();

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        Navigator.ClearHistory();
        await Task.Delay(500); // Simulate async loading
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        // Simulate a login process (email and password would be checked here)

        await Navigator.TaskRunner.RunAsBusyAsync(async () => await Task.Delay(2000));

        // Navigate to the main view after successful login

        await Navigator.NavigateAsync(Routes.MainRoot);
        Navigator.ClearHistory();
    }
}
