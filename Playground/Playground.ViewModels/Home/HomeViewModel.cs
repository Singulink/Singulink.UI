using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace Playground.ViewModels.Home;

public partial class HomeViewModel : ObservableObject, IRoutedViewModel
{
    [ObservableProperty]
    public partial string Message { get; private set; } = string.Empty;

    public Task OnNavigatedToAsync(NavigationArgs args)
    {
        Message = "Welcome to the Home View!";
        return Task.CompletedTask;
    }
}
