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
    public NavigationArgs(NavigationType navigationType, bool hasChildNavigation)
    {
        navigationType.ThrowIfNotValid(nameof(navigationType));
        NavigationType = navigationType;
        HasChildNavigation = hasChildNavigation;
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
    /// Gets or sets an action that will be invoked to redirect the navigation to a different route. The action must call a navigation method on the provided
    /// navigator to perform the redirection. If the action does not call a navigation method, the original navigation will proceed as normal.
    /// </summary>
    public Action<IRedirectNavigator>? Redirect { get; set; }
}
