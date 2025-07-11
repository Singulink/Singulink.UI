using Singulink.Enums;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information to a view model when its route is navigating away and allows it to be cancelled.
/// </summary>
public class NavigatingArgs
{
    private readonly NavigatingFlags _flags;
    private readonly RouteOptions _newRouteOptions;
    private readonly NavigationType _navigationType;

    /// <summary>
    /// Initializes a new instance of the <see cref="NavigatingArgs"/> class with the specified navigation type, flags, and new route options.
    /// </summary>
    /// <param name="navigationType">The navigation type that is occurring.</param>
    /// <param name="flags">Flags that provide additional information about the navigation.</param>
    /// <param name="newRouteOptions">Options for the new route.</param>
    public NavigatingArgs(NavigationType navigationType, NavigatingFlags flags, RouteOptions newRouteOptions)
    {
        navigationType.ThrowIfNotValid(nameof(navigationType));
        flags.ThrowIfNotValid(nameof(flags));

        _navigationType = navigationType;
        _flags = flags;
        _newRouteOptions = newRouteOptions;
    }

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType => _navigationType;

    /// <summary>
    /// Gets a value indicating whether this view model will be navigated away from if the navigation is not cancelled. If this is <see langword="false"/> then
    /// the view model will remain active in the new route but child child navigations or route options may be changing (or not, i.e. in the case of a
    /// refresh).
    /// </summary>
    public bool WillBeNavigatedFrom => _flags.HasFlag(NavigatingFlags.WillBeNavigatedFrom);

    /// <summary>
    /// Gets the options for the new route that will be navigated to.
    /// </summary>
    public RouteOptions RouteOptions => _newRouteOptions;

    /// <summary>
    /// Gets or sets a value indicating whether the navigation should be canceled and the current route should remain active.
    /// </summary>
    public bool Cancel { get; set; }
}
