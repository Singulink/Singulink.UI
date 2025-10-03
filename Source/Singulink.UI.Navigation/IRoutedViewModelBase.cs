namespace Singulink.UI.Navigation;

/// <summary>
/// Represents a view model that can be navigated to.
/// </summary>
public interface IRoutedViewModelBase
{
    /// <summary>
    /// Gets a value indicating whether the view model and its associated view can be cached in navigation stack.
    /// </summary>
    /// <remarks>
    /// <para>
    /// If the view or view model is consuming a large amount of memory, this property should return <see langword="false"/> to avoid caching the when they are
    /// not active. Removing a parent view model that provided services to a child view model from the cache will also remove of all of its children from the
    /// cache.</para>
    /// <para>
    /// Use <see cref="INavigatorBuilder.ConfigureNavigationStacks(int, int, int)"/> to control the maximum depth of cached views and view models.</para>
    /// </remarks>
    public bool CanBeCached => true;

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
