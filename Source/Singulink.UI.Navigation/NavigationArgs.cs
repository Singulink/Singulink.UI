using Singulink.Enums;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information to a view model when its route is being navigated to.
/// </summary>
public sealed class NavigationArgs
{
    private readonly NavigationFlags _flags;
    private readonly RouteOptions _routeOptions;
    private readonly NavigationType _navigationType;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationArgs"/> class with the specified navigation type, flags, and route options.
    /// </summary>
    /// <param name="navigationType">The navigation type that is occurring.</param>
    /// <param name="flags">Flags that provide additional information about the navigation.</param>
    /// <param name="routeOptions">Options for the new route.</param>
    public NavigationArgs(NavigationType navigationType, NavigationFlags flags, RouteOptions routeOptions)
    {
        _navigationType.ThrowIfNotValid(nameof(navigationType));
        _flags.ThrowIfNotValid(nameof(flags));

        _navigationType = navigationType;
        _flags = flags;
        _routeOptions = routeOptions;
    }

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType => _navigationType;

    /// <summary>
    /// Gets a value indicating whether this is the first time the view is being navigated to.
    /// </summary>
    /// <remarks>
    /// The value of this property is only <see langword="true"/> for the first call to <see cref="IRoutedViewModelBase.OnNavigatedToAsync"/> on a view model,
    /// even if the navigation is cancelled/rerouted during that call.
    /// </remarks>
    public bool IsFirstNavigation => _flags.HasFlag(NavigationFlags.FirstNavigation);

    /// <summary>
    /// Gets a value indicating whether the view was already navigated to during the last navigation.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The value of this property will be <see langword="true"/> if the view model had already been navigated to during a previous navigation but had not been
    /// navigated away from. Some situations where this can happen are:</para>
    /// <list type="bullet">
    /// <item>The new route is the same as the last navigated route.</item>
    /// <item>Only route options changed but the rest of the route is the same.</item>
    /// <item>A refresh of the current route was requested (in which case <see cref="NavigationType"/> will be set to <see
    /// cref="NavigationType.Refresh"/>).</item>
    /// <item>The view model is a common parent for this route and the last navigated route but nested child navigations changed.</item>
    /// </list>
    /// <para>
    /// Any logic that depends on being paired with navigations away from the view model (i.e. adding/removing event handlers) should be conditional on this
    /// property being <see langword="false"/>, otherwise the navigated to logic may be executed more times than the navigated away logic. Additionally, the
    /// view model will not be transitioned to a "navigated to" state if it cancels/reroutes the navigation, so logic that should be paired with navigations
    /// away from the view model should not be executed if this view model cancels the navigation - a subsequent navigated away from call will not occur in that
    /// case.</para>
    /// </remarks>
    public bool AlreadyNavigatedTo => _flags.HasFlag(NavigationFlags.AlreadyNavigatedTo);

    /// <summary>
    /// Gets a value indicating whether a nested navigation will occur to a child view after this navigation completes.
    /// </summary>
    public bool HasNestedNavigation => _flags.HasFlag(NavigationFlags.HasNestedNavigation);

    /// <summary>
    /// Gets the options for the current route that is being navigated to.
    /// </summary>
    public RouteOptions RouteOptions => _routeOptions;
}
