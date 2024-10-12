namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to with a parameterless route.
/// </summary>
public interface IRoutedViewModel : IRoutedViewModelBase
{
    /// <summary>
    /// Called when the view model is navigated to.
    /// </summary>
    public Task OnNavigatedToAsync(INavigator navigator, NavigationArgs args);
}

/// <summary>
/// Represents a view model that can be navigated to with a route that contains parameters.
/// </summary>
/// <typeparam name="TParam">The type that contains the parameters. Can be a tuple in the case of multiple parameters.</typeparam>
public interface IRoutedViewModel<TParam> : IRoutedViewModelBase
{
    /// <summary>
    /// Called when the view model is navigated to.
    /// </summary>
    public Task OnNavigatedToAsync(INavigator navigator, TParam param, NavigationArgs args);
}
