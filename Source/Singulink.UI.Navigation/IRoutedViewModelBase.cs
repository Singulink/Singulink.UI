namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to.
/// </summary>
public interface IRoutedViewModelBase
{
    /// <summary>
    /// Gets a value indicating whether the view model and its associated view can be cached in the navigation stacks. Defaults to <see langword="false"/>,
    /// meaning a fresh view model and view are created every time the route is navigated to.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Return <see langword="true"/> to keep the view model and view alive while they are inactive so that returning to them (e.g. by navigating back) is
    /// instant and preserves their state. A cached view model is navigated to again on the same instance, so <see cref="OnNavigatedToAsync(NavigationArgs)"/>
    /// must handle being called repeatedly (e.g. by skipping work that was already done and not subscribing to events twice). If a parent view model that
    /// provided services to a child view model is evicted from cache then all of its children are also evicted.</para>
    /// <para>
    /// Use <see cref="INavigatorBuilder.ConfigureNavigationStacks(int, int, int)"/> to control the maximum depth of cached views and view models.</para>
    /// </remarks>
    public bool CanBeCached => false;

    /// <summary>
    /// Gets a value indicating whether the view model and its associated view can be retained by a <see cref="RoutePin"/> (see
    /// <see cref="INavigator.PinCurrentRoute"/>). Defaults to the value of <see cref="CanBeCached"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A pinned view model is navigated to again with its state intact when its route is returned to, so it must handle repeated calls to <see
    /// cref="OnNavigatedToAsync(NavigationArgs)"/> on the same instance in the same way a cached view model does. View models that opt out of caching
    /// therefore cannot be pinned unless they explicitly override this property to return <see langword="true"/>.</para>
    /// <para>
    /// When a route is pinned, the leaf view model must be pinnable and each of its ancestors is pinned as well up to the first ancestor that is not pinnable.
    /// Ancestors from that point up follow the normal caching rules.</para>
    /// </remarks>
    public bool CanBePinned => CanBeCached;

    /// <summary>
    /// Called when the view model is navigated to (i.e. becomes visible). May be called multiple times on the same view model instance if the view model is
    /// navigated away from but still cached when a route containing the view model is navigated to again. Calls are always paired with future calls to <see
    /// cref="OnNavigatedAwayAsync"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method can show dialogs as long as they are closed before the returned task completes or <see cref="NavigationArgs.HasChildNavigation"/> on
    /// <paramref name="args"/> is <see langword="false"/>.</para>
    /// <para>
    /// A redirection can be requested setting the <see cref="NavigationArgs.Redirect"/> property on <paramref name="args"/>. If a redirection is requested, the
    /// rest of the current navigation will be cancelled and the redirection will occur after the task returned by this method completes. This method will not
    /// be called again if the view model is still active in the redirected route.</para>
    /// </remarks>
    public Task OnNavigatedToAsync(NavigationArgs args) => Task.CompletedTask;

    /// <summary>
    /// Called when the view model is being navigated away from. Can be used to cancel the new navigation (e.g. if there is unsaved data).
    /// </summary>
    /// <remarks>
    /// This method can show dialogs as long as they are closed before the returned task completes. The <see cref="NavigatingArgs.Cancel"/> property on
    /// <paramref name="args"/> is checked after the task returned by this method completes to determine whether the new navigation should be cancelled.
    /// </remarks>
    public Task OnNavigatingAwayAsync(NavigatingArgs args) => Task.CompletedTask;

    /// <summary>
    /// Called when the view model is navigated away from.
    /// </summary>
    /// <remarks>
    /// This method cannot show dialogs or cancel/reroute the new navigation. It should only be used to clean up resources or unhook event handlers that were
    /// added in <see cref="OnNavigatedToAsync(NavigationArgs)"/>. Calls are always paired with previous calls to <see
    /// cref="OnNavigatedToAsync(NavigationArgs)"/>.
    /// </remarks>
    public Task OnNavigatedAwayAsync() => Task.CompletedTask;

    /// <summary>
    /// Called whenever a route that contains the view model is navigated to, even if the view model was already active in the previous route. If the view model
    /// was not already active in the previous route then it will be called after <see cref="OnNavigatedToAsync(NavigationArgs)"/>, and any time the route is
    /// refreshed or changed while the view model remains active in the new route.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method can show dialogs as long as they are closed before the returned task completes or <see cref="NavigationArgs.HasChildNavigation"/> on
    /// <paramref name="args"/> is <see langword="false"/>.</para>
    /// <para>
    /// A redirection can be requested by setting the <see cref="NavigationArgs.Redirect"/> property on <paramref name="args"/>. If a redirection is requested,
    /// the rest of the current navigation will be cancelled and the redirection will occur after the task returned by this method completes. If the view model
    /// remains active in the redirected route, this method will be called again.</para>
    /// </remarks>
    public Task OnRouteNavigatedAsync(NavigationArgs args) => Task.CompletedTask;

    /// <summary>
    /// Called whenever the view model is active in the current route and a new route is being navigated to, even if the view model will remain active in the
    /// new route. If the view model will not remain active in the new route then it will be called after <see cref="OnNavigatingAwayAsync(NavigatingArgs)"/>.
    /// </summary>
    /// <remarks>
    /// This method can show dialogs as long as they are closed before the returned task completes. The <see cref="NavigatingArgs.Cancel"/> property on
    /// <paramref name="args"/> is checked after the task returned by this method completes to determine whether the new navigation should be cancelled.
    /// </remarks>
    public Task OnRouteNavigatingAsync(NavigatingArgs args) => Task.CompletedTask;
}
