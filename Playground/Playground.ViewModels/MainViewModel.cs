using Singulink.UI.Navigation;
using Singulink.UI.Navigation.MvvmToolkit;
using Singulink.UI.Tasks;

namespace Playground.ViewModels;

public class MainViewModel : RoutedObservableViewModel, IProvideTaskRunner
{
    public ITaskRunner TaskRunner { get => field ?? throw new InvalidOperationException("Task runner not set."); set; }

    public INavigator Navigator { get => field ?? throw new InvalidOperationException("Navigator not set."); set; }

    public override async ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args)
    {
        Navigator = navigator;

        if (!args.AlreadyNavigatedTo)
            await Task.Delay(1000); // Simulate async loading

        if (!args.HasNestedNavigation)
            await navigator.NavigatePartialAsync(Routes.Main.HomeRoute);
    }
}
