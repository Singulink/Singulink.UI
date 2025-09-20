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
    public NavigationArgs(NavigationType navigationType, bool hasChildNavigation, IRedirectNavigator redirectNavigator)
    {
        navigationType.ThrowIfNotValid(nameof(navigationType));
        NavigationType = navigationType;
        HasChildNavigation = hasChildNavigation;
        RedirectNavigator = redirectNavigator;
    }

    /// <summary>
    /// Gets the type of navigation that is occurring.
    /// </summary>
    public NavigationType NavigationType { get; }

    /// <summary>
    /// Gets a value indicating whether a navigation will occur to a child view model.
    /// </summary>
    public bool HasChildNavigation { get; }

    /// <summary>
    /// Gets a navigator that can be used to request a redirect to a different route. The redirect navigation occurs after the current navigation handler
    /// completes.
    /// </summary>
    public IRedirectNavigator RedirectNavigator { get; }
}
