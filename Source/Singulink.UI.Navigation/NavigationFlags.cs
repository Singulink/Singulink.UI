namespace Singulink.UI.Navigation;

/// <summary>
/// Specifies flags that provide additional information for items in a route that is being navigated to.
/// </summary>
/// <remarks>
/// See the property documentation on <see cref="NavigationArgs"/> for more information on what these flags mean and how they are used.
/// </remarks>
[Flags]
public enum NavigationFlags
{
    /// <summary>
    /// Default value with no flags specified.
    /// </summary>
    None = 0,

    /// <summary>
    /// Indicates that this is the first time the view is being navigated to.
    /// </summary>
    FirstNavigation = 1,

    /// <summary>
    /// Indicates that the view was already navigated to during the last navigation.
    /// </summary>
    AlreadyNavigatedTo = 2,

    /// <summary>
    /// Indicates that a child navigation will occur to a child view after this navigation completes.
    /// </summary>
    HasChildNavigation = 4,
}
