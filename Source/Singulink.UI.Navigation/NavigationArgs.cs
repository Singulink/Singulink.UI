namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information about a navigation to a view model.
/// </summary>
public sealed class NavigationArgs(bool isFirst, bool alreadyNavigated, bool hasNested, RouteOptions routeOptions, NavigationType type)
{
    private readonly RouteOptions? _routeOptions = routeOptions;

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType => type;

    /// <summary>
    /// Gets a value indicating whether this is the first time the view is being navigated to.
    /// </summary>
    /// <remarks>
    /// The value of this property is only <see langword="true"/> for the first call to <see cref="IRoutedViewModelBase.OnNavigatedToAsync"/> on a view model,
    /// even if the navigation is cancelled/rerouted during that call.
    /// </remarks>
    public bool IsFirstNavigation => isFirst;

    /// <summary>
    /// Gets a value indicating whether the view was already navigated to and its view was already showing.
    /// </summary>
    /// <remarks>
    /// The value of this property will be <see langword="true"/> if the view model was not navigated away from beforehand but route options or nested
    /// navigations have changed, therefore there will not have been a call to <see cref="IRoutedViewModelBase.OnNavigatedFrom"/> beforehand, so any logic that
    /// depends on being paired with navigations from the view model (i.e. adding/removing event handlers) should be conditional on this property being <see
    /// langword="false"/>. Additionally, the view model will not be considered "navigated to" if the view model cancels/reroutes the navigation, so logic that
    /// should be paired with navigations away from the view model should not execute in that case since there will not be a subsequent navigation away.
    /// </remarks>
    public bool AlreadyNavigatedTo => alreadyNavigated;

    /// <summary>
    /// Gets a value indicating whether a nested navigation will occur to a child view after this navigation completes.
    /// </summary>
    public bool HasNestedNavigation => hasNested;

    /// <summary>
    /// Gets the options for the current route.
    /// </summary>
    public RouteOptions RouteOptions { get; } = routeOptions;
}
