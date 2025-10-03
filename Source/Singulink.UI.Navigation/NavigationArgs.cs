using Singulink.Enums;

namespace Singulink.UI.Navigation;

/// <summary>
/// Provides information to a view model when its route is being navigated to.
/// </summary>
public sealed class NavigationArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NavigationArgs"/> class.
    /// </summary>
    public NavigationArgs(INavigator navigator, NavigationType navigationType, bool hasChildNavigation)
    {
        navigationType.ThrowIfNotValid(nameof(navigationType));

        Navigator = navigator;
        NavigationType = navigationType;
        HasChildNavigation = hasChildNavigation;
    }

    /// <summary>
    /// Gets the navigator that is performing the navigation.
    /// </summary>
    public INavigator Navigator { get; }

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType { get; }

    /// <summary>
    /// Gets a value indicating whether a navigation will occur to a child view model.
    /// </summary>
    public bool HasChildNavigation { get; }

    /// <summary>
    /// Gets or sets a redirect to a different route.
    /// </summary>
    /// <remarks>
    /// This property is checked after the handler that provided these args completes, and if set, any remaining navigation is cancelled and the redirect is
    /// performed. The cancelled navigation does not appear in the navigation history.
    /// </remarks>
    public Redirect? Redirect { get; set; }
}
