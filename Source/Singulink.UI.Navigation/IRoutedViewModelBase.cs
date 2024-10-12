namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to.
/// </summary>
public interface IRoutedViewModelBase
{
    /// <summary>
    /// Invoked when the view model is navigated to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// All view models in the route have this method called on each navigation, even if they are already navigated to (see <see
    /// cref="NavigationArgs.AlreadyNavigatedTo"/> for more information).</para>
    /// <para>
    /// This method can show dialogs as long as <see cref="NavigationArgs.HasNestedNavigation"/> on <paramref name="args"/> is <see langword="false"/> or the
    /// dialogs are closed before the task completes. This method can cancel the current navigation and reroute to another destination by calling a navigation
    /// method on the <see cref="INavigator"/> before the task completes, in which case it should stop any further processing and complete its task.</para>
    /// </remarks>
    public ValueTask OnNavigatedToAsync(INavigator navigator, NavigationArgs args);

    /// <summary>
    /// Invoked when the view model is being navigated away from and allows the navigation to be cancelled.
    /// </summary>
    /// <remarks>
    /// This method can show dialogs as long as they are closed before the task completes. The <see cref="NavigatingArgs.Cancel"/> property is checked on
    /// <paramref name="args"/> after the task returned by this method completes. This method cannot reroute to another destination.
    /// </remarks>
    public ValueTask OnNavigatingFromAsync(INavigator navigator, NavigatingArgs args);

    /// <summary>
    /// Invoked when the view model is navigated away from.
    /// </summary>
    /// <remarks>
    /// This method cannot show dialogs or cancel/reroute the navigation.
    /// </remarks>
    public void OnNavigatedFrom();
}
