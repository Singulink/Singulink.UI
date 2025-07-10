using Singulink.UI.Navigation.InternalServices;

namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to.
/// </summary>
public interface IRoutedViewModelBase
{
    /// <summary>
    /// Gets a value indicating whether the view can be cached in the back navigation stack. Defaults to <see langword="true"/>. You may want to set to this to <see
    /// langword="false"/> if the view or view model can use a large amount of memory, in which case it will be recreated if navigated to again.
    /// </summary>
    /// <remarks>
    /// Use <see cref="INavigatorBuilder.MaxNavigationStacksSize"/> and <see cref="INavigatorBuilder.MaxBackStackCachedViewDepth"/> to control the maximum depth of
    /// the back navigation stack and its view cache.
    /// </remarks>
    public bool CanViewBeCached => true;

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
    /// method on the view model's <see cref="INavigator"/> before the task completes, in which case it should stop any further processing and complete its
    /// task.</para>
    /// </remarks>
    public Task OnNavigatedToAsync(NavigationArgs args) => Task.CompletedTask;

    /// <summary>
    /// Invoked when the view model is being navigated away from and allows the navigation to be cancelled.
    /// </summary>
    /// <remarks>
    /// This method can show dialogs as long as they are closed before the task completes. The <see cref="NavigatingArgs.Cancel"/> property is checked on
    /// <paramref name="args"/> after the task returned by this method completes. This method cannot reroute to another destination.
    /// </remarks>
    public Task OnNavigatingFromAsync(NavigatingArgs args) => Task.CompletedTask;

    /// <summary>
    /// Invoked when the view model is navigated away from.
    /// </summary>
    /// <remarks>
    /// This method cannot show dialogs or cancel/reroute the navigation.
    /// </remarks>
    public void OnNavigatedFrom() { }
}
