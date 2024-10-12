using Singulink.Enums;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information to a view model when its route is navigating away from and allows it to be cancelled.
/// </summary>
public class NavigatingArgs(NavigationType type, NavigatingFlags flags, RouteOptions newRouteOptions)
{
    private readonly NavigatingFlags _flags = flags.IsValid() ? flags : throw new ArgumentException("Invalid navigating flags.", nameof(flags));

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType { get; } = type.IsValid() ? type : throw new ArgumentException("Invalid navigation type.", nameof(type));

    /// <summary>
    /// Gets a value indicating whether this view model will be navigated away from if the navigation is not cancelled. If this is <see langword="false"/> then
    /// the view model will remain active in the new route but nested child navigations or route options may be changing (or not, i.e. in the case of a
    /// refresh).
    /// </summary>
    public bool WillBeNavigatedFrom => _flags.HasFlag(NavigatingFlags.WillBeNavigatedFrom);

    /// <summary>
    /// Gets the route options for the new route that will be navigated to.
    /// </summary>
    public RouteOptions RouteOptions => newRouteOptions;

    /// <summary>
    /// Gets or sets a value indicating whether the navigation should be canceled and the current route should remain active.
    /// </summary>
    public bool Cancel { get; set; }
}
