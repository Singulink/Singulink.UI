namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information about a navigation.
/// </summary>
public sealed class NavigationArgs(bool isInitialNavigation, bool hasChildNavigation, RouteOptions? routeOptions, NavigationType navigationType)
{
    private readonly RouteOptions? _routeOptions = routeOptions;

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType { get; }

    /// <summary>
    /// Gets a value indicating whether this is an initial navigation to this view model or if it is being shown again, i.e. via a back/forward navigation
    /// request.
    /// </summary>
    public bool IsInitialNavigation => isInitialNavigation;

    /// <summary>
    /// Gets a value indicating whether a nested navigation will occur after this navigation completes.
    /// </summary>
    public bool HasNestedNavigation => hasChildNavigation;

    /// <summary>
    /// Gets the anchor for the current route.
    /// </summary>
    public string? Anchor => _routeOptions?.Anchor;
}
