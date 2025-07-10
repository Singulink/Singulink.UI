using CommunityToolkit.Mvvm.ComponentModel;
using Singulink.UI.Navigation;

namespace Playground.ViewModels;

public class MainViewModel : ObservableObject, IRoutedViewModel
{
    public INavigator Navigator => this.GetNavigator();

    public async Task OnNavigatedToAsync(NavigationArgs args)
    {
        if (!args.AlreadyNavigatedTo)
            await Task.Delay(1000); // Simulate async loading

        if (!args.HasNestedNavigation)
            await Navigator.NavigatePartialAsync(Routes.Main.HomeRoute);
    }
}
