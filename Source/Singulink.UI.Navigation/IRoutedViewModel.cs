namespace Singulink.UI.Navigation;

public interface IRoutedViewModel : IRoutedViewModelBase
{
    public Task OnNavigatedToAsync(INavigator navigator, NavigationArgs args);
}

public interface IRoutedViewModel<TParam> : IRoutedViewModelBase
{
    public Task OnNavigatedToAsync(INavigator navigator, TParam param, NavigationArgs args);
}
