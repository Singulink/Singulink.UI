namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to using a route.
/// </summary>
public interface IRoutedViewModelBase
{
    /// <summary>
    /// Called when the view model is navigated from.
    /// </summary>
    public Task OnNavigatedFrom();
}
